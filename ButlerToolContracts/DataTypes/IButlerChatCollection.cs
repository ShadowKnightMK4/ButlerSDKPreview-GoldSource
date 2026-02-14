using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContracts.DataTypes
{

    public interface IButlerTrenchImplementation : IButlerChatCollection
    {
        public void AddPostToolCallFollowup(IReadOnlyList<(string CallID, IButlerToolBaseInterface Tool)> ToolsUsed, string MessageTemplate);
        public void RemoveTemporaryMessages();

        public void AddPromptInjectionMessage(ButlerChatMessage message, IButlerToolPromptInjection Source);
        public void ClearPromptInjections();
        public int CountSystemMessages { get; }
        public int CountPromptInjection { get; }
        public int CountRunningContextWindow { get; }
    }

    public interface IButlerChatCollection : INotifyPropertyChanged, IList<ButlerChatMessage>
    {


        public void AddUserMessage(string text);
        public void AddToolMessage(string CallID, string text);

        public void AddSystemMessage(string text);

        IReadOnlyList<ButlerChatMessage> GetSliceOfMessages(int LastUserMessageIndex, int LastAiTurnIndex);

    }
}
