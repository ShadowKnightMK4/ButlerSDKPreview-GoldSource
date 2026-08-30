using ApiKeyMgr;
using SecureStringHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace TheWorks
{
    class EmptyEnvKeyException: Exception
    {
        public EmptyEnvKeyException(string message) : base(message)
        {

        }
    }

    class KeyFileNotFoundExceptin: FileNotFoundException
    {
        public KeyFileNotFoundExceptin(string message) : base(message)
        {
        }
        public KeyFileNotFoundExceptin() : base() { }

        public KeyFileNotFoundExceptin(string? message, Exception? innerException) : base(message, innerException) { }
        
    }
    /// <summary>
    /// This exists to consulate varius data flow to the vault interface for butler
    /// </summary>
    public static class Database
    {
        public static readonly InMemoryApiKey KeyCollection = new InMemoryApiKey();
        public static readonly Dictionary<string, string> CrossRef = new();

        public static SecureString? Lookup(string ct)
        {
            if (CrossRef.TryGetValue(ct, out string? id))
            {
                if (id is null)
                {
                    throw new InvalidOperationException();
                }
                return KeyCollection.ResolveKey(ct);
            }
            return null;
        }
        static void AddProviderFromEnv(string provider_name, string keyname)
        {
            CrossRef.Add(provider_name, keyname);
            var ss = new SecureString();
            var reader = Environment.GetEnvironmentVariable(keyname);
            if (string.IsNullOrEmpty(reader))
            {
                throw new EmptyEnvKeyException($"{provider_name} key empty. Requires non empty enviromental key");
            }
            KeyCollection.AddKey(provider_name, reader);

        }
        public static void AddOpenAiFromEnv(string keyname)
        {
            AddProviderFromEnv("OPENAI", keyname);
        }

        
        public static void AddGeminiFromEnd(string keyname)
        {
            AddProviderFromEnv("GEMINI", keyname);
        }

        static SecureString KeyFromFile(string local)
        {
            SecureString x = new();
            string y = File.ReadAllText(local);
            if (y is not null)
                x.AssignStringThenReadOnly(y);
            else
                throw new InvalidOperationException();
            return x;
        }

        static void AddProviderFromFile(string PROVIDER, string location)
        {
            if (location.Contains(':'))
            {
                CrossRef.Add(PROVIDER, location);
                try
                {
                    KeyCollection.AddKey(PROVIDER, KeyFromFile(location));
                }
                catch (FileNotFoundException e)
                {
                    throw new KeyFileNotFoundExceptin(location, e);
                }
            }
            else
            {
                CrossRef.Add(PROVIDER, location);
                string p = Path.Combine(Directory.GetCurrentDirectory(), location);
                if (File.Exists(p))
                {
                    KeyCollection.AddKey(PROVIDER, KeyFromFile(p));
                }
                else
                {
                    throw new KeyFileNotFoundExceptin(location);
                }
            }
        }
        /// <summary>
        /// load from a file. As a anti github suck up the key commit thing, this *assumes* current user directory which is probably not in the repo UNLESS you do semi : qualified path
        /// </summary>
        /// <param name="location"></param>
        public static void AddOpenAiFromFile(string location)
        {
            AddProviderFromFile("OPENAI", location);
        }

        public static void AddGeminiFromFile(string location)
        {
            AddProviderFromFile("GEMINI", location);
        }

    }
}
