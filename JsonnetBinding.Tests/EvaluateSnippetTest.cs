using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    [TestClass]
    public class EvaluateSnippetTest : JsonnetTestBase
    {
        protected override string Filename => "test.jsonnet";

        protected override string Evaluate(string snippet)
        {
            return Vm.EvaluateSnippet(Filename, snippet);
        }
    }
}