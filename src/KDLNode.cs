using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable enable

namespace KdlDotNet
{
    public class KDLNode : IKDLObject
    {
        private static readonly IReadOnlyDictionary<string, KDLValue> EmptyProps = new Dictionary<string, KDLValue>(0);
        private static readonly IReadOnlyList<KDLValue> EmptyArgs = new List<KDLValue>(0);

        public KDLNode(string identifier) : this(identifier, null, EmptyProps, EmptyArgs, null) { }
        
        public KDLNode(string identifier, string? type) : this(identifier, type, EmptyProps, EmptyArgs, null) { }

        public KDLNode(string identifier, KDLDocument? child = null) : this(identifier, null, EmptyProps, EmptyArgs, child) { }
        public KDLNode(string identifier, string? type, KDLDocument? child = null) : this(identifier, type, EmptyProps, EmptyArgs, child) { }
        
        public KDLNode(string identifier, IReadOnlyList<KDLValue> args, KDLDocument? child = null) : this(identifier, null, EmptyProps, args, child) { }
        public KDLNode(string identifier, string? type, IReadOnlyList<KDLValue> args, KDLDocument? child = null) : this(identifier, type, EmptyProps, args, child) { }
        
        public KDLNode(string identifier, IReadOnlyDictionary<string, KDLValue> props) : this(identifier, null, props, EmptyArgs, null) { }
        public KDLNode(string identifier, string? type, IReadOnlyDictionary<string, KDLValue> props) : this(identifier, type, props, EmptyArgs, null) { }

        public KDLNode(string identifier, IReadOnlyDictionary<string, KDLValue> props, IReadOnlyList<KDLValue> args, KDLDocument? child = null) : this(identifier, null, props, args, child) { }
        public KDLNode(string identifier, string? type, IReadOnlyDictionary<string, KDLValue> props, IReadOnlyList<KDLValue> args, KDLDocument? child = null)
        {
            Identifier = identifier;
            Type = type;
            Props = props; // C# Note: The java version wraps this in Collections.unmodifiableList. We use IReadOnlyList which is not quite the same, but hopefully close enough
            Args = args;
            Child = child;
        }


        public string Identifier { get; }
        public string? Type { get; }
        public IReadOnlyDictionary<string, KDLValue> Props { get; }
        public IReadOnlyList<KDLValue> Args { get; }
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
            if (Type != null)
            {
                writer.Write('(');
                PrintUtil.WriteStringQuotedAppropriately(writer, Type, true, printConfig);
                writer.Write(')');
            }

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

        public override string ToString()
            => $"KDLNode{{identifier={Identifier}, type={Type}, props={string.Join(",", Props)}, args={string.Join(",", Args)}, child=}}";

        public override bool Equals(object obj)
        {
            if (!(obj is KDLNode other))
                return false;

            return Identifier == other.Identifier && Type == other.Type && Props.SequenceEqual(other.Props) && Args.SequenceEqual(other.Args) && Equals(Child, other.Child);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Identifier.GetHashCode();

                if(Type != null)
                    hash = hash * 23 + Type.GetHashCode();

                foreach (var prop in Props)
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
