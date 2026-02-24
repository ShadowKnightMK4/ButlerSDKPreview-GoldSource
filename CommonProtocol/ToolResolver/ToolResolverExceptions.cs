using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.Core.Abstractions.ToolResolver
{
    /// <summary>
    /// This exception triggers by <see cref="ToolResolver"/> attempt to run schedule with no tools to run
    /// </summary>
    public class NoToolScheduledException : Exception
    {
        public NoToolScheduledException() : base()
        {

        }
        public NoToolScheduledException(string? message) : base(message)
        {

        }

        public NoToolScheduledException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
