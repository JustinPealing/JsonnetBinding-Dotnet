using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    [TestClass]
    public class EvaluateSnippetTest : JsonnetTestBase
    {
        protected override string Filename => "test.jsonnet";

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
            return Jsonnet.EvaluateSnippet(
                Filename,
                snippet,
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