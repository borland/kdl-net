using System;
using Microsoft.Extensions.FileProviders;

#nullable enable

namespace Microsoft.Extensions.Configuration
{
    public static class KdlConfigurationExtensions
    {
        /// <summary>
        /// Use AddKdlFile in the same way you would use AddJsonFile when configuring your asp.net core application.
        /// 
        /// When parsing a KDL file, node properties get treated the same as child nodes with attributes. e.g.
        ///
        ///    FooConfig { 
        ///      Bar "37" 
        ///    }
        ///
        /// is the same as 
        ///
        ///    FooConfig Bar=37
        ///
        /// Both Produce a config setting of "FooConfig:Bar" = "37".
        /// You can mix and match freely, for example, the following JSON configuration
        /// 
        ///  "Logging": {
        ///    "IncludeScopes": false,
        ///    "Debug": {
        ///      "LogLevel": {
        ///        "Default": "Information"
        ///      }
        ///    },
        ///    "Console": {
        ///      "LogLevel": {
        ///        "Microsoft.AspNetCore.Mvc.Razor.Internal": "Warning",
        ///        "Microsoft.AspNetCore.Mvc.Razor": "Error",
        ///        "Default": "Warning"
        ///      }
        ///    },
        ///    "LogLevel": {
        ///      "Default": "Debug"
        ///    }
        /// },
        /// 
        /// Can be expressed much more concisely using KDL as
        /// 
        ///  Logging IncludeScopes=false {
        ///    Debug {
        ///      LogLevel Default="Information"
        ///    }
        ///    Console {
        ///      LogLevel Default="Warning" {
        ///        Microsoft.AspNetCore.Mvc.Razor.Internal "Warning"
        ///        Microsoft.AspNetCore.Mvc.Razor "Error"
        ///      }
        ///    }
        ///    LogLevel Default="Debug"
        ///  }
        /// </summary>
        /// <param name="builder">Configuration builder</param>
        /// <param name="path">Path to the KDL file</param>
        /// <param name="optional">Whether or not to fail if the file is missing</param>
        /// <param name="reloadOnChange">Whether or not to monitor for filesystem changes and reload the configuration if the file is altered</param>
        /// <returns>builder</returns>
        public static IConfigurationBuilder AddKdlFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
            => AddKdlFile(builder, null, path, optional, reloadOnChange);

        public static IConfigurationBuilder AddKdlFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional, bool reloadOnChange)
        {
            return builder.AddKdlFile(s => {
                s.FileProvider = provider;
                s.Path = path;
                s.Optional = optional;
                s.ReloadOnChange = reloadOnChange;
                s.ResolveFileProvider();
            });
        }

        public static IConfigurationBuilder AddKdlFile(this IConfigurationBuilder builder, Action<KdlConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}
