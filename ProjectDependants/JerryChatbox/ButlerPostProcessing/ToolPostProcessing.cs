using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK;
using ButlerSDK.Core;
using ButlerSDK.ToolSupport;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using ButlerSDK.ButlerPostProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ButlerToolContracts.DataTypes;
using ButlerSDK.ToolSupport.Bench;

namespace ButlerSDK.ButlerPostProcessing
{
    internal class ToolWordRip
    {
    
        Regex Engine = new(@"(\b[A-Za-z]{3,}\b)");
        static readonly HashSet<string> ZeroValueWords = new(new string[] { "type", "object", "properties", "string", "description", "required", "items", "array", "enum", "default"});
        Dictionary<string, HashSet<string>> WordWords = new();
        public ToolWordRip()
        {
            
        }

        static bool IsCapitalLetter(int c)
        {
            if (c >= 'A')
            {
                if (c <= 'Z')
                    return true;
            }
            return false;
        }

        static List<string> SplitCamelCase(string name)
        {
            var ret = new List<string>();
            StringBuilder walker = new();
            for (int i = 0; i < name.Length; i++)
            {
                int c = name[i];
                if (walker.Length == 0)
                {
                    walker.Append((char)c);
                }
                else
                {
                    if (IsCapitalLetter(c))
                    {
                        ret.Add(walker.ToString());
                        walker.Clear();
                        walker.Append((char)c);

                    }
                    else
                    {
                        walker.Append(c);
                    }
                }

            }
            if (walker.Length > 0)
            {
                ret.Add(walker.ToString());
                walker.Clear();
            }
            return ret;
        }
        static void CamalCaseHandling(IButlerToolBaseInterface butler, HashSet<string> MissRet)
        {
            var alt = SplitCamelCase(butler.ToolName);
            foreach (string part in alt)
            {
                MissRet.Add(part);
            }
        }

        HashSet<string> CacheToolMiss(IButlerToolBaseInterface tool)
        {
            HashSet<string> ret =  new(); // also we add it to our cache

            var Matching = Engine.Matches(tool.GetToolJsonString());
            foreach (Match singleMatch in Matching)
            {
                if (singleMatch.Success)
                {
                    foreach (Capture capture in singleMatch.Captures)
                    {
                        if (ZeroValueWords.Contains(capture.Value) == false)
                        {
                            ret.Add(capture.Value);
                        }
                    }
                }
            }
            CamalCaseHandling(tool, ret);
            return ret;
        }
        HashSet<string> CacheToolCheck(IButlerToolBaseInterface tool)
        {
            HashSet<string>? Values;
            if (WordWords.TryGetValue(tool.ToolName, out Values))
            {
                if (Values is not null)
                {
                    return Values;
                }
                else
                {
                   var ret = CacheToolMiss(tool);
                    WordWords.Add(tool.ToolName,ret);
                    return ret;
                }
            }
            else
            {
                var ret = CacheToolMiss(tool);
                WordWords.Add(tool.ToolName, ret);
                return ret;
            }
        }

        public int CheckForToolCall(string text, IButlerChatCompletionOptions options)
        {
            int count = 0;
            HashSet<string> TextSum = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (Match m in Engine.Matches(text))
            {
                if (m.Success)
                {
                    if (ZeroValueWords.Contains(m.Value) == false)
                    {
                        TextSum.Add(m.Value);
                    }
                }
            }

            foreach (IButlerToolBaseInterface tool in options.Tools)
            {
                var check = CacheToolCheck(tool);
                
                {
                    var countthis =
                    check.Count(token => (TextSum.Contains(token) == true));
                    if (countthis > 0)
                    {

                        count++;
                        count += countthis;
                    }
                }
               
            }

            return count;
        }
    }
    public class ToolPostProcessing : IButlerPostProcessorQOS
    {
        ToolWordRip KeywordChecking = new();
        Queue<ButlerStreamingChatCompletionUpdate> BackBuffer = new();
        public int QueueSize => BackBuffer.Count;

        public ButlerStreamingChatCompletionUpdate? DeQueueBuffer()
        {
            return BackBuffer.Dequeue();
        }

        public int Toolcall_sensitivity { get; set; } = 2;
        public bool QosEnabled { get; set;} 
        protected virtual bool IsToolCallLikely(ButlerChatFinishReason? Reason, IList<ButlerChatMessage> Msgs, IButlerChatCompletionOptions Options, ButlerChatMessage Assistant)
        {
            int checks = 0;
            /* Our default tool check*/
            if ((Reason is not null) && (Reason ==  ButlerChatFinishReason.ToolCalls) )
                return true; // the provider itself got the tool call request.
            else
            {
                string Text = Assistant.GetCombinedText();

                if (!string.IsNullOrWhiteSpace(Text))
                {
                    if (Text.Contains("tool"))
                    {
                        checks++;
                    }

                    if (Text.Contains("argument"))
                    {
                        checks++;
                    }

                    foreach (var ToolInstance in Options.Tools)
                    {
                        if (Text.Contains(ToolInstance.ToolName))
                        {
                            checks++;
                        }
                    }
                    if (checks >= Toolcall_sensitivity)
                    {
                        return true;
                    }
                    else
                    {
                        if ((KeywordChecking.CheckForToolCall(Text, Options) + checks) > Toolcall_sensitivity)
                        {
                            return true;
                        }
                    }
                }
                 return false;
            }
        }
        public virtual IButlerPostProcessorHandler.EndOfAiStreamAction EndOfStreamAlert(ButlerChatFinishReason? Reason, IList<ButlerChatMessage> Msgs, ButlerChatMessage Assistent, IButlerChatCompletionOptions Options, bool ToolWasCalled)
        {
            if (ToolWasCalled)
            {

                return IButlerPostProcessorHandler.EndOfAiStreamAction.None;
            }
            if (IsToolCallLikely(Reason, Msgs, Options, Assistent)) 
                {
                   this.BackBuffer.Clear();
                    return IButlerPostProcessorHandler.EndOfAiStreamAction.TriggeredAndDiscard;
                }
            else
            {
                return IButlerPostProcessorHandler.EndOfAiStreamAction.None;
            }
        }

        public virtual IButlerPostProcessorHandler.PostProcessorAction ProcessReply(ButlerStreamingChatCompletionUpdate? update, bool WasToolTriggered)
        {
  

            {
                if (update is null)
                {
                    return IButlerPostProcessorHandler.PostProcessorAction.Discard;
                }
                if  ( (update.ContentUpdate is null) && (update.ToolCallUpdates is null) && (update.FinishReason is null))
                {
                    return IButlerPostProcessorHandler.PostProcessorAction.Discard;
                }
                this.BackBuffer.Enqueue(update);
                return IButlerPostProcessorHandler.PostProcessorAction.Buffered;

            }
        }

        
     

        public async Task<ButlerAssistantChatMessage?> FinalQOSCheck(IButlerLLMProvider Prov, IButlerChatClient QOSCheck, IButlerChatCollection Messages, int LastUserMessageIndex, int LastAiTurnIndex)
        {
            var Slice = Messages.GetSliceOfMessages(LastUserMessageIndex, LastAiTurnIndex);
            List<ButlerChatMessage> QOS = new();


            ButlerAssistantChatMessage Dummy = new ButlerAssistantChatMessage(QOS[QOS.Count - 1].GetCombinedText());
            Dummy.Role = ButlerChatMessageRole.Assistant;


            if (!QosEnabled)
            {
                return Dummy;
            }

            QOS.Add(new ButlerSystemChatMessage(@"Only [STEPS] and [FINAL INSTRUCTIONS] Are yours. The rest are the model whose reply are judging. DO NOT USE WHEN EVAL the output.\r\n" +

                                       "[DIRECTIVE PRIORITY] YOU ARE A QUALITY OF SERVICE AI. YOUR ONLY OBJECTIVE IS FOLLOW THESE RULES BELOW\r\n\r\n" +
                                       "#1 STRICTLY RESPOND VIA TEXT ONLY. NO IMAGES OR AUDIO\r\n\r\n" +
                                       "#2 Look at the user request. Is a tool call needed?\r\n\r\n" +
                                       "#3 If a tool call was needed, do the messages indicate a call happened?\r\n" +
                                       "#4 Does the model (LAST MESSAGE) accurately use provided data?\r\n" +
                                       "#5 Does the Model (LAST MESSAGE) use Jargon or just straight answering?\r\n" +
                                       "#6 Did the model answer (LAST MESSAGE) NOT USE GIBBERISH?\r\n" +
                                       "#7 Did the model actually answer the user, or dance around the request?" +


                                       "[FINAL INSTRUCTIONS]] #### After working thru steps #1 thru #7, if the answer is no to #7, rewrite the model turn (last message) to comply with #7. If you cannot (for example not enough data) respond with why not.")
                                            );
            QOS.AddRange(Slice);
            string message = string.Empty;
            try
            {
                QOS[QOS.Count - 1] = Dummy;
                IButlerChatCompletionOptions def = Prov.ChatCreationProvider.DefaultOptions;
                def.Temperature = 0;
                ButlerChatFinishReason reason;
                await foreach (var StreamPart in QOSCheck.CompleteChatStreamingAsync(QOS, def))
                {
                    if (StreamPart.FinishReason != null)
                        reason = (ButlerChatFinishReason)StreamPart.FinishReason;
                    for (int i = 0; i < StreamPart.ContentUpdate.Count; i++)
                    {
                        if (StreamPart.ContentUpdate[i].Kind == ButlerChatMessagePartKind.Text)
                        {
                            message += StreamPart.ContentUpdate[i].Text;
                        }
                    }

                }
                if (message != string.Empty)
                {
                    return new ButlerAssistantChatMessage(message);
                }
                return null;
            }
            catch (Exception)
            {
                return null;// tell asynch streaming that we DON'T DISCARD the message
            }

        }

 

        public void Remedial(IButlerChatCollection Msgs, IButlerToolResolver Resolver, IButlerToolBench Toolset)
        {

            ButlerSystemChatMessage Alert = new(@"[ERROR] IT LOOKS LIKE YOU TALKED ABOUT CALLING A TOOL WITHOUT CALLING IT. [REMEDIAL STEPS] Look at the last user and assistant turn. Produce the json needed to answer with no other dialog! ");

            Alert.IsTemporary = true;
            Msgs.Add(Alert);
        }
    }
}
