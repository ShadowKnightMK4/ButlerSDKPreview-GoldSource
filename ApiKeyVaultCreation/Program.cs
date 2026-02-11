using System;
using System.Reflection;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ApiKeyVaultCreation
{
    /*
     * 
     */
    
    internal class Program
    {
        
        static void HandleVault(Dictionary<string, string>? IModeKeys)
        {
            using (WindowsVaultCreator Vault = new WindowsVaultCreator())
            {
                VaultCreatorBase.VersionData VDate = new();
              
                if (Directory.Exists(KeysFolder))
                {
                    Vault.AddKeys(KeysFolder, out VDate);
                }
                if (IModeKeys is not null)
                {
                    Vault.AddKeys(IModeKeys);
                }

                Vault.SaveVaultToDisk(OutputBlob, WindowsVaultCreator.SaveMode.EncryptToLocalUser, VDate);
            }
 
   

           
        }

        /// <summary>
        /// This be the source of where we're reading keys from if ripping from a holder
        /// </summary>
        static string KeysFolder = string.Empty;
        
        /// <summary>
        /// Target location to drop the encrypted zip file too
        /// </summary>
        static string OutputBlob = string.Empty;
        static string GetExecFolderLocation()
        {
            var self = Assembly.GetExecutingAssembly();
            var entry = Assembly.GetCallingAssembly();
            string? ret = entry.Location;
            ret = Path.GetDirectoryName(ret);
            ret += Path.DirectorySeparatorChar;

            return ret;
        }
        static void Usage()
        {
            Console.WriteLine("vault create program for Windows ButlerSDK");
            Console.WriteLine("ApiKeyVaultCreation.exe {Keys folder OR '-I' OR '-V'}  {output}");
            Console.WriteLine("Inputing Folders for this app works like this. Assume Current Director is C:\\MyDocuments:");
            Console.WriteLine("\"C:\\Windows\\SpecificLocation\" => maps to \"C:\\Windows\\SpecificLocation\"");
            Console.WriteLine("\"..\\RelativeLocation\" => maps to \"C:\\Windows\\SpecificLocation\\RelativeLocation\\");
            

        }


      
        static void Main(string[] args)
        {
            bool WantIMode = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            {
                Console.WriteLine("ERROR: Creating a vault currently is only supported on Windows machines (for now)");
                Console.WriteLine("A vault is not strictly required but it is recommanded to keep api keys secure for ButlerSDK software.");
                Console.WriteLine("Do not rely on the vault to preserve your keys for recoverey. Keep a backup!");
                Console.WriteLine("This program will now exit.");
                Environment.Exit(255);
            }


            if (args.Length == 0)
            {
                Usage();
                return;
            }

            // output blob.
            if (args.Length >= 1)
            {
                if (args[0].StartsWith('.')) /// that means relative to us folder
                {
                    // do the general path combine of current directory, strip any path separate ie \\ or .
                    // then add the specified folder (also strip dots)

                    string rel_arg = args[0].Trim('.').Trim(Path.DirectorySeparatorChar).Trim(Path.AltDirectorySeparatorChar);
                    string rel_folder = Path.Combine(Directory.GetCurrentDirectory());//, args[0].Trim('.'));
                    if ( (rel_folder.EndsWith(Path.DirectorySeparatorChar) == false) && (rel_folder.EndsWith(Path.AltDirectorySeparatorChar) == false))
                    {
                        rel_folder += Path.DirectorySeparatorChar;
                    }
                    rel_folder += rel_arg;


                    // does the current directory have a relative folder location ? USE THAT!
                    if (Directory.Exists(rel_folder) == false)
                    {
                        Console.WriteLine($"Warning target {rel_folder} for keys does not appear to exist. Trying alt...");
                        rel_folder = GetExecFolderLocation();
                        if ((rel_folder.EndsWith(Path.DirectorySeparatorChar) == false) && (rel_folder.EndsWith(Path.AltDirectorySeparatorChar) == false))
                        {
                            rel_folder += Path.DirectorySeparatorChar;
                        }
                        rel_folder += rel_arg;
                        if (Directory.Exists(rel_folder) == false)
                        {
                            Console.WriteLine($"Unable to continue {rel_folder} also doesn't seem to exist. Quitting...");
                        }
                        else
                        {
                            Console.WriteLine($"Assigning {rel_folder} to be source to read keys from");
                            KeysFolder = rel_folder;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Assigning \"{rel_folder}\" to be source to read keys from");
                        KeysFolder = rel_folder;
                    }
                }
                else // this is a specific folder
                {
                    // first check for flag
                    if (string.Compare(args[0], "-I", true) == 0)
                    {
                        Console.WriteLine("Interactive mode for key source. Stand bye");
                        WantIMode = true;
                    }
                    else
                    {
                        if (Directory.Exists(args[0]))
                        {
                            Console.WriteLine($"Assigning {args[0]} to be source to read keys from");
                            KeysFolder = args[0];
                        }
                    }
                }
            }

            if (args.Length >= 2)
            {
                if (args[1].StartsWith('.')) /// that means relative to us folder
                {
                    string rel_arg = args[1].Trim('.').Trim(Path.DirectorySeparatorChar).Trim(Path.AltDirectorySeparatorChar);
                    string vault_location = Path.Combine(Directory.GetCurrentDirectory());//, args[0].Trim('.'));
                    if ((vault_location.EndsWith(Path.DirectorySeparatorChar) == false) && (vault_location.EndsWith(Path.AltDirectorySeparatorChar) == false))
                    {
                        vault_location += Path.DirectorySeparatorChar;
                    }
                    vault_location += rel_arg;


                    if (File.Exists(vault_location))
                    {
                        Console.WriteLine($"Warning: {vault_location} exists. This action will destroy that file.");
                    }
                    Console.WriteLine($"Setting {vault_location} to be where new ButlerSDK Vault is created");
                    OutputBlob = vault_location;

                   
                }
                else // this is a specific folder
                {

                    if (File.Exists(args[1]))
                    {
                        Console.WriteLine($"Warning: {args[1]} exists. This action will destroy that file.");
                    }
                    Console.WriteLine($"Setting {args[1]} to be where new ButlerSDK Vault is created");
                    OutputBlob = args[1];
                }
            }
            Dictionary<string, string>? Keys = null;
            if (WantIMode)
            {
                Keys = new();
                Console.WriteLine("Interactive Mode:");
                Console.WriteLine("Format is: API.KEY value");
                Console.WriteLine("Example: API-BOB Key12");
                Console.WriteLine("When done, press enter.");
                while (true)
                {
                    string? Line =  Console.ReadLine()?.Trim();
  

                    if (string.IsNullOrEmpty(Line))
                    { 
                        Console.WriteLine("Done We assume. (nothing seen) ");
                        break;
                    }
                    else
                    {
                        if (Line.Equals("\n"))
                        {
                            Console.WriteLine("Done");
                            break;
                        }
                        string[] Split = Line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if ((Split.Length % 2) != 0)
                        {
                            Console.WriteLine("Warning: Ensure Each key has a pair name  ");
                        }
                        else
                        {
                            for (int i = 0; i < Split.Length; i+= 2)
                            {
                                Keys[Split[i]] = Split[i + 1];
                            }
                            Console.WriteLine($"{Split.Length / 2} Keys prepped to begin using");
                            Console.WriteLine($"Total keys so far: {Keys.Count}");
                        }
                    }

                }
            }

            Console.WriteLine("Starting Vault creation on Windows");
            try
            {
                HandleVault(Keys);

                Console.WriteLine($"Saved vault to {OutputBlob}. Quitting.");
            }
            catch (Exception)
            {
                Console.WriteLine($"There was an error saving to {OutputBlob}. Exiting.");
            }
            }
    }
}