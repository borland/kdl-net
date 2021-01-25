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

The scope of the library is small so you could probably use it safely in a constrained environment, and it has what I would consider very good unit test coverage.
I would be happy to use it in production in a constrained environment, such as loading configuration files, etc. It hasn't had any proper performance benchmarking
or profiling, so it is probably not suitable for use in a hot performance-critical codepath.

I have successfully used this library to configure and boot up an ASP.NET Core application, using KDL in place of what would usually be a JSON file. KDL fits very nicely in this kind of environment.

Refer: [Screenshot](https://twitter.com/borland/status/1349596439840661505?s=20)
and  
[asp.net configuration helper code](https://gist.github.com/borland/3a8a0a8a56b3a4ef315e1f83f5ab4073)

## Usage

Create a new instance of `KDLParser` and call the `Parse` method, either on a `Stream` if you have one from a file/network, or from a `string`.  
You will get back a `KDLDocument`; you can then iterate the document `Nodes`, and for each `KDLNode`, inspect its `Args` and `Props`.

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

### PULL REQUESTS APPRECIATED 
