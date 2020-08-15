using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JsonnetBinding
{
    internal static class JsonHelper
    {
        /// <summary>
        /// Converts the managed object returned from a native callback into its native jsonnet equivalent.
        /// </summary>
        public static IntPtr ConvertToNative(JsonnetVmHandle vm, object v)
        {
            return v switch
            {
                null => NativeMethods.jsonnet_json_make_null(vm),
                string str => NativeMethods.jsonnet_json_make_string(vm, str),
                bool b => NativeMethods.jsonnet_json_make_bool(vm, b),
                int i => NativeMethods.jsonnet_json_make_number(vm, i),
                double d => NativeMethods.jsonnet_json_make_number(vm, d),
                IDictionary<string, object> dictionary => ConvertDictionaryToNative(vm, dictionary),
                IEnumerable enumerable => ConvertEnumerableToNative(vm, enumerable),
                _ => ConvertObjectPropertiesToNative(vm, v)
            };
        }

        private static IntPtr ConvertDictionaryToNative(JsonnetVmHandle vm, IDictionary<string, object> dictionary)
        {
            var obj = NativeMethods.jsonnet_json_make_object(vm);
            try
            {
                foreach (var val in dictionary)
                    NativeMethods.jsonnet_json_object_append(vm, obj, val.Key, ConvertToNative(vm, val.Value));
                return obj;
            }
            catch (Exception e)
            {
                NativeMethods.jsonnet_json_destroy(vm, obj);
                throw;
            }
        }

        private static IntPtr ConvertEnumerableToNative(JsonnetVmHandle vm, IEnumerable enumerable)
        {
            var array = NativeMethods.jsonnet_json_make_array(vm);
            try
            {
                foreach (var val in enumerable)
                    NativeMethods.jsonnet_json_array_append(vm, array, ConvertToNative(vm, val));
                return array;
            }
            catch
            {
                NativeMethods.jsonnet_json_destroy(vm, array);
                throw;
            }
        }

        private static IntPtr ConvertObjectPropertiesToNative(JsonnetVmHandle vm, object v)
        {
            var obj = NativeMethods.jsonnet_json_make_object(vm);
            try
            {
                foreach (var p in v.GetType().GetProperties())
                    NativeMethods.jsonnet_json_object_append(vm, obj, p.Name, ConvertToNative(vm, p.GetValue(v)));
                return obj;
            }
            catch
            {
                NativeMethods.jsonnet_json_destroy(vm, obj);
                throw;
            }
        }

        /// <summary>
        /// Converts a native jsonnet value supplied to a native method into its managed equivalent.
        /// </summary>
        public static object ConvertNativeArgumentToManaged(JsonnetVmHandle vm, IntPtr v)
        {
            if (NativeMethods.jsonnet_json_extract_null(vm, v))
                return null;

            var str = NativeMethods.jsonnet_json_extract_string(vm, v);
            if (str != IntPtr.Zero)
            {
                // TODO: I Don't understand why this works, but putting the return type on the method as string does not 
                return Marshal.PtrToStringUTF8(str);
            }
            
            if (NativeMethods.jsonnet_json_extract_number(vm, v, out var val))
                return val;

            var b = NativeMethods.jsonnet_json_extract_bool(vm, v);
            if (b == 0) return false;
            if (b == 1) return true;
            
            throw new InvalidOperationException("Unknown JsonnetJsonValue");
        }
    }
}