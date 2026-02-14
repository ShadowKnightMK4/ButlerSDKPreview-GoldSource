using ButlerSDK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.UnitTesting.MockProvider;
using ApiKeyMgr;
using ButlerToolContract.DataTypes;
using ButlerToolContracts.DataTypes;
using System.ComponentModel;
using System.Collections;

namespace CoreUnitTests.CurrentTests
{
    class DummyList : IButlerChatCollection
    {
        public ButlerChatMessage this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Add(ButlerChatMessage item)
        {
            throw new NotImplementedException();
        }

        public void AddSystemMessage(string text)
        {
            throw new NotImplementedException();
        }

        public void AddToolMessage(string CallID, string text)
        {
            throw new NotImplementedException();
        }

        public void AddUserMessage(string text)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(ButlerChatMessage item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ButlerChatMessage[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<ButlerChatMessage> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<ButlerChatMessage> GetSliceOfMessages(int LastUserMessageIndex, int LastAiTurnIndex)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(ButlerChatMessage item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, ButlerChatMessage item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ButlerChatMessage item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    [TestClass]
    public class ButlerClass_TrenchCoat_Tests
    {

        [TestMethod]
        public void InitializeAsDefault_TrenchCoat_IfUnSpecified()
        {
            /* main different between this and below is this wants the trenchy */
            InMemoryApiKey dummy = new();
            Butler TestMe = new(dummy, new MockProviderEntryPoint(), null, "stub", Butler.NoApiKey);

            Assert.IsNotNull(TestMe);

            Assert.IsInstanceOfType(TestMe, typeof(Butler));

            Assert.IsInstanceOfType(TestMe.ChatCollection, typeof(TrenchCoatChatCollection));
        }

        [TestMethod]
        public void InitializeAsDefault_TrenchCoat_IfNullFor_ChatHandler_UsesDefaultInstead()
        {
            Butler? TestMe = null;
            InMemoryApiKey dummy = new();
       
                TestMe = new(dummy, new MockProviderEntryPoint(), null, "stub", Butler.NoApiKey, null, null, null);
  

            Assert.IsNotNull(TestMe);

            Assert.IsInstanceOfType(TestMe, typeof(Butler));

            Assert.IsInstanceOfType(TestMe.ChatCollection, typeof(TrenchCoatChatCollection));
        }

        [TestMethod]
        public void Initialize_TrenchCoat_NonDefaultHandler_TypeMatches_NoExceptionRaise()
        {
            
            InMemoryApiKey dummy = new();
            Butler TestMe = new Butler(dummy, new MockProviderEntryPoint(), null, "stub", Butler.NoApiKey,new DummyList(), null, null);

            Assert.IsNotNull(TestMe);

            Assert.IsInstanceOfType(TestMe, typeof(Butler));

            Assert.IsInstanceOfType(TestMe.ChatCollection, typeof(DummyList));
        }
    }
}
