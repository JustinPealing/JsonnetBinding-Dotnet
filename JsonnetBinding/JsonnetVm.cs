using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace JsonnetBinding
{
    public class JsonnetVm : IDisposable
    {
        private readonly JsonnetVmHandle _handle;

        public JsonnetVm() => _handle = NativeMethods.jsonnet_make();

        public uint MaxStack
        {
            set => NativeMethods.jsonnet_max_stack(_handle, value);
        }

        public uint GcMinObjects
        {
            set => NativeMethods.jsonnet_gc_min_objects(_handle, value);
        }

        public uint MaxTrace
        {
            set => NativeMethods.jsonnet_max_trace(_handle, value);
        }

        public string EvaluateFile(string filename)
        {
            var result = NativeMethods.jsonnet_evaluate_file(_handle, filename, out bool error);
            var resultString = MarshalAndDeallocateString(result);

            if (error) throw new JsonnetException(resultString);

            return resultString;
        }
        
        public string EvaluateSnippet(string filename, string snippet)
        {
            var result = NativeMethods.jsonnet_evaluate_snippet(_handle, filename, snippet, out bool error);
            var resultString = MarshalAndDeallocateString(result);

            if (error) throw new JsonnetException(resultString);

            return resultString;
        }

        public JsonnetVm AddExtVar(string key, string value)
        {
            NativeMethods.jsonnet_ext_var(_handle, key, value);
            return this;
        }

        public JsonnetVm AddExtCode(string key, string value)
        {
            NativeMethods.jsonnet_ext_code(_handle, key, value);
            return this;
        }

        public JsonnetVm AddTlaVar(string key, string value)
        {
            NativeMethods.jsonnet_tla_var(_handle, key, value);
            return this;
        }

        public JsonnetVm AddTlaCode(string key, string value)
        {
            NativeMethods.jsonnet_tla_code(_handle, key, value);
            return this;
        }

        public JsonnetVm SetImportCallback(ImportCallback importCallback)
        {
            NativeMethods.jsonnet_import_callback(_handle,
                (IntPtr ctx, string dir, string rel, out IntPtr here, out bool success) =>
                {
                    try
                    {
                        var result = importCallback(dir, rel, out var foundHere);
                        here = AllocJsonnetString(_handle, foundHere);
                        success = true;
                        return AllocJsonnetString(_handle, result);
                    }
                    catch (Exception e)
                    {
                        success = false;
                        here = IntPtr.Zero;
                        return AllocJsonnetString(_handle, e.Message);
                    }
                }, IntPtr.Zero);
            return this;
        }
        
        public JsonnetVm AddNativeCallback(string name, string[] parameters, NativeCallback callback)
        {
            NativeMethods.jsonnet_native_callback(_handle, name,
                (IntPtr ctx, IntPtr argv, out bool success) =>
                {
                    try
                    {
                        var convertedArgs = MarshalNativeCallbackArgs(argv, parameters.Length);
                        var result = callback(convertedArgs);
                        success = true;
                        return JsonHelper.ConvertToNative(_handle, result);
                    }
                    catch (TargetInvocationException ex)
                    {
                        success = false;
                        return JsonHelper.ConvertToNative(_handle, ex.InnerException?.Message);
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        return JsonHelper.ConvertToNative(_handle, ex.Message);
                    }
                },
                IntPtr.Zero,
                parameters.Append(null).ToArray());
            return this;
        }

        private object[] MarshalNativeCallbackArgs(IntPtr argv, int parameters)
        {
            if (parameters == 0) return Array.Empty<object>();
            var args = new IntPtr[parameters];
            Marshal.Copy(argv, args, 0, parameters);
            return args.Select(a => JsonHelper.ToManaged(_handle, a)).ToArray();
        }

        /// <summary>
        /// Marshal the contents of a string returned from Jsonnet, then de-allocates it.
        /// </summary>
        private string MarshalAndDeallocateString(IntPtr result)
        {
            try
            {
                return Marshal.PtrToStringUTF8(result);
            }
            finally
            {
                NativeMethods.jsonnet_realloc(_handle, result, UIntPtr.Zero);    
            }
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
        
        public void Dispose() => _handle?.Dispose();
    }
}