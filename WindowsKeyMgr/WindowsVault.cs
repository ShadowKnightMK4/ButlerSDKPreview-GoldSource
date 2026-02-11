using ApiKeys;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using ButlerSDK.ApiKeyMgr.Contract;
namespace ButlerSDK.ApiKeyMgr.WindowsVault
{
    public class WindowsVault : IButlerVaultKeyCollection
    {
        private bool disposedValue;
        MemoryStream keyVaultStream = new MemoryStream();
        ZipArchive? KeyVault;
        bool IsAuthorized=false;
        public bool Authenticate(Assembly Target)
        {
            IsAuthorized = false;
            if (Target is null)
                return false;
            var OtherId = Target.GetName().GetPublicKey();
            var Self = Assembly.GetExecutingAssembly();
            var MySignedId = Self.GetName().GetPublicKeyToken();
            if (OtherId == null)
            {
#if DEBUG
                Debug.WriteLine("WARNING: Called from unsigned target. DEBUG VERSION WILL LET IT THRU BUT RELEASE WILL KICK OUT");
                return true;
#else
                throw new InvalidOperationException("Attempt to use key vault while target is not matching signature");
#endif
            }
            if (MySignedId == null)
            {
#if DEBUG
                throw new InvalidOperationException("DEV: ensure the assembly having WindowsKeyManager class is signed.  That sign is the door ");
#else
                throw new InvalidOperationException("Attempt to use key vault while target is not signed.");
#endif
            }
            else
            {
                if (MySignedId.SequenceEqual(OtherId))
                {
                    IsAuthorized = true;
                    return true;
                    
                }
                return false;
            }
        }


   
        public void InitVault(string location)
        {
            if (!IsAuthorized)
            {
                throw new InvalidOperationException("Error: Unauthenticated vault");
            }
            if (File.Exists(location))
            {


                // write our assumingly protected data to memory
                byte[] data = File.ReadAllBytes(location);

                // dump in the memory buffer
                keyVaultStream.Write(data, 0, data.Length);
                keyVaultStream.Position = 0;
    
          

                {
                    // reset memory buffer to pl
                    //keyVaultStream.Position = HeaderId.Length-1;
                    keyVaultStream.Write(ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine));
                    keyVaultStream.Position = 0;
                    // reset to zero and end up with zip archive ready for business
                    KeyVault = new ZipArchive(keyVaultStream, ZipArchiveMode.Read);
                }
            }
            else
            {
                throw new FileNotFoundException($"Key vault file not found at location: {location}");

            }
        }

        /// <summary>
        /// returns the zip stream archive of the stream (if supporting zip ok) and null if not
        /// </summary>
        /// <param name="ID"></param>
        /// <returns>null if init issue OR if archive entry not found</returns>
        ZipArchiveEntry? FetchArchive(string ID)
        {
            ZipArchiveEntry? entry = null;
            if (KeyVault is not null)
            {
                entry = KeyVault.GetEntry(ID);
                return entry;
            }
            else
            {
                return null;
            }
        }
        

        public SecureString? ResolveKey(string ID)
        {
            if (!IsAuthorized)
            {
                throw new InvalidOperationException("Error: Unauthenticated vault");
            }
            if (this.KeyVault is null)
            {
                throw new InvalidOperationException("Error: Supporting data stream for memory is null");
            }
            /*
             * Our generally plan is 
             * take our zip stream -> goto archive entry[ID] => read that to secure string -> return that 
             */
            SecureString? key = new SecureString();
            var Arch = FetchArchive(ID);
            if (ID.EndsWith(".KEY", StringComparison.OrdinalIgnoreCase))
            {
                Arch = FetchArchive(ID.Substring(0, ID.Length - 4));
            }
            if (Arch == null)
                return null;
            else
            {
                List<byte> Data = new();
                using (var OpenArch = Arch.Open())
                {
                    int c = 0;
                    while (c != -1)
                    { 
                        c = OpenArch.ReadByte();
                        if (c is not -1)
                        {
                            Data.Add((byte)c);
                        }
                    }

                    // decrypt
                    var decrypt = ProtectedData.Unprotect(Data.ToArray(), null, DataProtectionScope.LocalMachine);
                    for (int i = 0; i < decrypt.Length; i++)
                    {
                        key.AppendChar((char) decrypt[i]);
                    }

                }

                    key.MakeReadOnly();
                return key;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WindowsKeyManager()
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
