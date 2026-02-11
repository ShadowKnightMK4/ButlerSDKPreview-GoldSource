using System.Runtime.InteropServices;
using System.Security;

namespace SecureStringHelper
{
    public static class SecureStringHelpers
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
}
