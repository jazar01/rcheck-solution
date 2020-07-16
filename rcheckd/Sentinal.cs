using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace rcheckd
{
    public class Sentinal
    {
        /**********************************************************
         * A sentinal is a file of random data where              *
         *    the first 32 bytes of the file are a SHA256 hash of *
         *    remaining contents.                                 *
         *                                                        *
         * If a single bit of a sentinal file has changed, the    *
         *    sentinal test will fail.                            *
         **********************************************************/

        public string Path { get; set; }
        public int Length { get; set; }

        public string Error { get; set; }

        static readonly SHA256 hasher = SHA256.Create();
        static int HashSize = hasher.HashSize / 8;  // calculate hash bytes

        // constructors
        public Sentinal()
        {
            return;
        }
        public Sentinal(string path)
        {
            Path = path;
        }

        public Sentinal(string path, int size)
        {
            Path = path;
            Length = size;
        }

        /*
         *  characteristics of office documents
         *  
         *  Office files
         *  first bytes = 
         *  50 4B 03 04 14 00 06 00 08 00 00 00 21 00 77 23
         *  46 33 EB 01 00 00 58 0D 00 00 13 00 82 02 5B 43
         *  6F 6E 74 65 6E 74 5F 54 79 70 65 73 5D 2E 78 6D
         *  6C 20 A2 04 02 28 A0 00 02
         *  
         *  504B0304 PK signature
         *  1400 version
         *  0800 compression method
         *  0000 Last mod time
         *  2100 Last mod date
         *  77234633 CRC
         *  EB010000 Compressed Size
         *  580D0000 Uncompressed Size
         *  1300 File name length
         *  8202 Extrafield Length
         *  5B - 6C [Content_Types].xml
         *  20A2040228A00002 
         *  
         *  0x3A - 0x23A null
         *  
         *  occasional blocks of 256 nulls
         */

        /// <summary>
        /// Generate a low entropy sentinal
        /// This version generates files with similar charcteristics as 
        /// Microsoft office documents.  The ChiSquare value is generally
        /// in the range of 45,000+
        /// </summary>
        /// <returns></returns>
        public bool GenerateType1()
        {
            if (Length < 2048)
                throw new ApplicationException("Specifed length of Sentinal is insufficient: "+ Length);

            // make a buffer of random data
            byte[] randomData = new byte[Length - HashSize];
            Random rnd = new Random();
            rnd.NextBytes(randomData);
            // signature typically found on front of office documents
            byte[] signature = new byte[]
                {0x50,0x4B,0x03,0x04,0x14,0x00,0x06,0x00,0x18,0x00,0x00,0x00,0x21,0x00,0x77,0x23,
                 0x46,0x33,0xEB,0x01,0x00,0x00,0x58,0x0D,0x00,0x00,0x13,0x00,0x82,0x02,0x5B,0x43,
                 0x6F,0x6E,0x74,0x65,0x6E,0x74,0x5F,0x54,0x79,0x70,0x65,0x73,0x5D,0x2E,0x78,0x6D,
                 0x6C,0x20,0xA2,0x0f,0x02,0x28,0xA0,0x00,0x02};

            // fill the buffer with random data
            Buffer.BlockCopy(signature, 0, randomData, 0, signature.Length);

            // clear out some sections (typical of office documents)
            Array.Clear(randomData, 0x3A, 0x200);
            Array.Clear(randomData, 0x3C1, 0x200);
            Array.Clear(randomData, 0x6F5, 0x80);

            // write some zeros at random intervals to make sure entropy is lowered throughout file
            //    similar pattern seen in office documents
            for (int i = 2048; i < Length; i += rnd.Next(768, 2048))
                if (i+512 < Length)
                    Array.Clear(randomData, i, rnd.Next(128, 512));

            // create hash of random data
            byte[] hashvalue = hasher.ComputeHash(randomData);

            //combine the hash and random data
            byte[] combined = new byte[hashvalue.Length + randomData.Length];
            Buffer.BlockCopy(hashvalue, 0, combined, 0, hashvalue.Length);
            Buffer.BlockCopy(randomData, 0, combined, hashvalue.Length, randomData.Length);

            // write the hash + random data to file

            // this will cause directories in the path to be generated if they
            // don't already exist.
            System.IO.FileInfo file = new System.IO.FileInfo(Path);
            file.Directory.Create(); // If the directory already exists, this method does nothing.

            File.WriteAllBytes(Path, combined);

            return true;  // TODO -  Fix this hardcode or make it a void.
        }
        /// <summary>
        /// Generate a random Sentinal file
        /// </summary>
        /// <returns>true if succesful</returns>
        public bool Generate()
        {
            // make a buffer of random data
            byte[] randomData = new byte[Length - HashSize];
            Random rnd = new Random();
            rnd.NextBytes(randomData);

            // create hash of random data
            byte[] hashvalue = hasher.ComputeHash(randomData);

            //combine the hash and random data
            byte[] combined = new byte[hashvalue.Length + randomData.Length];
            Buffer.BlockCopy(hashvalue, 0, combined, 0, hashvalue.Length);
            Buffer.BlockCopy(randomData, 0, combined, hashvalue.Length, randomData.Length);

            // write the hash + random data to file

            // this will cause directories in the path to be generated if they
            // don't already exist.
            System.IO.FileInfo file = new System.IO.FileInfo(Path);
            file.Directory.Create(); // If the directory already exists, this method does nothing.

            File.WriteAllBytes(Path, combined);

            return true;
        }

        /// <summary>
        /// Test a file to see if it has been tampered with.
        /// </summary>
        public bool Test()
        {
            bool rc;
            byte[] filedata;
            try
            {
                filedata = File.ReadAllBytes(Path);
            }
            catch (Exception e)
            {
                Error = e.Message;
                return false;
            }

            if (filedata.Length < 1024)
            {
                Error = "Invalid file format - too short";
                return false;
            }

            byte[] hash = new byte[HashSize];
            byte[] data = new byte[filedata.Length - hash.Length];

            Buffer.BlockCopy(filedata, 0, hash, 0, hash.Length);
            Buffer.BlockCopy(filedata, hash.Length, data, 0, data.Length);

            rc = hash.SequenceEqual(hasher.ComputeHash(data));
            if (!rc)
                Error = "Content changed";

            return rc;
        }

        /// <summary>
        /// Delete a sentinal file
        ///   but first make sure it is really a sentinal file.
        /// </summary>
        /// <returns>bool</returns>
        public bool Delete()
        {
            if (Test())
            {
                try
                {
                    File.Delete(Path);
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return false;
                }
                return true;
            }
            else
                return false; // don't delete files that don't pass the test
        }

        /// <summary>
        /// returns a number indicating the randomness of the data
        /// closer to zero is perfect, below 23 is random
        /// </summary>
        public int Randomness 
        {
            get
            {
                try
                {
                    return Math.Abs(256 - (int)ChiSquare.ComputeChiSquare(Path, 0, 32768));
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Unable to compute ChiSquare distribtuion for file: " + Path +
                        " - file may not exists or a permissions problem. - " + e.Message);
                }
            }
        }

    }
}
