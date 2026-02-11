using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ApiKeyVaultCreation
{
    public class WindowsVaultCreator: VaultCreatorBase
    {
        public WindowsVaultCreator() : base (SaveMode.EncryptToLocalUser){ }
        protected override byte[] EncryptBytes(byte[] Data, byte[]? Entropy, SaveMode Mode)
        {
            DataProtectionScope cipher =0;
            if (Mode.HasFlag(SaveMode.EncryptToLocalUser))
            {
                cipher = DataProtectionScope.CurrentUser;
            }
            else
            {
                if (Mode.HasFlag(SaveMode.EncryptToLocalMachine))
                {
                    cipher = DataProtectionScope.LocalMachine;
                }
            }

            if (cipher == 0)
            {
                return Data;
            }
             return  ProtectedData.Protect(Data, Entropy, cipher);
        }
    }
}
