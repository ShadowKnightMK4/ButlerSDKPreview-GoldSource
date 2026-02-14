using ButlerToolContracts.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipes;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{


    /// <summary>
    /// This presents the list of messages that Butler passes to the LLM. It separates system from prompt injection, from context window. 
    /// </summary>
    /// <remarks>To access the simulated list as an index just use [n] style access.</remarks>
    /// <example>TrenchCoatChatCollection y;    y.AddMessage(MSG);   Console.Write(y[0]); </example>
    public class TrenchCoatChatCollection : ButlerChatCollectionBase , IButlerChatCollection
    {
        public class ReadOnlyStitcher<ButlerChatMessage> : IReadOnlyList<ButlerChatMessage>
        {
            IList<ButlerChatMessage> SystemMessages;
            IList<(ButlerChatMessage, IButlerToolPromptInjection)> PromptInjection;
            IList<ButlerChatMessage> RunningContextWindow;

            public ButlerChatMessage GetLastMessage()
            {
                return this[this.Count - 1];
            }
            public ReadOnlyStitcher(IList<ButlerChatMessage> systemMessages, IList<(ButlerChatMessage, IButlerToolPromptInjection)> promptInjection, IList<ButlerChatMessage> runningContextWindow)
            {
                SystemMessages = systemMessages;
                PromptInjection = promptInjection;
                RunningContextWindow = runningContextWindow;
            }

 
            public ReadOnlyStitcher(IList<ButlerChatMessage> systemMessages, IList<(ButlerChatMessage, IButlerToolPromptInjection)> promptInjection, IList<ButlerChatMessage> runningContextWindow, int i, int j): this(systemMessages, promptInjection, runningContextWindow)
            {
            }

 
            public ButlerChatMessage this[int index]
            {
                get
                {
   
                    if (index < (SystemMessages.Count))
                    {
                        return SystemMessages[index];
                    }
                    index -= SystemMessages.Count;
                    if (index < PromptInjection.Count)
                    {
                        return PromptInjection[index].Item1;
                    }
                    index -= PromptInjection.Count;
                    if (index < RunningContextWindow.Count)
                    {
                        return RunningContextWindow[index];
                    }

                    throw new IndexOutOfRangeException();


           
                }
            }

            public int Count => RunningContextWindow.Count + PromptInjection.Count + SystemMessages.Count;

            public int CountSystemMessages => SystemMessages.Count;
            public int CountPromptInjection => PromptInjection.Count;
            public int CountRunningContextWindow => RunningContextWindow.Count;
            public bool IsReadOnly => true;


            public IEnumerator<ButlerChatMessage> GetEnumerator()
            {
                if (SystemMessages is not null)
                {
                    foreach (var sys in SystemMessages)
                    {
                        yield return sys;
                    }
                }
                if (PromptInjection is not null)
                {
                    foreach (var ToolPrompt in PromptInjection)
                    {
                        yield return ToolPrompt.Item1;
                    }
                }

                if (RunningContextWindow is not null)
                {
                    foreach (var ContextWindow in RunningContextWindow)
                    {
                        yield return ContextWindow;
                    }
                }
            }

            public int IndexOf(ButlerChatMessage item)
            {
                var first = SystemMessages.IndexOf(item);
                if (first != -1) return first;
                
                int second = -1;
                for (int i = 0; i < PromptInjection.Count; i++)
                {
                    var InspectedItem = PromptInjection[i].Item1;
                    if (InspectedItem is not null)
                    {
                        if (InspectedItem.Equals(item))
                        {
                            second = i;
                            break;
                        }
                    }/*
                    if (PromptInjection[i].Item1.Equals(item))
                    {
                        second = i;
                        break;
                    }*/
                }
                if (second != -1) return SystemMessages.Count + second;
                var third = RunningContextWindow.IndexOf(item);
                if (third != -1) return SystemMessages.Count + PromptInjection.Count + third;
                return -1;
            }



            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        /// <summary>
        /// System (or developer) messages injected in the system prompt
        /// </summary>
        List<ButlerSystemChatMessage> SystemMessages = new List<ButlerSystemChatMessage>();
        /// <summary>
        /// Prompt injection messages from tools aka <see cref="IButlerToolPromptInjection"/> is implemented
        /// </summary>
        List<(ButlerChatMessage, IButlerToolPromptInjection)> PromptInjection = new();
        /// <summary>4
        /// The running context window
        /// </summary>
        List<ButlerChatMessage> RunningContextWindow = new();

        public IReadOnlyList<ButlerChatMessage> MessageBrowser
        {
            get
            {
                List<ButlerChatMessage> Sys = new();
                List<(ButlerChatMessage, IButlerToolPromptInjection)> Prompt = new();
                List<ButlerChatMessage> Slide = new();
                Sys.AddRange(SystemMessages);
                Prompt.AddRange(PromptInjection);
                Slide.AddRange(RunningContextWindow);
                return (IReadOnlyList<ButlerChatMessage>)new ReadOnlyStitcher<ButlerChatMessage>(Sys, Prompt, Slide);
            }


        }



        protected override ButlerChatMessage? AccessorMethod(int index)
        {
            if (index < SystemMessages.Count)
            {
                return SystemMessages[index];
            }
            index -= SystemMessages.Count;
            if (index < PromptInjection.Count)
            {
                return PromptInjection[index].Item1;
            }
            index -= PromptInjection.Count;
            if (index < RunningContextWindow.Count)
            {
                return RunningContextWindow[index];
            }
            return null;
        }


        /// <summary>
        /// Get a count of all messages in the collection
        /// </summary>
        public override int Count
        {
            get
            {
                return SystemMessages.Count + PromptInjection.Count + RunningContextWindow.Count;

            }
        }

        /// <summary>
        /// How many system prompt messages are in the current collection
        /// </summary>

        public int SystemPromptCount
        {
            get => SystemMessages.Count;
        }

        /// <summary>
        /// How many prompt injection messages are in the current collection
        /// </summary>
        public int PromptInjectionCount
        {
            get => PromptInjection.Count;
        }

        /// <summary>
        /// How many messages are in the running context window
        /// </summary>
        public int RunningContextWindowCount
        {
            get => RunningContextWindow.Count;  
        }
        /// <summary>
        /// The unredacted full message list (including system and tool messages)
        /// </summary>
        public IList<ButlerChatMessage> AuditLog => base._Messages;



        /// <summary>
        /// Add a message to the prompt injection section
        /// </summary>
        /// <param name="message">Message to add. I recommanded a <see cref="ButlerSystemChatMessage"/></param>
        /// <param name="Source">tool it's sourced from. Note: on removal <see cref="RemovePromptInjection(IButlerToolPromptInjection)"/> ALL TOOLS that match this are removed</param>
        public void AddPromptInjectionMessage(ButlerChatMessage message, IButlerToolPromptInjection Source)
        {
            PromptInjection.Add((message, Source)); 
        }

        /// <summary>
        /// Remove all prompt injection messages sourced from the given tool
        /// </summary>
        /// <param name="Source"></param>
        public void RemovePromptInjection(IButlerToolPromptInjection Source)
        {
            PromptInjection.RemoveAll(x => x.Item2 == Source);
        }

        /// <summary>
        /// Clear all the prompt inject section is one go.
        /// </summary>
        public void ClearPromptInjections()
        {
            PromptInjection.Clear();
        }


        internal enum ListSource
        {
            /// <summary>
            /// ERROR STATE
            /// </summary>
            FIRE =0,
            /// <summary>
            /// Sourcing from <see cref="SystemMessages"/>
            /// </summary>
            SystemPrompt,
            /// <summary>
            /// Sourcing from <see cref="PromptInjection"/>
            /// </summary>
            ToolInjectionPrompt,
            /// <summary>
            /// Sourcing from <see cref="RunningContextWindow"/>    
            /// </summary>
            ContextWindow
        }

        /// <summary>
        /// This method takes a given index and tells you which sublist it's from and the adjusted index for that sublist.
        /// </summary>
        /// <param name="index">index - <see cref="this[int]]"/>.</param>
        /// <param name="Source">The list the index reads from</param>
        /// <returns>the recalculated index to read the section for the ref list</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when out of bounds</exception>
        internal int CalcIndex(int index, ref ListSource Source)
        {
            if (index < 0 || index >= (this.SystemMessages.Count + this.PromptInjection.Count + this.RunningContextWindow.Count))
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                if (index < SystemMessages.Count)
                {
                    Source = ListSource.SystemPrompt;
                    return index;
                }
                else
                {
                    if (index >= SystemMessages.Count)
                    {
                        index -= SystemMessages.Count;
                    }


                    if (index < PromptInjection.Count)
                    {
                        Source = ListSource.ToolInjectionPrompt;
                        return index;
                    }
                    else
                    {
                        index -= PromptInjection.Count;

                        if (index < RunningContextWindow.Count)
                        {
                            Source = ListSource.ContextWindow;
                            return index;
                        }
                        else
                        {
                            throw new IndexOutOfRangeException();
                        }
                    }
                }
            }
        }

        public ReadOnlyStitcher<ButlerChatMessage> GetSliceOfMessages(int Min, int Max)
        {
            var cache = SystemMessages as IList<ButlerChatMessage>;
            if (cache == null)
            {
                cache = new List<ButlerChatMessage>();
                foreach (ButlerChatMessage message in SystemMessages)
                {
                    cache.Add(message);
                }
            }
          return new ReadOnlyStitcher<ButlerChatMessage>(cache, this.PromptInjection, this.RunningContextWindow.Take(new Range(Min, Max)).ToList());
        }
        
        public ButlerChatMessage this[int index]
        {
            get
            {
                ListSource Target = ListSource.FIRE;
                int adjusted = CalcIndex(index, ref Target);
                if (Target == ListSource.FIRE)
                {
                    throw new InvalidOperationException("UNKNOWN LIST SOURCE to Index in TrenchCoat!");
                }
                else
                {
                    switch (Target)
                    {
                        case ListSource.SystemPrompt:
                            {
                                return SystemMessages[adjusted];
                            }
                        case ListSource.ToolInjectionPrompt:
                            {
                                return PromptInjection[adjusted].Item1;
                            }
                        case ListSource.ContextWindow:
                            {
                                return RunningContextWindow[adjusted];
                            }
                        default:
                            {
                                throw new NotImplementedException("UNKNOWN LIST SOURCE");
                            }
                    }
                }
            }
            set
            {
                ListSource Target = ListSource.FIRE;
                int adjusted = CalcIndex(index, ref Target);
                if (Target == ListSource.FIRE)
                {
                    throw new InvalidOperationException("UNKNOWN LIST SOURCE to Index in TrenchCoat!");
                }
                else
                {
                    switch (Target)
                    {
                        case ListSource.SystemPrompt:
                            {
                                if (value is ButlerSystemChatMessage svalue)
                                {
                                    this.SystemMessages[adjusted] = svalue;
                                }
                                else
                                {
                                    throw new InvalidCastException("Cannot set non system message into system prompt area of TrenchCoatChatCollection");
                                }
                                break;
                            }
                        case ListSource.ToolInjectionPrompt:
                            {
                                var old_entry = PromptInjection[adjusted];
                                old_entry.Item1 = value;
                                this.PromptInjection[adjusted] = old_entry;
                                break;
                            }
                        case ListSource.ContextWindow:
                            {
                                this.RunningContextWindow[adjusted] = value;
                                break;
                            }
                    }
                }

            }
        }

        /// <summary>
        /// Seed a post tool call followup message for each tool call that returned data. If the tool implements <see cref="IButlerToolPostCallInjection"/> it will add specific instructions too.
        /// </summary>
        /// <param name="ToolCallRefocusing"></param>
        /// <param name="MessageTemplate"></param>
        public void AddPostToolCallFollowup(IReadOnlyList<(string callid,IButlerToolBaseInterface tool)> ToolCallRefocusing, string MessageTemplate)
        {
            ArgumentNullException.ThrowIfNull(ToolCallRefocusing);
            for (int i = 0; i < ToolCallRefocusing.Count; i++)
            {
                var entry = ToolCallRefocusing[i];
                string msg = $"[DIRECTIVE] A tool with call id ##{entry.ToString}## returned data. USE THAT DATA IN YOUR REPLY OR AS INPUT TO ANOTHER TOOL!!\\r\\n ";
                if (entry.tool is  IButlerToolPostCallInjection Ps)
                {
                    /* the tldrp lan is tuple or what ever it's called
                     * 
                     * Item1=call id
                     * Item 2 = toolname
                     * 
                     * message if not PostCallInjkection
                     * [DIRECTIVE] Tool (callid) name toolname ran. USE THAT OUTPUT! as temporary
                     * 
                     * message if post call 
                     * [DIRECTIVE] Tool (callid) toolname. Use that OUTPUT!: Specific instructurions <msg> / END DIRECTIVE)
                     */

                    msg += $"[DIRECTIVE] Tool specific instructions: \"{Ps.GetToolPostCallDirection()}\"/[END_DIRECTIVE]\r\n";
                }
                ButlerSystemChatMessage steering = new ButlerSystemChatMessage(msg);
                steering.IsTemporary = true;
                this.Add(steering);
            }
        }
        protected override void AddMessage(ButlerChatMessage message)
        {
            if (message.IsTemporary == false)
            {

                base.AddMessage(message); // which adds this perminate message to the audit log
                if (message is ButlerSystemChatMessage sysmsg)
                {
                    SystemMessages.Add(sysmsg);
                }
                else
                {
                    RunningContextWindow.Add(message);
                }
            }
            else
            {
                // main thing is we don't add temporary messages to the audit log
                if (message is ButlerSystemChatMessage sysmsg)
                {
                    SystemMessages.Add(sysmsg);
                }
                else
                    RunningContextWindow.Add(message);
            }
            AgeOutContextWindowMessages(MaxContextWindowMessages);


        }

        /// <summary>
        /// Remove all temporary messages from <see cref="SystemMessages"/>  and <see cref="RunningContextWindow"/>. To remove prompt injection messages use <see cref="RemovePromptInjection(IButlerToolPromptInjection)"/>
        /// </summary>
        public void RemoveTemporaryMessages()
        {
            for (int i = RunningContextWindow.Count - 1; i >= 0; i--)
            {
                if (RunningContextWindow[i].IsTemporary)
                {
                    RunningContextWindow.RemoveAt(i);
                }
            }
            for (int i = SystemMessages.Count -1; i >= 0; i--)
            {
                if (SystemMessages[i].IsTemporary)
                {
                    SystemMessages.RemoveAt(i);
                }
            }
            for (int i = PromptInjection.Count -1; i >= 0; i--)
            {
                if (PromptInjection[i].Item1.IsTemporary)
                {
                    PromptInjection.RemoveAt(i);
                }
            }
        }


        public const int DefaultMaxContextWindowMessages = 20;

        /// <summary>
        /// Disable context window trimming by seeting <see cref="MaxContextWindowMessages"/> to this
        /// </summary>
        public const int UnlimitedContextWindow = -1;
        /// <summary>
        /// The max number of messages to keep in the context window. Default is 20. If you set to <see cref="UnlimitedContextWindow"/> OR 0, no trimming of the context will occur. Warning: The underlying SDK can possibly through exceptions if you exceed context window. />
        /// </summary>
        /// <remarks>This ONLY effects <see cref="RunningContextWindow"/></remarks>
        /// <exception cref="InvalidOperationException">Thrown if less than or equal to 0 and not <see cref="UnlimitedContextWindow"/></exception>
        public int MaxContextWindowMessages
        {
            get => _AgeOutContextWindowMessages_Counter;
            set
            {
                if (value == UnlimitedContextWindow)
                {
                    _AgeOutContextWindowMessages_Counter = value;
                }
                else
                {
                    if (value > 0)
                    {
                        _AgeOutContextWindowMessages_Counter = value;
                    }
                    else
                    {
                        if ((value < 0) && (value != MaxContextWindowMessages))
                        {
                            throw new InvalidOperationException("Context Window limit needs to be >= 0 or equal to UnlimitedContextWindow value. Got invalid value");
                        }
                        _AgeOutContextWindowMessages_Counter = value;
                    }
                }
            }
        }

        private int _AgeOutContextWindowMessages_Counter = DefaultMaxContextWindowMessages;
        protected void AgeOutContextWindowMessages(int MaxMessages)
        {
            if (MaxMessages == UnlimitedContextWindow)
            {
                return;
            }
            while (RunningContextWindow.Count > MaxMessages)
            {
                if (RunningContextWindow[0].Role == ButlerChatMessageRole.ToolCall)
                {
                    RunningContextWindow.RemoveAt(0);
                    if (RunningContextWindow.Count != 0)
                    {
                        if (RunningContextWindow[0].Role == ButlerChatMessageRole.ToolResult)
                        {
                            RunningContextWindow.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    RunningContextWindow.RemoveAt(0);
                }
            }
        }

        IReadOnlyList<ButlerChatMessage> IButlerChatCollection.GetSliceOfMessages(int LastUserMessageIndex, int LastAiTurnIndex)
        {
            return GetSliceOfMessages(LastUserMessageIndex, LastAiTurnIndex);
        }
    }
    /// <summary>
    /// This lets the someone filter chat messages
    /// </summary>
    public class FilterdButlerChatCollection : ButlerChatCollection
    {
        /// <summary>
        /// If true, you'll get any system level messages. Probably gonna want to turn this to false
        /// </summary>
        public bool WantSystem = true;
        /// <summary>
        /// If true, you'll get direct raw tool results.
        /// </summary>
        public bool WantTools = true;
        public bool WantAssistant = true;
        public bool WantUser = true;
        public bool IsDirty = false;
        public FilterdButlerChatCollection()
        {
            // set our dirty flag;
            this.MessageAdded += (string name, ButlerChatMessage msg) => { IsDirty = true; };
            _Filter = new ObservableCollection<ButlerChatMessage>();

            this.PropertyChanged += (object? sender, PropertyChangedEventArgs e) => { IsDirty = true; };
        }


        /// <summary>
        ///  can be expansize if a lot of messages
        /// </summary>
        public void UpdateFilter()
        {
            if (IsDirty)
            {

                this._Filter.Clear();
                foreach (ButlerChatMessage x in Messages)
                {
                    switch (x.Role)
                    {
                        case  ButlerChatMessageRole.System: { if (this.WantSystem) _Filter.Add(x); { break; } }
                        case  ButlerChatMessageRole.Assistant: { if (this.WantAssistant) _Filter.Add(x); { break; } }
                        case  ButlerChatMessageRole.User: { if (this.WantUser) _Filter.Add(x); { break; } }
                        case  ButlerChatMessageRole.ToolCall: { if (this.WantTools) _Filter.Add(x); { break; } }
                    }

                }
                OnPropertyChained(nameof(FilterMessages));
            }
        }

        /// <summary>
        /// By default the filter will just check the bools exported and discard adding the message if that's unwanted.
        /// </summary>
        /// <param name="x"></param>
        /// <remarks>No need to directly call this only overring the <see cref="AddMessage(ChatMessage)"/> override here. It'll call this already </remarks>
        protected virtual void AddFilteredMessage(ButlerChatMessage x)
        {
            switch (x.Role)
            {
                case ButlerChatMessageRole.System: { if (this.WantSystem) _Filter.Add(x); { break; } }
                case ButlerChatMessageRole.Assistant: { if (this.WantAssistant) _Filter.Add(x); { break; } }
                case ButlerChatMessageRole.User: { if (this.WantUser) _Filter.Add(x); { break; } }
                case ButlerChatMessageRole.ToolCall: { if (this.WantTools) _Filter.Add(x); { break; } }
            }
        }
        
        protected override void AddMessage(ButlerChatMessage message)
        {
            IsDirty = true;
            base.AddMessage(message);
            AddFilteredMessage(message);
        }


        /// <summary>
        /// Messages the filter process go here.
        /// </summary>
        public ObservableCollection<ButlerChatMessage> FilterMessages
        {
            get
            {
                if (!IsDirty)
                {

                    return _Filter;
                }
                else
                {


                    IsDirty = false;
                    return this._Filter;

                }
            }
        }
        ObservableCollection<ButlerChatMessage> _Filter;

    }




    public class ButlerChatCollection : ButlerChatCollectionBase
    {
        public ObservableCollection<ButlerChatMessage> Messages
        {
            get => base._Messages;
        }
    }



    /// <summary>
    ///  Tracks a collection of <see cref="ChatMessage"/> in a way that can be bound to a UI control or just get notified a message was added or removed. Can pass as an IList too
    /// </summary>
    /// <remarks>Remeber to put a public access variable (SHOULD BE 'Message') that exports the <see cref="_Messages"/> To show example of why no - see <see cref="TrenchCoatChatCollection"/></remarks>
    public class ButlerChatCollectionBase : INotifyPropertyChanged, IList<ButlerChatMessage>
    {
        protected ObservableCollection<ButlerChatMessage> _Messages = new ObservableCollection<ButlerChatMessage>();

        #region publi bindable stuff for WPF/Maui
        public virtual int Count => _Messages.Count;

        public bool IsReadOnly => false;

        /// <summary>
        /// Small exposed collection of messages. BY DEFAULT THIS IS NOT Public
        /// </summary>
        /*protected virtual ObservableCollection<ButlerChatMessage> Messages
        {
            get => _Messages;
        }*/
        #endregion

        /// <summary>
        /// Triggers when something changes the collection. This is used to notify the UI or other listeners that a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// Triggers on when a message is added. string is currently always _Messages, and that ChatMessage is what was actually added.
        /// </summary>
        public event Action<string, ButlerChatMessage>? MessageAdded;
        //public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Triggers when a property changes. This is used to notify the UI or other listeners that a property has changed.
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChained(string name)
        {
            PropertyChanged?.Invoke(name, new PropertyChangedEventArgs(name));
        }

        protected void OnMessageAdded(string name, ButlerChatMessage message)
        {
            MessageAdded?.Invoke(name, message);
        }

        protected virtual void AddMessage(ButlerChatMessage message)
        {
            _Messages.Add(message);
            OnPropertyChained(nameof(_Messages));
            OnPropertyChained(nameof(Count));
            OnMessageAdded(nameof(_Messages), message);

        }

        public ReadOnlyCollection<ButlerChatMessage> GetMessages()
        {
            return new ReadOnlyCollection<ButlerChatMessage>(_Messages);
        }

        public ButlerChatMessage? GetLastMessage()
        {
            if (_Messages.Count > 0)
            {
                return _Messages[_Messages.Count - 1];
            }
            return null;
        }

        public void ClearMessages()
        {
            _Messages.Clear();
            OnPropertyChained(nameof(_Messages));
            OnPropertyChained(nameof(Count));
        }


        /// <summary>
        /// This default implementation just forwards to <see cref="_Messages"/>[index]. It's gonna throw <see cref="IndexOutOfRangeException"/> likely if you go out of bounds
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual ButlerChatMessage? AccessorMethod(int index)
        {
            return _Messages[index];
        }
        ButlerChatMessage IList<ButlerChatMessage>.this[int index]
        {
            get
            {
                var ret = AccessorMethod(index);
                if (ret is null)
                {
                    throw new IndexOutOfRangeException();
                }
                return ret;

            }
            set => throw new NotImplementedException();
        }

        //public ButlerChatMessage this[int index] => _Messages[index];

        public void AddAssistantMessage(string text)
        {
            ButlerChatMessage part = new ButlerAssistantChatMessage(text);
            AddMessage(part);
        }
        public void AddAssistantMessage(ButlerAssistantChatMessage msg)
        {
            AddMessage(msg);
        }
        public void AddUserMessage(string text)
        {
            ButlerChatMessage part = new ButlerUserChatMessage(text);
            part.Role = ButlerChatMessageRole.User;
            AddMessage(part);
        }

        public void AddToolMessage(string CallID, string text)
        {
            ButlerChatToolCallMessage part = new ButlerChatToolCallMessage(CallID, text);
            AddMessage(part);
        }

        public void AddSystemMessage(string text)
        {
            ButlerSystemChatMessage part = new ButlerSystemChatMessage(text);
            AddMessage(part);
        }

        public int IndexOf(ButlerChatMessage item)
        {
            return _Messages.IndexOf(item);
        }

        public void Insert(int index, ButlerChatMessage item)
        {
            _Messages.Insert(index, item);
            OnPropertyChained(nameof(_Messages));
            OnPropertyChained(nameof(Count));
        }

        public void RemoveAt(int index)
        {
            _Messages.RemoveAt(index);
            OnPropertyChained(nameof(_Messages));
            OnPropertyChained(nameof(Count));
        }

        public void Add(ButlerChatMessage item) => AddMessage(item);

        public void Clear()
        {
            ClearMessages();
            OnPropertyChained(nameof(Count));
            OnPropertyChained(nameof(_Messages));
        }

        public bool Contains(ButlerChatMessage item)
        {
            return _Messages.Contains(item);
        }

        public void CopyTo(ButlerChatMessage[] array, int arrayIndex)
        {
            _Messages.CopyTo(array, arrayIndex);
        }

        public bool Remove(ButlerChatMessage item)
        {
            bool r = _Messages.Remove(item);
            if (r) { OnPropertyChained(nameof(_Messages)); OnPropertyChained(nameof(Count)); }
            return r;
        }

        public IEnumerator<ButlerChatMessage> GetEnumerator()
        {
            return _Messages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


    }
}
