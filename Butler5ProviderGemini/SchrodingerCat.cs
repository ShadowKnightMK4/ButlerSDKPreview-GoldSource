using GenerativeAI;
using GenerativeAI.Types;
using GenerativeAI.Types.RagEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.Provider.Gemini
{
    /*
     * GeminiProvider uses this to keep calls to gemini GoogleAI vs VertexAI on the same code path. It is internal for a reason markeed and can change between releases as the provider changes.
     */ 
    internal interface SchrodingerCat
    {
        GenerativeModel CreateGenerativeModel(string modelName, GenerationConfig? config = null, ICollection<SafetySetting>? safetyRatings = null, string? systemInstruction = null, string? corpusIdForRag = null, RagRetrievalConfig? ragRetrievalConfig = null);
        public Task<ListModelsResponse> ListModelsAsync(int? pageSize = null, string? pageToken = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
