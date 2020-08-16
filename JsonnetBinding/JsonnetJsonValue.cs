using System;

namespace JsonnetBinding
{
    /// <summary>
    /// Strongly-typed equivalent to IntPtr to represent the opaque JsonnetJsonValue type.
    /// </summary>
    internal struct JsonnetJsonValue
    {
        private IntPtr _ptr;
        public JsonnetJsonValue(IntPtr ptr) => _ptr = ptr;
    }
}