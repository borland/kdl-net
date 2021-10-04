using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#nullable enable

namespace KdlDotNet
{
    using static CharClasses;

    /**
 * Internal class used to print strings at the minimum level of quoting required
 */
    public static class PrintUtil
    {
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


            if (bareAllowed && IsValidBareId(str))
            {
                writer.Write(str);
                return;
            }

            writer.Write('"');
            for (int i = 0; i < str.Length; i++)
            {
                int c = str[i];
                if (printConfig.RequiresEscape(c))
                {
                    writer.Write(GetEscapeIncludingUnicode(c));
                }
                else
                {
                    // write codepoint rather than integer value
                    if (c < char.MaxValue) // char.ConvertFromUtf32 allocates a temp string for multi-char codepoints; avoid for smaller chars
                        writer.Write((char)c);
                    else
                        writer.Write(char.ConvertFromUtf32(c)); // would fail for codepoints > 32 bits but I'm not sure we have any of those
                }
            }
            writer.Write('"');

        }
    }


    /**
     * A config object controlling various aspects of how KDL documents are printed.
     */
    public class PrintConfig
    {
        public static PrintConfig PrettyDefault => new PrintConfig();
        public static PrintConfig RawDefault => new PrintConfig(indent: 0, escapeNonAscii: false, printEmptyChildren: false);

        /** 
         * Check if character has been set to force strings containing it to be escaped 
         */
        public HashSet<int>? ForceEscapeChars { get; }

        public bool EscapeNonPrintableAscii { get; }

        public bool EscapeLinespace { get; }
        public bool EscapeNonAscii { get; }
        public bool EscapeCommon { get; }

        /**
         * @return true if each node should be terminated with a ';', false if semicolons will be omitted entirely
         */
        public bool RequireSemicolons { get; }

        /**
         * @return true if each number should be printed with its specified radix, false if they should be printed just base-10
         */
        public bool RespectRadix { get; }

        /**
         * @return get the string used to print newlines
         */
        public string NewLine { get; }

        /**
         * @return how many getIndentChar() characters lines will be indented for each level they are away from the root.
         *         If 0, no indentation will be performed
         */
        public int Indent { get; }

        /**
         * @return the character used to indent lines
         */
        public char IndentChar { get; }

        /**
         * @return the character used to indicate the beginning of the exponent part of floating point numbers
         */
        public char ExponentChar { get; }

        /**
         * @return true if empty children should be printed with braces containing no nodes, false if they shouldn't be printed
         */
        public bool PrintEmptyChildren { get; }

        /**
         * @return true if node arguments with the literal value 'null' will be printed
         */
        public bool PrintNullArgs { get; }

        /**
         * @return true if node properties with the literal value 'null' will be printed
         */
        public bool PrintNullProps { get; }

        public PrintConfig(
            bool escapeNonPrintableAscii = true,
            bool escapeLinespace = true,
            bool escapeNonAscii = false,
            bool escapeCommon = true,
            bool requireSemicolons = false,
            bool respectRadix = true,
            string newLine = "\n",
            int indent = 4,
            char indentChar = ' ',
            char exponentChar = 'E',
            bool printEmptyChildren = true,
            bool printNullArgs = true,
            bool printNullProps = true,
            IEnumerable<int>? forceEscapeChars = null)
        {
            if (exponentChar != 'e' && exponentChar != 'E')
                throw new ArgumentException("Exponent character must be either 'e' or 'E'", nameof(exponentChar));

            if (newLine.Any(c => !IsUnicodeLinespace(c)))
                throw new ArgumentException("All characters in specified 'newline' must be unicode vertical space", nameof(newLine));

            if (!IsUnicodeWhitespace(indentChar))
                throw new ArgumentException("Indent character must be unicode whitespace", nameof(indentChar));

            EscapeNonPrintableAscii = escapeNonPrintableAscii;
            EscapeLinespace = escapeLinespace;
            EscapeNonAscii = escapeNonAscii;
            EscapeCommon = escapeCommon;
            RequireSemicolons = requireSemicolons;
            RespectRadix = respectRadix;
            NewLine = newLine;
            Indent = indent;
            IndentChar = indentChar;
            ExponentChar = exponentChar;
            PrintEmptyChildren = printEmptyChildren;
            PrintNullArgs = printNullArgs;
            PrintNullProps = printNullProps;

            if (forceEscapeChars != null)
                ForceEscapeChars = new HashSet<int>(forceEscapeChars);
        }

        public bool RequiresEscape(int c)
        {
            if (ShouldForceEscape(c))
            {
                return true;
            }
            else if (MustEscape(c))
            {
                return true;
            }
            else if (EscapeLinespace && IsUnicodeLinespace(c))
            {
                return true;
            }
            else if (EscapeNonPrintableAscii && !IsNonAscii(c) && !IsPrintableAscii(c))
            {
                return true;
            }
            else if (EscapeNonAscii && IsNonAscii(c))
            {
                return true;
            }
            else if (EscapeCommon && IsCommonEscape(c))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
        * Check if character has been set to force strings containing it to be escaped
        *
        * @param c the character to check
        * @return true if the character should be escaped, false otherwise.
        */
        public bool ShouldForceEscape(int c)
            => ForceEscapeChars?.Contains(c) ?? false;
    }
}
