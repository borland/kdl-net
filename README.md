## KdlDotNet

This is a C# implementation of a parser for the [KDL Document Language](https://github.com/kdl-org/kdl).

It is semi-literally ported from the Java KDL parser implementation [kdl4j](https://github.com/hkolbeck/kdl4j) by [Hannah Kolbeck](https://github.com/hkolbeck).
Many thanks for the original implementation.

## Platform

The library is built against .NET Standard 2.0, which means it should work on the .NET Desktop framework 4.7.2 or later, and .NET Core 2.0 and later. The unit tests run against both .NET 4.7.2 and .NET Core 3.1 and all passs.

## Installation

Install the [KdlDotNet Nuget package](https://www.nuget.org/packages/KdlDotNet), or you can build the source yourself.

## License

KDL-net is licensed under the Creative Commons Attribution 4.0 License.

## Status

The scope of the library is small and it has what I would consider excellent unit test coverage.

I would be happy to use it in production in a constrained environment, such as loading configuration files, etc. It hasn't had any proper performance benchmarking
or profiling, so it is probably not suitable for use in a performance-critical codepath such as a tight loop.

One Caveat: The KDL language spec itself is not final and may still yet change so I can't in all honesty say this is a production library. In my personal view, the spec looks pretty solid, and if it does change, I wouldn't expect it to do so in a major breaking way; rather I'd expect simple clarification of edge cases and bug-fixes. Hopefully nothing dramatic.

## Why KDL?

I have successfully used this library to configure and boot up an ASP.NET Core application, using KDL in place of what would usually be a JSON file. KDL fits very nicely in this kind of environment. I've created an extension library Nuget Package called [KdlDotNet.Extensions.Configuration](https://www.nuget.org/packages/KdlDotNet.Extensions.Configuration/) to help with this.

Look at the difference: Here is a snippet from a configuration file, first using the traditional JSON file syntax:

```javascript
"Logging": {
    "IncludeScopes": false,
    "Debug": {
        "LogLevel": {
            "Default": "Information"
        }
    },
    "Console": {
        "LogLevel": {
            "Microsoft.AspNetCore.Mvc.Razor.Internal": "Warning",
            "Microsoft.AspNetCore.Mvc.Razor": "Error",
            "Default": "Warning"
        }
    },
    "LogLevel": {
        "Default": "Debug"
    }
},
```

Now look at the same thing in KDL using **KdlDotNet.Extensions.Configuration**.  
It uses the flexibility of KDL's nodes and attributes to make it much shorter, it has less syntactical noise, and is less error prone. You can't screw up the trailing commas and so forth. KDL also has much improved support for comments which is nice.

```java
Logging IncludeScopes=false {
    Debug {
        LogLevel Default="Information"
    }
    Console {
        LogLevel Default="Warning" {
            Microsoft.AspNetCore.Mvc.Razor.Internal "Warning"
            Microsoft.AspNetCore.Mvc.Razor "Error"
        }
    }
    LogLevel Default="Debug"
}
```

## Usage

### ASP.NET Core configuration

To use it for your asp.net core projects, simply add a single reference to the nuget package `KdlDotNet.Extensions.Configuration` - eg

```xml
<PackageReference Include="KdlDotNet.Extensions.Configuration" Version="5.0.0" />
```

Once you have done so, find your `ConfigurationBuilder` (usually in Program.cs), and change `.AddJsonFile` to `.AddKdlFile`, then reformat your JSON into KDL and enjoy

### Other non-specific usage

If you want to do something other than configure asp.net core applications, you'll need to interact with the parser directly.
Add a nuget package reference to the [KdlDotNet](https://www.nuget.org/packages/KdlDotNet/) package

```xml
<PackageReference Include="KdlDotNet" Version="1.0.0" />
```

Then create a new instance of `KdlDotNet.KDLParser` and call the `Parse` method, either on a `Stream` if you have one from a file/network, or from a `string`.  
You will get back a `KDLDocument`.
You can then iterate the document `Nodes`, and for each `KDLNode`, inspect its `Args` and `Props`.

For an example, see [KdlConfigurationFileParser.cs from KdlDotNet.Extensions.Configuration](https://github.com/borland/kdl-net/blob/main/KdlDotNet.Extensions.Configuration/src/KdlConfigurationFileParser.cs)

Alternatively, you can manually create a `KDLDocument` and the various child nodes, and then call `ToKDLPretty` to produce a KDL string, or write to a stream.

## Differences from kdl4j

This port is faithful to the original kdl4j in all areas apart from handling of numbers and the "Searcher" type.
All of the code has been properly ported from Java, along with all key unit tests

#### Searcher
I don't think this is as neccessary in C#, given that it has LINQ for traversing object graphs. If you disagree and would like it, please file an issue, preferably with an attached pull request :-)

#### Numeric Handling
kdl4j uses the java BigDecimal type as the internal representation for all numbers. 
Presumably this was done for ease of implementation as it lets kdl4j have a simpler code path for handling numbers, 
however BigDecimal is a large and complex thing, weighing in at a minimum of 32 bytes per instance (over and above the surrounding KDLNumber object).

kdl-net in contrast uses an 8 byte storage structure, which is a union containing either an int32, int64, or double (8 byte floating point) value.
This means kdl-net can't handle numbers that exceed the size of an int64 or double, however it should require a lot less memory and be faster.
Given that the primary use-case for KDL at this stage seems to be human-readable configuration, this seems like a better tradeoff.

**PULL REQUESTS APPRECIATED**
