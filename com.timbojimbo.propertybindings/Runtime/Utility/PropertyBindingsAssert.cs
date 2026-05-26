#if !TJ_PROPERTY_BINDINGS_STRIP_ASSERTIONS
#define TJ_PROPERTY_BINDINGS_ASSERTIONS
#endif
using UnityEngine.Assertions;

namespace TimboJimbo.PropertyBindings
{
    internal class PropBindingsAssert
    {
        const string TJAssertionSymbol = "TJ_PROPERTY_BINDINGS_ASSERTIONS";
        
        [System.Diagnostics.Conditional(TJAssertionSymbol)]
        public static void IsTrue(bool condition, string message = "")
        {
            Assert.IsTrue(condition, message);
        }
        
        [System.Diagnostics.Conditional(TJAssertionSymbol)]
        public static void IsFalse(bool condition, string message = "")
        {
            Assert.IsFalse(condition, message);
        }
        
        [System.Diagnostics.Conditional(TJAssertionSymbol)]
        public static void IsNotNull<T>(T value, string message = "") where T : class
        {
            Assert.IsNotNull(value, message);
        }
        
        [System.Diagnostics.Conditional(TJAssertionSymbol)]
        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            Assert.AreEqual(expected, actual, message);
        }
    }
}
