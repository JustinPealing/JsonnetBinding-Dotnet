using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    [TestClass]
    public class EvaluateFileTest : JsonnetTestBase
    {
        protected override string Filename { get; } = Path.GetTempFileName();

        protected override string Evaluate(
            string snippet,
            int maxStack,
            int gcMinObjects,
            IDictionary<string, string> extVars,
            IDictionary<string, string> extCodes,
            IDictionary<string, string> tlaVars,
            IDictionary<string, string> tlaCodes,
            int maxTrace,
            ImportCallback importCallback)
        {
            File.WriteAllText(Filename, snippet);
            return Jsonnet.EvaluateFile(
                Filename,
                maxStack,
                gcMinObjects,
                extVars,
                extCodes,
                tlaVars,
                tlaCodes,
                maxTrace,
                importCallback);
        }
    }
}