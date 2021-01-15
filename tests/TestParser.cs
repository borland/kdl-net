using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using KdlDotNet;

#nullable enable

namespace KdlDotNetTests
{
    [TestClass]
    public class TestParser
    {
#pragma warning disable IDE0022, IDE1006, IDE0051
        static readonly KDLParser parser = new KDLParser();
        static KDLDocument doc() => KDLDocument.Empty;
        static KDLDocument doc(params KDLNode[] nodes) => new KDLDocument(nodes);

        static List<IKDLValue> list(params IKDLValue[] values) => new List<IKDLValue>(values);
        static List<object> list(params object[] values) => new List<object>(values);

        static Dictionary<string, object> dict(string key, object value) => new Dictionary<string, object>(1) { [key] = value };

        static KDLNode node(string ident) => new KDLNode(ident);
        static KDLNode node(string ident, params KDLNode[] nodes) => new KDLNode(ident, child: doc(nodes));

        static KDLNode node(string ident, Dictionary<string, object> props)
            => node(ident, args: new List<object>(0), props: props);

        static KDLNode node(string ident, List<object> args, Dictionary<string, object> props)
            => new KDLNode(
                ident, 
                args: args.Select(a => KDLValue.From(a)).ToList(),
                props: props.ToDictionary(
                    keySelector: (kv) => kv.Key,
                    elementSelector: (kv) => KDLValue.From(kv.Value)),
                child: null);

        static KDLNode node(string ident, List<object> args, params KDLNode[] nodes)
            => new KDLNode(ident, args: args.Select(a => KDLValue.From(a)).ToList(), child: nodes.Length > 0 ? doc(nodes) : null);

#pragma warning restore

        [TestMethod]
        public void ParseEmptyString()
        {
            Assert.AreEqual(doc(), parser.Parse(""));
            Assert.AreEqual(doc(), parser.Parse(" "));
            Assert.AreEqual(doc(), parser.Parse("\n"));
        }

        [TestMethod]
        public void ParseNodes()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node\n"));
            Assert.AreEqual(doc(node("node")), parser.Parse("\nnode\n"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\nnode2"));
        }

        [TestMethod]
        public void ParseNode()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node;"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node 1"));
            Assert.AreEqual(doc(node("node", list(1, 2, "3", true, false, KDLNull.Instance))), parser.Parse("node 1 2 \"3\" true false null"));
            Assert.AreEqual(doc(node("node", node("node2"))), parser.Parse("node {\n    node2\n}"));
        }

        [TestMethod]
        public void ParseSlashDashComment()
        {
            Assert.AreEqual(doc(), parser.Parse("/-node"));
            Assert.AreEqual(doc(), parser.Parse("/- node"));
            Assert.AreEqual(doc(), parser.Parse("/- node\n"));
            Assert.AreEqual(doc(), parser.Parse("/-node 1 2 3"));
            Assert.AreEqual(doc(), parser.Parse("/-node key=false"));
            Assert.AreEqual(doc(), parser.Parse("/-node{\nnode\n}"));
            Assert.AreEqual(doc(), parser.Parse("/-node 1 2 3 key=\"value\" \\\n{\nnode\n}"));
        }

        [TestMethod]
        public void ParseArgSlashdashComment()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node /-1"));
            Assert.AreEqual(doc(node("node", list(2))), parser.Parse("node /-1 2"));
            Assert.AreEqual(doc(node("node", list(1, 3))), parser.Parse("node 1 /- 2 3"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /--1"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /- -1"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node \\\n/- -1"));
        }

        [TestMethod]
        public void ParseProp_slashdash_comment()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node /-key=1"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /- key=1"));
            Assert.AreEqual(doc(node("node", dict("key", 1))), parser.Parse("node key=1 /-key2=2"));
        }

        [TestMethod]
        public void ParseChildrenSlashdashComment()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node /-{}"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /- {}"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /-{\nnode2\n}"));
        }

        [TestMethod]
        public void ParseString()
        {
            Assert.AreEqual(doc(node("node", list(""))), parser.Parse("node \"\""));
            Assert.AreEqual(doc(node("node", list("hello"))), parser.Parse("node \"hello\""));
            Assert.AreEqual(doc(node("node", list("hello\nworld"))), parser.Parse("node \"hello\\nworld\""));
            Assert.AreEqual(doc(node("node", list("\uD83D\uDC08"))), parser.Parse("node \"\\u{1F408}\""));
            Assert.AreEqual(
                doc(node("node", list("\"\\/\u0008\u000C\n\r\t"))),
                parser.Parse("node \"\\\"\\\\\\/\\b\\f\\n\\r\\t\""));
            Assert.AreEqual(doc(node("node", list("\u0010"))), parser.Parse("node \"\\u{10}\""));

            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node \"\\i\""));
            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node \"\\u{c0ffee}\""));
        }

        //    [TestMethod]
        //    public void float()
        //    {
        //        Assert.AreEqual(doc(node("node", list(1.0))), parser.Parse("node 1.0"));
        //        Assert.AreEqual(doc(node("node", list(0.0))), parser.Parse("node 0.0"));
        //        Assert.AreEqual(doc(node("node", list(-1.0))), parser.Parse("node -1.0"));
        //        Assert.AreEqual(doc(node("node", list(1.0))), parser.Parse("node +1.0"));
        //        Assert.AreEqual(doc(node("node", list(1.0e10))), parser.Parse("node 1.0e10"));
        //        Assert.AreEqual(doc(node("node", list(1.0e-10))), parser.Parse("node 1.0e-10"));
        //        assertThat(parser.Parse("node 123_456_789.0"),
        //                equalTo(doc(node("node", list(new BigDecimal("123456789.0"))))));

        //        Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 123_456_789.0_"));
        //        assertThat(() -> parser.Parse("node ?1.0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node _1.0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node .0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1.0E100E10"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1.0E1.10"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1.0.0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1.0.0E7"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1.E7"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1._0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.Parse("node 1."), throwsException(KDLParseException.class));
        //    }

        [TestMethod]
        public void ParseInteger()
        {
            Assert.AreEqual(doc(node("node", list(0))), parser.Parse("node 0"));
            Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node 0123456789"));
            Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node 0123_456_789"));
            Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node 0123_456_789_"));
            Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node +0123456789"));
            Assert.AreEqual(doc(node("node", list(-123456789))), parser.Parse("node -0123456789"));

            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node ?0123456789"));
            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node _0123456789"));
            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node a"));
            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node --"));
            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0x"));
            Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0x_1"));
        }

        //    [TestMethod]
        //    public void hexadecimal()
        //{
        //    KDLNumber kdlNumber = new KDLNumber(new BigDecimal(new BigInteger("0123456789abcdef", 16)), 16);

        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0x0123456789abcdef"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0x01234567_89abcdef"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0x01234567_89abcdef_"));

        //    Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0x_123"));
        //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0xg"));
        //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0xx"));
        //    }

        //    [TestMethod]
        //    public void octal()
        //{
        //    KDLNumber kdlNumber = new KDLNumber(new BigDecimal(01234567), 8);

        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0o01234567"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0o0123_4567"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0o01234567_"));

        //    Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0o_123"));
        //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0o8"));
        //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0oo"));
        //    }

        //    [TestMethod]
        //    public void binary()
        //{
        //    KDLNumber kdlNumber = new KDLNumber(new BigDecimal(6), 2);

        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b0110"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b01_10"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b01___10"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b0110_"));

        //    Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0b_0110"));
        //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0b20"));
        //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node 0bb"));
        //    }

        [TestMethod]
        public void ParseRaw_string()
        {
            Assert.AreEqual(doc(node("node", list("foo"))), parser.Parse("node r\"foo\""));
            Assert.AreEqual(doc(node("node", list("foo\\nbar"))), parser.Parse("node r\"foo\\nbar\""));
            Assert.AreEqual(doc(node("node", list("foo"))), parser.Parse("node r#\"foo\"#"));
            Assert.AreEqual(doc(node("node", list("foo"))), parser.Parse("node r##\"foo\"##"));
            Assert.AreEqual(doc(node("node", list("\\nfoo\\r"))), parser.Parse("node r\"\\nfoo\\r\""));
            Assert.AreEqual(doc(node("node", list("hello\"world"))), parser.Parse("node r#\"hello\"world\"#"));

            //Assert.ThrowsException<KDLParseException>(() => parser.Parse("node r##\"foo\"#"));
        }

        [TestMethod]
        public void ParseBoolean()
        {
            Assert.AreEqual(doc(node("node", list(true))), parser.Parse("node true"));
            Assert.AreEqual(doc(node("node", list(false))), parser.Parse("node false"));
        }

        [TestMethod]
        public void ParseNode_space()
        {
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node 1"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node\t1"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node\t \\\n 1"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node\t \\ // hello\n 1"));
        }

        [TestMethod]
        public void ParseSingle_line_comment()
        {
            Assert.AreEqual(doc(), parser.Parse("//hello"));
            Assert.AreEqual(doc(), parser.Parse("// \thello"));
            Assert.AreEqual(doc(), parser.Parse("//hello\n"));
            Assert.AreEqual(doc(), parser.Parse("//hello\r\n"));
            Assert.AreEqual(doc(), parser.Parse("//hello\n\r"));
            Assert.AreEqual(doc(node("world")), parser.Parse("//hello\rworld"));
            Assert.AreEqual(doc(node("world")), parser.Parse("//hello\nworld\r\n"));
        }

        [TestMethod]
        public void ParseMulti_line_comment()
        {
            Assert.AreEqual(doc(), parser.Parse("/*hello*/"));
            Assert.AreEqual(doc(), parser.Parse("/*hello*/\n"));
            Assert.AreEqual(doc(), parser.Parse("/*\nhello\r\n*/"));
            Assert.AreEqual(doc(), parser.Parse("/*\nhello** /\n*/"));
            Assert.AreEqual(doc(), parser.Parse("/**\nhello** /\n*/"));
            Assert.AreEqual(doc(node("world")), parser.Parse("/*hello*/world"));
        }

        [TestMethod]
        public void ParseEscline()
        {
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\\nfoo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\\n    foo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\    \t \nfoo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\ // test \nfoo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\ // test \n    foo"));
        }

        [TestMethod]
        public void ParseWhitespace()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse(" node"));
            Assert.AreEqual(doc(node("node")), parser.Parse("\tnode"));
            Assert.AreEqual(doc(node("etc")), parser.Parse("/* \nfoo\r\n */ etc"));
        }

        [TestMethod]
        public void ParseNewline()
        {
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\nnode2"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\rnode2"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\r\nnode2"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\n\nnode2"));
        }

        [TestMethod]
        public void ParseNestedChildNodes()
        {
            KDLDocument actual = parser.Parse(
                "content { \n" +
                        "    section \"First section\" {\n" +
                        "        paragraph \"This is the first paragraph\"\n" +
                        "        paragraph \"This is the second paragraph\"\n" +
                        "    }\n" +
                        "}"
            );

            KDLDocument expected = doc(
                    node("content",
                            node("section", list("First section"),
                                    node("paragraph", list("This is the first paragraph")),
                                    node("paragraph", list("This is the second paragraph"))
                            )
                    )
            );

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ParseSemicolon()
        {
            Assert.AreEqual(doc(node("node1"), node("node2"), node("node3")), parser.Parse("node1; node2; node3"));
            Assert.AreEqual(doc(node("node1", node("node2")), node("node3")), parser.Parse("node1 { node2; }; node3"));
        }

        [TestMethod]
        public void ParseMultiline_strings()
        {
            Assert.AreEqual(doc(node("string", list("my\nmultiline\nvalue"))), parser.Parse("string \"my\nmultiline\nvalue\""));
        }

        [TestMethod]
        public void ParseComments()
        {
            KDLDocument actual = parser.Parse(
                    "// C style\n" +

                            "/*\n" +
                            "C style multiline\n" +
                            "*/\n" +

                            "tag /*foo=true*/ bar=false\n" +

                            "/*/*\n" +
                            "hello\n" +
                            "*/*/"
            );

            KDLDocument expected = doc(
                    node("tag", dict("bar", false))
            );

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ParseMultiline_nodes()
        {
            KDLDocument actual = parser.Parse(
                    "title \\\n" +
                            "    \"Some title\"\n" +
                            "my-node 1 2 \\    // comments are ok after \\\n" +
                            "        3 4\n"
            );

            KDLDocument expected = doc(
                    node("title", list("Some title")),
                    node("my-node", list(1, 2, 3, 4))
            );

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ParseUtf8()
        {
            Assert.AreEqual(doc(node("smile", list("😁"))), parser.Parse("smile \"😁\""));

            Assert.AreEqual(
                doc(node("ノード", dict("お名前", "☜(ﾟヮﾟ☜)"))), 
                parser.Parse("ノード お名前=\"☜(ﾟヮﾟ☜)\""));
        }

        [TestMethod]
        public void ParseNode_names()
        {
            Assert.AreEqual(
                doc(node("!@#$@$%Q#$%~@!40", list("1.2.3"), dict("!!!!!", true))), 
                parser.Parse("\"!@#$@$%Q#$%~@!40\" \"1.2.3\" \"!!!!!\"=true"));

            Assert.AreEqual(
                doc(node("foo123~!@#$%^&*.:'|/?+", list("weeee"))), 
                parser.Parse("foo123~!@#$%^&*.:'|/?+ \"weeee\""));
        }
    }
}
