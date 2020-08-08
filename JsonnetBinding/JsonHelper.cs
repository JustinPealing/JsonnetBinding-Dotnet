using System;

namespace JsonnetBinding
{
    internal static class JsonHelper
    {
        public static IntPtr ConvertToNative(JsonnetVmHandle vm, object v)
        {
            return NativeMethods.jsonnet_json_make_string(vm, "This is a test");
        }

        public static object ToManaged(JsonnetVmHandle vm, IntPtr v)
        {
            return null;
        }
    }
}