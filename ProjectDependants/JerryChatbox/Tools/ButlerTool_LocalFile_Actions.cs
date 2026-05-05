using ButlerLLMProviderPlatform.DataTypes;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace ButlerSDK.Tools
{
    /// <summary>
    /// load text from disk and save text to disk
    /// </summary>
    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_LocalFile_Load : ButlerToolBase
    {
        private List<string> _SandBoxPathReadOnly=new();
        private List<string> _SandBoxPathWriteOnly= new();
        public IReadOnlyList<string> ReadOnlySandBoxPath => _SandBoxPathReadOnly;
        public IReadOnlyList<string> WriteOnlySandBoxPath => _SandBoxPathWriteOnly;

        public enum SandBoxPathFilter
        {
            Unknown =0,
            Read = 1,
            Write = 2,
            Both = 3,
            ReadWrite = Both
        }

        private bool does_sandbox_exist(DirectoryInfo Info)
        {
            ArgumentNullException.ThrowIfNull(Info);
            return Info.Exists;
        }
        private bool does_sandbox_exist(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            return does_sandbox_exist(new DirectoryInfo(path));
        }
        public void AddSandBoxPath(string path, SandBoxPathFilter Filter)
        {
           AddSandBoxPath(new DirectoryInfo(path), Filter);
        }
        public void AddSandBoxPath(FileSystemInfo Location, SandBoxPathFilter Filter)
        {
            if (!does_sandbox_exist(Location.FullName))
            {
                throw new DirectoryNotFoundException($"{Location.FullName} does not seem to exist. Ensure it does before added to ButlerSDK File tool");
            }
            if (Filter == SandBoxPathFilter.Unknown)
            {
                throw new NotSupportedException("Unknown sandbox path. Pick Read/ Write or Both");
            }
            
            
            switch (Filter)
            {
                case SandBoxPathFilter.Write:
                    _SandBoxPathWriteOnly.Add(Location.FullName); break;
                case SandBoxPathFilter.ReadWrite:
                    _SandBoxPathWriteOnly.Add(Location.FullName);
                    _SandBoxPathReadOnly.Add(Location.FullName); break;
                case SandBoxPathFilter.Read:
                    _SandBoxPathReadOnly.Add(Location.FullName);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unknown/Unsupported Enum. Select Read, Write or ReadWrite/Both");
            }
        }

        /// <summary>
        /// The short story is we get the system to tell us where and return null if out out our sandbox.
        /// </summary>
        /// <param name="RequestedPath">on input, this is passed to Path.GetFullPath first</param>
        /// <param name="Filter">Pick Both, read or write</param>
        /// <returns>null if outside of all passed directories, the full path if in</returns>
        string? GetSecurePath(string RequestedPath, SandBoxPathFilter Filter)
        {
            RequestedPath = Path.GetFullPath(RequestedPath);
            RequestedPath = RequestedPath.Trim();
            if (string.IsNullOrEmpty(RequestedPath))
                return null;
            List<string> search;
            switch (Filter)
            {
                case SandBoxPathFilter.Write:
                    search = _SandBoxPathWriteOnly;
                    break;
                case SandBoxPathFilter.Read:
                    search = _SandBoxPathReadOnly;
                    break;
                case SandBoxPathFilter.Both:
                    search = new();
                    search.AddRange( _SandBoxPathWriteOnly );
                    search.AddRange(_SandBoxPathReadOnly);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unknown/unsupported enum. Select Both, Read or Write");
            }

     
            if ( (search is null))
            {
                return null;
            }
            
            if (search.Count == 0)
            {
                return null;
            }

            bool one = false;


           foreach (string s in search)
            {
                string target;
                if (!(s.EndsWith(Path.DirectorySeparatorChar) || (s.EndsWith(Path.AltDirectorySeparatorChar))))
                {
                    target = Path.GetFullPath(s) + Path.DirectorySeparatorChar;
                }
                else
                {
                    target = Path.GetFullPath(s);
                }
                
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (RequestedPath.StartsWith(target, StringComparison.OrdinalIgnoreCase))
                    {
                        one = true;
                        break;
                    }
                }
                else
                {
                    if (RequestedPath.StartsWith(target))
                    {
                        one = true;
                        break;

                    }
                }
            }
            if (one)
            {
                return RequestedPath;
            }
            else
            {
                return null;
            }
        }
                const string format_unicode_text = "utf8-txt";
        const string format_ansi_text = "ANSI-txt";
        const string format_pdf_doc = "PDF";
        const string command_save = "save";
        const string command_load = "load";

        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""baseaction"": {
            ""type"": ""string"",
            ""description"": ""pass 'load' to load data or 'save' to save data""
        },
        ""target"": {
            ""type"": ""string"",
            ""description"": ""This indicates which file to act on using the running exe's abilities""
        },
        ""formatdiff"": {  
            ""type"": ""string"",
            ""description"": ""Pass the format to act on with. Choose from 'utf8-txt' -> Unicode text. 'format_ansi_text' -> ANSI text""
        },
        ""data"": {
            ""type"": ""string"",
            ""description"": ""If saving text, this is the string to save. Ignored if not saving something""
        }
    },
    ""required"": [ ""baseaction"" , ""target"", ""formatdiff"" ]
}";
        public ButlerTool_LocalFile_Load(IButlerVaultKeyCollection? KeyHandler) : base(KeyHandler)
        {
            this._SandBoxPathReadOnly = new();
            this._SandBoxPathWriteOnly = new();

            var genericDefault = Path.Combine(Path.GetTempPath(), "ButlerSDK");
            genericDefault += Path.DirectorySeparatorChar;

            genericDefault += Guid.NewGuid().ToString().Substring(0, 8);
            Directory.CreateDirectory(genericDefault);

            AddSandBoxPath(genericDefault, SandBoxPathFilter.Both);
           
        }

        public ButlerTool_LocalFile_Load(IButlerVaultKeyCollection? KeyHandler, List<string> AllowedReads) : base(KeyHandler)
        {
            this._SandBoxPathReadOnly = new();
            this._SandBoxPathReadOnly.AddRange(AllowedReads);
        }

        public ButlerTool_LocalFile_Load(IButlerVaultKeyCollection? KeyHandler, List<string> AllowedReads, List<string> AllowedWrites) : base(KeyHandler)
        {
            this._SandBoxPathReadOnly = new();
            this._SandBoxPathReadOnly.AddRange(AllowedReads);
            this._SandBoxPathWriteOnly = new();
            this._SandBoxPathWriteOnly.AddRange(AllowedWrites);
        }


        public override string ToolName => "PerformLocalFileAction";

        public override string ToolDescription => "Retrieve files on the local system indicated by user";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            return json_template;
        }



        static ButlerChatToolResultMessage? LoadMode(string target, string format, string? id, ButlerTool_LocalFile_Load that)
        {
            string? sani_target = that.GetSecurePath(target, SandBoxPathFilter.Write);
            if (sani_target == null)
            {
                return new ButlerChatToolResultMessage(id, $"Requested path {target} is outside the sanbox. Unable to load data with this tool.");
            }
            ButlerChatToolResultMessage ret;
            switch (format)
            {
                case format_ansi_text:
                case format_unicode_text:
                    {
                        string data;
                        try
                        {
                            data = File.ReadAllText(sani_target);
                        }
                        catch (IOException e)
                        {
                            data = $"Error: This call failed. Exception data {e.Message}";
                        }
                        ret = new ButlerChatToolResultMessage(id, data);
                        return ret;
                    }
                default: return new ButlerChatToolResultMessage(id, $"Error: This mode is not supported: {format}");
            }
        }

        static ButlerChatToolResultMessage? SaveMode(string target, string format, string data, string? id, ButlerTool_LocalFile_Load that)
        {
            string? sani_target = that.GetSecurePath(target, SandBoxPathFilter.Write);
            if (sani_target == null)
            {
                return new ButlerChatToolResultMessage(id, $"Requested path {target} is outside the sanbox. Unable to save data with this tool.");
            }
            ButlerChatToolResultMessage ret;
            switch (format)
            {
                case format_unicode_text:
                    {
                        
                        try
                        {
                            File.WriteAllText(sani_target, data);
                            ret = new ButlerChatToolResultMessage(id, $"{sani_target} was successful saved as {format} data");
                        }
                        catch (IOException e)
                        {
                            ret = new ButlerChatToolResultMessage(id, $"{sani_target} encountered an error saving as {format} data. Error is {e.Message}");
                        }
                        return ret;
                    }
                case format_ansi_text:
                    {
                        byte[] DataAsBytes = Encoding.ASCII.GetBytes(data);
                        try
                        {
                            File.WriteAllBytes(sani_target, DataAsBytes);
                            ret = new ButlerChatToolResultMessage(id, $"{sani_target} was successful saved as {format} data");
                        }
                        catch (IOException e)
                        {
                            ret = new ButlerChatToolResultMessage(id, $"{sani_target} encountered an error saving as {format} data. Error is {e.Message}");
                        }
                        return ret;
                    }
                default: return new ButlerChatToolResultMessage(id, $"Error: This mode is not supported: {format}");
            }
        }
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId,Call, this, out JsonDocument ?Args))
            {
                return null;
            }
            if (Call != null)
            {
                if (Call.FunctionArguments is not null)
                {
                    FunctionCallArguments = Call.FunctionArguments;
                }
                else
                {
                    return null;
                }
                    FuncId = Call.Id;
            }
            


            string baseaction  = Args.RootElement.GetProperty("baseaction").ToString();
            string targetfile = Args.RootElement.GetProperty("target").ToString();
            string formatdiff = Args.RootElement.GetProperty("formatdiff").ToString();

            switch (baseaction)
            {
                case "load":
                    return LoadMode(targetfile, formatdiff, FuncId, this);
                case "save":
                    string data = Args.RootElement.GetProperty("data").ToString();
                    return SaveMode(targetfile, formatdiff, data, FuncId,this);
                default:
                    return new ButlerChatToolResultMessage(FuncId, $"Error: Invalid Mode selected. No Action do" +
                        $"ne with  {targetfile}");
            }
        }

        string[] ValidFormat = { "utf8-txt" };
        string[] ValidBaseAction = { "load", "save" };
        bool ValidateBaseActionWord(string action)
        {
            return ValidBaseAction.Contains(action);
        }

        bool ValidateFormat(string format)
        {
            return ValidFormat.Contains(format);
        }
        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? Doc)
        {
            JsonDocument FunctionCheck=null!;
            if (Doc != null)
            {
                FunctionCheck = Doc;
            }
            else
            {
                if (Call is not null)
                {
                    if (Call.FunctionArguments is not null)
                    {
                        FunctionCheck = JsonDocument.Parse(Call.FunctionArguments);
                    }
                    else
                    {
                        return false;
                    }
                }
                
            }

            string baseaction = FunctionCheck.RootElement.GetProperty("baseaction").ToString();
            string targetfile = FunctionCheck.RootElement.GetProperty("target").ToString();
            string formatdiff = FunctionCheck.RootElement.GetProperty("formatdiff").ToString();

            if (string.IsNullOrEmpty(baseaction))
                return false;
            else
            {
                if (!ValidateBaseActionWord(baseaction)) return false;
            }

            if (string.IsNullOrEmpty(targetfile)) return false;

            if (string.IsNullOrEmpty(formatdiff)) return false;
            if (!ValidateFormat(formatdiff)) return false;

            return true;
            
        }
    }
}
