using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerLLMProviderPlatform.DataTypes
{
    public enum ButlerClientResultType
    {
        String,
        Image
    }
    /// <summary>
    /// Used by Butler5 to get resulst from a non streamed call to a chat client
    /// </summary>
    public interface IButlerClientResult
    {
        public string? GetResult();
        public byte[]? GetBytes();

        public ButlerClientResultType GetResultType();

    }
}
