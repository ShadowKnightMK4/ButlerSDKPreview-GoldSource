using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TheWorks
{
    public class KeyReader
    {
        Dictionary<string, string> Keys = new();
        public string JsonOutput()
        {
            return JsonSerializer.Serialize(Keys);
        }
    }
}
