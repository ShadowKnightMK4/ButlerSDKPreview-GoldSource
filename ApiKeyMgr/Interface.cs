using System.Security;
using System;
using System.Reflection;
namespace ButlerSDK.ApiKeyMgr.Contract
{
 
    public interface IButlerVaultKeyCollection: IDisposable
    {
        
        /// <summary>
        /// Initialize your value. Should be called in your constructor. Location is where to store it but its on your code to handle that
        /// </summary>
        /// <param name="location"></param>
        public void InitVault(string location);
        /// <summary>
        /// When given a ID such as "MyCoolKey", return the API key in secure string form
        /// </summary>
        /// <param name="ID"></param>
        /// <returns>Your routine should return null or possibly thru and exception if it can't locate the ID key.</returns>
        public SecureString? ResolveKey(string ID);
        /// <summary>
        /// Authenticate the target
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        public bool Authenticate(Assembly Target);


    }
}
