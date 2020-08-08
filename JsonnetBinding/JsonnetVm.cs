using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace JsonnetBinding
{
    public class JsonnetVm : IDisposable
    {
        internal readonly JsonnetVmHandle Handle;

        public JsonnetVm()
        {
            Handle = NativeMethods.jsonnet_make();
        }

        public uint MaxStack
        {
            set => NativeMethods.jsonnet_max_stack(Handle, value);
        }

        public string EvaluateFile(string filename)
        {
            var result = NativeMethods.jsonnet_evaluate_file(Handle, filename, out bool error);
            var resultString = MarshalAndDeallocateString(result);

            if (error) throw new JsonnetException(resultString);

            return resultString;
        }
        
        public string EvaluateSnippet(string filename, string snippet)
        {
            var result = NativeMethods.jsonnet_evaluate_snippet(Handle, filename, snippet, out bool error);
            var resultString = MarshalAndDeallocateString(result);

            if (error) throw new JsonnetException(resultString);

            return resultString;
        }

        public JsonnetVm WithNativeCallback(string name, string[] parameters, NativeCallback callback)
        {
            NativeMethods.jsonnet_native_callback(Handle, name,
                (IntPtr ctx, IntPtr argv, out bool success) =>
                {
                    try
                    {
                        var convertedArgs = MarshalArgs(argv, parameters.Length);
                        var result = callback(convertedArgs, out success);
                        return JsonHelper.ConvertToNative(Handle, result);
                    }
                    catch
                    {
                        success = false;
                        return IntPtr.Zero;
                    }
                },
                IntPtr.Zero,
                parameters.Append(null).ToArray());
            return this;
        }

        private object[] MarshalArgs(IntPtr argv, int parameters)
        {
            if (parameters == 0)
            {
                return null;
            }
            var args = new IntPtr[parameters];
            Marshal.Copy(argv, args, 0, parameters);
            var convertedArgs = args.Select(a => JsonHelper.ToManaged(Handle, a)).ToArray();
            return convertedArgs;
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
                NativeMethods.jsonnet_realloc(Handle, result, UIntPtr.Zero);    
            }
        }
        
        public void Dispose() => Handle?.Dispose();
    }
}