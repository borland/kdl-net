﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace KdlDotNet
{
    using static CharClasses;

    // The core parser object. Instances are stateless and safe to share between threads.
    public class KDLParser
    {
        internal const int EOF = -1;
        internal const int MaxUnicode = 0x10FFFF;

        internal enum WhitespaceResult
        {
            NoWhitespace, EndNode, SkipNext, NodeSpace
        }

        internal enum SlashAction
        {
            EndNode, SkipNext, Nothing
        }

        /**
         * Parse the given stream into a KDLDocument model object.
         *
         * @param reader the stream reader to parse from
         * @return the parsed document
         * @throws IOException if any error occurs while reading the stream
         * @throws KDLParseException if the document is invalid for any reason
         */
        public KDLDocument Parse(StreamReader reader)
        {
            var context = new KDLParseContext(reader);

            return ParseDocument(context, true);
        }

        public KDLDocument Parse(Stream stream) => Parse(new StreamReader(stream));

        public KDLDocument Parse(string str) => Parse(
            new StreamReader(
                new MemoryStream(Encoding.UTF8.GetBytes(str)), Encoding.UTF8));

        // @throws IOException
        private KDLDocument ParseDocument(KDLParseContext context, bool root)
        {
            var c = context.Peek();
            if (c == EOF)
                return new KDLDocument(new List<KDLNode>(0));

            var nodes = new List<KDLNode>();
            while (true)
            {
                bool skippingNode = false;
                switch (ConsumeWhitespaceAndLinespace(context))
                {
                    case WhitespaceResult.NodeSpace:
                    case WhitespaceResult.NoWhitespace:
                        break;
                    case WhitespaceResult.EndNode:
                        c = context.Peek();
                        if (c == EOF)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    case WhitespaceResult.SkipNext:
                        skippingNode = true;
                        break;
                }

                c = context.Peek();
                if (c == EOF)
                {
                    if (root)
                    {
                        return new KDLDocument(nodes);
                    }
                    else
                    {
                        throw new KDLParseException("Got EOF, expected a node or '}'");
                    }
                }
                else if (c == '}')
                {
                    if (root)
                    {
                        throw new KDLParseException("Unexpected '}' in root document");
                    }
                    else
                    {
                        return new KDLDocument(nodes);
                    }
                }

                var node = ParseNode(context);
                ConsumeAfterNode(context);
                if (!skippingNode && node != null)
                {
                    nodes.Add(node);
                }
            }
        }


        internal KDLNode? ParseNode(KDLParseContext context) // throws IOException
        {
            var args = new List<KDLValue>();
            var properties = new Dictionary<string, KDLValue>();
            KDLDocument? child = null;

            int c = context.Peek();
            if (c == '}')
            {
                return null;
            }

            var type = ParseTypeIfPresent(context);
            var identifier = ParseIdentifier(context);
            while (true)
            {
                var whitespaceResult = ConsumeWhitespaceAndBlockComments(context);
                c = context.Peek();
                switch (whitespaceResult)
                {
                    case WhitespaceResult.NodeSpace:
                        if (c == '{')
                        {
                            child = ParseChild(context);
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        else if (IsUnicodeLinespace(c))
                        {
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        if (c == EOF)
                        {
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        else
                        {
                            var obj = ParseArgOrProp(context);
                            if (obj is KDLValue kdlValue)
                            {
                                args.Add(kdlValue);
                            }
                            else if (obj is KDLProperty kdlProp)
                            {
                                properties[kdlProp.Key] = kdlProp.Value;
                            }
                            else
                            {
                                throw new KDLInternalException(
                                        string.Format("Unexpected type found, expected property, arg, or child: '{0}' type: {1}",
                                                obj.ToKDL(), obj.GetType().ToString()));
                            }
                        }
                        break;

                    case WhitespaceResult.NoWhitespace:
                        if (c == '{')
                        {
                            child = ParseChild(context);
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        else if (IsUnicodeLinespace(c) || c == EOF)
                        {
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        else if (c == ';')
                        {
                            context.Read();
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        else
                        {
                            throw new KDLParseException($"Unexpected character: '{(char)c}'");
                        }
                    case WhitespaceResult.EndNode:
                        return new KDLNode(identifier, type, properties, args, child);
                    case WhitespaceResult.SkipNext:
                        if (c == '{')
                        {
                            ParseChild(context); //Ignored
                            return new KDLNode(identifier, type, properties, args, child);
                        }
                        else if (IsUnicodeLinespace(c))
                        {
                            throw new KDLParseException("Unexpected skip marker before newline");
                        }
                        else if (c == EOF)
                        {
                            throw new KDLParseException("Unexpected EOF following skip marker");
                        }
                        else
                        {
                            var obj = ParseArgOrProp(context);
                            if (!(obj is KDLValue) && !(obj is KDLProperty))
                            {
                                throw new KDLInternalException(
                                        string.Format("Unexpected type found, expected property, arg, or child: '{0}' type: {1}",
                                                obj.ToKDL(), obj.GetType()));
                            }
                        }
                        break;
                }
            }
        }

        internal string ParseIdentifier(KDLParseContext context) // throws IOException
        {
            int c = context.Peek();
            if (c == '"')
            {
                return ParseEscapedString(context);
            }
            else if (IsValidBareIdStart(c))
            {
                if (c == 'r')
                {
                    context.Read();
                    int next = context.Peek();
                    context.Unread('r');
                    if (next == '"' || next == '#')
                    {
                        return ParseRawString(context);
                    }
                    else
                    {
                        return ParseBareIdentifier(context);
                    }
                }
                else
                {
                    return ParseBareIdentifier(context);
                }
            }
            else
            {
                throw new KDLParseException(string.Format("Expected an identifier, but identifiers can't start with '{0}'", (char)c));
            }
        }

        IKDLObject ParseArgOrProp(KDLParseContext context) // throws IOException
        {
            IKDLObject obj;
            var type = ParseTypeIfPresent(context);
            bool isBare = false;
            int c = context.Peek();
            if (c == '"')
            {
                obj = new KDLString(ParseEscapedString(context), type);
            }
            else if (IsValidNumericStart(c))
            {
                obj = ParseNumber(context, type);
            }
            else if (IsValidBareIdStart(c))
            {
                string strVal;
                if (c == 'r')
                {
                    context.Read();
                    int next = context.Peek();
                    context.Unread('r');
                    if (next == '"' || next == '#')
                    {
                        strVal = ParseRawString(context);
                    }
                    else
                    {
                        isBare = true;
                        strVal = ParseBareIdentifier(context);
                    }
                }
                else
                {
                    isBare = true;
                    strVal = ParseBareIdentifier(context);
                }

                if (isBare)
                {
                    if ("true" == strVal)
                    {
                        obj = KDLBoolean.From(true, type);
                    }
                    else if ("false" == strVal)
                    {
                        obj = KDLBoolean.From(false, type);
                    }
                    else if ("null" == strVal)
                    {
                        obj = KDLNull.From(type);
                    }
                    else
                    {
                        obj = new KDLString(strVal, type);
                    }
                }
                else
                {
                    obj = new KDLString(strVal, type);
                }
            }
            else
            {
                throw new KDLParseException($"Unexpected character: '{(char)c}'");
            }

            if (obj is KDLString kdlString)
            {
                c = context.Peek();
                if (c == '=')
                {
                    if(type != null)
                        throw new KDLParseException("Illegal type annotation before property, annotations should follow the '=' and precede the value");

                    context.Read();
                    var value = ParseValue(context);
                    return new KDLProperty(kdlString.Value, value);
                }
                else if (isBare)
                {
                    throw new KDLParseException("Arguments may not be bare");
                }
                else
                {
                    return obj;
                }
            }
            else
            {
                return obj;
            }
        }

        internal KDLDocument ParseChild(KDLParseContext context) // throws IOException
        {
            int c = context.Read();
            if (c != '{')
            {
                throw new KDLInternalException(string.Format("Expected '{{' but found '{0}'", (char)c));
            }

            var document = ParseDocument(context, false);

            switch (ConsumeWhitespaceAndLinespace(context))
            {
                case WhitespaceResult.EndNode:
                    throw new KDLInternalException("Got unexpected END_NODE");
                case WhitespaceResult.SkipNext:
                    throw new KDLParseException("Trailing skip markers are not allowed");
                    // default:
                    //Fall through
            }

            c = context.Read();
            if (c != '}')
            {
                throw new KDLParseException("No closing brace found for child");
            }

            return document;
        }

        internal string? ParseTypeIfPresent(KDLParseContext context) // throws IOException
        {
            string? type = null;
            int c = context.Peek();
            if (c == '(') 
            {
                context.Read();
                type = ParseIdentifier(context);
                c = context.Read();
                if (c != ')')
                    throw new KDLParseException("Un-terminated type annotation, missing closing paren.");
            }

            return type;
        }

        internal KDLValue ParseValue(KDLParseContext context) // throws IOException
        {
            var type = ParseTypeIfPresent(context);
            int c = context.Peek();
            if (c == '"')
            {
                return new KDLString(ParseEscapedString(context), type);
            }
            else if (c == 'r')
            {
                return new KDLString(ParseRawString(context), type);
            }
            else if (IsValidNumericStart(c))
            {
                return ParseNumber(context, type);
            }
            else
            {
                var stringBuilder = new StringBuilder();

                while (IsLiteralChar(c))
                {
                    context.Read();
                    stringBuilder.AppendCodePoint(c);
                    c = context.Peek();
                }

                var strVal = stringBuilder.ToString();
                return strVal switch {
                    "true" => new KDLBoolean(true, type),
                    "false" => new KDLBoolean(false, type),
                    "null" => new KDLNull(type),
                    _ => throw new KDLParseException(string.Format("Unknown literal in property value: '{0}' Expected 'true', 'false', or 'null'", strVal))
                };
            }
        }

        internal KDLNumber ParseNumber(KDLParseContext context, string? type = null) // throws IOException
        {
            int radix = 10;
            Predicate<int> legalChars = x => throw new InvalidOperationException("ParseNonDecimalNumber should not be invoked for a decimal number");

            int c = context.Peek();
            if (c == '0')
            {
                context.Read();
                c = context.Peek();
                if (c == 'x')
                {
                    context.Read();
                    radix = 16;
                    legalChars = IsValidHexChar;
                }
                else if (c == 'o')
                {
                    context.Read();
                    radix = 8;
                    legalChars = IsValidOctalChar;
                }
                else if (c == 'b')
                {
                    context.Read();
                    radix = 2;
                    legalChars = IsValidBinaryChar;
                }
                else
                {
                    context.Unread('0');
                    radix = 10;
                }
            }
            else
            {
                radix = 10;
            }

            if (radix == 10)
            {
                return ParseDecimalNumber(context, type);
            }
            else
            {
                return ParseNonDecimalNumber(context, legalChars, radix, type);
            }
        }

        KDLNumber ParseNonDecimalNumber(KDLParseContext context, Predicate<int> legalChars, int radix, string? type) // throws IOException
        {
            var stringBuilder = new StringBuilder();

            int c = context.Peek();
            if (c == '_')
            {
                throw new KDLParseException("The first character after radix indicator must not be '_'");
            }

            while (legalChars(c) || c == '_')
            {
                context.Read();
                if (c != '_')
                {
                    stringBuilder.AppendCodePoint((char)c);
                }
                c = context.Peek();
            }

            var str = stringBuilder.ToString();
            if (str.Length == 0)
            {
                throw new KDLParseException("Must include at least one digit following radix marker");
            }

            return KDLNumber.From(str, radix, type, KDLNumberParseFlags.None)!;
        }

        // Unfortunately, in order to match the grammar we have to do a lot of parsing ourselves here
        KDLNumber ParseDecimalNumber(KDLParseContext context, string? type) // throws IOException
        {
            var stringBuilder = new StringBuilder();
            KDLNumberParseFlags parseFlags = KDLNumberParseFlags.None;

            bool inFraction = false;
            bool inExponent = false;
            bool signLegal = true;
            int c = context.Peek();
            if (c == '_' || c == 'E' || c == 'e')
            {
                throw new KDLParseException(string.Format("Decimal numbers may not begin with an '{0}' character", (char)c));
            }
            else if (c == '+' || c == '-')
            {
                context.Read();
                int sign = c;
                c = context.Peek();
                if (c == '_')
                {
                    throw new KDLParseException("Numbers may not begin with an '_' character after sign");
                }
                else
                {
                    stringBuilder.AppendCodePoint(sign);
                    signLegal = false;
                }
            }

            c = context.Peek();
            while (IsValidDecimalChar(c) || c == 'e' || c == 'E' || c == '_' || c == '.' || c == '-' || c == '+')
            {
                context.Read();
                if (c == '.')
                {
                    if (inFraction || inExponent)
                    {
                        throw new KDLParseException("The '.' character is not allowed in the fraction or exponent of a decimal");
                    }

                    if (!IsValidDecimalChar(context.Peek()))
                    {
                        throw new KDLParseException("The character following '.' in a decimal number must be a decimal digit");
                    }

                    parseFlags |= KDLNumberParseFlags.HasDecimalPoint; // hint for KDLNumber.From

                    inFraction = true;
                    signLegal = false;
                    stringBuilder.AppendCodePoint(c);
                }
                else if (c == 'e' || c == 'E')
                {
                    if (inExponent)
                    {
                        throw new KDLParseException(string.Format("Found '{0}' in exponent", (char)c));
                    }

                    parseFlags |= KDLNumberParseFlags.HasScientificNotation; // hint for KDLNumber.From

                    inExponent = true;
                    inFraction = false;
                    signLegal = true;
                    stringBuilder.AppendCodePoint(c);

                    if (context.Peek() == '_')
                    {
                        throw new KDLParseException("Character following exponent marker must not be '_'");
                    }
                }
                else if (c == '_')
                {
                    if (inFraction)
                    {
                        throw new KDLParseException("The '_' character is not allowed in the fraction portion of decimal");
                    }
                    signLegal = false;
                }
                else if (c == '+' || c == '-')
                {
                    if (!signLegal)
                    {
                        throw new KDLParseException(string.Format("The sign character '{0}' is not allowed here", (char)c));
                    }

                    signLegal = false;
                    stringBuilder.AppendCodePoint(c);
                }
                else
                {
                    signLegal = false;
                    stringBuilder.AppendCodePoint(c);
                }

                c = context.Peek();
            }

            var val = stringBuilder.ToString();
            try
            {
                return KDLNumber.From(val, 10, type, parseFlags)!; // radix must always be 10 here
            }
            catch (FormatException e)
            {
                throw new KDLInternalException(string.Format("Couldn't parse pre-vetted input '{0}' into a BigDecimal", val), e);
            }
        }

        internal string ParseBareIdentifier(KDLParseContext context) // throws IOException
        {
            int c = context.Read();
            if (!IsValidBareIdStart(c))
            {
                throw new KDLParseException("Illegal character at start of bare identifier");
            }
            else if (c == EOF)
            {
                throw new KDLInternalException("EOF when a bare identifer expected");
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendCodePoint(c);

            c = context.Peek();
            while (IsValidBareIdChar(c) && c != EOF)
            {
                stringBuilder.AppendCodePoint(context.Read());
                c = context.Peek();
            }

            return stringBuilder.ToString();
        }

        internal string ParseEscapedString(KDLParseContext context) // throws IOException
        {
            int c = context.Read();
            if (c != '"')
                throw new KDLInternalException("No quote at the beginning of escaped string");

            var stringBuilder = new StringBuilder();
            bool inEscape = false;
            while (true)
            {
                c = context.Read();
                if (!inEscape && c == '\\')
                {
                    inEscape = true;
                }
                else if (c == '"' && !inEscape)
                {
                    return stringBuilder.ToString();
                }
                else if (inEscape)
                {
                    stringBuilder.AppendCodePoint(GetEscaped(c, context));
                    inEscape = false;
                }
                else if (c == EOF)
                {
                    throw new KDLParseException("EOF while reading an escaped string");
                }
                else
                {
                    stringBuilder.AppendCodePoint(c);
                }
            }
        }

        internal int GetEscaped(int c, KDLParseContext context) // throws IOException
        {
            switch (c)
            {
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case '\\':
                    return '\\';
                case '/':
                    return '/';
                case '"':
                    return '\"';
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'u':
                    {
                        var stringBuilder = new StringBuilder(6);
                        c = context.Read();
                        if (c != '{')
                            throw new KDLParseException("Unicode escape sequences must be surround by {} brackets");

                        c = context.Read();
                        while (c != '}')
                        {
                            if (c == EOF)
                            {
                                throw new KDLParseException("Reached EOF while reading unicode escape sequence");
                            }
                            else if (!IsValidHexChar(c))
                            {
                                throw new KDLParseException(string.Format("Unicode escape sequences must be valid hex chars, got: '{0}'", (char)c));
                            }

                            stringBuilder.AppendCodePoint(c);
                            c = context.Read();
                        }

                        var strCode = stringBuilder.ToString();
                        if (strCode.Length == 0 || strCode.Length > 6)
                        {
                            throw new KDLParseException(string.Format("Unicode escape sequences must be between 1 and 6 characters in length. Got: '{0}'", strCode));
                        }

                        int code;
                        try
                        {
                            code = Convert.ToInt32(strCode, 16);
                        }
                        catch (FormatException)
                        {
                            throw new KDLParseException(string.Format("Couldn't parse '{0}' as a hex integer", strCode));
                        }

                        if (code < 0 || MaxUnicode < code)
                        {
                            throw new KDLParseException(string.Format("Unicode code point is outside allowed range [0, {0}]: {0}", MaxUnicode, code));
                        }
                        else
                        {
                            return code;
                        }
                    }
                default:
                    throw new KDLParseException(string.Format("Illegal escape sequence: '\\{0}'", (char)c));
            }
        }

        internal string ParseRawString(KDLParseContext context) // throws IOException
        {
            int c = context.Read();
            if (c != 'r')
            {
                throw new KDLInternalException("Raw string should start with 'r'");
            }

            int hashDepth = 0;
            c = context.Read();
            while (c == '#')
            {
                hashDepth++;
                c = context.Read();
            }

            if (c != '"')
            {
                throw new KDLParseException("Malformed raw string");
            }

            var stringBuilder = new StringBuilder();
            while (true)
            {
                c = context.Read();
                if (c == '"')
                {
                    StringBuilder subStringBuilder = new StringBuilder();
                    subStringBuilder.Append('"');
                    int hashDepthHere = 0;
                    while (true)
                    {
                        c = context.Peek();
                        if (c == '#')
                        {
                            context.Read();
                            hashDepthHere++;
                            subStringBuilder.Append('#');
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (hashDepthHere < hashDepth)
                    {
                        stringBuilder.Append(subStringBuilder);
                    }
                    else if (hashDepthHere == hashDepth)
                    {
                        return stringBuilder.ToString();
                    }
                    else
                    {
                        throw new KDLParseException("Too many # characters when closing raw string");
                    }
                }
                else if (c == EOF)
                {
                    throw new KDLParseException("EOF while reading raw string");
                }
                else
                {
                    stringBuilder.AppendCodePoint(c);
                }
            }
        }

        internal SlashAction GetSlashAction(KDLParseContext context, bool escaped) // throws IOException
        {
            int c = context.Read();
            if (c != '/')
            {
                throw new KDLParseException("");
            }

            c = context.Read();
            switch (c)
            {
                case '-':
                    return SlashAction.SkipNext;
                case '*':
                    ConsumeBlockComment(context);
                    return SlashAction.Nothing;
                case '/':
                    ConsumeLineComment(context);
                    if (escaped)
                    {
                        return SlashAction.Nothing;
                    }
                    else
                    {
                        return SlashAction.EndNode;
                    }
                default:
                    throw new KDLParseException(string.Format("Unexpected character: '{0}'", (char)c));
            }
        }

        internal void ConsumeAfterNode(KDLParseContext context) // throws IOException
        {
            int c = context.Peek();
            while (c == ';' || IsUnicodeWhitespace(c))
            {
                context.Read();
                c = context.Peek();
            }
        }

        internal WhitespaceResult ConsumeWhitespaceAndBlockComments(KDLParseContext context) // throws IOException
        {
            bool skipping = false;
            bool foundWhitespace = false;
            bool inLineEscape = false;
            bool foundSemicolon = false;
            int c = context.Peek();
            while (c == '/' || c == '\\' || c == ';' || IsUnicodeWhitespace(c) || IsUnicodeLinespace(c))
            {
                if (c == '/')
                {
                    switch (GetSlashAction(context, inLineEscape))
                    {
                        case SlashAction.EndNode:
                            if (inLineEscape)
                            {
                                foundWhitespace = true;
                                inLineEscape = false;
                                break;
                            }
                            return WhitespaceResult.EndNode;

                        case SlashAction.SkipNext:
                            if (inLineEscape)
                            {
                                throw new KDLParseException("Found skip marker after line escape");
                            }

                            if (skipping)
                            {
                                throw new KDLParseException("Node/Token skip may only be specified once per node/token");
                            }
                            else
                            {
                                skipping = true;
                            }
                            break;

                        case SlashAction.Nothing:
                            foundWhitespace = true;
                            break;
                    }
                }
                else if (c == ';')
                {
                    context.Read();
                    foundSemicolon = true;
                }
                else if (c == '\\')
                {
                    context.Read();
                    inLineEscape = true;
                }
                else if (IsUnicodeLinespace(c))
                {
                    if (inLineEscape)
                    {
                        context.Read();
                        if (c == '\r')
                        {
                            c = context.Peek();
                            if (c == '\n')
                            {
                                context.Read();
                            }
                        }

                        inLineEscape = false;
                        foundWhitespace = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    context.Read();
                    foundWhitespace = true;
                }

                c = context.Peek();
            }

            if (skipping)
            {
                return WhitespaceResult.SkipNext;
            }
            else if (foundSemicolon)
            {
                return WhitespaceResult.EndNode;
            }
            else if (foundWhitespace)
            {
                return WhitespaceResult.NodeSpace;
            }
            else
            {
                return WhitespaceResult.NoWhitespace;
            }
        }

        void ConsumeLineComment(KDLParseContext context) // throws IOException
        {
            int c = context.Peek();
            while (!IsUnicodeLinespace(c) && c != EOF)
            {
                context.Read();
                if (c == '\r')
                {
                    c = context.Peek();
                    if (c == '\n')
                    {
                        context.Read();
                    }
                    return;
                }
                c = context.Peek();
            }
        }

        void ConsumeBlockComment(KDLParseContext context) // throws IOException
        {
            while (true)
            {
                int c = context.Read();
                while (c != '/' && c != '*' && c != EOF)
                {
                    c = context.Read();
                }

                if (c == EOF)
                {
                    throw new KDLParseException("Got EOF while reading block comment");
                }

                if (c == '/')
                {
                    c = context.Peek();
                    if (c == '*')
                    {
                        context.Read();
                        ConsumeBlockComment(context);
                    }
                }
                else
                { // c == '*'
                    c = context.Peek();
                    if (c == '/')
                    {
                        context.Read();
                        return;
                    }
                }
            }
        }

        internal WhitespaceResult ConsumeWhitespaceAndLinespace(KDLParseContext context) // throws IOException
        {
            bool skipNext = false;
            bool foundWhitespace = false;
            bool inEscape = false;
            while (true)
            {
                int c = context.Peek();
                bool isLinespace = IsUnicodeLinespace(c);
                while (IsUnicodeWhitespace(c) || isLinespace)
                {
                    foundWhitespace = true;
                    if (isLinespace && skipNext)
                    {
                        throw new KDLParseException("Unexpected newline after skip marker");
                    }

                    if (isLinespace && inEscape)
                    {
                        inEscape = false;
                    }

                    context.Read();
                    c = context.Peek();
                    isLinespace = IsUnicodeLinespace(c);
                }

                if (c == '/')
                {
                    switch (GetSlashAction(context, inEscape))
                    {
                        case SlashAction.EndNode:
                        case SlashAction.Nothing:
                            foundWhitespace = true;
                            break;
                        case SlashAction.SkipNext:
                            foundWhitespace = true;
                            skipNext = true;
                            break;
                    }
                }
                else if (c == '\\')
                {
                    context.Read();
                    foundWhitespace = true;
                    inEscape = true;
                }
                else if (c == EOF)
                {
                    if (skipNext)
                    {
                        throw new KDLParseException("Unexpected EOF after skip marker");
                    }
                    else if (inEscape)
                    {
                        throw new KDLParseException("Unexpected EOF after line escape");
                    }
                    else
                    {
                        return WhitespaceResult.EndNode;
                    }
                }
                else
                {
                    if (inEscape)
                    {
                        throw new KDLParseException("Expected newline or line comment following escape");
                    }

                    if (skipNext)
                    {
                        return WhitespaceResult.SkipNext;
                    }
                    else if (foundWhitespace)
                    {
                        return WhitespaceResult.NodeSpace;
                    }
                    else
                    {
                        return WhitespaceResult.NoWhitespace;
                    }
                }
            }
        }
    }
}

static class StringBuilderExtensions
{
    internal static StringBuilder AppendCodePoint(this StringBuilder builder, int codePoint)
    {
        if (codePoint < char.MaxValue) // char.ConvertFromUtf32 allocates a temp string for multi-char codepoints; avoid for smaller chars
            return builder.Append((char)codePoint);

        return builder.Append(char.ConvertFromUtf32(codePoint)); // would fail for codepoints > 32 bits but I'm not sure we have any of those
    }
}