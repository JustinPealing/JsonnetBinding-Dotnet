using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonnetBinding.Tests
{
    public static class AssertExtensions
    {
        public static void StartsWith(this Assert assert, string expected, string actual)
        {
            Assert.IsNotNull(expected);
            if (expected.StartsWith(actual))
                return;
            throw new AssertFailedException($@"Assert.StartsWith failed. Expected:<{expected}> to start with <{actual}>");
        }
    }
}