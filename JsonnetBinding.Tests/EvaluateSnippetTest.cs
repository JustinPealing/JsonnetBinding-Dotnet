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
            uint? maxStack,
            uint? gcMinObjects,
            IDictionary<string, string> extVars,
            IDictionary<string, string> extCodes,
            IDictionary<string, string> tlaVars,
            IDictionary<string, string> tlaCodes,
            uint? maxTrace,
            ImportCallback importCallback,
            IDictionary<string, NativeCallback> nativeCallbacks)
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
                importCallback,
                nativeCallbacks);
        }
    }
}