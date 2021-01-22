using KdlDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace KdlDotNetTests
{
    [TestClass]
    public class TestParseInternals
    {
        [DataRow("", KDLParser.WhitespaceResult.NoWhitespace, "")]
        [DataRow("\\\r\na", KDLParser.WhitespaceResult.NodeSpace, "a")]
        [DataRow(" \\\r\n \\\n \\\ra", KDLParser.WhitespaceResult.NodeSpace, "a")]
        [DataRow(" a ", KDLParser.WhitespaceResult.NodeSpace, "a ")]
        [DataRow("a", KDLParser.WhitespaceResult.NoWhitespace, "a")]
        [DataRow("\\\na", KDLParser.WhitespaceResult.NodeSpace, "a")]
        [DataRow("\\\ra", KDLParser.WhitespaceResult.NodeSpace, "a")]
        [DataRow("\t", KDLParser.WhitespaceResult.NodeSpace, "")]
        [DataRow("/* comment */a", KDLParser.WhitespaceResult.NodeSpace, "a")]
        [DataRow("\t a", KDLParser.WhitespaceResult.NodeSpace, "a")]
        [DataRow("/- /- a", null, " a")]
        [DataTestMethod]
        public void TestConsumeWhitespaceAndBlockComments(string input, object expectedResultObj, string expectedRemainder)
        {
            var expectedResult = expectedResultObj as KDLParser.WhitespaceResult?; // WhitespaceResult is an internal type so it can't be a parameter
            var context = TestUtil.StrToContext(input);

            try
            {
                var whitespaceResult = TestUtil.Parser.ConsumeWhitespaceAndBlockComments(context);
                Assert.AreEqual(expectedResult, whitespaceResult);
            }
            catch (KDLParseException e)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }

            var rem = TestUtil.ReadRemainder(context);
            Assert.AreEqual(expectedRemainder, rem);
        }


        [DataRow("", KDLParser.WhitespaceResult.EndNode, "")]
        [DataRow("\n", KDLParser.WhitespaceResult.EndNode, "")]
        [DataRow( "   \n/- a", KDLParser.WhitespaceResult.SkipNext, "a" )]
        [DataRow( "\\\r\na", KDLParser.WhitespaceResult.NodeSpace, "a" )]
        [DataRow( " \\\r\n \\\n \\\ra", KDLParser.WhitespaceResult.NodeSpace, "a" )]
        [DataRow( " a ", KDLParser.WhitespaceResult.NodeSpace, "a " )]
        [DataRow( "a", KDLParser.WhitespaceResult.NoWhitespace, "a" )]
        [DataRow( "\\\na", KDLParser.WhitespaceResult.NodeSpace, "a" )]
        [DataRow( "\\\ra", KDLParser.WhitespaceResult.NodeSpace, "a" )]
        [DataRow( "\t", KDLParser.WhitespaceResult.EndNode, "" )]
        [DataRow( "/* comment */a", KDLParser.WhitespaceResult.NodeSpace, "a" )]
        [DataRow( "\t a", KDLParser.WhitespaceResult.NodeSpace, "a" )]
        [DataRow( "\\ a", null, "a" )]
        [DataRow( "/- ", null, "" )]
        [DataRow( "/- \n", null, "\n")]
        [DataTestMethod]
        public void TestConsumeWhitespaceAndLinespace(string input, object expectedResultObj, string expectedRemainder)
        {
            var expectedResult = expectedResultObj as KDLParser.WhitespaceResult?; // WhitespaceResult is an internal type so it can't be a parameter
            var context = TestUtil.StrToContext(input);

            try
            {
                var whitespaceResult = TestUtil.Parser.ConsumeWhitespaceAndLinespace(context);
                Assert.AreEqual(expectedResult, whitespaceResult);
            }
            catch (KDLParseException e)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }

            var rem = TestUtil.ReadRemainder(context);
            Assert.AreEqual(expectedRemainder, rem);
        }

        [DataRow("n", '\n')]
        [DataRow("r", '\r')]
        [DataRow("t", '\t')]
        [DataRow("\\", '\\')]
        [DataRow("/", '/')]
        [DataRow("\"", '\"')]
        [DataRow("b", '\b')]
        [DataRow("f", '\f')]
        [DataRow("u{1}", '\u0001')]
        [DataRow("u{01}", '\u0001')]
        [DataRow("u{001}", '\u0001')]
        [DataRow("u{001}", '\u0001')]
        [DataRow("u{0001}", '\u0001')]
        [DataRow("u{00001}", '\u0001')]
        [DataRow("u{000001}", '\u0001')]
        [DataRow("u{10FFFF}", 0x10FFFF)]
        [DataRow("i", -2)]
        [DataRow("ux", -2)]
        [DataRow("u{x}", -2)]
        [DataRow("u{0001", -2)]
        [DataRow("u{AX}", -2)]
        [DataRow("u{}", -2)]
        [DataRow("u{0000001}", -2)]
        [DataRow("u{110000}", -2)]
        [DataTestMethod]
        public void TestGetEscaped(string input, int expectedResult)
        {
            var context = TestUtil.StrToContext(input);
            int initial = context.Read();

            try
            {
                int result = TestUtil.Parser.GetEscaped(initial, context);
                Assert.AreEqual(expectedResult, result);
            }
            catch (KDLParseException e)
            {
                if (expectedResult > 0)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("// stuff\n", KDLParser.SlashAction.EndNode, "\n")]
        [DataRow("// stuff \r\n", KDLParser.SlashAction.EndNode, "\r\n")]
        [DataRow("/- stuff", KDLParser.SlashAction.SkipNext, " stuff")]
        [DataRow("/* comment */", KDLParser.SlashAction.Nothing, "")]
        [DataRow("/* comment */", KDLParser.SlashAction.Nothing, "")]
        [DataRow("/**/", KDLParser.SlashAction.Nothing, "")]
        [DataRow("/*/**/*/", KDLParser.SlashAction.Nothing, "")]
        [DataRow("/*   /*  */*/", KDLParser.SlashAction.Nothing, "")]
        [DataRow("/* ", null, "")]
        [DataRow("/? ", null, "? ")]
        [DataTestMethod]
        public void TestGetSlashAction(string input, object expectedResultObj, string expectedRemainder)
        {
            var expectedResult = expectedResultObj as KDLParser.SlashAction?; // SlashAction is an internal type so it can't be a parameter
            var context = TestUtil.StrToContext(input);

            try
            {
                var action = TestUtil.Parser.GetSlashAction(context, false);
                var rem = TestUtil.ReadRemainder(context);

                Assert.AreEqual(expectedResult, action);
                Assert.AreEqual(expectedRemainder, rem);
            }
            catch (KDLParseException e)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("bare", "bare")]
        [DataRow("-10", "-10")]
        [DataRow("r", "r")]
        [DataRow("rrrr", "rrrr")]
        [DataRow("r\"raw\"", "raw")]
        [DataRow("#goals", "#goals")]
        [DataRow("=goals", null)]
        [DataTestMethod]
        public void TestParseArgOrProp(string input, string? expectedResult)
        {
            var context = TestUtil.StrToContext(input);

            try
            {
                string val = TestUtil.Parser.ParseIdentifier(context);
                Assert.AreEqual(expectedResult, val);
            }
            catch (KDLParseException e)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("r", "r")]
        [DataRow("bare", "bare")]
        [DataRow("ぁ", "ぁ")]
        [DataRow("-r", "-r")]
        [DataRow("-1", "-1")] //Yes, really. Should it be is another question
        [DataRow("0hno", null)]
        [DataRow("=no", null)]
        [DataRow("", null)]
        [DataTestMethod]
        public void TestParseBareIdentifier(string input, string? expectedResult)
        {
            var context = TestUtil.StrToContext(input);

            try
            {
                string val = TestUtil.Parser.ParseBareIdentifier(context);
                Assert.AreEqual(expectedResult, val);
            }
            catch (Exception e) when (e is KDLParseException || e is KDLInternalException)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("{}", "doc()")]
        [DataRow("{\n\n}", "doc()")]
        [DataRow("{\na\n}", "doc(a)")]
        [DataRow("{\n\na\n\nb\n}", "doc(a,b)")]
        [DataRow("{\na\nb\n}", "doc(a,b)")]
        [DataRow("", null)]
        [DataRow("{", null)]
        [DataRow("{\n", null)]
        [DataRow("{\na /-", null)]
        [DataRow("{\na\n/-", null)]
        [DataTestMethod]
        public void TestParseChild(string input, string? expectedResultPlaceholder)
        {
            var context = TestUtil.StrToContext(input);

            // C# can't have complex expressions in attributes, so we use a string literal as a proxy for DataRow
            var expectedResult = expectedResultPlaceholder switch {
                "doc()" => KDLDocument.Empty,
                "doc(a)" => new KDLDocument(new KDLNode("a")),
                "doc(a,b)" => new KDLDocument(new KDLNode("a"), new KDLNode("b")),
                _ => null
            };

            try
            {
                KDLDocument val = TestUtil.Parser.ParseChild(context);
                Assert.AreEqual(expectedResult, val);
            }
            catch (Exception e) when(e is KDLParseException || e is KDLInternalException)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("\"\"", "")]
        [DataRow("\"a\"", "a")]
        [DataRow("\"a\nb\"", "a\nb")]
        [DataRow("\"\\n\"", "\n")]
        [DataRow("\"\\u{0001}\"", "\u0001")]
        [DataRow("\"ぁ\"", "ぁ")]
        [DataRow("\"\\u{3041}\"", "ぁ")]
        [DataRow("\"", null)]
        [DataRow("", null)]
        [DataTestMethod]
        public void TestParseEscapedString(string input, string? expectedResult)
        {
            var context = TestUtil.StrToContext(input);

            try
            {
                string val = TestUtil.Parser.ParseEscapedString(context);
                Assert.AreEqual(expectedResult, val);
            }
            catch (Exception e) when (e is KDLParseException || e is KDLInternalException)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("r", "r")]
        [DataRow("bare", "bare")]
        [DataRow("ぁ", "ぁ")]
        [DataRow("-r", "-r")]
        [DataRow("-1", "-1")] //Yes, really. Should it be is another question
        [DataRow("0hno", null)]
        [DataRow("=no", null)]
        [DataRow("", null)]
        [DataTestMethod]
        public void TestParseIdentifier(string input, string? expectedResult)
        {
            var context = TestUtil.StrToContext(input);

            try
            {
                string val = TestUtil.Parser.ParseIdentifier(context);
                Assert.AreEqual(expectedResult, val);
            }
            catch (Exception e) when (e is KDLParseException || e is KDLInternalException)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }

        [DataRow("a", "node(a)")]
        [DataRow("a\n", "node(a)")]
        [DataRow("\"a\"", "node(a)")]
        [DataRow("r\"a\"", "node(a)")]
        [DataRow("r", "node(r)")]
        [DataRow("rrrr", "node(rrrr)")]
        [DataRow("a // stuff", "node(a)")]
        [DataRow("a \"arg\"", "node(a) arg")]
        [DataRow("a key=\"val\"", "node(a) key=val")]
        [DataRow("a key=true", "node(a) key=true")]
        [DataRow("a \"arg\" key=\"val\"", "node(a) key=val arg")]
        [DataRow("a r#\"arg\"\"# key=\"val\"", "node(a) key=val arg\"")]
        [DataRow("a true false null", "node(a) true false null")]
        [DataRow("a /- \"arg1\" \"arg2\"", "node(a) arg2")]
        [DataRow("a key=\"val\" key=\"val2\"", "node(a) key=val2")]
        [DataRow("a key=\"val\" /- key=\"val2\"", "node(a) key=val")]
        [DataRow("a {}", "node(a) {}")]
        //[DataRow("a {\nb\n}", KDLNode.builder().setIdentifier("a").setChild(KDLDocument.builder().addNode(KDLNode.builder().setIdentifier("b").build()).build()).build())]
        //[DataRow("a \"arg\" key=null \\\n{\nb\n}", KDLNode.builder().setIdentifier("a").addArg("arg").addNullProp("key").setChild(KDLDocument.builder().addNode(KDLNode.builder().setIdentifier("b").build()).build()).build())]
        [DataRow("a {\n\n}", "node(a) {}")]
        [DataRow("a{\n\n}", "node(a) {}")]
        [DataRow("a\"arg\"", null)]
        [DataRow("a=", null)]
        [DataRow("a /-", null)]
        [DataTestMethod]
        public void TestParseNode(string input, string? expectedResultPlaceholder)
        {
            var context = TestUtil.StrToContext(input);
            var expectedResult = expectedResultPlaceholder switch { // can't have complex data in a DataRow attribute in C#
                "node(a)" => new KDLNode("a"),
                "node(r)" => new KDLNode("r"),
                "node(rrrr)" => new KDLNode("rrrr"),
                "node(a) arg" => new KDLNode("a", args: new[] { new KDLString("arg") }),
                "node(a) key=val" => new KDLNode("a", props: new Dictionary<string, IKDLValue> { ["key"] = new KDLString("val") }),
                "node(a) key=true" => new KDLNode("a", props: new Dictionary<string, IKDLValue> { ["key"] = KDLBoolean.True }),
                "node(a) key=val arg" => new KDLNode("a", props: new Dictionary<string, IKDLValue> { ["key"] = new KDLString("val") }, args: new[] { new KDLString("arg") }),
                "node(a) key=val arg\"" => new KDLNode("a", props: new Dictionary<string, IKDLValue> { ["key"] = new KDLString("val") }, args: new[] { new KDLString("arg\"") }),
                "node(a) true false null" => new KDLNode("a", args: new IKDLValue [] { KDLBoolean.True, KDLBoolean.False, KDLNull.Instance }),
                "node(a) arg2" => new KDLNode("a", args: new[] { new KDLString("arg2") }),
                "node(a) key=val2" => new KDLNode("a", props: new Dictionary<string, IKDLValue>{ ["key"] = new KDLString("val2") }),
                "node(a) {}" => new KDLNode("a", child: KDLDocument.Empty),
                _ => null
            };

            try
            {
                KDLNode? val = TestUtil.Parser.ParseNode(context);
                Assert.AreEqual(expectedResult, val);
            }
            catch (KDLParseException e)
            {
                if (expectedResult != null)
                {
                    throw new KDLParseException("Expected no errors", e);
                }
            }
        }
    }
}
