using ButlerSDK.ApiKeyMgr.Contract;
using System.Reflection;
using System.Security;

namespace ApiKeyMgr
{
    /// <summary>
    /// When you just need to pass a collection of keys that you assemble at run time.
    /// </summary>
    /// <remarks>This does not do anything beyond what your .net env does with SecureString. On Windows, some security. Off Windows? None. Also the shipped facade class (ButlerStarter extension rotines) use this</remarks>
    public class InMemoryApiKey : IButlerVaultKeyCollection
    {
        private bool disposedValue;
        Dictionary<string, SecureString> Keys = new Dictionary<string, SecureString>();

        public bool Authenticate(Assembly Target)
        {
            // nothing to do here.f
            return true;
        }

        public void InitVault(string location)
        {
            // location is not needed. This is created in memory
        }

        public SecureString? ResolveKey(string ID)
        {
            SecureString? ret;
            if (Keys.TryGetValue(ID, out ret))
            {
                return ret.Copy();
            }
            else
            {
                return null;
            }
        }

        public void AddKey(string ID, SecureString Key)
        {
            Keys[ID] = Key;
        }
        public void AddKey(string ID, string key)
        {
            SecureString s = new SecureString();
            foreach (char c in key)
            {
                s.AppendChar(c);
            }
            s.MakeReadOnly();
            Keys[ID] = s;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    foreach (SecureString s in Keys.Values)
                    {
                        s.Dispose();
                    }
                    Keys.Clear();
                    Keys = null!; // its fine
                }


                disposedValue = true;
            }
        }


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
