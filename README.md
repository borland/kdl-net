This is a C# implementation of a parser for the [KDL Document Language](https://github.com/kdl-org/kdl).

It is semi-literally ported from the Java KDL parser implementation [kdl4j](https://github.com/hkolbeck/kdl4j) by [Hannah Kolbeck](https://github.com/hkolbeck).
Many thanks for the original implementation.

## License

kdl4j is licensed under CC-BY-SA 4.0, and so this must also be. I like it.

## Status

Does not yet work.

Most things are ported straight across from Java, and the code compiles, but most unit tests are still not ported, and they all fail.

Functionality should be the same as Java but the primary exception is KDLNumber which at the moment only works with int32's and nothing else

