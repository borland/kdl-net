using KdlDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

#nullable enable

namespace KdlDotNetTests
{
    [TestClass]
    public class TestRoundTrip
    {
        static readonly PrintConfig PrintConfig = new PrintConfig(escapeLinespace: true, respectRadix: false);

        [DataTestMethod]
        [DynamicData(nameof(GetData), DynamicDataSourceType.Method)]
        public void RoundTripTest(string name, string inputPath, string expectedOutputPath)
        {
            var inputText = File.ReadAllText(inputPath);
            var outputText = File.ReadAllText(expectedOutputPath);

            var parser = new KDLParser();

            var doc = parser.Parse(inputText);
            var generatedOutput = doc.ToKDLPretty(PrintConfig);

            if(outputText != generatedOutput)
            {
                Console.WriteLine("Failure: input:\n{0}\n{1}\n expected:\n{2}\n{3}\nactual:\n{4}\n{5}",
                    inputText,
                    Encoding.UTF8.GetBytes(inputText).ToHexString(),
                    outputText,
                    Encoding.UTF8.GetBytes(outputText).ToHexString(),
                    generatedOutput,
                    Encoding.UTF8.GetBytes(generatedOutput).ToHexString());

                // this is going to fail
                Assert.AreEqual(outputText, generatedOutput);
            }
        }

        public static IEnumerable<object[]> GetData()
        {
            var inputDir = "../../../test_cases/test_cases/input";
            var expectedOutputDir = "../../../test_cases/test_cases/expected_kdl";

            foreach (var input in Directory.GetFileSystemEntries(inputDir))
            {
                var name = Path.GetFileName(input);
                var fullInputPath = Path.GetFullPath(input);
                var fullOutputPath = Path.Combine(expectedOutputDir, name);

                if(name == "sci_notation_large.kdl" || name == "sci_notation_small.kdl")
                {
                    Console.WriteLine("Skipping extremely large or small scientific notation test. KDL.NET does not support these edge cases at the moment");
                    continue;
                }

                if (!File.Exists(fullOutputPath))
                {
                    Console.WriteLine($"{name} file could not find counterpart in expected_kdl folder!");
                    continue;
                }

                yield return new object[] { name, fullInputPath, fullOutputPath };
            }
        }
    }
}