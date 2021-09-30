using System.IO;

#nullable enable

namespace KdlDotNet
{

    /**
     * An object presenting a key=value pair in a KDL document. Only used during parsing.
     */
    public class KDLProperty : IKDLObject
    {
        public KDLProperty(string key, KDLValue value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public KDLValue Value { get; }

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig) // throws IOException
        {
            if (Value == KDLNull.Instance && printConfig.PrintNullProps)
                return;

            PrintUtil.WriteStringQuotedAppropriately(writer, Key, true, printConfig);
            writer.Write('=');
            Value.WriteKDL(writer, printConfig);
        }

        public override string ToString() => $"KDLProperty{{key={Key}, value={Value}}}";

        public override bool Equals(object obj)
            => obj is KDLProperty other && Key == other.Key && Value.Equals(other.Value);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Key.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
    }
}
