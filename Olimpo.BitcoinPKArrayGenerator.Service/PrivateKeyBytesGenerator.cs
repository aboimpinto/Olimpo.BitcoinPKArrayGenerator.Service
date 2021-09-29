using System;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
    public class PrivateKeyBytesGenerator : IPrivateKeyBytesGenerator
    {
        public byte[] GetRandomBytes(int size, int seedStretchingIterations=5000)
        {
            //varies from system to system, a tiny amount of entropy, tiny
            int processorCount = System.Environment.ProcessorCount;

            //another tiny amount of entropy due to the varying nature of thread id
            int currentThreadId = System.Environment.CurrentManagedThreadId;

            //a GUID is considered unique so also provides some entropy
            byte[] guidBytes = Guid.NewGuid().ToByteArray();

            //this combined with DateTime.Now is the default seed in BouncyCastles SecureRandom
            byte[] threadedSeedBytes = new ThreadedSeedGenerator().GenerateSeed(24, true);

            byte[] output = new byte[size];

            //if for whatever reason it says 0 or less processors just make it 16
            if (processorCount <= 0)
            {
                processorCount = 16;
            }

            //if some fool trys to set stretching to < 0 we protect them from themselves
            if (seedStretchingIterations < 0)
            {
                seedStretchingIterations = 0;
            }

            //we create a SecureRandom based off SHA256 just to get a random int which will be used to determine what bytes to "take" from our built seed hash and then rehash those taken seed bytes using a KDF (key stretching) such that it would slow down anyone trying to rebuild private keys from common seeds.
            var seedByteTakeDetermine = SecureRandom.GetInstance("SHA256PRNG");

            guidBytes = Utilities.HmacSha512Digest(
                guidBytes, 
                0, 
                guidBytes.Length, 
                Utilities.MergeByteArrays(threadedSeedBytes, UTF8Encoding.UTF8.GetBytes(Convert.ToString(System.Environment.TickCount))));

            try
            {
                seedByteTakeDetermine.SetSeed(((DateTime.Now.Ticks - System.Environment.TickCount) * processorCount) + currentThreadId);
                seedByteTakeDetermine.SetSeed(guidBytes);
                seedByteTakeDetermine.SetSeed(seedByteTakeDetermine.GenerateSeed(1 + currentThreadId));
                seedByteTakeDetermine.SetSeed(threadedSeedBytes);
            }
            catch
            {
                try
                {
                    //if the number is too big or causes an error or whatever we will failover to this, as it's not our main source of random bytes and not used in the KDF stretching it's ok.
                    seedByteTakeDetermine.SetSeed((DateTime.Now.Ticks - System.Environment.TickCount) + currentThreadId);
                    seedByteTakeDetermine.SetSeed(guidBytes);
                    seedByteTakeDetermine.SetSeed(seedByteTakeDetermine.GenerateSeed(1 + currentThreadId));
                    seedByteTakeDetermine.SetSeed(threadedSeedBytes);
                }
                catch
                {
                    //if again the number is too big or causes an error or whatever we will failover to this, as it's not our main source of random bytes and not used in the KDF stretching it's ok.
                    seedByteTakeDetermine.SetSeed(DateTime.Now.Ticks - System.Environment.TickCount);
                    seedByteTakeDetermine.SetSeed(guidBytes);
                    seedByteTakeDetermine.SetSeed(seedByteTakeDetermine.GenerateSeed(1 + currentThreadId));
                    seedByteTakeDetermine.SetSeed(threadedSeedBytes);
                }
            }

            //hardened seed
            byte[] toHashForSeed;

            try
            {
                toHashForSeed = BitConverter.GetBytes(((processorCount - seedByteTakeDetermine.Next(0, processorCount)) * System.Environment.TickCount) * currentThreadId);
            }
            catch
            {
                try
                {
                    //if the number was too large or something we failover to this
                    toHashForSeed = BitConverter.GetBytes(((processorCount - seedByteTakeDetermine.Next(0, processorCount)) + System.Environment.TickCount) * currentThreadId);
                }
                catch
                {
                    //if the number was again too large or something we failover to this
                    toHashForSeed = BitConverter.GetBytes(((processorCount - seedByteTakeDetermine.Next(0, processorCount)) + System.Environment.TickCount) + currentThreadId);
                }
            }
                
            toHashForSeed = Utilities.Sha512Digest(toHashForSeed, 0, toHashForSeed.Length);
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, guidBytes);
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, BitConverter.GetBytes(currentThreadId));
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, BitConverter.GetBytes(DateTime.UtcNow.Ticks));
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, BitConverter.GetBytes(DateTime.Now.Ticks));
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, BitConverter.GetBytes(System.Environment.TickCount));
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, BitConverter.GetBytes(processorCount));
            toHashForSeed = Utilities.MergeByteArrays(toHashForSeed, threadedSeedBytes);
            toHashForSeed = Utilities.Sha512Digest(toHashForSeed, 0, toHashForSeed.Length);

            //we grab a random amount of bytes between 24 and 64 to rehash  make a new set of 64 bytes, using guidBytes as hmackey
            toHashForSeed = Utilities.Sha512Digest(Utilities.HmacSha512Digest(toHashForSeed,0,seedByteTakeDetermine.Next(24, 64),guidBytes),0,64);

            seedByteTakeDetermine.SetSeed(currentThreadId + (DateTime.Now.Ticks - System.Environment.TickCount));
            
            //by making the iterations also random we are again making it hard to determin our seed by brute force
            int iterations = seedStretchingIterations - (seedByteTakeDetermine.Next(0, (seedStretchingIterations / seedByteTakeDetermine.Next(9, 100))));

            //here we use key stretching techniques to make it harder to replay the random seed values by forcing computational time up            
            byte[] seedMaterial = Rfc2898_pbkdf2_hmacsha512.PBKDF2(toHashForSeed, seedByteTakeDetermine.GenerateSeed(64), iterations);

            //build a SecureRandom object that uses Sha512 to provide randomness and we will give it our created above hardened seed
            var secRand = new SecureRandom(new Org.BouncyCastle.Crypto.Prng.DigestRandomGenerator(new Sha512Digest()));

            //set the seed that we created just above
            secRand.SetSeed(seedMaterial);

            //generate more seed materisal            
            secRand.SetSeed(currentThreadId);
            secRand.SetSeed(Utilities.MergeByteArrays(guidBytes, threadedSeedBytes));
            secRand.SetSeed(secRand.GenerateSeed(1 + secRand.Next(64)));
            
            //add our prefab seed again onto the previous material just to be sure the above statements are adding and not clobbering seed material
            secRand.SetSeed(seedMaterial);

            //here we derive our random bytes
            secRand.NextBytes(output, 0, size);
                    
            return output;
        }
    }
}