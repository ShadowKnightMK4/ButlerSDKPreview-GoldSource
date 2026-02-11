using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public class ModuleNotFoundException: Exception 
    {
        public ModuleNotFoundException() { }
        public ModuleNotFoundException(string message) : base(message) { }
        public ModuleNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
