

/*
 * This test suit might need to be setup first.
 * It test WindowsKeyManager functionality.
 * 
 * That class must be tested on Windows OS only.
 * We need to be able see if we can:
 * 
 * read the magic id of the memory stream the (MZ) (currenty O'B') of the thing.
 * 
 * Encrpyted file must be able to be decrypted.
 * 
 * We must be able to present keys as requested via ResolveKey
 * we most be able to work without issue and be somewhat fast.
 * 
 * Code wise:
 * we read the encrypted data:
 * memorystream it.
 * Decrypt it.
 * use that to go for keeping a zip archive running
 * 
 * StretchGoal
 * WindowsKeyManager() gets a small IFileInterface that lets just enough be mocked for unit testing
 * 
 */


using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.ApiKeyMgr.WindowsVault;

namespace WindowApiKeyMgr_Tests
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void CreateInstance_IsNotNull()
        {
            var TestMe = new WindowsVault();
            Assert.IsNotNull(TestMe);
        }

        [TestMethod]
        public void AuthenticRejects_Null()
        {
            var TestMe = new WindowsVault();
            Assert.IsNotNull(TestMe);
            Assert.IsFalse(TestMe.Authenticate(null!)); // we want it to handle null as false

        }


    }
}
