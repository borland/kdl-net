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

Alpha quality, but the scope of the library is small so you could probably use it safely in a constrained environment.
Most of the code has been properly ported from Java, along with the key unit tests.
The only missing piece is the parsing and handling of non-integer numbers, and the unit tests for more detailed internal parts of the library.
The KDL grammar supports hex, octal, float/decimal and scientific notation in addition to plain integers which makes this quite tricky to handle correctly, making it tricky to port across from Java.

I have successfully used this library to configure and boot up an ASP.NET Core application, using KDL in place of what would usually be a JSON file.

Refer: https://twitter.com/borland/status/1349596439840661505?s=20  
and  
https://gist.github.com/borland/3a8a0a8a56b3a4ef315e1f83f5ab4073


### PULL REQUESTS APPRECIATED 
