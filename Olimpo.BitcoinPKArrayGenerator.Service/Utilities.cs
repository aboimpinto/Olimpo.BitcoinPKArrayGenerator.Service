using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
public class Utilities
    {
        internal static byte[] HmacSha512Digest(byte[] input, int offset, int length, byte[] hmacKey)
        {
            var output = new byte[64];
            HMac hmacsha512Obj = new HMac(new Sha512Digest());

            var param = new Org.BouncyCastle.Crypto.Parameters.KeyParameter(hmacKey);
            hmacsha512Obj.Init(param);
            hmacsha512Obj.BlockUpdate(input, offset, length);
            hmacsha512Obj.DoFinal(output, 0);
            
            return output;
        }

        internal static byte[] MergeByteArrays(byte[] source1, byte[] source2)
        {
            //Most efficient way to merge two arrays this according to http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
            var buffer = new byte[source1.Length + source2.Length];
            System.Buffer.BlockCopy(source1, 0, buffer, 0, source1.Length);
            System.Buffer.BlockCopy(source2, 0, buffer, source1.Length, source2.Length);

            return buffer;
        }

        internal static byte[] Sha512Digest(byte[] input, int offset, int length)
        {
            var algorithm = new Sha512Digest();
            var firstHash = new byte[algorithm.GetDigestSize()];
            algorithm.BlockUpdate(input, offset, length);
            algorithm.DoFinal(firstHash, 0);
            
            return firstHash;
        }
    }
}