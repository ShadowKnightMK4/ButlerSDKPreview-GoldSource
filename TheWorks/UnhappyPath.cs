using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheWorks
{
    public class UnhappyPath
    {
        public static void Fail(string message)
        {
            Console.WriteLine(message);
            Environment.Exit(-9999);
        }
        public static void Fail(string message, Exception exceptionTrigger)
        {
            Console.WriteLine(message);
            throw exceptionTrigger;
        }
    }
}