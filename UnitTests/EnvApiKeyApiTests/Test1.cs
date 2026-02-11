using ApiKeyMgr;
using ButlerSDK.ApiKeyMgr.Contract;
using System.Reflection;
using System.Security;

namespace EnvApiKeyApiTests
{
    [TestClass]
    public sealed class EnvironmentApiKeyMgr_TESTS
    {
        [TestMethod]
        public void EnvironmentApiKeyMgr_CanMakeInstance_IsNotNull()
        {
            var TestMe = new EnvironmentApiKeyMgr();
            Assert.IsNotNull(TestMe);
        }

        [TestMethod]
        public void EnvironmentApiKeyMgr_CanMakeInstance_TypeMatches()
        {
            var TestMe = new EnvironmentApiKeyMgr();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(EnvironmentApiKeyMgr));
        }


        [TestMethod]
        public void EnvironmentApiKeyMgr_CanMakeInstance_InterfaceMatches()
        {
            var TestMe = new EnvironmentApiKeyMgr();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(EnvironmentApiKeyMgr));
            Assert.IsTrue(TestMe is IButlerVaultKeyCollection, "Error Varaible is not matching the correct interface" + nameof(IButlerVaultKeyCollection));
        }



        [TestMethod]
        public void EnvironmentApiKeyMgr_CanAuthenticate_CallingAssembly()
        {
            var TestMe = new EnvironmentApiKeyMgr();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(EnvironmentApiKeyMgr));

            Assert.IsTrue(TestMe.Authenticate(Assembly.GetCallingAssembly()));//strictly speaking this call does nothing, the routine is a stub
        }


        [TestMethod]
        public void EnvironmentApiKeyMgr_CanMakeInstance_CanInitValue_StateIsNeedsAuthCallFirst()
        {
            var TestMe = new EnvironmentApiKeyMgr();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(EnvironmentApiKeyMgr));

            Assert.Throws<InvalidOperationException>(() => {
                TestMe.InitVault("ANYTHING"); // we having not authenticated first, it should throw
            });

            TestMe.Authenticate(Assembly.GetExecutingAssembly());
                TestMe.InitVault("ANYTHING"); // we having authenticated first, it should NOT THROW
 
        }


        [TestMethod]
        public void EnvironmentApiKeyMgr_ResolveKey_RequiresAuthFirst()
        {
            var TestMe = new EnvironmentApiKeyMgr();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(EnvironmentApiKeyMgr));

            string? cache = Environment.GetEnvironmentVariable("TESTVAR_DO_NOT_USE");
            Environment.SetEnvironmentVariable("TESTVAR_DO_NOT_USE", "TOP");
            Assert.Throws<InvalidOperationException>(() =>
            {
                SecureString? tmp = TestMe.ResolveKey("TESTVAR_DO_NOT_USE");
            });

            // the call should work now
                TestMe.Authenticate(Assembly.GetExecutingAssembly());
                SecureString? tmp = TestMe.ResolveKey("TESTVAR_DO_NOT_USE");
            Assert.IsInstanceOfType(tmp,typeof(SecureString));

            Assert.IsTrue(TestMe.Authenticate(Assembly.GetCallingAssembly()));//strictly speaking this call does nothing, the routine is a stub
        }
    }
}
