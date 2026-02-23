using System.Runtime.InteropServices;
using System.Security;

namespace SecureStringHelper
{
    public static class SecureStringHelpers
    {

        /// <summary>
        /// Helper for requesting a plain ol' string from a SecureString
        /// </summary>
        /// <param name="ret">the <see cref="SecureString"/> to convert</param>
        /// <returns>the plain .net string</returns>
        /// <remarks>While this does zero some of the memory it alloc</remarks>
        public static string DecryptString(this SecureString ret)
        {
            ArgumentNullException.ThrowIfNull(ret);
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


        /// <summary>
        /// Slightly more secure. Read specific length of string from the reader one char at a time
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="Input">string source stream</param>
        /// <param name="len">if len is less than 1, we read until end of stream. Otherwise we read len characters</param>
        public static void AssignStringThenReadOnly(this SecureString ret, StreamReader Input, int len=-1)
        {
            ArgumentNullException.ThrowIfNull(ret);
            ArgumentNullException.ThrowIfNull(Input);
            bool ToEof = false;
            if (len <= 0)
            {
                ToEof = true;
            }
            
            while (true)
            {
                char read = (char) Input.Read();
         
                if (ToEof)
                {
                    if (Input.EndOfStream)
                    {
                        break;
                    }
                    else
                    {
                        ret.AppendChar(read);     
                    }
                }
                else
                {
                    if (!Input.EndOfStream)
                    {
                        ret.AppendChar(read);

                        len -= 1;
                        if (len < 1)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            ret.MakeReadOnly();
        }
        /// <summary>
        /// Convience routine. Assign a string as a secure string.
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="y">string to assign</param>
        /// <remarks>Butler primaryily uses <see cref="SecureString"/> for the disposal. Also good practice is append char by char so the whole password isn't in const memory. However, if you're already reading from plain strings, the password is already in memory.</remarks>
        public static void AssignStringThenReadOnly(this SecureString ret, string y)
        {
            ArgumentNullException.ThrowIfNull(ret);
            ArgumentNullException.ThrowIfNull(y);
            int i;
            for (i = 0; i < y.Length; i++)
            {
                ret.AppendChar(y[i]);
            }
            ret.MakeReadOnly();
        }
    }
}
