using System;
using System.Diagnostics;

namespace TestConsoleUsage
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // This would trigger RS1035 if we were using Console.WriteLine
                // Console.WriteLine("This would cause RS1035 warning");
                
                // This is the correct approach - using Debug.WriteLine instead
                Debug.WriteLine("[TestAdapter] Test message: Operation completed successfully");
            }
            catch (Exception ex)
            {
                // This is how we handle exceptions now - no Console usage
                Debug.WriteLine($"[TestAdapter] Exception occurred: {ex.Message}");
            }
        }
    }
}
