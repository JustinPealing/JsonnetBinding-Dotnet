using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace JsonnetBinding
{
    public class JsonnetVm
    {
        private readonly JsonnetVmHandle _handle;
        private readonly IDictionary<string, NativeMethods.JsonnetNativeCallback> _nativeCallbacks =
            new Dictionary<string, NativeMethods.JsonnetNativeCallback>();
        private NativeMethods.JsonnetImportCallback _importCallback;
        
        public JsonnetVm() => _handle = NativeMethods.jsonnet_make();

        /// <summary>
        /// Set the maximum stack depth.
        /// </summary>
        public uint MaxStack
        {
            set => NativeMethods.jsonnet_max_stack(_handle, value);
        }

        /// <summary>
        /// Set the number of objects required before a garbage collection cycle is allowed.
        /// </summary>
        public uint GcMinObjects
        {
            set => NativeMethods.jsonnet_gc_min_objects(_handle, value);
        }

        /// <summary>
        /// Run the garbage collector after this amount of growth in the number of objects.
        /// </summary>
        public double GcGrowthTrigger
        {
            set => NativeMethods.jsonnet_gc_growth_trigger(_handle, value);
        }

        /// <summary>
        /// Set the number of lines of stack trace to display (0 for all of them).
        /// </summary>
        public uint MaxTrace
        {
            set => NativeMethods.jsonnet_max_trace(_handle, value);
        }

        /// <summary>
        /// Expect a string as output and don't JSON encode it.
        /// </summary>
        public bool StringOutput
        {
            set => NativeMethods.jsonnet_string_output(_handle, value ? 1 : 0);
        }
        
        /// <summary>
        /// Set the callback used to load imports.
        /// </summary>
        /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
        public ImportCallback ImportCallback
        {
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                
                _importCallback = (IntPtr ctx, string dir, string rel, out IntPtr here, out bool success) =>
                {
                    try
                    {
                        var result = value(dir, rel, out var foundHere);
                        // TODO: Make sure that these strings are de-allocated in a failure
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
                };
                NativeMethods.jsonnet_import_callback(_handle, _importCallback, IntPtr.Zero);
            }
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

        public JsonnetVm AddNativeCallback(string name, Delegate d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            
            var parameters = d.Method.GetParameters();

            JsonnetJsonValue NativeCallback(IntPtr ctx, IntPtr argv, out bool success)
            {
                try
                {
                    var convertedArgs = MarshalNativeCallbackArgs(argv, parameters);
                    var result = d.Method.Invoke(d.Target, convertedArgs);
                    success = true;
                    return JsonConvert.ConvertToNative(_handle, result);
                }
                catch (TargetInvocationException ex)
                {
                    // Because we are invoking a delegate, any exception thrown by the delegate will be wrapped
                    success = false;
                    return JsonConvert.ConvertToNative(_handle, ex.InnerException?.Message);
                }
                catch (Exception ex)
                {
                    success = false;
                    return JsonConvert.ConvertToNative(_handle, ex.Message);
                }
            }

            var parameterNames = parameters.Select(p => p.Name).Append(null).ToArray();
            
            NativeMethods.jsonnet_native_callback(_handle, name, NativeCallback, IntPtr.Zero, parameterNames);
            
            // To make sure the delegate passed to jsonnet_native_callback is not garbage collected
            _nativeCallbacks[name] = NativeCallback;
            return this;
        }
        
        private object[] MarshalNativeCallbackArgs(IntPtr argv, ParameterInfo[] parameters)
        {
            // argv is a pointer to a null-terminated array of arguments, however we know that the length of the array
            // will be equal to the number of parameters on the method as the jsonnet vm validates that the number of
            // arguments supplied matches the number of arguments on the native callback
            if (parameters.Length == 0) return Array.Empty<object>();
            var args = new IntPtr[parameters.Length];
            Marshal.Copy(argv, args, 0, parameters.Length);
            return parameters
                .Select((t, i) => JsonConvert.ConvertNativeArgumentToManaged(_handle, new JsonnetJsonValue(args[i])))
                .ToArray();
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
    }
}