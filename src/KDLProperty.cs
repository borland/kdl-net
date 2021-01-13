using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace kdl_net
{

    /**
     * An object presenting a key=value pair in a KDL document. Only used during parsing.
     */
    public class KDLProperty : IKDLObject
    {
        public KDLProperty(string key, IKDLValue value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public IKDLValue Value { get; }

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig) // throws IOException
        {
            if (Value == KDLNull.Instance && printConfig.PrintNullProps)
            {
                return;
            }

            PrintUtil.WriteStringQuotedAppropriately(writer, Key, true, printConfig);
            writer.Write('=');
            Value.WriteKDL(writer, printConfig);
        }

        public override string ToString() => $"KDLProperty{{key={Key}, value={Value}}}";
    }
}
