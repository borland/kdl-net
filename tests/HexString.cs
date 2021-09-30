using System;
using System.Collections.Generic;
using System.Text;

namespace KdlDotNetTests
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this IEnumerable<byte> bytes, char padWith = (char)0)
        {
            var hex = new StringBuilder();
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:X2}", b);
                if (padWith != (char)0)
                    hex.Append(padWith);
            }

            return hex.ToString();
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            static int GetHexVal(char hex)
            {
                int val = (int)hex;
                //For uppercase A-F letters:
                //return val - (val < 58 ? 48 : 55);
                //For lowercase a-f letters:
                //return val - (val < 58 ? 48 : 87);
                //Or the two combined, but a bit slower:
                return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
            }

            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }
    }
}
