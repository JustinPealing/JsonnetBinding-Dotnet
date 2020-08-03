using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleApp;

namespace JsonnetBinding
{
    /// <summary>
    /// Static methods for evaluating Jsonnet.
    /// </summary>
    public static class Jsonnet
    {
        public static string EvaluateFile(
            string filename,
            int maxStack = -1,
            int gcMinObjects = -1,
            IDictionary<string, string> extVars = null,
            IDictionary<string, string> extCodes = null,
            IDictionary<string, string> tlaVars = null,
            IDictionary<string, string> tlaCodes = null,
            int maxTrace = -1,
            ImportCallback importCallback = null)
        {
            using var vm = MakeVm(maxStack, gcMinObjects, extVars, extCodes, tlaVars, tlaCodes, maxTrace, importCallback);

            var result = NativeMethods.jsonnet_evaluate_file(vm, filename, out bool error);
            var resultString = MarshalAndDeallocateString(vm, result);

            if (error) throw new JsonnetException(resultString);

            return resultString;
        }

        public static string EvaluateSnippet(
            string filename,
            string snippet,
            int maxStack = -1,
            int gcMinObjects = -1,
            IDictionary<string, string> extVars = null,
            IDictionary<string, string> extCodes = null,
            IDictionary<string, string> tlaVars = null,
            IDictionary<string, string> tlaCodes = null,
            int maxTrace = -1,
            ImportCallback importCallback = null)
        {
            using var vm = MakeVm(maxStack, gcMinObjects, extVars, extCodes, tlaVars, tlaCodes, maxTrace, importCallback);

            var result = NativeMethods.jsonnet_evaluate_snippet(vm, filename, snippet, out bool error);
            var resultString = MarshalAndDeallocateString(vm, result);

            if (error) throw new JsonnetException(resultString);

            return resultString;
        }

        private static JsonnetVmHandle MakeVm(
            int maxStack,
            int gcMinObjects,
            IDictionary<string, string> extVars,
            IDictionary<string, string> extCodes,
            IDictionary<string, string> tlaVars,
            IDictionary<string, string> tlaCodes,
            int maxTrace,
            ImportCallback importCallback)
        {
            var vm = NativeMethods.jsonnet_make();

            if (maxStack != -1) NativeMethods.jsonnet_max_stack(vm, (uint) maxStack);
            if (gcMinObjects != -1) NativeMethods.jsonnet_gc_min_objects(vm, (uint) gcMinObjects);
            if (maxTrace != -1) NativeMethods.jsonnet_max_trace(vm, (uint) maxTrace);

            if (extVars != null)
                foreach (var extVar in extVars)
                    NativeMethods.jsonnet_ext_var(vm, extVar.Key, extVar.Value);

            if (extCodes != null)
                foreach (var extCode in extCodes)
                    NativeMethods.jsonnet_ext_code(vm, extCode.Key, extCode.Value);

            if (tlaVars != null)
                foreach (var extCode in tlaVars)
                    NativeMethods.jsonnet_tla_var(vm, extCode.Key, extCode.Value);

            if (tlaCodes != null)
                foreach (var extCode in tlaCodes)
                    NativeMethods.jsonnet_tla_code(vm, extCode.Key, extCode.Value);

            if (importCallback != null)
            {
                NativeMethods.jsonnet_import_callback(vm,
                    (IntPtr ctx, string dir, string rel, out IntPtr here, out int success) =>
                    {
                        var result = importCallback(dir, rel, out var foundHere, out bool isSuccess);
                        if (isSuccess)
                        {
                            success = 1;
                            here = AllocJsonnetString(vm, foundHere);
                        }
                        else
                        {
                            success = 0;
                            here = IntPtr.Zero;
                        }
                        return AllocJsonnetString(vm, result);
                    }, IntPtr.Zero);
            }
            
            return vm;
        }

        /// <summary>
        /// Allocates a Jsonnet string.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="value">Value to give the string.</param>
        /// <returns>IntPtr to the allocated Jsonnet string</returns>
        /// <remarks> 
        /// This method allocates a Jsonnet string (using jsonnet_realloc) of the correct length for the supplied
        /// string, and then copies the supplied value into the allocated string.
        /// </remarks>
        private static IntPtr AllocJsonnetString(JsonnetVmHandle vm, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value).Append((byte) 0).ToArray();
            var result = NativeMethods.jsonnet_realloc(vm, IntPtr.Zero, new UIntPtr((uint) bytes.Length + 1));
            Marshal.Copy(bytes, 0, result, bytes.Length);
            return result;
        }

        /// <summary>
        /// Marshal the contents of a string returned from Jsonnet, then de-allocates it.
        /// </summary>
        private static string MarshalAndDeallocateString(JsonnetVmHandle vm, IntPtr result)
        {
            try
            {
                return Marshal.PtrToStringAuto(result);
            }
            finally
            {
                NativeMethods.jsonnet_realloc(vm, result, UIntPtr.Zero);    
            }
        }
    }
}
