using System;

namespace JsonnetBinding
{
    /// <summary>
    /// Exception thrown when an error is returned from a Jsonnet call.
    /// </summary>
    public class JsonnetException : Exception
    {
        public JsonnetException(in int error, string message) : base(message) => HResult = error;
    }
}