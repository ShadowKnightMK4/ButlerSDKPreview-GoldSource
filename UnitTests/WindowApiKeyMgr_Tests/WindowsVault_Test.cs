

/*
 * This test suit might need to be setup first.
 * It test WindowsKeyManager functionality.
 * 
 * That class must be tested on Windows OS only.
 * We need to be able see if we can:
 * 
 * read the magic id of the memory stream the (MZ) (currenty O'B') of the thing.
 * 
 * Encrpyted file must be able to be decrypted.
 * 
 * We must be able to present keys as requested via ResolveKey
 * we most be able to work without issue and be somewhat fast.
 * 
 * Code wise:
 * we read the encrypted data:
 * memorystream it.
 * Decrypt it.
 * use that to go for keeping a zip archive running
 * 
 * StretchGoal
 * WindowsKeyManager() gets a small IFileInterface that lets just enough be mocked for unit testing
 * 
 */


using ApiKeyVaultCreation;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.ApiKeyMgr.WindowsVault;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using static System.Formats.Asn1.AsnWriter;

namespace WindowApiKeyMgr_Tests
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void CreateInstance_IsNotNull()
        {
            var TestMe = new WindowsVault();
            Assert.IsNotNull(TestMe);
        }

        [TestMethod]
        public void AuthenticRejects_Null()
        {
            var TestMe = new WindowsVault();
            Assert.IsNotNull(TestMe);
            Assert.IsFalse(TestMe.Authenticate(null!)); // we want it to handle null as false

        }

        [DataRow(VaultCreatorBase.SaveMode.EncryptToLocalUser)]
        [DataRow(VaultCreatorBase.SaveMode.EncryptToLocalMachine)]
        [TestMethod]
        public void VaultCreation_CanEncrypt_CanDecrypt_Ok_LocalUserPath(VaultCreatorBase.SaveMode Settings)
        {
            DataProtectionScope UnDat;
            if (Settings.HasFlag(VaultCreatorBase.SaveMode.EncryptToLocalMachine))
            {
                UnDat = DataProtectionScope.LocalMachine;
            }
            else
            {
                if (Settings.HasFlag(VaultCreatorBase.SaveMode.EncryptToLocalUser))
                {
                    UnDat = DataProtectionScope.CurrentUser;
                }
                else
                {
                    Assert.Inconclusive("This test is for encryption modes only");
                    return;
                }
            }

            string ZipToString(ZipArchiveEntry Arch)
            {
                string key = string.Empty;
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
                        var decrypt = ProtectedData.Unprotect(Data.ToArray(), null, UnDat);
                        for (int i = 0; i < decrypt.Length; i++)
                        {
                            key += ((char)decrypt[i]);
                        }

                    }

                    
                   return key;
                 
            }
            if (PlatformID.Win32NT != Environment.OSVersion.Platform)
            {
                Assert.Inconclusive("This test is for Windows OS only");
            }
            var TestMe = new WindowsVault();
            WindowsVaultCreator creator = new WindowsVaultCreator();
            Dictionary<string, string> Dummy = new Dictionary<string, string>();
            Dummy["key1"] = "value1";
            Dummy["key2"] = "value2";
            creator.AddKeys(Dummy);
            MemoryStream Container = new();
            creator.SaveVault(Container, Settings);
            Container.Position = 0;

            MemoryStream Decrypt;
            Decrypt = new();
            Container.CopyTo(Decrypt);
            Decrypt.Position = 0;
            var output= ProtectedData.Unprotect(Decrypt.GetBuffer(), null, DataProtectionScope.CurrentUser);
            Decrypt.SetLength(0);

            Decrypt.Write(output);
            using (ZipArchive ZipMe = new ZipArchive(Decrypt))
            {
                Dictionary<string, ZipArchiveEntry> ZippedStuff = new();
                
                foreach (ZipArchiveEntry entry in ZipMe.Entries)
                {
                    ZippedStuff[entry.Name] = entry;
                }

                Assert.HasCount(2, ZippedStuff.Keys);
                Assert.Contains("key1", ZippedStuff.Keys);
                Assert.Contains("key2", ZippedStuff.Keys);
                Assert.AreEqual("value1",ZipToString(  ZippedStuff["key1"]));
                Assert.AreEqual("value2", ZipToString(ZippedStuff["key2"]));

            }
        }


    }
}
