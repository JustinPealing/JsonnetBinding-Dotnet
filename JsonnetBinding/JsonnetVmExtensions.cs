using System;
using System.Linq;

namespace JsonnetBinding
{
    public static class JsonnetVmExtensions
    {
        public static JsonnetVm AddNativeCallback(this JsonnetVm vm, string name, Delegate d)
        {
            var parameters = d.Method.GetParameters().Select(p => p.Name).ToArray();
            return vm.AddNativeCallback(name, parameters, args => d.Method.Invoke(d.Target, args));
        }
    }
}