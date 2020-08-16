using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    /// <summary>
    /// Tests for <see cref="JsonnetVm"/>.
    /// </summary>
    [TestClass]
    public class JsonnetVmTest
    {
        protected readonly JsonnetVm Vm = new JsonnetVm();
        
        /// <summary>
        /// EvaludateFile is used to evaluate a jsonnet file.
        /// </summary>
        [TestMethod]
        public void EvaluateFile()
        {
            var filename = Path.GetTempFileName();
            File.WriteAllText(filename, "{ x: 1 , y: self.x + 1 } { x: 10 }");
            var result = Vm.EvaluateFile(filename);

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
                Vm.EvaluateSnippet("test.jsonnet", "{ x: 1 , y: self.x / 0 } { x: 10 }"));
            
            Assert.That.StartsWith(ex.Message, "RUNTIME ERROR: division by zero.");
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

            var ex = Assert.ThrowsException<JsonnetException>(() => Vm.EvaluateSnippet("test.jsonnet", snippet));

            Assert.That.StartsWith(ex.Message, "RUNTIME ERROR: max stack frames exceeded.");
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
                    {
                        "f", new
                        {
                            Foo = "bar"
                        }
                    }
                }));

            var result = Vm.EvaluateSnippet("test.jsonnet", @"
std.assertEqual(({ x: 1, y: self.x } { x: 2 }).y, 2) &&
std.assertEqual(std.native('concat')('foo', 'bar'), 'foobar') &&
std.assertEqual(std.native('return_types')(), {a: [1, 2, 3, null, []], b: 1, c: true, d: null, e: {x: 1, y: 2, z: ['foo']}, f: { Foo: 'bar' }}) &&
true
");
            
            Assert.AreEqual("true\n", result);
        }

        /// <summary>
        /// The native callback delegate cannot be null.
        /// </summary>
        [TestMethod]
        public void NativeCallbackIsNull()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(() => Vm.AddNativeCallback("test", null));
            Assert.AreEqual("d", ex.ParamName);
        }

        /// <summary>
        /// If the type supplied to a native callback is not compatible with the type on the callback, then an
        /// exception will be thrown.
        /// </summary>
        [TestMethod]
        public void NativeCallbackTypeMismatch()
        {
            Vm.AddNativeCallback("test", new Func<int, string>(s => "aaa"));

            var ex = Assert.ThrowsException<JsonnetException>(() =>
                Vm.EvaluateSnippet("test.jsonnet", "std.native('test')('a')"));
            Assert.That.StartsWith(ex.Message,
                "RUNTIME ERROR: Object of type 'System.String' cannot be converted to type 'System.Int32'.");
        }

        /// <summary>
        /// If a native callback fails, it should throw an exception.
        /// </summary>
        [TestMethod]
        public void NativeCallbackExceptionThown()
        {
            Vm.AddNativeCallback("test", new Func<object, string>(s => throw new Exception("Test error")));

            var ex = Assert.ThrowsException<JsonnetException>(() =>
                Vm.EvaluateSnippet("test.jsonner", "std.native('test')('a')"));
            Assert.That.StartsWith(ex.Message, "RUNTIME ERROR: Test error");
        }
        
        /// <summary>
        /// The import callback is invoked when jsonnet wants to load an external file.
        /// </summary>
        [TestMethod]
        public void ImportCallback()
        {
            Vm.ImportCallback = (string dir, string rel, out string here) =>
            {
                Assert.AreEqual("/some/path/", dir);
                Assert.AreEqual("bar.libsonnet", rel);
                here = "";
                return "42";
            };
            
            GC.Collect();

            var result = Vm.EvaluateSnippet("/some/path/test.jsonnet", "local bar = import 'bar.libsonnet';bar");
            
            Assert.AreEqual("42" + Environment.NewLine, result);
        }

        /// <summary>
        /// The import callback cannot be null.
        /// </summary>
        [TestMethod]
        public void ImportCallbackIsNull() => Assert.ThrowsException<ArgumentNullException>(() => Vm.ImportCallback = null);

        /// <summary>
        /// The here output argument of the import callback is used in stack traces in the case where there is an error
        /// in the imported file.
        /// </summary>
        [TestMethod]
        public void ImportCallbackReturnsHere()
        {
            Vm.ImportCallback = (string dir, string rel, out string here) =>
            {
                here = "/a/b/bar.libsonnet";
                return "{,}";
            };

            GC.Collect();

            var ex = Assert.ThrowsException<JsonnetException>(() =>
                Vm.EvaluateSnippet("test.jsonnet", "local foo = import 'foo.libsonnet';{'foo': foo}"));

            Assert.That.StartsWith(ex.Message, "STATIC ERROR: /a/b/bar.libsonnet:1:2");
        }
        
        /// <summary>
        /// The import callback should throw an exception if there is an error.
        /// </summary>
        [TestMethod]
        public void ImportCallbackExceptionThrown()
        {
            Vm.ImportCallback = (string dir, string rel, out string here) => throw new Exception("Test error");
            
            GC.Collect();

            var ex = Assert.ThrowsException<JsonnetException>(() =>
                Vm.EvaluateSnippet("test.jsonnet", "import 'test.libjsonnet'"));
            Assert.That.StartsWith(ex.Message,
                "RUNTIME ERROR: couldn't open import \"test.libjsonnet\": Test error");
        }
    }
}