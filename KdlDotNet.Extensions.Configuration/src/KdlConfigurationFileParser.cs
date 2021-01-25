using KdlDotNet;
using System.Collections.Generic;
using System.IO;

#nullable enable

namespace Microsoft.Extensions.Configuration
{
    static class KdlConfigurationFileParser
    {
        public static Dictionary<string, string> Parse(Stream stream)
        {
            var doc = new KDLParser().Parse(stream);
            var context = new List<string>(capacity: 4);
            var result = new Dictionary<string, string>();

            VisitKdlDoc(doc, context, result);

            return result;
        }

        public static void VisitKdlDoc(KDLDocument doc, List<string> context, Dictionary<string, string> result)
        {
            foreach (var node in doc.Nodes)
            {
                context.Add(node.Identifier);

                if (node.Props.Count > 0)
                {
                    foreach (var (k, v) in node.Props)
                    {
                        context.Add(k);
                        result[string.Join(':', context)] = v.AsString().Value;
                        context.RemoveAt(context.Count - 1);
                    }
                }

                if (node.Child != null)
                {
                    VisitKdlDoc(node.Child, context, result);
                }
                else if (node.Args.Count > 0)
                {
                    result[string.Join(':', context)] = node.Args[0].AsString().Value;
                }

                context.RemoveAt(context.Count - 1);
            }
        }
    }
}
