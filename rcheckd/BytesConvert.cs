using System;

namespace rcheckd
{
    public static class BytesConvert
    {
        public static int ToInt(string inString)
        {
            decimal d = ToLong(inString);
            if (d < int.MaxValue)
                return (int)d;
            else
                throw new ArgumentOutOfRangeException("Exceeds maximum allowable value: " + inString);
        }
        /// <summary>
        /// Converts a string repesentation of Bytes to a long
        ///   can include KB, MB, GB
        ///   can include commas and decimal points.
        /// </summary>
        /// <param name="inString"></param>
        /// <returns></returns>
        public static long ToLong(string inString)
        {
            string nstring = null;
            string lstring = null;
            char c;
            /* ************************************** */
            /* Finite state machine                   */
            /*   parses String into count of bytes    */
            /* ************************************** */

            string tstring = inString.Trim().ToLower();
            int i = -1;

            Number:
            i++;
            if (i >= tstring.Length)
                goto Done;
            c = tstring[i];

            if (c < 58 && c > 47)
            {
                nstring += c;
                goto Number;
            }
            else if (c == ',')  // probably shouldnt ignore commas, use a regex to validate
            {
                goto Number;
            }
            else if (c == '.')  // its possible to have more than one decimal point, but that will caught in the tryparse below.
            {
                nstring += c;
                goto Number;
            }
            else if (char.IsWhiteSpace(c))
            {
                goto Spaces;
            }
            else if (c < 123 && c > 96)
            {
                lstring += c;
                goto Letter;
            }
            else
                goto Error;


            Spaces:
            i++;
            if (i >= tstring.Length)
                goto Done;
            c = tstring[i];
            if (char.IsWhiteSpace(c))
                goto Spaces;
            else if (c < 123 && c > 96)
            {
                lstring += c;
                goto Letter;
            }
            else
                goto Error;

            Letter:
            i++;
            if (i >= tstring.Length)
                goto Done;
            c = tstring[i];
            if (c < 123 && c > 96)
            {
                lstring += c;
                goto Letter;
            }
            else
                goto Error;

            Error:
            throw new ArgumentException("Error parsing string to bytes: " + inString);

            Done:
            /* ************************************** */
            /* End of Finite state machine            */
            /* ************************************** */

            if (string.IsNullOrEmpty(nstring))
                return 0;

            if(!decimal.TryParse(nstring, out decimal d))
                throw new ArgumentException("Error parsing number: " + inString);

            switch (lstring)
            {
                case "kb":
                case "k":
                    d *= 1024;
                    break;
                case "mb":
                case "m":
                    d *= 1024 * 1024;
                    break;
                case "gb":
                case "g":
                    d *= 1024 * 1024 * 1024;
                    break;
                case "bytes":
                case null:
                    break;
                case "tb":     // note these sizes are beyond what is usable for this application
                case "t":
                case "pb":
                case "p":
                        throw new ArgumentOutOfRangeException("Exceeds maximum allowed: " + inString);
                default:
                    throw new ArgumentException("Notation not understood or implemented: " + inString);
            }

            return (long)d;
        }
        /// <summary>
        /// Convert an integer number to a readable string representation of bytes.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ToString(int length)
        {
            double number = length;
            string suffix;
            if (number < 10000)
                suffix = "Bytes";
            else if (number < 1000 * 1024)
            {
                number /= 1024;
                suffix = "KB";
            }
            else if ( number < 1000 * 1024 * 1024)
            {
                number /= 1024 * 1024;
                suffix = "MB";
            }
            else 
            {
                number /= 1024 * 1024 * 1024;
                suffix = "GB";
            }

            return string.Format("{0:#.#} {1}", number, suffix);
            
        }

    }
}
