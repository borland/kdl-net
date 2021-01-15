using KdlDotNet;
using System.IO;
using System.Text;

namespace KdlDotNetTests
{
    static class TestUtil
    {
        public static readonly KDLParser Parser = new KDLParser();

        public static KDLParseContext StrToContext(string str)
            => new KDLParseContext(new StreamReader(
                new MemoryStream(Encoding.UTF8.GetBytes(str)), Encoding.UTF8));

        public static string ReadRemainder(KDLParseContext context)
        {
            var stringBuilder = new StringBuilder();
            
            int read = context.Read();
            while (read != KDLParser.EOF)
            {
                stringBuilder.AppendCodePoint(read);
                read = context.Read();
            }

            return stringBuilder.ToString();
        }
    }
}
