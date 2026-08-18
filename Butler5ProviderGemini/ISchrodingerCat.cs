using GenerativeAI;
using GenerativeAI.Types;
using GenerativeAI.Types.RagEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.Gemini;
namespace ButlerSDK.Providers.Gemini
{

    /// <summary>
    /// Internal adapter interface that allows <see cref="ButlerGeminiProvider"/>
    /// to share a single implementation path between AI Studio and Vertex AI backends.
    /// </summary>
    /// <remarks>Important: Do not depend on this to be the same between Gemini Provider builds for butler. This interface intentionally not exposed outside the the Gemini Provider boundary.</remarks>
    internal interface ISchrodingerCat
    {
        /*This interface exists purely to unify Butler Gemini Provider  backend SDK differences so that all translation,
         * tool handling, and streaming logic is 
         * implemented once and shared across AI Studio and Vertex AI.*/
        GenerativeModel CreateGenerativeModel(string modelName, GenerationConfig? config = null, ICollection<SafetySetting>? safetyRatings = null, string? systemInstruction = null, string? corpusIdForRag = null, RagRetrievalConfig? ragRetrievalConfig = null);
        public Task<ListModelsResponse> ListModelsAsync(int? pageSize = null, string? pageToken = null, CancellationToken cancellationToken = default(CancellationToken));
    }

    internal class SchrodingerCat: ISchrodingerCat
    {
        readonly VertexAI? _VertexMode;
        readonly GoogleAi? _ApiKeyMode;
        public GenerativeModel CreateGenerativeModel(string modelName, GenerationConfig? config = null, ICollection<SafetySetting>? safetyRatings = null, string? systemInstruction = null, string? corpusIdForRag = null, RagRetrievalConfig? ragRetrievalConfig = null)
        {
            if (_ApiKeyMode is not null)
            {
                return _ApiKeyMode.CreateGenerativeModel(modelName, config, safetyRatings, systemInstruction);
            }
            else
            {
                if (_VertexMode is not null)
                {
                    return _VertexMode.CreateGenerativeModel(modelName, config, safetyRatings, systemInstruction);
                }
                else
                {
                    throw new InvalidOperationException("Gemini provide config is in unsupported mode. Check the SchrodingerCat code path");
                }
            }
        }

        public Task<ListModelsResponse> ListModelsAsync(int? pageSize = null, string? pageToken = null, CancellationToken cancellationToken = default)
        {
            if (_ApiKeyMode is not null)
            {
                return _ApiKeyMode.ListModelsAsync(pageSize, pageToken, cancellationToken);
            }
            else
            {
                if (_VertexMode is not null)
                {
                    return _VertexMode.ListModelsAsync(pageSize, pageToken, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("Gemini provide config is in unsupported mode. Check the SchrodingerCat code path");
                }
            }
        }

        public SchrodingerCat(GoogleAi ApikeyMode)
        {
            this._ApiKeyMode = ApikeyMode;
            this._VertexMode = null;
        }

        public SchrodingerCat(VertexAI Vertex)
        {
            this._ApiKeyMode = null;
            this._VertexMode = Vertex;
        }
    }
}
