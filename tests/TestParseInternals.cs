using KdlDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        [DataRow("-1", "-1")]//Yes, really. Should it be is another question
        [DataRow("0hno", null)]
        [DataRow("=no", null)]
        //[DataRow("", null)] // TODO: This fails in C# but passes in java
        [DataTestMethod]
        public void TestParseBareIdentifier(string input, string? expectedResult)
        {
            var context = TestUtil.StrToContext(input);

            try
            {
                string val = TestUtil.Parser.ParseBareIdentifier(context);
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
