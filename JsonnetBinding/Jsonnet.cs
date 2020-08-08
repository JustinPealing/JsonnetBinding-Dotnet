using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
            ImportCallback importCallback = null,
            IDictionary<string, NativeCallback> nativeCallbacks = null)
        {
            using var vm = MakeVm(maxStack, gcMinObjects, extVars, extCodes, tlaVars, tlaCodes, maxTrace, importCallback, nativeCallbacks);
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
            ImportCallback importCallback = null,
            IDictionary<string, NativeCallback> nativeCallbacks = null)
        {
            using var vm = MakeVm(maxStack, gcMinObjects, extVars, extCodes, tlaVars, tlaCodes, maxTrace, importCallback, nativeCallbacks);

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
            ImportCallback importCallback,
            IDictionary<string, NativeCallback> nativeCallbacks)
        {
            var vm = new JsonnetVm();

            if (maxStack != null) NativeMethods.jsonnet_max_stack(vm.Handle, maxStack.Value);
            if (gcMinObjects != null) NativeMethods.jsonnet_gc_min_objects(vm.Handle, gcMinObjects.Value);
            if (maxTrace != null) NativeMethods.jsonnet_max_trace(vm.Handle, maxTrace.Value);

            if (extVars != null)
                foreach (var extVar in extVars)
                    NativeMethods.jsonnet_ext_var(vm.Handle, extVar.Key, extVar.Value);

            if (extCodes != null)
                foreach (var extCode in extCodes)
                    NativeMethods.jsonnet_ext_code(vm.Handle, extCode.Key, extCode.Value);

            if (tlaVars != null)
                foreach (var extCode in tlaVars)
                    NativeMethods.jsonnet_tla_var(vm.Handle, extCode.Key, extCode.Value);

            if (tlaCodes != null)
                foreach (var extCode in tlaCodes)
                    NativeMethods.jsonnet_tla_code(vm.Handle, extCode.Key, extCode.Value);

            if (importCallback != null)
            {
                NativeMethods.jsonnet_import_callback(vm.Handle,
                    (IntPtr ctx, string dir, string rel, out IntPtr here, out int success) =>
                    {
                        var result = importCallback(dir, rel, out var foundHere, out bool isSuccess);
                        if (isSuccess)
                        {
                            success = 1;
                            here = AllocJsonnetString(vm.Handle, foundHere);
                        }
                        else
                        {
                            success = 0;
                            here = IntPtr.Zero;
                        }
                        return AllocJsonnetString(vm.Handle, result);
                    }, IntPtr.Zero);
            }

            if (nativeCallbacks != null)
            {
                foreach (var callback in nativeCallbacks)
                {
                    NativeMethods.jsonnet_native_callback(vm.Handle, callback.Key,
                        (IntPtr ctx, IntPtr argv, out bool success) =>
                        {
                            try
                            {
                                var args = new IntPtr[2];
                                Marshal.Copy(argv, args, 0, 2);
                                var convertedArgs = args.Select(a => JsonHelper.ToManaged(vm.Handle, a)).ToArray();
                                var result = callback.Value(convertedArgs, out success);
                                return JsonHelper.ConvertToNative(vm.Handle, result);
                            }
                            catch
                            {
                                success = false;
                                return IntPtr.Zero;
                            }
                        }, 
                        IntPtr.Zero,
                        new string[]
                        {
                            // TODO: How should the caller pass these?
                            "foo", "bar", null
                        });
                }
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
    }
}
