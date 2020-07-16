using System;
using System.IO;

namespace rcheckd
{
    /* 
     *  ChiSquare is statistical indictor of the 'randomness' of a file.
     *  It works by counting the number of occurences of each byte value.
     *  In a completely random file there would be an equal chance of any
     *  particular byte value occuring at any position. 
     *  
     *  For this calculation to work for an 8bit byte size, we need at least
     *  2560 bytes.  This is the 10x the 256 possible values of a byte.
     * 
     *  The return value is a number, a number close to 256 indicates random,
     *  the further away from 256, the less like the file is to be random.
     *  
     *  (Note JA discovered a flaw in this method, in that a file could be
     *  intentionally designed to appear random but it is not at all random.
     *  This is not a problem for this application.
     *  
     *  An encrypted file will test out to be nearly random. 
     */
    public static class ChiSquare
    {
/// <summary>
/// Computes ChiSquare for a sample of bytes from file
/// </summary>
/// <param name="filename"></param>
/// <param name="offset"></param>
/// <param name="samplesize">number of bytes to read for sample, must be min 2560</param>
/// <returns></returns>
        public static double ComputeChiSquare(string filename, int offset, int samplesize)   // TODO BUG-use offset
        {
            // ChiSquare computation is not valid if there are not enough bytes in the sample.
            const int MINBUFFER = 2560;

            if (samplesize < 2560)
                throw new ArgumentException("Sample size for ChiSquare must be at least " + MINBUFFER +" : " + samplesize);
          
            int bufferLength = samplesize;

            // array of all posible values for an 8 bit byte;
            long[] map = new long[256];
            Array.Clear(map, 0, 256);  // make sure every value starts at zero

            FileStream fs;

            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None, bufferLength);
            }
            catch (Exception)
            {
                return 0;
            }

           

            Byte[] fBuffer = new Byte[bufferLength];
            int count = fs.Read(fBuffer, 0, bufferLength);
            if (count < MINBUFFER)
                throw new ArgumentException("Buffersize for ChiSquare must be at least " + MINBUFFER + 
                    " only " + count + " bytes were read from the specified offset, possibly end of file.");

            for (int i = 0; i < count; i++)  // iterate through buffer and update map counts
                map[fBuffer[i]]++;

            fs.Close();

            double ChiSquare = 0.0;
            double variance;
            double Expected = count / 256;
            for (int i = 0; i < 256; i++)
            {
                variance = map[i] - Expected;
                ChiSquare += (variance * variance);
            }
            ChiSquare /= Expected;
            return ChiSquare;

            // https://en.wikibooks.org/wiki/Algorithm_Implementation/Pseudorandom_Numbers/Chi-Square_Test
            // degrees of freedom = 256.
            // we are creating a sum of ChiSquares representing variance from expected result
            // expected result is perfectly uniform distribution in case of pure random data

            // filesize must be bigger than 2560
            // if the value is between 233 and 278 then the file is random
            //    and most likely encrypted.  test with TC validates this


        }

    }
}
