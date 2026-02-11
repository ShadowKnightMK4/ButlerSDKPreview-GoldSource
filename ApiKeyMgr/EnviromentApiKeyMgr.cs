using ButlerSDK.ApiKeyMgr.Contract;
using SecureStringHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ApiKeyMgr
{

    /// <summary>
    /// Read environment variables for tools and LLMs.
    /// </summary>
    public class EnvironmentApiKeyMgr : IButlerVaultKeyCollection
    {
        bool isAuthenticated = false;
        private bool disposedValue;

        public bool Authenticate(Assembly Target)
        {
            isAuthenticated = true;
            return true;// we're running, it's authenticated
        }

        /// <summary>
        /// Placeholder, the current design throws exception if not authenticated but does nothing with the argument
        /// </summary>
        /// <param name="location"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void InitVault(string location)
        {
            if (!isAuthenticated)
            {
                throw new InvalidOperationException("Call Authenticate first");
            }
            // nothing to init, we're pulling from environment
        }

        public SecureString? ResolveKey(string ID)
        {
            if (!isAuthenticated)
            {
                throw new InvalidOperationException("Call Authenticate first");
            }
            string? val = Environment.GetEnvironmentVariable(ID);
            if (val is null)
            {
                val = Environment.GetEnvironmentVariable(Path.GetFileNameWithoutExtension(ID));
            }
            if (val is not null)
            {
                SecureString ret = new SecureString();
                ret.AssignStringThenReadyOnly(val);
                val = null;
                return ret;
            }
            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~EnvironmentApiKeyMgr()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
