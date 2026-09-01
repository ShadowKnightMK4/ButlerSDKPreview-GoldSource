using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ApiKeyVaultCreation
{
    internal class KeyFolderScanner
    {
        /// <summary>
        /// We do assume unciode encoding
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        public static SecureString ReadKeyFile(Stream Target)
        {
            ArgumentNullException.ThrowIfNull(Target, "target");
            SecureString ret = new SecureString();

            while (Target.Length > Target.Position)
            {
                byte[] UnicodeChar =new byte[2];
                Target.ReadExactly(UnicodeChar, 0, UnicodeChar.Length);

                ret.AppendChar(Encoding.UTF8.GetString(UnicodeChar)[0]);
            }
            return ret;
        }
    }
}
