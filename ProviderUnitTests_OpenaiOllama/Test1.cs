using ButlerSDK.Providers.OpenAI;
using ButlerSDK.Providers.OpenAI.Ollama;
using UnitTestDataTypes;
namespace ProviderUnitTests_OpenaiOllama
{
    [TestClass]
    public sealed class Test1
    {
        [TestInitialize]
        public void TestInit()
        {
            // This method is called before each test method.
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // This method is called after each test method.
        }

        [TestMethod]
        public void Create_Provider_NullArg()
        {
            var Ollama = new OllamaOpenAiProvider(null);


            var OpenAiOne = Ollama.GetPrivateField<ButlerOpenAiProvider>("local");

            Assert.IsNotNull(OpenAiOne);
            Assert.IsTrue(OpenAiOne is ButlerOpenAiProvider);

            var EndPoint = OpenAiOne.GetPrivateField<Uri>("ChangedEndPoint");

            Assert.IsNotNull(EndPoint);
            Assert.IsTrue(EndPoint is Uri);


            var Def = OllamaOpenAiProvider.DefaultTarget;

            Assert.IsTrue(Def.Equals(EndPoint.AbsolutePath));
        }

        [TestMethod]
        public void Create_Provider_NotNullArg_takesit()
        {
            string new_arg = "www.exampleend.com/v2";
            var Ollama = new OllamaOpenAiProvider(new Uri(new_arg)); 


            var OpenAiOne = Ollama.GetPrivateField<ButlerOpenAiProvider>("local");

            Assert.IsNotNull(OpenAiOne);
            Assert.IsTrue(OpenAiOne is ButlerOpenAiProvider);

            var EndPoint = OpenAiOne.GetPrivateField<Uri>("ChangedEndPoint");

            Assert.IsNotNull(EndPoint);
            Assert.IsTrue(EndPoint is Uri);


            var Def = OllamaOpenAiProvider.DefaultTarget;

            Assert.IsFalse(Def.Equals(EndPoint.AbsolutePath));
            Assert.IsTrue(EndPoint.AbsoluteUri == new_arg);
        }
    }
}
