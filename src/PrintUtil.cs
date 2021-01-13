using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#nullable enable

namespace kdl_net
{
    using static CharClasses;

    /**
 * Internal class used to print strings at the minimum level of quoting required
 */
    public static class PrintUtil
    {
        private static readonly Regex ValidBareId = new Regex(
            "^[^\n\r\u000C\u0085\u2028\u2029{}<>;\\\\\\[\\]=,\"\u0009\u0020\u00A0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u30000-9]" +
            "[^\n\r\u000C\u0085\u2028\u2029{}<>;\\\\\\[\\]=,\"\u0009\u0020\u00A0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000]*$"
        );

        // throws IOException
        public static void WriteStringQuotedAppropriately(
            StreamWriter writer,
            string str,
            bool bareAllowed,
            PrintConfig printConfig)
        {
            if (str.Length == 0)
            {
                writer.Write("\"\"");
                return;
            }

            int hashDepth = 0;
            if (!printConfig.EscapeNonPrintableAscii)
            {
                int quoteAt = str.IndexOf('"');
                while (quoteAt >= 0)
                {
                    int hashesNeeded = 1;
                    for (int i = quoteAt + 1; i < str.Length && str[i] == '#'; i++)
                    {
                        hashesNeeded++;
                    }
                    hashDepth = Math.Max(hashDepth, hashesNeeded);
                    quoteAt = str.IndexOf('"', quoteAt + 1);
                }
            }

            if (hashDepth == 0 && !str.Contains("\\") && !str.Contains("\""))
            {
                if (bareAllowed && ValidBareId.IsMatch(str))
                {
                    writer.Write(str);
                }
                else
                {
                    writer.Write('"');
                    for (int i = 0; i < str.Length; i++)
                    {
                        char c = str[i];
                        if (IsPrintableAscii(c))
                        {
                            writer.Write(c);
                        }
                        else if (c == '\n' && printConfig.EscapeNewLines)
                        {
                            writer.Write("\\n");
                        }
                        else if (printConfig.EscapeNonPrintableAscii)
                        {
                            writer.Write(GetEscapeIncludingUnicode(c));
                        }
                        else if (printConfig.EscapeCommon)
                        {
                            writer.Write(GetCommonEscape(c) ?? c.ToString());
                        }
                        else
                        {
                            writer.Write(c);
                        }
                    }
                    writer.Write('"');
                }
            }
            else
            {
                writer.Write('r');
                for (int i = 0; i < hashDepth; i++)
                {
                    writer.Write('#');
                }

                writer.Write('"');
                writer.Write(str);
                writer.Write('"');

                for (int i = 0; i < hashDepth; i++)
                {
                    writer.Write('#');
                }
            }
        }
    }


    /**
     * A config object controlling various aspects of how KDL documents are printed.
     */
    public class PrintConfig
    {
        public static PrintConfig PrettyDefault => new PrintConfig();
        public static PrintConfig RawDefault => new PrintConfig(indent: 0, printEmptyChildren: false, escapeNewLines: true);

        public bool RequireSemicolons { get; }
        public string NewLine { get; }
        public int Indent { get; }
        public char IndentChar { get; }
        public char ExponentChar { get; }
        public bool EscapeCommon { get; }
        public bool EscapeNonPrintableAscii { get; }
        public bool PrintEmptyChildren { get; }
        public bool PrintNullArgs { get; }
        public bool PrintNullProps { get; }
        public bool EscapeNewLines { get; }

        public PrintConfig(bool requireSemicolons = false, string newline = "\n", bool escapeCommon = true, bool escapeNonAscii = false, bool escapeNewLines = false, int indent = 4, char indentChar = ' ', char exponentChar = 'E', bool printEmptyChildren = true, bool printNullArgs = true, bool printNullProps = true)
        {
            if (exponentChar != 'e' && exponentChar != 'E')
                throw new ArgumentException("Exponent character must be either 'e' or 'E'");

            for (int i = 0; i < newline.Length; i++)
            {
                if (!IsUnicodeLinespace(newline[i]))
                    throw new ArgumentException("All characters in specified 'newline' must be unicode vertical space");
            }

            if (!IsUnicodeWhitespace(indentChar))
                throw new ArgumentException("Indent character must be unicode whitespace");

            RequireSemicolons = requireSemicolons;
            NewLine = newline;
            Indent = indent;
            IndentChar = indentChar;
            ExponentChar = exponentChar;
            EscapeCommon = escapeCommon;
            EscapeNonPrintableAscii = escapeNonAscii;
            PrintEmptyChildren = printEmptyChildren;
            PrintNullArgs = printNullArgs;
            PrintNullProps = printNullProps;
            EscapeNewLines = escapeNewLines;
        }
    }
}
