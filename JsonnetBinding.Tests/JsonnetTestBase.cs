using System;
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
        protected readonly JsonnetVm Vm = new JsonnetVm();
        protected abstract string Filename { get; }
        protected abstract string Evaluate(string snippet);
        
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
            Vm.MaxStack = 2;
            
            var snippet = @"
{
    a: { x: 0 },
    b: self.a { x +: 1 },
    c: self.b { x +: 1 } ,
    d: self.c { x +: 1 } 
}";

            var ex = Assert.ThrowsException<JsonnetException>(() => Evaluate(snippet));
            
            Assert.AreEqual($@"RUNTIME ERROR: max stack frames exceeded.
	{Filename}:4:15-25	object <anonymous>
	{Filename}:5:15-25	object <anonymous>
	{Filename}:6:15-25	object <anonymous>
	{Filename}:6:8-25	object <anonymous>
	During manifestation	
",
                ex.Message);
        }

        [TestMethod]
        public void NativeCallbacks()
        {
            var snippet = @"
std.assertEqual(({ x: 1, y: self.x } { x: 2 }).y, 2) &&
std.assertEqual(std.native('concat')('foo', 'bar'), 'foobar') &&
std.assertEqual(std.native('return_types')(), {a: [1, 2, 3, null, []], b: 1, c: true, d: null, e: {x: 1, y: 2, z: ['foo']}}) &&
true
";

            Vm.AddNativeCallback("concat", new[] {"foo", "bar"}, (object[] args, out bool success) =>
            {
                success = true;
                return args[0].ToString() + args[1];
            });
            Vm.AddNativeCallback("return_types", new string[0], (object[] args, out bool success) =>
            {
                success = true;
                return new Dictionary<string, object>
                {
                    {"a", new object[] {1, 2, 3, null, new object[] { }}},
                    {"b", 1.0},
                    {"c", true},
                    {"d", null},
                    {
                        "e", new Dictionary<string, object>
                        {
                            {"x", 1},
                            {"y", 2},
                            {"z", new[] {"foo"}},
                        }
                    },
                };
            });

            var result = Evaluate(snippet);
            
            Assert.AreEqual("true\n", result);
        }
    }
}