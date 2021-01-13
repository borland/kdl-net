using kdl_net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace kdl_net_tests
{
    [TestClass]
    public class TestParser
    {
        static readonly KDLParser parser = new KDLParser();
        static KDLDocument doc() => KDLDocument.Empty;
        static KDLDocument doc(params KDLNode[] nodes) => new KDLDocument(nodes);

        static List<IKDLValue> list(params IKDLValue[] values) => new List<IKDLValue>(values);
        static List<object> list(params object[] values) => new List<object>(values);

        static Dictionary<string, object> dict(string key, object value) => new Dictionary<string, object>(1) { [key] = value };

        static KDLNode node(string ident) => new KDLNode(ident);
        static KDLNode node(string ident, params KDLNode[] nodes) => new KDLNode(ident, child: doc(nodes));
        static KDLNode node(string ident, Dictionary<string, object> props)
            => new KDLNode(ident, props: props.ToDictionary(
                keySelector: (kv) => kv.Key,
                elementSelector: (kv) => KDLValue.From(kv.Value)));

        static KDLNode node(string ident, List<object> args, params KDLNode[] nodes)
            => new KDLNode(ident, args: args.Select(a => KDLValue.From(a)).ToList(), child: doc(nodes));

        [TestMethod]
        public void ParseEmptyString()
        {
            Assert.AreEqual(doc(), parser.Parse(""));
            Assert.AreEqual(doc(), parser.Parse(" "));
            Assert.AreEqual(doc(), parser.Parse("\n"));
        }

        [TestMethod]
        public void Nodes()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node\n"));
            Assert.AreEqual(doc(node("node")), parser.Parse("\nnode\n"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\nnode2"));
        }

        [TestMethod]
        public void node()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node;"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node 1"));
            Assert.AreEqual(doc(node("node", list(1, 2, "3", true, false, KDLNull.Instance))), parser.Parse("node 1 2 \"3\" true false null"));
            Assert.AreEqual(doc(node("node", node("node2"))), parser.Parse("node {\n    node2\n}"));
        }

        [TestMethod]
        public void slashDashComment()
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
        public void argSlashdashComment()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node /-1"));
            Assert.AreEqual(doc(node("node", list(2))), parser.Parse("node /-1 2"));
            Assert.AreEqual(doc(node("node", list(1, 3))), parser.Parse("node 1 /- 2 3"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /--1"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /- -1"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node \\\n/- -1"));
        }

        [TestMethod]
        public void prop_slashdash_comment()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse("node /-key=1"));
            Assert.AreEqual(doc(node("node")), parser.Parse("node /- key=1"));
            Assert.AreEqual(doc(node("node", dict("key", 1))), parser.Parse("node key=1 /-key2=2"));
        }

        [TestMethod]
        public void childrenSlashdashComment()
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

            //    assertThat(()->parser.parse("node \"\\i\""), throwsException(KDLParseException.class));
            //assertThat(() -> parser.parse("node \"\\u{c0ffee}\""), throwsException(KDLParseException.class));
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
        //        assertThat(parser.parse("node 123_456_789.0"),
        //                equalTo(doc(node("node", list(new BigDecimal("123456789.0"))))));

        //        assertThat(()->parser.parse("node 123_456_789.0_"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node ?1.0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node _1.0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node .0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1.0E100E10"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1.0E1.10"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1.0.0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1.0.0E7"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1.E7"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1._0"), throwsException(KDLParseException.class));
        //        assertThat(() -> parser.parse("node 1."), throwsException(KDLParseException.class));
        //    }

        //[TestMethod]
        //    public void integer()
        //{
        //    Assert.AreEqual(doc(node("node", list(0))), parser.Parse("node 0"));
        //    Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node 0123456789"));
        //    Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node 0123_456_789"));
        //    Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node 0123_456_789_"));
        //    Assert.AreEqual(doc(node("node", list(123456789))), parser.Parse("node +0123456789"));
        //    Assert.AreEqual(doc(node("node", list(-123456789))), parser.Parse("node -0123456789"));

        //    assertThat(()->parser.parse("node ?0123456789"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node _0123456789"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node a"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node --"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0x"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0x_1"), throwsException(KDLParseException.class));
        //    }

        //    [TestMethod]
        //    public void hexadecimal()
        //{
        //    KDLNumber kdlNumber = new KDLNumber(new BigDecimal(new BigInteger("0123456789abcdef", 16)), 16);

        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0x0123456789abcdef"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0x01234567_89abcdef"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0x01234567_89abcdef_"));

        //    assertThat(()->parser.parse("node 0x_123"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0xg"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0xx"), throwsException(KDLParseException.class));
        //    }

        //    [TestMethod]
        //    public void octal()
        //{
        //    KDLNumber kdlNumber = new KDLNumber(new BigDecimal(01234567), 8);

        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0o01234567"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0o0123_4567"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0o01234567_"));

        //    assertThat(()->parser.parse("node 0o_123"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0o8"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0oo"), throwsException(KDLParseException.class));
        //    }

        //    [TestMethod]
        //    public void binary()
        //{
        //    KDLNumber kdlNumber = new KDLNumber(new BigDecimal(6), 2);

        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b0110"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b01_10"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b01___10"));
        //    Assert.AreEqual(doc(node("node", list(kdlNumber))), parser.Parse("node 0b0110_"));

        //    assertThat(()->parser.parse("node 0b_0110"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0b20"), throwsException(KDLParseException.class));
        //assertThat(()->parser.parse("node 0bb"), throwsException(KDLParseException.class));
        //    }

        [TestMethod]
        public void raw_string()
        {
            Assert.AreEqual(doc(node("node", list("foo"))), parser.Parse("node r\"foo\""));
            Assert.AreEqual(doc(node("node", list("foo\\nbar"))), parser.Parse("node r\"foo\\nbar\""));
            Assert.AreEqual(doc(node("node", list("foo"))), parser.Parse("node r#\"foo\"#"));
            Assert.AreEqual(doc(node("node", list("foo"))), parser.Parse("node r##\"foo\"##"));
            Assert.AreEqual(doc(node("node", list("\\nfoo\\r"))), parser.Parse("node r\"\\nfoo\\r\""));
            Assert.AreEqual(doc(node("node", list("hello\"world"))), parser.Parse("node r#\"hello\"world\"#"));

            //assertThat(()->parser.parse("node r##\"foo\"#"), throwsException(KDLParseException.class));
        }

        [TestMethod]
        public void boolean()
        {
            Assert.AreEqual(doc(node("node", list(true))), parser.Parse("node true"));
            Assert.AreEqual(doc(node("node", list(false))), parser.Parse("node false"));
        }

        [TestMethod]
        public void node_space()
        {
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node 1"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node\t1"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node\t \\\n 1"));
            Assert.AreEqual(doc(node("node", list(1))), parser.Parse("node\t \\ // hello\n 1"));
        }

        [TestMethod]
        public void single_line_comment()
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
        public void multi_line_comment()
        {
            Assert.AreEqual(doc(), parser.Parse("/*hello*/"));
            Assert.AreEqual(doc(), parser.Parse("/*hello*/\n"));
            Assert.AreEqual(doc(), parser.Parse("/*\nhello\r\n*/"));
            Assert.AreEqual(doc(), parser.Parse("/*\nhello** /\n*/"));
            Assert.AreEqual(doc(), parser.Parse("/**\nhello** /\n*/"));
            Assert.AreEqual(doc(node("world")), parser.Parse("/*hello*/world"));
        }

        [TestMethod]
        public void escline()
        {
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\\nfoo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\\n    foo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\    \t \nfoo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\ // test \nfoo"));
            Assert.AreEqual(doc(node("foo")), parser.Parse("\\ // test \n    foo"));
        }

        [TestMethod]
        public void whitespace()
        {
            Assert.AreEqual(doc(node("node")), parser.Parse(" node"));
            Assert.AreEqual(doc(node("node")), parser.Parse("\tnode"));
            Assert.AreEqual(doc(node("etc")), parser.Parse("/* \nfoo\r\n */ etc"));
        }

        [TestMethod]
        public void newline()
        {
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\nnode2"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\rnode2"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\r\nnode2"));
            Assert.AreEqual(doc(node("node1"), node("node2")), parser.Parse("node1\n\nnode2"));
        }
    }
}
