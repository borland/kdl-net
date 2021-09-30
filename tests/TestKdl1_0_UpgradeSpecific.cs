using KdlDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static KdlDotNetTests.ByteArrayExtensions;

#nullable enable

namespace KdlDotNetTests
{
    // When moving from the pre-1.0 to 1.0 KDL spec, a bunch of "spec" test cases broke.
    // this replicates some of them as in-process unit tests rather than loading external files to make it easier to debug/troubleshoot
    // the upgrade process
    [TestClass]
    public class TestKdl1_0_UpgradeSpecific
    {
        static readonly PrintConfig PrintConfig = new PrintConfig(); // TODO escapeLineSpace and radix

        [TestMethod]
        public void ArgFalseType()
        {
            var input = HexStringToByteArray("6E6F64652028747970652966616C73650A");

            var output = HexStringToByteArray("6E6F64652028747970652966616C73650A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

    }
}
