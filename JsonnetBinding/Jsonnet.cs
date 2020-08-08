using System.Collections.Generic;

namespace JsonnetBinding
{
    /// <summary>
    /// Static methods for evaluating Jsonnet.
    /// </summary>
    public static class Jsonnet
    {
        public static string EvaluateFile(
            string filename,
            uint? maxStack = null,
            uint? gcMinObjects = null,
            IDictionary<string, string> extVars = null,
            IDictionary<string, string> extCodes = null,
            IDictionary<string, string> tlaVars = null,
            IDictionary<string, string> tlaCodes = null,
            uint? maxTrace = null,
            ImportCallback importCallback = null)
        {
            using var vm = MakeVm(maxStack, gcMinObjects, extVars, extCodes, tlaVars, tlaCodes, maxTrace, importCallback);
            return vm.EvaluateFile(filename);
        }

        public static string EvaluateSnippet(
            string filename,
            string snippet,
            uint? maxStack = null,
            uint? gcMinObjects = null,
            IDictionary<string, string> extVars = null,
            IDictionary<string, string> extCodes = null,
            IDictionary<string, string> tlaVars = null,
            IDictionary<string, string> tlaCodes = null,
            uint? maxTrace = null,
            ImportCallback importCallback = null)
        {
            using var vm = MakeVm(maxStack, gcMinObjects, extVars, extCodes, tlaVars, tlaCodes, maxTrace, importCallback);

            return vm.EvaluateSnippet(filename, snippet); 
            
        }

        private static JsonnetVm MakeVm(
            uint? maxStack,
            uint? gcMinObjects,
            IDictionary<string, string> extVars,
            IDictionary<string, string> extCodes,
            IDictionary<string, string> tlaVars,
            IDictionary<string, string> tlaCodes,
            uint? maxTrace,
            ImportCallback importCallback)
        {
            var vm = new JsonnetVm();

            if (maxStack != null) vm.MaxStack = maxStack.Value;
            if (gcMinObjects != null) vm.GcMinObjects = gcMinObjects.Value;
            if (maxTrace != null) vm.MaxTrace = maxTrace.Value;

            if (extVars != null)
                foreach (var extVar in extVars)
                    vm.AddExtVar(extVar.Key, extVar.Value);

            if (extCodes != null)
                foreach (var extCode in extCodes)
                    vm.AddExtCode(extCode.Key, extCode.Value);

            if (tlaVars != null)
                foreach (var tlaVar in tlaVars)
                    vm.AddTlaVar(tlaVar.Key, tlaVar.Value);

            if (tlaCodes != null)
                foreach (var tlaCode in tlaCodes)
                    vm.AddTlaCode(tlaCode.Key, tlaCode.Value);

            if (importCallback != null)
            {
                vm.SetImportCallback(importCallback);
            }

            return vm;
        }
    }
}
