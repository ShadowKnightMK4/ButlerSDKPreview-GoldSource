using System.Runtime.CompilerServices;

namespace TheWorks
{
    internal class Program
    {
        enum TestingMode
        {
            None = 0,
            OllamaConnect = 1,
            All = OllamaConnect
        }


        static TestingMode FinalMode = TestingMode.None;
        static readonly Dictionary<string, TestingMode> CmdArgs = new();

        const string ollama_init = "-olama";
        const string all_tests = "-all";
        static void preinit()
        {
            CmdArgs.Add(ollama_init.ToLowerInvariant(), TestingMode.OllamaConnect);
            CmdArgs.Add(all_tests.ToLowerInvariant(), TestingMode.All);
        }

        static void test_dispatch()
        {
            if (FinalMode.HasFlag(TestingMode.OllamaConnect) || FinalMode.HasFlag(TestingMode.All))
            {
                OllamaTest.GladosMode();
            }
        }
        static void Main(string[] args)
        {
            preinit();
            Console.WriteLine("BEGINING BUTLERSDK the works test");
            Console.WriteLine("The works is ment to test DOES THE CHAT THING WORK");
            
            for (int i = 0; i < args.Length; i++)
            {
                string step = args[i].ToLowerInvariant();
                if (CmdArgs.TryGetValue(step, out TestingMode testingMode))
                {
                    FinalMode |= testingMode;
                }
            }

            if (FinalMode == TestingMode.None)
            {
                Console.WriteLine("No tests selected. Running all!");
                FinalMode = TestingMode.All;
            }

            test_dispatch();


            Console.WriteLine("TESTING Ollama Provider connection. If you don't have Ollama installed, this test won't work");
        }
    }
}
