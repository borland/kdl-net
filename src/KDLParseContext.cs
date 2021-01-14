using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace kdl_net
{
    using static KDLParser;
    using static CharClasses;

    /**
 * Internal class wrapping the stream containing the document being read. Maintains a list of the last three lines read
 * in order to provide context in the event of a parse error.
 */
    public class KDLParseContext
    {
        private readonly PushBackReader reader;
        private readonly List<StringBuilder> lines = new List<StringBuilder>(); // C# Note: List is probably not very efficient; Java used a Dequeue here

        private int positionInLine;
        private int lineNumber;

        private bool invalidated;

        public KDLParseContext(StreamReader reader)
        {
            lines.Add(new StringBuilder());

            this.reader = new PushBackReader(reader, 2);
            this.positionInLine = 0;
            this.lineNumber = 1;
            this.invalidated = false;
        }

        /**
         * Read a character from the underlying stream. Stores it in a buffer as well for error reporting.
         *
         * @return the character read or EOF if the stream has been exhausted
         * @throws IOException if any error is encountered in the stream read operation
         */
        public int Read()
        {
            if (invalidated)
                throw new KDLInternalException("Attempt to read from an invalidated context");

            int c = reader.Read();
            if (c == EOF)
            {
                return c;
            }
            else if (IsUnicodeLinespace(c))
            {
                // We're cheating a bit here and not checking for CRLF
                positionInLine = 0;
                lineNumber++;
                lines.Add(new StringBuilder());
                while (lines.Count > 3)
                {
                    lines.RemoveAt(lines.Count - 1);
                }
            }
            else
            {
                positionInLine++;
                lines[lines.Count - 1].Append((char)c);
            }

            return c;
        }

        /**
         * Pushes a single character back into the stream. If this method and the peek() function are invoked more than
         * two times without a read() in between an exception will be thrown.
         *
         * @param c the character to be pushed
         */
        public void Unread(int c)
        {
            if (invalidated)
            {
                throw new KDLInternalException("Attempt to unread from an invalidated context");
            }

            if (IsUnicodeLinespace(c))
            {
                lines.RemoveAt(lines.Count-1);
                lineNumber--;
                positionInLine = lines[lines.Count-1].Length - 1;
            }
            else if (c == EOF)
            {
                throw new KDLInternalException("Attempted to unread() EOF");
            }
            else
            {
                positionInLine--;
                var currLine = lines[lines.Count - 1];
                currLine.Remove(currLine.Length-1, 1);
            }

            try
            {
                reader.Unread(c);
            }
            catch (IOException e)
            {
                throw new KDLInternalException("Attempted to unread more than 2 characters in sequence", e);
            }
        }

        /**
         * Gets the next character in the stream without consuming it.
         *
         * @return the next character in the stream
         * @throws IOException if any error occurs reading from the stream
         */
        public int Peek()
        {
            if (invalidated)
                throw new KDLInternalException("Attempt to peek at an invalidated context");

            return reader.Peek();
        }

        /**
         * For use following parse and internal errors for error reporting. Invalidates the context, after which any
         * following operation on the context will fail. Reads the remainder of the current line and returns a string
         * holding the current line followed by a pointer to the character where the context had read to prior to this call.
         *
         * @return the string outlined above
         */
        public string GetErrorLocationAndInvalidateContext()
        {
            if (invalidated)
                throw new KDLInternalException("Attempted to getErrorLocationAndInvalidateContext from an invalid context");

            invalidated = true;

            var stringBuilder = new StringBuilder();
            if (lines.Count < 1)
                throw new KDLInternalException("Attempted to report an error, but there were no line objects in the stack");
            var line = lines[lines.Count - 1];

            try
            {
                int c = reader.Read();
                while (!IsUnicodeLinespace(c) && c != EOF)
                {
                    line.Append((char)c);
                    c = reader.Read();
                }
            }
            catch (IOException)
            {
                line.Append("<Read Error>");
            }

            stringBuilder.Append("Line ").Append(lineNumber).Append(":\n")
                    .Append(line).Append('\n');

            for (int i = 0; i < positionInLine; i++)
            {
                stringBuilder.Append('-');
            }

            return stringBuilder.Append('^').ToString();
        }
    }

    // mimics the java PushBackReader but only implements the few methods we use
    class PushBackReader
    {
        private readonly StreamReader reader;
        private readonly int[] pushbackQueue;
        private int pushbackPos = 0;

        public PushBackReader(StreamReader reader, int pushbackLimit)
        {
            this.reader = reader;
            this.pushbackQueue = new int[pushbackLimit];
        }

        public int Peek()
        {
            if (pushbackPos > 0)
                return pushbackQueue[pushbackPos - 1];
            return reader.Peek();
        }

        public int Read()
        {
            if (pushbackPos > 0)
            {
                return pushbackQueue[(pushbackPos--) - 1];
            }
            return reader.Read();
        }

        public void Unread(int value)
        {
            if (pushbackPos >= pushbackQueue.Length)
                throw new IOException("PushBackReader buffer length exceeded");
            pushbackQueue[pushbackPos++] = value;
        }
    }

}
