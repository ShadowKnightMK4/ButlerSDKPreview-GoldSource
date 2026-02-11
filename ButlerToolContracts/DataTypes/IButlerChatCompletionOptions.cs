using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public enum ButlerChatToolChoice
    {
        None,
        Auto,
        Required
    }
    /// <summary>
    /// The ChatCompletionOptions.
    /// Your class should implemeent this 
    /// </summary>
    public interface IButlerChatCompletionOptions
    {
        // --- Core Creativity & Control ---
        public int? MaxOutputTokenCount { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public IList<string> StopSequences { get; }

        // --- Tooling --- IMPORTANT. This should be a Interface to IButlerToolBase
        public IList<IButlerToolBaseInterface> Tools { get; }
        public ButlerChatToolChoice? ToolChoice { get; set; } // Your own agnostic class/enum

        // --- Penalties & Reproducibility ---
        public double? FrequencyPenalty { get; set; }
        public double? PresencePenalty { get; set; }
        public int? Seed { get; set; }

        // --- Output & User ---
        //public ButlerResponseFormat? ResponseFormat { get; set; } // Your own agnostic enum
        public string? EndUserId { get; set; }
       
        bool? AllowParallelToolCalls { get; set; }

    }

}
