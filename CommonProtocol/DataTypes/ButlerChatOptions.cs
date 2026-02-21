using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
namespace ButlerToolContract.DataTypes
{
    // In your ButlerLLMProviderPlatform project...



    public class ButlerChatCompletionOptions : IButlerChatCompletionOptions
    {
        // --- Core Creativity & Control ---
        public int? MaxOutputTokenCount { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public IList<string> StopSequences { get; } = new List<string>();

        public IList<IButlerToolBaseInterface> Tools { get; } = new List<IButlerToolBaseInterface>();
        public ButlerChatToolChoice? ToolChoice { get; set; } // Your own agnostic class/enum

        // --- Penalties & Reproducibility ---
        public double? FrequencyPenalty { get; set; }
        public double? PresencePenalty { get; set; }
        //public int? Seed { get; set; }

        // --- Output & User ---
        //public ButlerResponseFormat? ResponseFormat { get; set; } // Your own agnostic enum
        public string? EndUserId { get; set; }

        public bool? AllowParallelToolCalls { get; set; }
        public int? Seed { get; set; }
       
    }
}
