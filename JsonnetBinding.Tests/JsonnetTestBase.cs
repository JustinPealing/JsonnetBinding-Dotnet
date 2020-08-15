using System;
using System.Collections.Generic;
using System.IO;
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
        
        /// <summary>
        /// Returns the path to the file. In EvaluateSnippetTest this is a hard-coded value, but for EvaluateFileTest
        /// this is the path to some dynamically created temporary file.
        /// This properly is mostly used to check error messages.
        /// </summary>
        protected abstract string Filename { get; }
        
        /// <summary>
        /// Evaluates the given snippet. This will either invoke EvaluateSnippet, or write the snippet to a file and
        /// invoke EvaluateFile.
        /// </summary>
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
        public void ErrorEvaluatingThrowsException()
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
        
        /// <summary>
        /// Native callbacks allow the host application to extend jsonnet with extra functions implemented in C#. Its
        /// important that the callback cannot be garbage collected while in use.
        /// </summary>
        [TestMethod]
        public void NativeCallbackUsingDelegate()
        {
            Vm.AddNativeCallback("concat", new Func<string, string, object>((a, b) => a + b));
            Vm.AddNativeCallback("return_types", new Func<object>(() =>
                new Dictionary<string, object>
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
                }));

            var result = Evaluate(@"
std.assertEqual(({ x: 1, y: self.x } { x: 2 }).y, 2) &&
std.assertEqual(std.native('concat')('foo', 'bar'), 'foobar') &&
std.assertEqual(std.native('return_types')(), {a: [1, 2, 3, null, []], b: 1, c: true, d: null, e: {x: 1, y: 2, z: ['foo']}}) &&
true
");
            
            Assert.AreEqual("true\n", result);
        }

        /// <summary>
        /// If a native callback fails, it should throw an exception.
        /// </summary>
        [TestMethod]
        public void ExceptionThownInNativeCallback()
        {
            Vm.AddNativeCallback("test", new Func<object, string>(s => throw new Exception("Test error")));

            var ex = Assert.ThrowsException<JsonnetException>(() => Evaluate("std.native('test')('a')"));
            Assert.AreEqual($@"RUNTIME ERROR: Test error
	{Filename}:1:1-24	
", ex.Message);
        }

        /// <summary>
        /// The import callback is invoked when jsonnet wants to load an external file.
        /// </summary>
        [TestMethod]
        public void ImportCallback()
        {
            Vm.ImportCallback = (string dir, string rel, out string here) =>
            {
                Assert.AreEqual(Path.GetDirectoryName(Filename) + Path.DirectorySeparatorChar, dir);
                Assert.AreEqual("bar.libsonnet", rel);
                here = "";
                return "42";
            };
            
            GC.Collect();

            var result = Evaluate("local bar = import 'bar.libsonnet';bar");
            
            Assert.AreEqual("42" + Environment.NewLine, result);
        }

        /// <summary>
        /// The here output argument of the import callback is used in stack traces in the case where there is an error
        /// in the imported file.
        /// </summary>
        [TestMethod]
        public void HereReturnedByImportCallback()
        {
            Vm.ImportCallback = (string dir, string rel, out string here) =>
            {
                here = "/a/b/bar.libsonnet";
                return "{,}";
            };

            GC.Collect();

            var ex = Assert.ThrowsException<JsonnetException>(() => Evaluate(
                "local foo = import 'foo.libsonnet';{'foo': foo}"));

            Assert.That.StartsWith(ex.Message, "STATIC ERROR: /a/b/bar.libsonnet:1:2");
        }
        
        /// <summary>
        /// The import callback should throw an exception if there is an error.
        /// </summary>
        [TestMethod]
        public void ExceptionThrownInImportCallback()
        {
            Vm.ImportCallback = (string dir, string rel, out string here) => throw new Exception("Test error");
            
            GC.Collect();
            
            var ex = Assert.ThrowsException<JsonnetException>(() => Evaluate("import 'test.libjsonnet'"));
            Assert.That.StartsWith(ex.Message,
                "RUNTIME ERROR: couldn't open import \"test.libjsonnet\": Test error");
        }
    }
}