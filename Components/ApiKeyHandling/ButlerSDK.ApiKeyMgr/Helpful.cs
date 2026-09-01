using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.ApiKeyMgr
{
    /*
     * 
    public static class Helpful
    {

        public static string DecryptString(this SecureString ret)
        {
            nint ptr = 0;
            try
            {
                ptr = Marshal.SecureStringToBSTR(ret);
                return Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                if (ptr != 0)
                    Marshal.ZeroFreeBSTR(ptr);
                ptr = 0;
            }

        }
        public static void AssignStringThenReadyOnly(this SecureString ret, string y)
        {
            int i;
            for (i = 0; i < y.Length; i++)
            {
                ret.AppendChar(y[i]);
            }
            ret.MakeReadOnly();
        }
    }
    */
}
