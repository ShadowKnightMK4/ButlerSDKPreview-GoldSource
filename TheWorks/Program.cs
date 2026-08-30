using System.Runtime.CompilerServices;
using TheWorks.GladosTest;
using TheWorks.GladosTest.TheWorks.GladosTest;

namespace TheWorks
{
    internal class Program
    {
        enum TestingMode
        {
            None = 0,
            OllamaConnect = 1,
            GeminiConntext = 2,
            OpenaiConntext = 4,
            CrossRefTest = 8,
            All = OllamaConnect | GeminiConntext | OpenaiConntext | CrossRefTest
        }


        static TestingMode FinalMode = TestingMode.None;
        static readonly Dictionary<string, TestingMode> CmdArgs = new();

        const string ollama_init = "-olama";
        const string all_tests = "-all";
        const string gemini_tests = "-gemini";
        const string openai_tests = "-openai";

        const string crossref_tests = "-crossref_tests";
        const string enum_models = "-enum_models";
        /// <summary>
        /// load the openai key from i+1 where i is the command line following this
        /// </summary>
        const string KEY_SOURCE_OPENAI_ENV = "-envsource_openai";
        /// <summary>
        /// load the gemini key from i+1 where i is the command line following this
        /// </summary>

        const string KEY_SOURCE_GEMINI_ENV = "-envsource_gemini";

        const string KEY_SOURCE_LOCAL_FILE_OPEN = "-filesource_openai";
        const string KEY_SOURCE_LOCAL_FILE_GEM = "-filesource_gemini";
        static void preinit()
        {
            CmdArgs.Add(ollama_init.ToLowerInvariant(), TestingMode.OllamaConnect);
            CmdArgs.Add(all_tests.ToLowerInvariant(), TestingMode.All);
            CmdArgs.Add(openai_tests.ToLowerInvariant(), TestingMode.OpenaiConntext);
            CmdArgs.Add(gemini_tests.ToLowerInvariant(), TestingMode.GeminiConntext);
        }

        static void test_dispatch()
        {
            /*
            if (FinalMode.HasFlag(TestingMode.OllamaConnect) || FinalMode.HasFlag(TestingMode.All))
            {
                OllamaTest.GladDosMode(Database.KeyCollection);
            }

            if (FinalMode.HasFlag(TestingMode.GeminiConntext) || FinalMode.HasFlag(TestingMode.All))
            {
                GeminiTest.GladDosMode(Database.KeyCollection);
            }

            if (FinalMode.HasFlag(TestingMode.OpenaiConntext) || FinalMode.HasFlag(TestingMode.All))
            {
                OpenAiTest.GladDosMode(Database.KeyCollection);
            }*/
            if (FinalMode.HasFlag(TestingMode.CrossRefTest) || (FinalMode.HasFlag(TestingMode.All)))
            {
                CommonGroundGladosTest.Gladdos_CrossLLMTest(Database.KeyCollection);
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
                else
                {
                    if ((i + 1) < args.Length)
                    {
                        switch (step)
                        {
                            case KEY_SOURCE_GEMINI_ENV:
                                {
                                    i++;
                                    Console.WriteLine($"Sourcing GEMINI KEY From Enviroment variable: {args[i].ToLowerInvariant()}");
                                    try
                                    {
                                        Database.AddGeminiFromEnd(args[i].ToLowerInvariant());
                                    }
                                    catch (TheWorks.EmptyEnvKeyException e)
                                    {
                                        Console.WriteLine(e.Message);
                                        Console.WriteLine("Errror: Unable to run Gemini Test");
                                        Program.FinalMode = (TestingMode)(Program.FinalMode - TestingMode.GeminiConntext);
                                    }
                                    break;
                                }
                            case KEY_SOURCE_OPENAI_ENV:
                                {
                                    i++;
                                    Console.WriteLine($"Sourcing OPENAI KEY From Enviroment variable: {args[i].ToLowerInvariant()}");
                                    Database.AddOpenAiFromEnv(args[i].ToLowerInvariant());
                                    break;
                                }
                            case KEY_SOURCE_LOCAL_FILE_GEM:
                                {
                                    i++;
                                    Console.WriteLine($"Sourcing GEM KEY From disk file: {args[i].ToLowerInvariant()}");
                                    try
                                    {
                                        Database.AddGeminiFromFile(args[i].ToLowerInvariant());
                                    }
                                    catch (KeyFileNotFoundExceptin)
                                    {
                                        Console.WriteLine($"Error Unable to result the GEMINI key file to {args[i]}");
                                        Program.FinalMode = (TestingMode)(Program.FinalMode - TestingMode.GeminiConntext);
                                    }
                                    break;
                                }
                            case KEY_SOURCE_LOCAL_FILE_OPEN:
                                {
                                    i++;
                                    Console.WriteLine($"Sourcing GEM KEY From disk file: {args[i].ToLowerInvariant()}");
                                    try
                                    {
                                        Database.AddOpenAiFromFile(args[i].ToLowerInvariant());
                                    }
                                    catch (KeyFileNotFoundExceptin)
                                    {
                                        Console.WriteLine($"Error Unable to result the OPENAI key file to {args[i]}");
                                        Program.FinalMode = (TestingMode)(Program.FinalMode - TestingMode.GeminiConntext);
                                    }
                                    break;
                                }
                        }
                    }
                }
            }

            if (FinalMode == TestingMode.None)
            {
                Console.WriteLine("No tests selected. Running all!");
                FinalMode = TestingMode.All;
            }

            test_dispatch();


    
        }
    }
}
