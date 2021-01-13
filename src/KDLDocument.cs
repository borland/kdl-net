using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#nullable enable

namespace kdl_net
{

    /**
     * A model object representing a KDL Document. The only data in a document is the list of nodes, which may be empty.
     */
    public class KDLDocument : IKDLObject
    {
        public KDLDocument(IReadOnlyList<KDLNode> nodes)
        {
            Nodes = nodes;
        }

        public IReadOnlyList<KDLNode> Nodes { get; }

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
        {
            WriteKDLPretty(writer, printConfig);
        }

        /**
         * Writes a text representation of the document to the provided writer
         *
         * @param writer the writer to write to
         * @param printConfig configuration controlling how the document is written
         * @throws IOException if there's any error writing the document
         */
        public void WriteKDLPretty(StreamWriter writer, PrintConfig printConfig) // throws IOException
        {
            WriteKDL(writer, 0, printConfig);
        }

        /**
         * Write a text representation of the document to the provided writer with default 'pretty' settings
         *
         * @param writer the writer to write to
         * @throws IOException if there's any error writing the document
         */
        public void WriteKDLPretty(StreamWriter writer) // throws IOException
        {
            WriteKDLPretty(writer, PrintConfig.PrettyDefault);
        }

        /**
         * Get a string representation of the document
         *
         * @param printConfig configuration controlling how the document is written
         * @return the string
         */
        public string ToKDLPretty(PrintConfig printConfig)
        {
            var writer = new MemoryStream();
            var bufferedWriter = new StreamWriter(writer, Encoding.UTF8);

            WriteKDLPretty(bufferedWriter, printConfig);
            bufferedWriter.Flush();

            return Encoding.UTF8.GetString(writer.ToArray());
        }

        /**
         * Get a string representation of the document with default 'pretty' settings
         */
        public string ToKDLPretty() => ToKDLPretty(PrintConfig.PrettyDefault);

        internal void WriteKDL(StreamWriter writer, int depth, PrintConfig printConfig) /*throws IOException*/
        {
            if (Nodes.Count == 0 && depth == 0)
            {
                writer.Write(printConfig.NewLine);
                return;
            }

            foreach (var node in Nodes)
            {
                for (int i = 0; i < printConfig.Indent * depth; i++)
                {
                    writer.Write(printConfig.IndentChar);
                }
                node.WriteKDLPretty(writer, depth, printConfig);
                if (printConfig.RequireSemicolons)
                {
                    writer.Write(';');
                }
                writer.Write(printConfig.NewLine);
            }
        }

        /**
         * Get a document with no nodes
         *
         * @return the empty document
         */
        public static KDLDocument Empty => new KDLDocument(new List<KDLNode>(0));

        public override string ToString() => "KDLDocument{nodes=" + Nodes.Count +"}";

        public override bool Equals(object obj)
        {
            if (!(obj is KDLDocument other))
                return false;

            return Nodes.SequenceEqual(other.Nodes);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach(var node in Nodes)
                    hash = hash * 23 + node.GetHashCode();
                return hash;
            }
        }
    }
}
