using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    /// <summary>
    /// Test cases for both <see cref="Jsonnet.EvaluateSnippet"/> and <see cref="Jsonnet.EvaluateFile"/>. Most of the
    /// test cases for these two methods are the same, however it is necessary to test them each indepdendently, so
    /// this base class defines the test cases using the abstract <see cref="Evaluate"/> method, which is implemented
    /// twice, once in <see cref="EvaluateFileTest"/>, and again in <see cref="EvaluateSnippetTest"/>.
    /// </summary>
    public abstract class JsonnetTestBase
    {
        protected abstract string Filename { get; }
        protected abstract string Evaluate(
            string snippet, 
            int maxStack = -1,
            int gcMinObjects = -1,
            IDictionary<string, string> extVars = null,
            IDictionary<string, string> extCodes = null,
            IDictionary<string, string> tlaVars = null,
            IDictionary<string, string> tlaCodes = null,
            int maxTrace = -1,
            ImportCallback importCallback = null);
        
        /// <summary>
        /// Test evaluating a basic snippet with all optional arguments left with their default values.
        /// </summary>
        [TestMethod]
        public void EvaluateWithDefaults()
        {
            var result = Evaluate("{ x: 1 , y: self.x + 1 } { x: 10 }");

            Assert.AreEqual(@"{
   ""x"": 10,
   ""y"": 11
}
", result);
        }
        
        /// <summary>
        /// If there is an error in the supplied jsonnet, a <see cref="JsonnetException"/> is thrown. 
        /// </summary>
        [TestMethod]
        public void Error()
        {
            var ex = Assert.ThrowsException<JsonnetException>(() =>
                Evaluate("{ x: 1 , y: self.x / 0 } { x: 10 }"));
            
            Assert.AreEqual(@$"RUNTIME ERROR: division by zero.
	{Filename}:1:13-23	object <anonymous>
	During manifestation	
", ex.Message);
        }

        /// <summary>
        /// Check that the maxStack parameter is passed to the Jsonnet VM correctly by running a snippet that
        /// intentionally exceeds the supplied max stack size.
        /// </summary>
        [TestMethod]
        public void MaxStack()
        {
            var snippet = @"
{
    a: { x: 0 },
    b: self.a { x +: 1 },
    c: self.b { x +: 1 } ,
    d: self.c { x +: 1 } 
}";

            var ex = Assert.ThrowsException<JsonnetException>(() => Evaluate(snippet, maxStack: 2));
            
            Assert.AreEqual($@"RUNTIME ERROR: max stack frames exceeded.
	{Filename}:4:15-25	object <anonymous>
	{Filename}:5:15-25	object <anonymous>
	{Filename}:6:15-25	object <anonymous>
	{Filename}:6:8-25	object <anonymous>
	During manifestation	
",
                ex.Message);
        }
    }
}