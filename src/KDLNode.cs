using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable enable

namespace KdlDotNet
{
    public class KDLNode : IKDLObject
    {
        private static readonly IReadOnlyDictionary<string, IKDLValue> EmptyProps = new Dictionary<string, IKDLValue>(0);
        private static readonly IReadOnlyList<IKDLValue> EmptyArgs = new List<IKDLValue>(0);

        public KDLNode(string identifier) : this(identifier, EmptyProps, EmptyArgs, null) { }

        public KDLNode(string identifier, KDLDocument? child = null) : this(identifier, EmptyProps, EmptyArgs, child) { }

        public KDLNode(string identifier, IReadOnlyList<IKDLValue> args, KDLDocument? child = null) : this(identifier, EmptyProps, args, child) { }

        public KDLNode(string identifier, IReadOnlyDictionary<string, IKDLValue> props) : this(identifier, props, EmptyArgs, null) { }

        public KDLNode(string identifier, IReadOnlyDictionary<string, IKDLValue> props, IReadOnlyList<IKDLValue> args, KDLDocument? child = null)
        {
            Identifier = identifier;
            Props = props; // C# Note: The java version wraps this in Collections.unmodifiableList. We use IReadOnlyList which is not quite the same, but hopefully close enough
            Args = args;
            Child = child;
        }

        public string Identifier { get; }
        public IReadOnlyDictionary<string, IKDLValue> Props { get; }
        public IReadOnlyList<IKDLValue> Args { get; }
        public KDLDocument? Child { get; }

        /**
         * Writes a text representation of the node to the provided writer
         *
         * @param writer the writer to write to
         * @param printConfig configuration controlling how the node is written
         * @throws IOException if there's any error writing the node
         */
        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
        {
            WriteKDLPretty(writer, 0, printConfig);
        }

        internal void WriteKDLPretty(StreamWriter writer, int depth, PrintConfig printConfig)
        {
            PrintUtil.WriteStringQuotedAppropriately(writer, Identifier, true, printConfig);
            if (Args.Count > 0 || Props.Count > 0 || Child != null)
            {
                writer.Write(' ');
            }

            for (int i = 0; i < Args.Count; i++)
            {
                var value = Args[i];
                if (value != KDLNull.Instance || printConfig.PrintNullArgs)
                {
                    value.WriteKDL(writer, printConfig);
                    if (i < Args.Count - 1 || Props.Count > 0 || Child != null)
                    {
                        writer.Write(' ');
                    }
                }
            }

            var keys = Props.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                var value = Props[keys[i]];
                if (value != KDLNull.Instance|| printConfig.PrintNullProps)
                {
                    PrintUtil.WriteStringQuotedAppropriately(writer, keys[i], true, printConfig);
                    writer.Write('=');
                    value.WriteKDL(writer, printConfig);
                    if (i < keys.Count - 1 || Child != null)
                    {
                        writer.Write(' ');
                    }
                }
            }

            if (Child != null)
            {
                if (Child.Nodes.Count > 0 || printConfig.PrintEmptyChildren)
                {
                    writer.Write('{');
                    writer.Write(printConfig.NewLine);
                    Child.WriteKDL(writer, depth + 1, printConfig);
                    for (int i = 0; i < printConfig.Indent * depth; i++)
                    {
                        writer.Write(printConfig.IndentChar);
                    }
                    writer.Write('}');
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KDLNode other))
                return false;

            return Identifier == other.Identifier && Props.SequenceEqual(other.Props) && Args.SequenceEqual(other.Args) && Equals(Child, other.Child);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Identifier.GetHashCode();
                foreach(var prop in Props)
                    hash = hash * 23 + prop.GetHashCode();
                foreach (var arg in Args)
                    hash = hash * 23 + arg.GetHashCode();
                if(Child != null)
                    hash = hash * 23 + Child.GetHashCode();
                return hash;
            }
        }
    }
}
