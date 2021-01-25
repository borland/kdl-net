using KdlDotNet;
using System;
using System.IO;

#nullable enable

namespace Microsoft.Extensions.Configuration
{
    class KdlConfigurationProvider : FileConfigurationProvider
    {
        public KdlConfigurationProvider(KdlConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            try
            {
                Data = KdlConfigurationFileParser.Parse(stream);
            }
            catch (KDLParseException e)
            {
                throw new FormatException("Invalid KDL file", e);
            }
        }
    }

    public class KdlConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new KdlConfigurationProvider(this);
        }
    }
}
