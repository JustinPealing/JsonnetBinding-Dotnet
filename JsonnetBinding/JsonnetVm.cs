using System;
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