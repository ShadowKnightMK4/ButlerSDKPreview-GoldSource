using SecureStringHelper;
using System.Security;
using System.Text;

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

        [TestMethod]
        public void SecureStringExt_AssignString_ByStream_Ok()
        {
            using (SecureString Demo = new())
            {
                using (MemoryStream DemoData = new())
                {
                    var Dat = Encoding.UTF8.GetBytes(DemoString);
                    DemoData.Position = 0;
                    DemoData.Write(Dat);
                    DemoData.Position = 0;
                    Demo.AssignStringThenReadOnly(new StreamReader(DemoData), -1);
                }
                string decryp = Demo.DecryptString();
                Assert.AreEqual(DemoString, decryp);
                Assert.AreEqual(DemoString, Demo.DecryptString());
            }
            
        }

        [TestMethod]
        public void SecureStringExt_AssignString_ByStream_Ok_IsReadOnly()
        {
            using (SecureString Demo = new())
            {
                using (MemoryStream DemoData = new())
                {
                    
                    var Dat = Encoding.UTF8.GetBytes(DemoString);
                    DemoData.Write(Dat);
                    DemoData.Position = 0;// don't forget to undo the advance
                    Demo.AssignStringThenReadOnly(DemoData);
                }
                string decryp = Demo.DecryptString();
                Assert.AreEqual(DemoString, decryp);
                Assert.IsTrue(Demo.IsReadOnly());
            }

        }
    }
}
