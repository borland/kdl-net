using System;
using System.Collections.Generic;
using System.Text;

namespace kdl_net
{
    /**
     * Thrown if an unexpected state is encountered while parsing a document. If you encounter this
     * please create an issue on https://github.com/hkolbeck/kdl4j/issues with the offending document
     */
    public class KDLInternalException : Exception
    {
        public KDLInternalException(string message) : base(message) { }

        public KDLInternalException(string message, Exception innerException) : base(message, innerException) { }
    }

    /**
     * Thrown if a document cannot be parsed for any reason. The message will indicate the error and contain the line
     * and character where the parse failure occurred.
     */
    public class KDLParseException : Exception
    {
        public KDLParseException(string message) : base(message) { }
        
        public KDLParseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
