﻿using KdlDotNet;
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
        static readonly PrintConfig PrintConfig = new PrintConfig(escapeLinespace: true, respectRadix: false);

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

        [TestMethod]
        public void HexInt()
        {
            var input = HexStringToByteArray("6E6F646520307861626364656631323334353637383930");

            var output = HexStringToByteArray("6E6F64652031323337393831333831323137373839333532300A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

        [TestMethod]
        public void NegativeFloat()
        {
            var input = HexStringToByteArray("6E6F646520312E30652D3130");

            var output = HexStringToByteArray("6E6F646520312E30452D31300A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

        [TestMethod]
        public void NumericProp()
        {
            var input = HexStringToByteArray("6E6F64652070726F703D31302E30");

            var output = HexStringToByteArray("6E6F64652070726F703D31302E300A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

        [TestMethod]
        public void Octal()
        {
            var input = HexStringToByteArray("6E6F646520306F3736353433323130");

            var output = HexStringToByteArray("6E6F64652031363433343832340A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

        [TestMethod]
        public void PropFloatType()
        {
            var input = HexStringToByteArray("6E6F6465206B65793D287479706529322E354531300A");

            var output = HexStringToByteArray("6E6F6465206B65793D287479706529322E35452B31300A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

        [TestMethod]
        public void NoDecimalExponent()
        {
            var input = HexStringToByteArray("6E6F64652031653130");

            var output = HexStringToByteArray("6E6F64652031452B31300A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

        [TestMethod]
        public void AllEscapes()
        {
            var input = HexStringToByteArray("6E6F646520225C225C5C5C2F5C625C665C6E5C725C74220A");

            var output = HexStringToByteArray("6E6F646520225C225C5C2F5C625C665C6E5C725C74220A");

            var parser = new KDLParser();

            var doc = parser.Parse(new MemoryStream(input));
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            Assert.AreEqual(Encoding.UTF8.GetString(output), generatedOutput);
        }

    }
}
