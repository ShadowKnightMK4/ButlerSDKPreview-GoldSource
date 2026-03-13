using ApiKeyMgr;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ButlerSDK.ApiKeyMgr;
using ButlerSDK.ApiKeyMgr.Contract;
using SecureStringHelper;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security;

namespace ButlerSDK.ApiKeyMgr.AzureVault
{
    public class AzureKeyVault : IButlerVaultKeyCollection
    {
        bool IsAuthenticated = false;
        SecretClient? RemoteSecret;
        InMemoryApiKey? Cache;
        /// <summary>
        /// Add extra auth here.
        /// </summary>
        /// <param name="Target">who is asking</param>
        /// <returns></returns>
        public virtual bool Authenticate(Assembly Target)
        {

            if (Cache is null)
            {
                Cache = new InMemoryApiKey();
                Cache.Authenticate(Assembly.GetExecutingAssembly());
            }
            IsAuthenticated = true;
            return true;
        }

        public void Dispose()
        {
            if (Cache is not null)
            {
                Cache.Dispose();
                Cache = null;
            }
        }

        /// <summary>
        /// this is called by <see cref="InitVault(string)"/> to give a hook to adjust ect....
        /// </summary>
        protected virtual void PostInitVault()
        {

        }


        /// <summary>
        /// Create your credential to use here such as <see cref="DefaultAzureCredential"/>, 
        /// </summary>
        /// <returns></returns>
        protected virtual TokenCredential InitializeCredential()
        {
            var cli_redz = new EnvironmentCredentialOptions();
            // "273f21a8-b205-4bd2-b993-f07919d5d4c4";

            return new EnvironmentCredential(cli_redz);
        }
        /// <summary>
        /// The Azure vault wants the URL to look at.
        /// </summary>
        /// <param name="location"></param>
        public void InitVault(string location)
        {
            RemoteSecret = new SecretClient(new Uri(location), InitializeCredential());
            PostInitVault();
        }

        /// <summary>
        /// implement the remote call to get the key
        /// </summary>
        /// <param name="ID"></param>
        /// <exception cref="Azure.RequestFailedException">Is thrown if the Azure call throws it. </exception>
        protected virtual SecureString? RemoteResolveKey(string ID)
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("Call Authenticate first");
            }
            if (RemoteSecret is null)
            {
                throw new InvalidOperationException("Somehow the Azure Secret object is null. This don't happen normally.");
            }
            SecureString? ret = null;
            try
            {
                string tmp =  RemoteSecret.GetSecret(ID).Value.Value;
                ret = new SecureString();
                ret.AssignStringThenReadOnly(tmp);
            }
            catch (Azure.RequestFailedException)
            {
                throw;
            }
            return ret;
        }

        public SecureString? ResolveKey(string ID)
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("Call Authenticate first");
            }

            if (Cache is  null)
            {
                Cache = new InMemoryApiKey();
                Cache.Authenticate(Assembly.GetExecutingAssembly());    
            }
            SecureString? ret = this.Cache.ResolveKey(ID);

            if (ret is null)
            {
                ret =  RemoteResolveKey(ID);
                if (ret is not null)
                {
                    Cache.AddKey(ID, ret);
                    return ret.Copy();
                }
            }
            return ret;
        }
        
    }
}
