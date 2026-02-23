using SecureStringHelper;
using System.Security;

namespace SecureStringTests
{
    [TestClass]
    public sealed class SecureStringTests
    {
        const string DemoString = "My DEMO STRING ROCKS!";
        [TestMethod]
        public void SecureStringExt_AssignString_IsReadOnlySet()
        {
            using (SecureString Demo = new())
            {
                Demo.AssignStringThenReadOnly(DemoString);
                Assert.IsTrue(Demo.IsReadOnly());
            }
        }
        [TestMethod]
        public void SecureStringExt_AssignString_LengthOK()
        {
            using (SecureString Demo = new())
            {
                Demo.AssignStringThenReadOnly(DemoString);
                Assert.AreEqual(Demo.Length, DemoString.Length);
            }
        }

        [TestMethod]
        public void SecureStringExt_DecryptStringWorks_DontThrow()
        {
            using (SecureString Demo = new())
            {
                Demo.AssignStringThenReadOnly(DemoString);
                Assert.AreEqual(Demo.Length, DemoString.Length);
                string Decrypted = Demo.DecryptString();
                Assert.AreEqual(DemoString, Decrypted);
            }
        }
    }
}
