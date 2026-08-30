using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheWorks
{
    public static class GUI
    {
         static ConsoleColor GeneralMessage = ConsoleColor.White;
         static ConsoleColor OllamaMessage = ConsoleColor.Yellow;
         static ConsoleColor GeminiMessage = ConsoleColor.Green;
         static ConsoleColor OpenaiMessage = ConsoleColor.Cyan;

        static ConsoleColor UserMessage = ConsoleColor.Gray;

        static ConsoleColor ErrorMessage = ConsoleColor.Red;
        static ConsoleColor SystemPromptData = ConsoleColor.DarkRed;

        public static void WriteUserMessage(string text)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = UserMessage;
            Console.WriteLine(text);
            Console.ForegroundColor = old;
        }
        public static void WriteErrorMessage(string text)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ErrorMessage;
            Console.WriteLine(text);
            Console.ForegroundColor = old;
        }

        public static void WriteSystemPrompt(string text)
        {
            var old = Console.ForegroundColor;

            Console.ForegroundColor = SystemPromptData;
            Console.WriteLine(text+"\r\n");
            Console.ForegroundColor = old;
        }
        public static void WriteGeneralLine(string text)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = GeneralMessage;
            Console.WriteLine(text);
            Console.ForegroundColor = old;
        }

        public static void WriteOllamaLine(string text)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = OllamaMessage ;
            Console.WriteLine(text);
            Console.ForegroundColor = old;
        }

        public static void WriteGeminiLine(string text)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = GeminiMessage;
            Console.WriteLine(text);
            Console.ForegroundColor = old;
        }

        public static void WriteOpenAiLine(string text)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = OpenaiMessage;
            Console.WriteLine(text);
            Console.ForegroundColor = old;
        }
    }
}
