using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JsonnetBinding
{
    internal static class JsonHelper
    {
        public static IntPtr ConvertToNative(JsonnetVmHandle vm, object v)
        {
            if (v is null)
            {
                return NativeMethods.jsonnet_json_make_null(vm);
            }
            
            if (v is string str)
            {
                return NativeMethods.jsonnet_json_make_string(vm, str);
            }

            if (v is bool b)
            {
                return NativeMethods.jsonnet_json_make_bool(vm, b);
            }

            if (v is int i)
            {
                return NativeMethods.jsonnet_json_make_number(vm, i);
            }
            
            if (v is double d)
            {
                return NativeMethods.jsonnet_json_make_number(vm, d);
            }

            if (v is IDictionary<string, object> dictionary)
            {
                var obj = NativeMethods.jsonnet_json_make_object(vm);
                foreach (var val in dictionary)
                {
                    NativeMethods.jsonnet_json_object_append(vm, obj, val.Key, ConvertToNative(vm, val.Value));
                }

                return obj;
            }
            
            if (v is IEnumerable enumerable)
            {
                var array = NativeMethods.jsonnet_json_make_array(vm);
                foreach (var val in enumerable)
                {
                    NativeMethods.jsonnet_json_array_append(vm, array, ConvertToNative(vm, val));
                }

                return array;
            }
            
            throw new InvalidOperationException($"Not able to convert type {v.GetType()} to JsonnetJsonValue");
        }

        public static object ToManaged(JsonnetVmHandle vm, IntPtr v)
        {
            if (NativeMethods.jsonnet_json_extract_null(vm, v))
            {
                return null;
            }

            var str = NativeMethods.jsonnet_json_extract_string(vm, v);
            if (str != IntPtr.Zero)
            {
                return Marshal.PtrToStringUTF8(str);
            }
            
            if (NativeMethods.jsonnet_json_extract_number(vm, v, out var val))
            {
                return val;
            }

            var b = NativeMethods.jsonnet_json_extract_bool(vm, v);
            if (b == 0) return false;
            if (b == 1) return true;
            
            throw new InvalidOperationException("Unknown JsonnetJsonValue");
        }
    }
}