using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ApiKeys;
using ButlerSDK.ApiKeyMgr.Contract;
using SecureStringHelper;
namespace ButlerSDK.ApiKeyMgr
{


    /// <summary>
    /// Load and cache API keys from a files with matching name in SecureString
    /// </summary>
    public class FileApiKeyMgr : IButlerVaultKeyCollection
    {
        readonly Dictionary<string, SecureString> _keys = new();
        public bool Authenticate(Assembly Target)
        {
            return true;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);  
        }

        public void InitVault(string location)
        {
            
        }

        public SecureString? ResolveKey(string ID)
        {
#pragma warning disable CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method
            if (_keys.ContainsKey(ID))
            {
                return _keys[ID];
            }
            else
            {
                SecureString ret = new();
                
                string? key = ApiKeyFolder.ReadKey(ID);
                if (key is not null)
                {
                    ret.AssignStringThenReadyOnly(key);

                    _keys[ID] = ret;
                    return ret;
                }
                return null;
            }
#pragma warning restore CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method
        }
    }

#if DEBUG
    [Obsolete("DO NOT USE THIS IN DEPLOYMENT RELEASE")]
    /// <summary>
    /// DO NOT USE IN DEPLOYMENT. This is a quick and direct way of passing defined API  keys that will eventually lead a 'vault' of keys for the various things BUTLERSDK uses
    /// </summary>
    public class TestingApiKeyMgr : IButlerVaultKeyCollection
    {
        readonly string vaultKey = ApiKeyFolder.GetLocation();
        public TestingApiKeyMgr() 
        {
            
        }
        
        /// <summary>
        /// TESTING ONLY. This checks if we are ourselves (I.e. current domain == target)
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        public bool Authenticate(Assembly Target)
        {
            return Assembly.GetExecutingAssembly().GetName() == Target.GetName();
        }

        public void Dispose()
        {
            GC.SuppressFinalize (this); 
        }

        public void InitVault(string location)
        {
            
        }

         

        
        public SecureString? ResolveKey(string ID)
        {
            SecureString ret = new();
            string? Assigned ;
            switch (ID)
            {
                case "OPENAI":
                    {
                        Assigned = ApiKeyFolder.ReadKey("OPENAI.KEY");
                        break;
                    }
                case "AZUREMAPS":
                    {
                        Assigned = ApiKeyFolder.ReadKey("AZUREMAPS.KEY");
                        break;
                    }
                default:
                    return null;
            }

            ret.AssignStringThenReadyOnly(Assigned!);
            return ret;
        }
    }
#else
    public class TestingApiKeyMgr : IButlerVaultKeyCollection
    {
        public bool Authenticate(Assembly Target)
        {
            throw new InvalidOperationException(
                $@"TestingApiKeyMgr is for DEBUG/testing only and must not be used in release builds. Use WindowsVault or provide a production {nameof(IButlerVaultKeyCollection)} implementation."
                );


        }

        public void Dispose()
        {
            throw new InvalidOperationException(
                $@"TestingApiKeyMgr is for DEBUG/testing only and must not be used in release builds. Use WindowsVault or provide a production {nameof(IButlerVaultKeyCollection)} implementation."
                );
        }

        public void InitVault(string location)
        {
            throw new InvalidOperationException(
                $@"TestingApiKeyMgr is for DEBUG/testing only and must not be used in release builds. Use WindowsVault or provide a production {nameof(IButlerVaultKeyCollection)} implementation."
                );
        }

        public SecureString? ResolveKey(string ID)
        {
            throw new InvalidOperationException(
                $@"TestingApiKeyMgr is for DEBUG/testing only and must not be used in release builds. Use WindowsVault or provide a production {nameof(IButlerVaultKeyCollection)} implementation."
                );
        }
    }
#endif
}
