using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public class ButlerStreamingChatCompletionUpdate
    {
       
        public bool IsEmpty()
        {
            if ((ActualContent is null) || (ActualContent.Count == 0))
            {
                if ((ActualToolCalls is null) || (ActualToolCalls.Count == 0))
                {
                    if (this.FinishReason is null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// True live content
        /// </summary>
        private List<ButlerChatStreamingPart> ActualContent = new();
        private List<ButlerStreamingToolCallUpdatePart> ActualToolCalls = new();

        /// <summary>
        /// Editorable content translator  routines can use to adjust the content
        /// </summary>
        public IList<ButlerChatStreamingPart> EditorableContentUpdate { get =>ActualContent;}
        public IList<ButlerStreamingToolCallUpdatePart> EditableToolCallUpdates { get =>ActualToolCalls; }
        /// <summary>
        /// The one butler5 uses.
        /// </summary>
        public IReadOnlyList<ButlerChatStreamingPart> ContentUpdate { get => ActualContent; }
        public ButlerChatFinishReason? FinishReason { get; set; }
        public IReadOnlyList<ButlerStreamingToolCallUpdatePart> ToolCallUpdates { get => ActualToolCalls; }
        public string? FunctionName { get; set; }
        public string? FunctionArgumentsUpdate { get; set; }
        public string? Id { get; set; }
        public int Index { get; set; }

        public string? CompletionId { get; set; }
        public DateTimeOffset CreatedAt {  get; set; }
        public string? Model { get; set; }

        public string? RefusalUpdate {  get; set; }

        public ButlerChatMessageRole? Role { get; set; }
        public string? SystemFingerprint;

        /// <summary>
        /// for when provider needs something explitic. Other providers can safely ignore if not theres.
        /// </summary>
        public Dictionary<string, string> ProviderSpecificComponents { get; } = new();
    }
}
