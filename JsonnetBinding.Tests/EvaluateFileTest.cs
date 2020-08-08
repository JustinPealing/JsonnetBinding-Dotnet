using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    [TestClass]
    public class EvaluateFileTest : JsonnetTestBase
    {
        protected override string Filename { get; } = Path.GetTempFileName();

        protected override string Evaluate(string snippet)
        {
            File.WriteAllText(Filename, snippet);
            return Vm.EvaluateFile(Filename);
        }
    }
}