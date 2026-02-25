using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UndreamAI.LlamaLib
{
    // Data structure for chat messages
    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        public ChatMessage(string _role, string _content)
        {
            role = _role;
            content = _content;
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["role"] = role,
                ["content"] = content
            };
        }

        public static ChatMessage FromJson(JObject json)
        {
            return new ChatMessage(
                json["role"]?.ToString() ?? string.Empty,
                json["content"]?.ToString() ?? string.Empty
            );
        }

        public override bool Equals(object obj)
        {
            if (obj is not ChatMessage other)
                return false;
            return role == other.role && content == other.content;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (role?.GetHashCode() ?? 0);
                hash = hash * 23 + (content?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{role}: {content}";
        }
    }

    /// <summary>Strategy to apply when the chat history would exceed the model's context window.</summary>
    public enum ContextOverflowStrategy
    {
        /// <summary>No automatic handling — may crash if context is exceeded.</summary>
        None = 0,
        /// <summary>Remove oldest message pairs from the front until history fits within targetRatio of the context.</summary>
        Truncate = 1,
        /// <summary>Summarise history, embed summary in the system message, then truncate if still needed.</summary>
        Summarize = 2
    }

    // LLMAgent class
    public class LLMAgent : LLMLocal
    {
        private LLMLocal llmBase;

        public LLMAgent(LLMLocal _llm, string _systemPrompt = "")
        {
            if (_llm == null)
                throw new ArgumentNullException(nameof(_llm));
            if (_llm.disposed)
                throw new ObjectDisposedException(nameof(_llm));

            llmBase = _llm;
            llamaLib = llmBase.llamaLib;

            llm = llamaLib.LLMAgent_Construct(llmBase.llm, _systemPrompt ?? string.Empty);
            if (llm == IntPtr.Zero) throw new InvalidOperationException("Failed to create LLMAgent");
        }

        // Properties
        public int SlotId
        {
            get
            {
                CheckLlamaLib();
                return llamaLib.LLMAgent_Get_Slot(llm);
            }
            set
            {
                CheckLlamaLib();
                llamaLib.LLMAgent_Set_Slot(llm, value);
            }
        }

        public string SystemPrompt
        {
            get
            {
                CheckLlamaLib();
                return Marshal.PtrToStringAnsi(llamaLib.LLMAgent_Get_System_Prompt(llm)) ?? "";
            }
            set
            {
                CheckLlamaLib();
                llamaLib.LLMAgent_Set_System_Prompt(llm, value ?? string.Empty);
            }
        }

        // History management
        public JArray History
        {
            get
            {
                CheckLlamaLib();
                IntPtr result = llamaLib.LLMAgent_Get_History(llm);
                string historyStr = Marshal.PtrToStringAnsi(result) ?? "[]";
                try
                {
                    return JArray.Parse(historyStr);
                }
                catch
                {
                    return new JArray();
                }
            }
            set
            {
                CheckLlamaLib();
                string historyJson = value?.ToString() ?? "[]";
                llamaLib.LLMAgent_Set_History(llm, historyJson);
            }
        }

        public List<ChatMessage> GetHistory()
        {
            var history = History;
            var messages = new List<ChatMessage>();

            try
            {
                foreach (var item in history)
                {
                    if (item is JObject messageObj)
                    {
                        messages.Add(ChatMessage.FromJson(messageObj));
                    }
                }
            }
            catch {}

            return messages;
        }

        public void SetHistory(List<ChatMessage> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var historyArray = new JArray();
            foreach (var message in messages)
            {
                historyArray.Add(message.ToJson());
            }
            History = historyArray;
        }

        public void ClearHistory()
        {
            CheckLlamaLib();
            llamaLib.LLMAgent_Clear_History(llm);
        }

        public void AddUserMessage(string content)
        {
            CheckLlamaLib();
            llamaLib.LLMAgent_Add_User_Message(llm, content ?? string.Empty);
        }

        public void AddAssistantMessage(string content)
        {
            CheckLlamaLib();
            llamaLib.LLMAgent_Add_Assistant_Message(llm, content ?? string.Empty);
        }

        public void RemoveLastMessage()
        {
            CheckLlamaLib();
            llamaLib.LLMAgent_Remove_Last_Message(llm);
        }

        public void SaveHistory(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new ArgumentNullException(nameof(filepath));

            CheckLlamaLib();
            llamaLib.LLMAgent_Save_History(llm, filepath ?? string.Empty);
        }

        public void LoadHistory(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new ArgumentNullException(nameof(filepath));

            CheckLlamaLib();
            llamaLib.LLMAgent_Load_History(llm, filepath ?? string.Empty);
        }

        public int GetHistorySize()
        {
            CheckLlamaLib();
            return llamaLib.LLMAgent_Get_History_Size(llm);
        }

        // Context overflow management

        /// <summary>
        /// Configure how the agent handles context overflow.
        /// </summary>
        /// <param name="strategy">Overflow strategy to use.</param>
        /// <param name="targetRatio">Target fill ratio of context after truncation (0.0–1.0, default 0.5).</param>
        /// <param name="summarizePrompt">Custom prompt for summarization; null uses the built-in default.</param>
        public void SetOverflowStrategy(ContextOverflowStrategy strategy, float targetRatio = 0.5f, string summarizePrompt = null)
        {
            CheckLlamaLib();
            llamaLib.LLMAgent_Set_Overflow_Strategy(llm, (int)strategy, targetRatio, summarizePrompt);
        }

        /// <summary>
        /// Get the current rolling summary generated by the Summarize overflow strategy.
        /// Returns an empty string if no summary has been generated yet.
        /// </summary>
        public string GetSummary()
        {
            CheckLlamaLib();
            return llamaLib.LLMAgent_Get_Summary(llm) ?? string.Empty;
        }

        /// <summary>
        /// Set the rolling summary directly, e.g. to restore state after loading from file.
        /// </summary>
        public void SetSummary(string summary)
        {
            CheckLlamaLib();
            llamaLib.LLMAgent_Set_Summary(llm, summary ?? string.Empty);
        }

        // Chat functionality
        public string Chat(string userPrompt, bool addToHistory = true, LlamaLib.CharArrayCallback callback = null, bool returnResponseJson = false, bool debugPrompt = false)
        {
            CheckLlamaLib();
            IntPtr result = llamaLib.LLMAgent_Chat(llm, userPrompt ?? string.Empty, addToHistory, callback, returnResponseJson, debugPrompt);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public async Task<string> ChatAsync(string userPrompt, bool addToHistory = true, LlamaLib.CharArrayCallback callback = null, bool returnResponseJson = false, bool debugPrompt = false)
        {
            return await Task.Run(() => Chat(userPrompt, addToHistory, callback, returnResponseJson, debugPrompt));
        }

        // Override completion methods to use agent-specific implementations
        public string Completion(string prompt, LlamaLib.CharArrayCallback callback = null)
        {
            return Completion(prompt, callback, SlotId);
        }

        public async Task<string> CompletionAsync(string prompt, LlamaLib.CharArrayCallback callback = null)
        {
            return await Task.Run(() => Completion(prompt, callback));
        }

        public string SaveSlot(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new ArgumentNullException(nameof(filepath));

            CheckLlamaLib();
            IntPtr result = llamaLib.LLM_Save_Slot(llm, SlotId, filepath ?? string.Empty);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public string LoadSlot(string filepath)
        {
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                throw new ArgumentNullException(nameof(filepath));

            CheckLlamaLib();
            IntPtr result = llamaLib.LLM_Load_Slot(llm, SlotId, filepath ?? string.Empty);
            return Marshal.PtrToStringAnsi(result) ?? string.Empty;
        }

        public void Cancel()
        {
            CheckLlamaLib();
            llamaLib.LLM_Cancel(llm, SlotId);
        }

        // Override slot-based methods to hide them
        private new string SaveSlot(int id_slot, string filepath)
        {
            return SaveSlot(filepath);
        }

        private new string LoadSlot(int id_slot, string filepath)
        {
            return LoadSlot(filepath);
        }

        private new void Cancel(int id_slot)
        {
            Cancel();
        }
    }
}
