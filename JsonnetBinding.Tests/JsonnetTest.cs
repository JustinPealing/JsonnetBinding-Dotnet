using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Jsonnet"/>.
    /// </summary>
    [TestClass]
    public class JsonnetTest
    {
        /// <summary>
        /// The <see cref="Jsonnet.EvaluateFile"/> method is used to evaluate a Jsonnet file.
        /// </summary>
        [TestMethod]
        public void EvaluateFile()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "{ x: 1 , y: self.x + 1 } { x: 10 }");

            var result = Jsonnet.EvaluateFile(file);

            Assert.AreEqual(@"{
   ""x"": 10,
   ""y"": 11
}
", result);
        }

        /// <summary>
        /// The <see cref="Jsonnet.EvaluateSnippet"/> method is used to evaluate a Jsonnet snippet in-memory.
        /// </summary>
        [TestMethod]
        public void EvaluateSnippet()
        {
            var result = Jsonnet.EvaluateSnippet(
                "test.jsonnet",
                "{ x: 1 , y: self.x + 1 } { x: 10 }"
            );

            Assert.AreEqual(@"{
   ""x"": 10,
   ""y"": 11
}
", result);
        }
    }
}