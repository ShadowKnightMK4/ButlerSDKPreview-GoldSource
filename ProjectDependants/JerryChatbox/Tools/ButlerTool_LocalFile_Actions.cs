using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ""description"": ""Pass the format to act on with. Choose from 'utf8-txt' -> Unicode text. 'PDF' -> 'adobe PDF'""
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
        }

        public override string ToolName => "PerformLocalFileAction";

        public override string ToolDescription => "Retrieve files on the local system indicated by user";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            return json_template;
        }



        static ButlerChatToolResultMessage? LoadMode(string target, string format, string? id)
        {
            ButlerChatToolResultMessage ret;
            switch (format)
            {
                case format_ansi_text:
                case format_unicode_text:
                    {
                        string data;
                        try
                        {
                            data = File.ReadAllText(target);
                        }
                        catch (IOException e)
                        {
                            data = $"Error: This call failed. Exception data {e.Message}";
                        }
                        ret = new ButlerChatToolResultMessage(id, data);
                        return ret;
                    }
                case "DOCX":
                case "PDF": 
                default: return new ButlerChatToolResultMessage(id, $"Error: This mode is not supported: {format}");
            }
        }

        static ButlerChatToolResultMessage? SaveMode(string target, string format, string data, string? id)
        {
            ButlerChatToolResultMessage ret;
            switch (format)
            {
                case format_unicode_text:
                    {
                        
                        try
                        {
                            File.WriteAllText(target, data);
                            ret = new ButlerChatToolResultMessage(id, $"{target} was successful saved as {format} data");
                        }
                        catch (IOException e)
                        {
                            ret = new ButlerChatToolResultMessage(id, $"{target} encountered an error saving as {format} data. Error is {e.Message}");
                        }
                        return ret;
                    }
                case format_ansi_text:
                    {
                        byte[] DataAsBytes = Encoding.ASCII.GetBytes(data);
                        try
                        {
                            File.WriteAllBytes(target, DataAsBytes);
                            ret = new ButlerChatToolResultMessage(id, $"{target} was successful saved as {format} data");
                        }
                        catch (IOException e)
                        {
                            ret = new ButlerChatToolResultMessage(id, $"{target} encountered an error saving as {format} data. Error is {e.Message}");
                        }
                        return ret;
                    }
                case "DOCX":
                case "PDF":
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
                    return LoadMode(targetfile, formatdiff, FuncId);
                case "save":
                    string data = Args.RootElement.GetProperty("data").ToString();
                    return SaveMode(targetfile, formatdiff, data, FuncId);
                default:
                    return new ButlerChatToolResultMessage(FuncId, $"Error: Invalid Mode selected. No Action do" +
                        $"ne with  {targetfile}");
            }
        }

        string[] ValidFormat = { "utf8-txt", "PDF", "docx" };
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
