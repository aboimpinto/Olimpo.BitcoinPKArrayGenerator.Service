using System;
using System.Linq;

namespace Olimpo.BitcoinPKArrayGenerator.Service
{
public class Rfc2898_pbkdf2_hmacsha512
    {
        //I made the variable names match the definition in RFC2898 - PBKDF2 where possible, so you can trace the code functionality back to the specification
        private readonly byte[] P;
        private readonly byte[] S;
        private readonly int c;
        private int dkLen;

        public const int CMinIterations = 2048;
        //Minimum recommended salt length in Rfc2898
        public const int CMinSaltLength = 8;
        //Length of the Hash Digest Output - 512 bits - 64 bytes
        public const int hLen = 64;

        /// <summary>
        /// Constructor to create Rfc2898_pbkdf2_hmacsha512 object ready to perform Rfc2898 PBKDF2 functionality
        /// </summary>
        /// <param name="password">The Password to be hashed and is also the HMAC key</param>
        /// <param name="salt">Salt to be concatenated with the password</param>
        /// <param name="iterations">Number of iterations to perform HMACSHA Hashing for PBKDF2</param>
        public Rfc2898_pbkdf2_hmacsha512(byte[] password, byte[] salt, int iterations = CMinIterations)
        {            
            P = password;
            S = salt;
            c = iterations;            
        }

        /// <summary>
        /// Derive Key Bytes using PBKDF2 specification listed in Rfc2898 and HMACSHA512 as the underlying PRF (Psuedo Random Function)
        /// </summary>
        /// <param name="keyLength">Length in Bytes of Derived Key</param>
        /// <returns>Derived Key</returns>
        public byte[] GetDerivedKeyBytes_PBKDF2_HMACSHA512(int keyLength)
        {
            //no need to throw exception for dkLen too long as per spec because dkLen cannot be larger than Int32.MaxValue so not worth the overhead to check
            dkLen = keyLength;

            double l = Math.Ceiling((double)dkLen / hLen);

            byte[] finalBlock = new byte[0];

            for (int i = 1; i <= l; i++)
            {
                //Concatenate each block from F into the final block (T_1..T_l)
                finalBlock = Utilities.MergeByteArrays(finalBlock, F(P, S, c, i));
            }

            //returning DK note r not used as dkLen bytes of the final concatenated block returned rather than <0...r-1> substring of final intermediate block + prior blocks as per spec
            return finalBlock.Take(dkLen).ToArray();

        }

        /// <summary>
        /// A static publicly exposed version of GetDerivedKeyBytes_PBKDF2_HMACSHA512 which matches the exact specification in Rfc2898 PBKDF2 using HMACSHA512
        /// </summary>
        /// <param name="P">Password passed as a Byte Array</param>
        /// <param name="S">Salt passed as a Byte Array</param>
        /// <param name="c">Iterations to perform the underlying PRF over</param>
        /// <param name="dkLen">Length of Bytes to return, an AES 256 key wold require 32 Bytes</param>
        /// <returns>Derived Key in Byte Array form ready for use by chosen encryption function</returns>
        public static byte[] PBKDF2(byte[] P, byte[] S, int c = CMinIterations, int dkLen = hLen)
        {
            Rfc2898_pbkdf2_hmacsha512 rfcObj = new Rfc2898_pbkdf2_hmacsha512(P, S, c);
            return rfcObj.GetDerivedKeyBytes_PBKDF2_HMACSHA512(dkLen);
        }        

        //Main Function F as defined in Rfc2898 PBKDF2 spec
        private byte[] F(byte[] P, byte[] S, int c, int i)
        {

            //Salt and Block number Int(i) concatenated as per spec
            var Si = Utilities.MergeByteArrays(S, INT(i));

            //Initial hash (U_1) using password and salt concatenated with Int(i) as per spec
            var temp = PRF(Si,P);

            //Output block filled with initial hash value or U_1 as per spec
            var U_c = temp;

            for (var C = 1; C < c; C++)
            {
                //rehashing the password using the previous hash value as salt as per spec
                temp = PRF(temp,P);

                for (int j = 0; j < temp.Length; j++)
                {
                    //xor each byte of the each hash block with each byte of the output block as per spec
                    U_c[j] ^= temp[j];
                }
            }

            //return a T_i block for concatenation to create the final block as per spec
            return U_c;
        }

        //PRF function as defined in Rfc2898 PBKDF2 spec
        private byte[] PRF(byte[] S, byte[] hmacKey)
        {
            //HMACSHA512 Hashing, better than the HMACSHA1 in Microsofts implementation ;)
            return Utilities.HmacSha512Digest(S, 0, S.Count(), hmacKey);
        }

        //This method returns the 4 octet encoded Int32 with most significant bit first as per spec
        private byte[] INT(int i)
        {
            byte[] I = BitConverter.GetBytes(i);

            //Make sure most significant bit is first
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(I);
            }

            return I;
        }
    }
}