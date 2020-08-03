using System;
using System.Runtime.InteropServices;

namespace JsonnetBinding
{
    /// <summary>
    /// PInvoke calls for libjsonnet. See jsonnet.h for reference:
    ///
    /// https://github.com/google/jsonnet/blob/master/include/libjsonnet.h
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Callback used to load imports.
        /// </summary>
        /// <remarks>
        /// The returned char* should be allocated with jsonnet_realloc.  It will be cleaned up by
        /// libjsonnet when no-longer needed.
        /// </remarks>
        /// <param name="ctx">User pointer, given in jsonnet_import_callback.</param>
        /// <param name="baseDir">The directory containing the code that did the import.</param>
        /// <param name="rel">The path imported by the code.</param>
        /// <param name="foundHere">Set this byref param to path to the file, absolute or relative to the process's CWD.
        /// This is necessary so that imports from the content of the imported file can be resolved correctly. Allocate
        /// memory with jsonnet_realloc. Only use when *success = 1.</param>
        /// <param name="success">Set this byref param to 1 to indicate success and 0 for failure.</param>
        /// <returns>The content of the imported file, or an error message.</returns>
        public delegate IntPtr JsonnetImportCallback(
            IntPtr ctx, string baseDir, string rel, out IntPtr foundHere, out int success);

        /// <summary>
        /// Create a new Jsonnet virtual machine.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern JsonnetVmHandle jsonnet_make();

        /// <summary>
        /// Set the maximum stack depth.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_max_stack(JsonnetVmHandle vm, UInt32 v);
        
        /// <summary>
        /// Set the number of objects required before a garbage collection cycle is allowed.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_gc_min_objects(JsonnetVmHandle vm, UInt32 v);
        
        /// <summary>
        /// Run the garbage collector after this amount of growth in the number of objects.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_gc_growth_trigger(JsonnetVmHandle vm, double v);
        
        /// <summary>
        /// Expect a string as output and don't JSON encode it.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_string_output(JsonnetVmHandle vm, Int32 v);

        /// <summary>
        /// Bind a Jsonnet external var to the given string.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_ext_var(JsonnetVmHandle vm, string key, string value);

        /// <summary>
        /// Bind a Jsonnet external var to the given code.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_ext_code(JsonnetVmHandle vm, string key, string value);

        /// <summary>
        /// Bind a string top-level argument for a top-level parameter.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_tla_var(JsonnetVmHandle vm, string key, string value);

        /// <summary>
        /// Bind a code top-level argument for a top-level parameter.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_tla_code(JsonnetVmHandle vm, string key, string value);

        /// <summary>
        /// Set the number of lines of stack trace to display (0 for all of them).
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_max_trace(JsonnetVmHandle vm, UInt32 v);
        
        /// <summary>
        /// Evaluate a file containing Jsonnet code, return a JSON string.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="filename">Path to a file containing Jsonnet code.</param>
        /// <param name="error">Return whether or not there was an error.</param>
        /// <returns>Either JSON or the error message.</returns>
        [DllImport("libjsonnet.so")]
        public static extern IntPtr jsonnet_evaluate_file(JsonnetVmHandle vm, string filename, out bool error);
        
        /// <summary>
        /// Evaluate a string containing Jsonnet code, return a JSON string.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="filename">Path to a file (used in error messages).</param>
        /// <param name="snippet">Jsonnet code to execute.</param>
        /// <param name="error">Return whether or not there was an error.</param>
        /// <returns>Either JSON or the error message.</returns>
        [DllImport("libjsonnet.so")]
        public static extern IntPtr jsonnet_evaluate_snippet(JsonnetVmHandle vm, string filename, string snippet, out bool error);

        /// <summary>
        /// Allocate, resize, or free a buffer.  This will abort if the memory cannot be allocated.  It will only
        /// return NULL if sz was zero.
        /// </summary>
        /// <param name="buf">If NULL, allocate a new buffer. If an previously allocated buffer, resize it.</param>
        /// <param name="sz">The size of the buffer to return. If zero, frees the buffer.</param>
        /// <returns>The new buffer.</returns>
        [DllImport("libjsonnet.so")]
        public static extern IntPtr jsonnet_realloc(JsonnetVmHandle vm, IntPtr buf, UIntPtr sz);

        /// <summary>
        /// Override the callback used to locate imports.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_import_callback(JsonnetVmHandle vm, JsonnetImportCallback cb, IntPtr ctx);
        
        /// <summary>
        /// Complement of <see cref="jsonnet_make"/>.
        /// </summary>
        [DllImport("libjsonnet.so")]
        public static extern void jsonnet_destroy(IntPtr vm);
    }
}
