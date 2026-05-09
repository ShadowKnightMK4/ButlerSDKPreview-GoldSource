using ApiKeys;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Security;
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
        private List<string> _SandboxWhiteList = new();
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
        /// White list a specific link file. Symbolic links are resolved first
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <remarks>If you attach a symbol link of C:\\Data.txt that points to C:\\cache\\sdk\\data.txt it's treated as if you attached C:\\cache\\sdk\\data.txt</remarks>
        public void AttachFile(string path)
        {
            FileInfo FileData = new FileInfo(path);
            path = Path.GetFullPath(path);
            if (!FileData.Exists)
            {
                throw new FileNotFoundException(path);
            }
            else
            {
                var link = FileData.ResolveLinkTarget(true);
                if (link != null)
                {
                    string? tmp = link.FullName;
                    if (tmp is not null)
                    {
                        path = Path.GetFullPath(tmp);
                    }
                }
            }
                _SandboxWhiteList.Add(path);
        }

        string? FollowLink(string requested_resource)
        {
            Stack<string> rev = new Stack<string>();
            FileSystemInfo WalkMe=null;
            requested_resource = Path.GetFullPath(requested_resource.Trim());
            WalkMe = new FileInfo(requested_resource);
            do
            {
               if ( (!string.IsNullOrWhiteSpace(WalkMe.LinkTarget)) && (!WalkMe.Exists == false))
              
                {
                    return null;
                }
               else
                {
                    if (WalkMe.LinkTarget != null)
                    {
                        var x = WalkMe.ResolveLinkTarget(true);
                        if (x is not null)
                        {
                            WalkMe = x;
                        }
                        else
                        {
                            string? ParentNode = Path.GetDirectoryName(WalkMe.FullName);
                            if (ParentNode is null)
                            {
                                return WalkMe.FullName;
                            }
                            else
                            {
                                WalkMe = new FileInfo(ParentNode);
                            }
                        }
                    }
                    else
                    {
                        string? ParentNode = Path.GetDirectoryName(WalkMe.FullName);
                        if (ParentNode is null)
                        {
                            // I AM {g}rooot!
                            // ok we have the final path but in re
                            string ret = WalkMe.FullName;
                            if (WalkMe.FullName.EndsWith(Path.AltDirectorySeparatorChar) || (WalkMe.FullName.EndsWith(Path.DirectorySeparatorChar)))
                            {
                                var s = rev.TryPeek(out string slash);
                                if (slash?.Length > 0)
                                {
                                    if ((slash[0] == Path.AltDirectorySeparatorChar) || (slash[0] == Path.DirectorySeparatorChar))
                                    {
                                        rev.Pop();
                                    }
                                }
                            }
                            while (rev.Count > 0)
                            {
                                ret += rev.Pop();
                            }
                            WalkMe = null;
                            return ret;
                        }
                        else
                        {
                            rev.Push(WalkMe.Name);
                            rev.Push(Path.DirectorySeparatorChar.ToString());


                            WalkMe = new FileInfo(ParentNode);
                        }
                    }
                }
            } while (WalkMe is not null);
            if (WalkMe is not null)
            {
                return WalkMe.FullName;
            }
            else
            {
                return null;
            }
        }
        string? CheckAttachments(string requested_file)
        {
            FileInfo info;
            requested_file = Path.GetFullPath(requested_file);
            requested_file = requested_file.Trim();
            info = new FileInfo(requested_file);
            if (info.Exists)
            {
                string resolved_file;
                FileSystemInfo? caboose = info.ResolveLinkTarget(true);
                if (caboose is null)
                {
                    resolved_file =  requested_file;
                }
                else
                {
                    if (caboose is not DirectoryInfo)
                    {
                        resolved_file = Path.GetFullPath(caboose.FullName);
                    }
                    else
                    {
                        return null;
                    }
                }
                    foreach (string walk in this._SandboxWhiteList)
                    {
                        string sani = Path.GetFullPath(walk);
                        if (string.CompareOrdinal(resolved_file, sani) == 0)
                        {
                            return resolved_file;
                        }
                    }
            }
            return null;
        }
        /// <summary>
        /// The short story is we get the system to tell us where and return null if out out our sandbox.
        /// </summary>
        /// <param name="RequestedPath">on input, this is passed to Path.GetFullPath first</param>
        /// <param name="Filter">Pick Both, read or write</param>
        /// <returns>null if outside of all sandbox directores and not a whitelist <see cref="AttachFile(string)"/>, the full path if in</returns>
        /// <remarks>ensure trusted folks and NOT LLM can call AttachFile only.</remarks>
        string? GetSecurePath(string RequestedPath, SandBoxPathFilter Filter)
        {

            if (string.IsNullOrEmpty(RequestedPath))
            {
                return null;
            }

            RequestedPath = FollowLink(RequestedPath);
            if (RequestedPath is null)
            {
                return null;
            }
            RequestedPath = Path.GetFullPath(RequestedPath.Trim());
            if (string.IsNullOrEmpty(RequestedPath))
                return null;
            else
            {

                string? link_marker = null;
                if (Directory.Exists(RequestedPath))
                {
                    DirectoryInfo x = new DirectoryInfo(RequestedPath);
                    if (x.Exists)
                    {
                        if (x.LinkTarget != null)
                        {
                            var Final = x.ResolveLinkTarget(true);
                            if (Final is not null)
                            {
                                if (Final.Exists)
                                {
                                    link_marker = Final.FullName;
                                }
                            }
                        }
                        else
                        {
                            link_marker = null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (File.Exists(RequestedPath))
                    {
                        FileInfo x = new FileInfo(RequestedPath);
                        if (x.Exists)
                        {
                            if (x.LinkTarget is not null)
                            {
                                var Finale = x.ResolveLinkTarget(true);
                                if (Finale is not null)
                                {
                                    if (Finale.Exists)
                                    {
                                        link_marker = Finale.FullName;
                                    }
                                }
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }

            if ((Filter == SandBoxPathFilter.Read) || (Filter == SandBoxPathFilter.Both))
            {
                string? whitelist = CheckAttachments(RequestedPath);
                if (whitelist is not null)
                {
                    return whitelist;
                }
            }


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
                    search = _SandBoxPathReadOnly.Concat(_SandBoxPathWriteOnly).ToList();
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unknown/unsupported enum. Select Both, Read or Write");
            }

     
            if ( (search is null) || (search.Count == 0))
            {
                return null;
            }
            
            

            bool one = false;


           foreach (string s in search)
            {
                if (string.IsNullOrEmpty(s))
                    continue;

                string target = Path.GetFullPath(s.Trim());

                /*
                 * for future symbolink link following
                if (Directory.Exists(target))
                {
                    var fn = new DirectoryInfo(target);
                    var newtarget = fn.ResolveLinkTarget(true);
                    if (newtarget is not null)
                    {
                        target = Path.GetFullPath(newtarget.FullName.Trim());
                    }
                }*/
                if (!(target.EndsWith(Path.DirectorySeparatorChar) || (target.EndsWith(Path.AltDirectorySeparatorChar))))
                {
                    target += Path.DirectorySeparatorChar;
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
            this._SandBoxPathWriteOnly = new();

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


        static string FullAndSlashPath(string x)
        {
            if (string.IsNullOrEmpty(x))
                return x;
            else
            {
                x = x.Trim();
                x = Path.GetFullPath(x);
                if (!x.EndsWith('/'))
                {
                    if (!x.EndsWith('\\'))
                    {
                        x += '\\';
                    }
                }
                return x.Trim();
            }
        }

        static ButlerChatToolResultMessage? LoadMode(string target, string format, string? id, ButlerTool_LocalFile_Load that)
        {
            string? sani_target = that.GetSecurePath(target, SandBoxPathFilter.Read);
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
