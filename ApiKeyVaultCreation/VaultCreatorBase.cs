using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiKeyVaultCreation
{
    public abstract class VaultCreatorBase : IDisposable

    {
        public struct VersionData
        {
            /// <summary>
            /// The Daisy is a Hash512 of all the api keys smooshed together with there name (for example openai, gemini ect)
            /// </summary>
            public byte[] DaisyChain { get; set; }
            /// <summary>
            /// Number of keys stored. Note code should complain on decrypt if not matching.
            /// </summary>
           public int NumberOfEntries { get; set; }
        }

        ZipArchive? Arch;
        MemoryStream inmem;
        SaveMode TargetMode = SaveMode.EncryptToLocalUser;
        public VaultCreatorBase()
        {
            inmem = new MemoryStream();
            Arch = new ZipArchive(inmem, ZipArchiveMode.Create, true);

        }
        public VaultCreatorBase(SaveMode Mode): this()
        {
            TargetMode = Mode;
        }

        /// <summary>
        /// The chain is essential a sha 512 hash of ALL the keys & the name
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>

        byte[] CreateKeysDaisyChain(Dictionary<string, string> keys)
        {
            StringBuilder smoosh = new();
            foreach (var entry in keys)
            {
                smoosh.Append(entry.Key);
                smoosh.Append(entry.Value);
            }
            var MyHash = SHA512.HashData(Encoding.Unicode.GetBytes(smoosh.ToString()));
            // make the version.dat that gets storing the data.
            return MyHash;
        }

  
        void WriteEntryToArchive(ZipArchiveEntry Arch, string s)
        {
            using (var str = Arch.Open())
            {
                var bytes = Encoding.UTF8.GetBytes(s);

                // encode them
                var ebytes = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);

                str.Write(ebytes, 0, ebytes.Length);
            }
        }

        public enum SaveMode
        {
            // DO NOT ENCRYPT MEMORY STRIVE
            PassThru = 0,
            /// <summary>
            /// <see cref="DataProtectionScope.CurrentUser"/>
            /// </summary>
            EncryptToLocalUser = 1,
            /// <summary>
            /// <see cref="DataProtectionScope.LocalMachine"/>
            /// </summary>
            EncryptToLocalMachine = 2,
            /// <summary>
            /// Insert a file named "VERSION.DAT" in the system.
            /// </summary>
            AddVersioningData = 4

        }


        /// <summary>
        /// BYOE Bring your Own Encrpytion. Long as it matches <see cref="ProtectedData"/> function style
        /// </summary>
        /// <param name="Data">Data to encrypt in byte form</param>
        /// <param name="Entropy">Optional entrophy.</param>
        /// <param name="Mode">Target local machine or user or mode</param>
        /// <returns>encrypted data</returns>
        /// <remarks>This depends on how child class implements.</remarks>
        protected abstract byte[] EncryptBytes(byte[] Data, byte[]? Entropy, SaveMode Mode);

        void SetVersionDataFile(VersionData VerData)
        {
            if (Arch is null)
            {
                Arch = new ZipArchive(inmem, ZipArchiveMode.Create, true);
            }
            
            var V_Arch = Arch.CreateEntry("VERSION.DAT");

            using (var Entry = V_Arch.Open())
            {
                string VersionString = JsonSerializer.Serialize(VerData);
                Entry.Write(Encoding.Unicode.GetBytes(VersionString));
            }
        }
        public void SaveVaultToDisk(string location, SaveMode Mode, VersionData? VerData = null)
        {
            Arch?.Dispose();
            Arch = null;
            if (Mode == SaveMode.PassThru)
            {
                inmem.Position = 0;
                using (var Output = File.OpenWrite(location))
                {

                    inmem.Position = 0;
                    inmem.Flush();
                    inmem.Position = 0;
                    inmem.CopyTo(Output);
                    Output.Flush();
                }
            }
            if (Mode.HasFlag(SaveMode.EncryptToLocalMachine))
            {

                inmem.Position = 0;

                var Bytes = inmem.ToArray();
                Bytes = ProtectedData.Protect(Bytes, null, DataProtectionScope.LocalMachine);
                using (var Output = File.OpenWrite(location))
                {

                    Output.Write(Bytes);
                }
            }
        }

 
        /// <summary>
        /// For clearifiac
        /// </summary>
        /// <param name="keys"> Dictionary(reference , actually key)</param>
        public void AddKeys(Dictionary<string, string> keys)
        {
            foreach (string key in keys.Keys)
            {
                if (Arch is null)
                {
                    Arch = new ZipArchive(inmem, ZipArchiveMode.Create, true);
                }
                var NewEntry = Arch.CreateEntry(key, CompressionLevel.Optimal);
                WriteEntryToArchive(NewEntry, keys[key]);
            }
        }

        public void AddKeys(string Source, out VersionData VersionInfo)
        {
            Dictionary<string, string> Data = new Dictionary<string, string>();
            var Info = Directory.GetFiles(Source, "*.KEY");
            foreach (string file in Info)
            {
                Data[Path.GetFileNameWithoutExtension(file)] = string.Empty;
                Data[Path.GetFileNameWithoutExtension(file)] = File.ReadAllText(file);
            }
            AddKeys(Data);
            VersionInfo = new();
            VersionInfo.NumberOfEntries = Info.Length;
            VersionInfo.DaisyChain = CreateKeysDaisyChain(Data);
        }

        public void Dispose()
        {

        }
    }
}
