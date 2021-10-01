using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace KdlDotNet
{
    /**
     * The base interface all objects parsed from a KDL document must implement
     */
    public interface IKDLObject
    {
        /**
         * Write the object to the provided stream.
         *
         * @param writer the Writer to write to
         * @param printConfig a configuration object controlling how the object is printed
         * @throws IOException if there is any issue writing the object
         */
        void WriteKDL(StreamWriter writer, PrintConfig printConfig);
    }

    public static class KDLObjectExtensions
    {
        internal static UTF8Encoding UTF8NoByteOrderMarkerEncoding = new UTF8Encoding(false);

        /**
         * Generate a string with the text representation of the given object.
         *
         * @return the string
         */
        public static string ToKDL(this IKDLObject obj)
        {
            var ms = new MemoryStream();
            var streamWriter = new StreamWriter(ms, UTF8NoByteOrderMarkerEncoding);

            obj.WriteKDL(streamWriter, PrintConfig.PrettyDefault);
            streamWriter.Flush();

            return Encoding.UTF8.GetString(ms.ToArray()); // C# Note: copies the buffer, probably not efficient
        }
    }
}
