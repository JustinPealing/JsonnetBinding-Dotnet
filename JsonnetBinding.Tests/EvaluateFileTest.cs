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
                importCallback,
                nativeCallbacks);
        }
    }
}