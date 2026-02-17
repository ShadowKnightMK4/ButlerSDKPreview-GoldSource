using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK;
using ButlerSDK.ToolSupport;
using System.Security;
using ButlerSDK.ToolSupport.Bench;
using ButlerSDK.Core;
using ButlerBaseInternal;


namespace ButlerSDK.ToolSupport.DiscoverTool
{
  
    

    /// <summary>
    /// The default implementation of the <see cref="ButlerTool_DiscoverResource"/> that <see cref="ButlerTool_DiscoverTools"/> uses. It'll scan the loaded assemblies for any <see cref="IButlerToolBaseInterface"/>, add them to its kit if  <see cref="ButlerTool_DiscoverAttributes"/> don't forbid it and let the butler pick as needed
    /// </summary>
    public class DefaultButlerTool_DiscoverResource : ButlerTool_DiscoverResource
    {
        readonly List<IButlerToolBaseInterface> ToolCollection = [];

        /// <summary>
        /// Initializes the resource for <see cref="ButlerTool_DiscoverTools"/>. Note this implementation will scan current domain assemblies, create instances that match and pass the passed <see cref="IButlerVaultKeyCollection"/> as is to them.
        /// </summary>
        /// <param name="KeyHandler">instance of this interface to pass to all tools instanced</param>
        public void Initialize(IButlerVaultKeyCollection? KeyHandler)
        {
           
            // grab running assemblies

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var single = assemblies[i];
                // Possible below is essentially can it be assigned to, isn't abstract ,pure interface AND PUBLIC
                var Possible = single.GetTypes()
                    .Where(t => typeof(IButlerToolBaseInterface).IsAssignableFrom(t)
                                && !t.IsAbstract && !t.IsInterface && t.IsPublic);
                bool AllowDiscover = true;
                foreach (var TPos in Possible)
                {
                    // check for filter to skip
                    AllowDiscover = true;
                    var attribs = TPos.GetCustomAttributes();
                    foreach (var attr in attribs)
                    {
                        ButlerTool_DiscoverAttributes? Disc = (attr as ButlerTool_DiscoverAttributes);
                        if (Disc is not null)
                        {
                            if (Disc.DisableDiscover)
                            {
                                AllowDiscover = false;
                                break;
                            }
                        }
                    }

                    // if allowed to add - do so
                    if (AllowDiscover)
                    {
                        IButlerToolBaseInterface? Instance;
                        try
                        {
                            Instance = (IButlerToolBaseInterface?)Activator.CreateInstance(TPos, [KeyHandler]);
                        }
                        catch (MissingMemberException)
                        {
                            // the thing does not need a key handler, fallback to empty builder
                            Instance = (IButlerToolBaseInterface?)Activator.CreateInstance(TPos);
                        }

                        if (Instance is not null)
                        {
                            ToolCollection.Add(Instance);
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Override in a subclass. This routine exists to filter tools about to be instances to the  butler instance we're working with
        /// </summary>
        /// <param name="tool">The tool about to be </param>
        /// <returns>default true means accepted. false means DO NOT ADD to list returned by Search</returns>
        protected virtual bool FilterTool(IButlerToolBaseInterface tool)
        {
            return true;
        }

        public IList<string> Search(string terms)
        {
         return Search(new string[] { terms });  
        }
        public IList<string> Search(string[] terms)
        {
            List<string> ret = new();
            foreach (IButlerToolBaseInterface tool in ToolCollection)
            {
               if (FilterTool(tool))
                {
                    ret.Add(tool.ToolName);
                }
            }
            return ret;
        }

        /// <summary>
        /// Called by butler4 to get instances to the hits it searched for earlier
        /// </summary>
        /// <param name="Names"></param>
        /// <returns></returns>
        public IList<IButlerToolBaseInterface> Spawn(string[] Names)
        {
            List<IButlerToolBaseInterface> Start = new();
            for (int i =0; i < Names.Length;i++)
            {

                foreach (IButlerToolBaseInterface hit in ToolCollection.Where(tool => { string toss; toss = Names[i].Trim().Trim('\"'); return tool.ToolName == toss; }))
                {
                    Start.Add(hit);
                }
            }
            return Start;
        }

    
    }

    

    /// <summary>
    /// This is a special exception. To use with <see cref="Butler4"/>, you'll need to assign it to both <see cref="Butler4."/>
    /// </summary>
    public class ButlerTool_DiscoverTools : ButlerSystemToolBase,  ButlerTool_Discoverer
    {

        public const string SearchModeName = "Search";
        public const string ActivateModeString = "Activate";
        public const string CleanupModeString = "Remove";
        /// <summary>
        /// the active toolbox we use.
        /// </summary>
        ButlerToolBench? toolbox;
        /// <summary>
        /// The list of discover sources we scan/use/
        /// </summary>
        List<ButlerTool_DiscoverResource> toolCollectionSource = [];


        public ButlerTool_DiscoverTools(IButlerVaultKeyCollection KeyHandler): base(KeyHandler)
        {
            //this.KeyHandler = KeyHandler;
        }

        public override string ToolName => "DiscovererTool";

        public override string ToolVersion => "YES";

        public override string ToolDescription => "You can use this tool to adjust what available tools you have to self do a user request.  pass mode ";

        

        const string json_template_base = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""action"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""ONLY ONE OF [ 'Search', 'Activate', Remove]. Use 'Search' to search toolbox for tools. Use 'Activate' to activate tools. Use 'Remove' to deactivate tools.""
    },
    ""SearchTerm"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""General format: [ 'term1', 'term2' ]. Only used in 'Search' mode.""
    },
    ""ActivateList"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""List of tool names to make available for you. Only used in 'Activate' mode.""
    },
    ""RemoveList"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""Tools to take offline for you. call Activate to use them again. Only used in 'Remove' mode""
    }
  },
  ""required"": [ ""action"" ]
}
";
        string? json_template_cache = null;



        static List<string> SearchSplit(string input)
        {
            List<string> ret = new();

            input = input.Trim();
            if (input.StartsWith('['))
                { input = input.Substring(1); }
            if (input.EndsWith(']'))
                { input = input.Substring(0, input.Length - 1); }

            ret = input.Split(',', StringSplitOptions.TrimEntries).ToList();
            return ret;
        }


    internal enum DiscoverModes
        {
            /// <summary>
            /// undefined
            /// </summary>
            none = 0,
            /// <summary>
            /// mode is search mode
            /// </summary>
            SearchMode,
            /// <summary>
            /// Mode is activate mode
            /// </summary>
            ActivateMode,
            /// <summary>
            /// RESERVED AND NOT IMPLEMENTED. Mode is Search and Also Activate in 1 pass.
            /// </summary>
            SearchAndActivateModeReserved,
            /// <summary>
            /// Mode is remove mode
            /// </summary>
            RemoveMode
        }
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            JsonElement outdoc;
            string input;
            DiscoverModes DiscMode= DiscoverModes.none;
            JsonDocument? args;
            if (Jeeves is null)
            {
                return null;
            }
            if (!ButlerToolBase.BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out args))
            {
                return null;
            }
            else
            {
                if (args is null)
                {

                    return null;
                }
                else
                {
                    switch ((args.RootElement.GetProperty("action").ToString()))
                    {
                        case "Search":
                            {
                                DiscMode = DiscoverModes.SearchMode;
                                if (args.RootElement.TryGetProperty("SearchTerm", out outdoc))
                                {
                                    input = outdoc.ToString();
                                }
                                else
                                {
                                    input = string.Empty;
                                }
                                break;
                            }
                        case "Activate":
                            {
                                DiscMode = DiscoverModes.ActivateMode;
                                if (args.RootElement.TryGetProperty("ActivateList", out outdoc))
                                {
                                    input = outdoc.ToString();
                                }
                                else
                                {
                                    input = string.Empty;
                                }
                                break;
                            }
                        case "winddown":
                            {
                                DiscMode = DiscoverModes.RemoveMode;
                                if (args.RootElement.TryGetProperty("RemoveList", out outdoc))
                                {
                                    input = outdoc.ToString();
                                }
                                else
                                {
                                    input = string.Empty;
                                }
                                break;
                            }
                        default: return new ButlerChatToolResultMessage(FuncId, "Error: Tool Call failed due to unknown mode. Please pass either 'query', 'engage' or 'windown' as a mode");
                    }



                    switch (DiscMode)
                    {
                        case DiscoverModes.SearchMode:
                            return QueryMode(SearchSplit(input), FuncId);
                        case DiscoverModes.ActivateMode:
                            return EnageMode(SearchSplit(input), FuncId);
                        case DiscoverModes.RemoveMode:
                            return Windown(SearchSplit(input), FuncId);
                        default:
                            return null;
                    }
                }
            }

        }


        private ButlerChatToolResultMessage? Windown(List<string> StopThese, string? FuncID)
        {
            
            string ret = string.Empty;
            foreach ( string s in StopThese)
             {
                Jeeves!.DeleteTool(s, true);
                if (!Jeeves.ExistsTool(s))
                {
                    ret += s + ": tool deleted";
                }
                else
                {
                    ret += s + ": tool persisted after delete. ";
                }
            }


            return new ButlerChatToolResultMessage(FuncID, ret);
        }

        private ButlerChatToolResultMessage? EnageMode(List<string> StartThese, string? FuncID)
        {
            string ret = string.Empty;
            Jeeves!.AutoUpdateTooList = false;
          
           foreach ( string s in StartThese)
            {
                if (Jeeves.ExistsTool(s.Trim().Trim('\"')) == false)
                {
                    foreach (var source in this.toolCollectionSource)
                    {
                        var hits = source.Spawn(StartThese.ToArray());
                        foreach (IButlerToolBaseInterface tool in hits)
                        {
                            Jeeves.AddTool(tool);
                            ret += tool.ToolName + " added ,";
                        }
                        ret = ret.TrimEnd(',');
                    }
                }
            }
            Jeeves.AutoUpdateTooList = true;
            Jeeves.RecalcTools();
            return new ButlerChatToolResultMessage(FuncID, ret);
        }

        /// <summary>
        /// The query mode
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        private ButlerChatToolResultMessage? QueryMode(List<string> search, string? FuncID)
        {
            Dictionary<string, string> ReturnValue = new();
            ReturnValue["matchcount"] = "0";
            string match = string.Empty;
            int count = 0;
            try
            {
                foreach (var Source in toolCollectionSource)
                {

                    var posmatch = Source.Search([.. search]);
                    if (posmatch is not null)
                    {
                        foreach (string amatch in posmatch)
                        {
                            count++;
                            match += amatch + ",";
                        }
                        match = string.Concat("[", match.AsSpan(0, match.Length - 1), "]");

                    }
                }
            }
            catch (Exception)
                {
                ;
                }
            ReturnValue["matchcount"] = count.ToString();
            ReturnValue["matchlist"] = match.ToString();
            return new ButlerChatToolResultMessage(FuncID, JsonSerializer.Serialize(ReturnValue));
        }
        bool validate_tool_name_list(string name)
        {
            return true;
        }
        
        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            DiscoverModes DiscMode = DiscoverModes.none;
            bool IsJson = false;
            JsonDocument? doc=null;
            JsonElement OutputDocument;

            if (FunctionParse is not null)
            {
                doc = FunctionParse;
            }
            else
            {
                if (Call is not null)
                {
                    if (Call.FunctionArguments is null)
                    {
                        IsJson = false;
                    }
                    else
                    {
                        try
                        {
                            doc = JsonDocument.Parse(Call.FunctionArguments);
                            IsJson = true;
                        }
                        catch (JsonException)
                        {
                            IsJson = false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
  
            if ( (!IsJson) || (doc == null))
            {/* [ 'Search', 'Activate', Remove].*/
                return false;
            }
            switch ((doc.RootElement.GetProperty("action").ToString()))
            {
                case "Search": DiscMode = DiscoverModes.SearchMode; break;
                case "Activate": DiscMode = DiscoverModes.ActivateMode;  break;
                case "Remove": DiscMode = DiscoverModes.RemoveMode; break;
                default: return false;
            }
            /* 
    ""SearchTerm"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""General format: [ 'term1', 'term2' ]. Only used in 'Search' mode.""
    },
    ""ActivateList"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""List of tool names to make available for you. Only used in 'Activate' mode.""
    },
    ""RemoveList"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""Tools to take offline for you. call Activate to use them again. Only used in 'Remove' mode""
    }*/
            switch (DiscMode)
            {
                case  DiscoverModes.SearchMode:
                    {
                        string query = string.Empty;
                        if (doc.RootElement.TryGetProperty("SearchTerm", out OutputDocument))
                        {
                            query = OutputDocument.ToString();
                        }
                        
                        if (!validate_tool_name_list(query)) return false;

                    }
                    break;
                case DiscoverModes.ActivateMode:
                    {
                        string tool_engage = string.Empty;
                        if (doc.RootElement.TryGetProperty("ActivateList", out OutputDocument))
                        {
                            tool_engage = OutputDocument.ToString();
                        }
                        if (!validate_tool_name_list(tool_engage)) return false;
                    }
                    break;
                case DiscoverModes.RemoveMode:
                    {
                        string tool_disposal = string.Empty;
                        if (doc.RootElement.TryGetProperty("RemoveList", out OutputDocument))
                        {
                            tool_disposal = OutputDocument.ToString();
                        }
                        if (!validate_tool_name_list(tool_disposal)) return false;
                    }
                    break;
                    default: return false;
            }
            return true;
        }

        public override string GetToolJsonString()
        {
            if (json_template_cache is not null) return json_template_cache;
            else
            {
                json_template_cache = json_template_base;

                return json_template_base; 
            }
        }

        /// <summary>
        /// Assign the toolbox we're working with with this tool instance
        /// </summary>
        /// <param name="toolBox"></param>
        public void AssignToolBox(ButlerToolBench toolBox)
        {
            toolbox = toolBox;
        }

        /// <summary>
        /// Get the current box.
        /// </summary>
        /// <param name="toolBox"></param>
        /// <returns></returns>
        public ButlerToolBench? GetToolBox()
        {
            return toolbox ;
        }

  
        /// <summary>
        /// Clear all known tool sources
        /// </summary>
        public void ClearDiscoverSource()
        {
            toolCollectionSource.Clear();
        }

        /// <summary>
        /// Add a new source. Source will  searched for classes that implement <see cref="IButlerToolBaseInterface"/>
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>With discoverer implementing  <see cref="Initialize"/> you'll get your sources initialize called on first use. Any more beyond that, you'll need to manual call initialize if needed.</remarks>
        public void AddButlerToolSource(ButlerTool_DiscoverResource e, IButlerVaultKeyCollection KeyHandler)
        {
            toolCollectionSource.Add(e);
        }

        
        public override void Initialize()
        {
            if (toolCollectionSource.Count == 0)
            {
                throw new InvalidOperationException("Forgot to add tools before the first call. You should do that first by calling AddButlerSource or DefaultButlerSource");
            }
            foreach (var source in toolCollectionSource)
            {
                source.Initialize(Handler);
            }
        }

        public void AddDefaultButlerSources(IButlerVaultKeyCollection KeyHandler)
        {
            AddButlerToolSource(new DefaultButlerTool_DiscoverResource(), KeyHandler);
        }

        public void AssignButler(ButlerBase Target)
        {
            base.Jeeves = Target;
        }

        public void AddButlerToolSource(ButlerTool_DiscoverResource e)
        {
            toolCollectionSource.Add(e);
        }

    }
}
