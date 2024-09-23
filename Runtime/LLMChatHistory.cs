/// @file
/// @brief File implementing the LLMChatHistory.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    /// @ingroup llm
    /// <summary>
    /// Manages a single instance of a chat history.
    /// </summary>
    public class LLMChatHistory : MonoBehaviour
    {
        /// <summary> 
        /// The name of the file where this chat history will be saved.
        /// The file will be saved within the persistentDataPath directory (see https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html). 
        /// </summary>
        public string ChatHistoryFilename = string.Empty;

        /// <summary>
        /// If true, this component will automatically save a copy of its data to the filesystem with each update.
        /// </summary>
        public bool EnableAutoSave = true;

        /// <summary>
        /// The current chat history
        /// </summary>
        protected List<ChatMessage> _chatHistory;

        /// <summary>
        /// Ensures we're not trying to update the chat while saving or loading
        /// </summary>
        protected SemaphoreSlim chatLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The Unity Awake function that initializes the state before the application starts.
        /// </summary>
        public async void Awake()
        {
            // If a filename has been provided for the chat history, attempt to load it
            if (ChatHistoryFilename != string.Empty) {
                await Load();
            }
            else {
                _chatHistory = new List<ChatMessage>();
            }
        }

        /// <summary>
        /// Appends a new message to the end of this chat.
        /// </summary>
        public async Task AddMessage(string role, string content)
        {
            await WithChatLock(async () => {
                await Task.Run(() => _chatHistory.Add(new ChatMessage { role = role, content = content }));
            });

            if (EnableAutoSave) {
                _ = Save();
            }
        }

        public List<ChatMessage> GetChatMessages() {
            return new List<ChatMessage>(_chatHistory);
        }

        /// <summary>
        /// Saves the chat history to the file system
        /// </summary>
        public async Task Save()
        {
            // If no filename has been provided, create one
            if (ChatHistoryFilename == string.Empty) {
                ChatHistoryFilename = $"chat_{Guid.NewGuid()}";
            }

            string filePath = GetChatHistoryFilePath();
            string directoryName = Path.GetDirectoryName(filePath);

            // Ensure the directory exists
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

            // Save the chat history as json
            await WithChatLock(async () => {
                string json = JsonUtility.ToJson(new ChatListWrapper { chat = _chatHistory });
                await File.WriteAllTextAsync(filePath, json);
            });
        }

        /// <summary>
        /// Load the chat history from the file system
        /// </summary>
        public async Task Load()
        {
            string filePath = GetChatHistoryFilePath();

            if (!File.Exists(filePath))
            {
                LLMUnitySetup.LogError($"File {filePath} does not exist.");
                return;
            }

            // Load the chat from the json file
            await WithChatLock(async () => {
                string json = await File.ReadAllTextAsync(filePath);
                _chatHistory = JsonUtility.FromJson<ChatListWrapper>(json).chat;
                LLMUnitySetup.Log($"Loaded {filePath}");
            });
        }

        /// <summary>
        /// Clears out the current chat history.
        /// </summary>
        public async Task Clear() {
            await WithChatLock(async () => {
                await Task.Run(() => _chatHistory.Clear());
            });

            if (EnableAutoSave) {
                _ = Save();
            }
        }

        public bool IsEmpty() {
            return _chatHistory?.Count == 0;
        }

        protected string GetChatHistoryFilePath()
        {
            return Path.Combine(Application.persistentDataPath, ChatHistoryFilename + ".json").Replace('\\', '/');
        }

        protected async Task WithChatLock(Func<Task> action) {
            await chatLock.WaitAsync();
            try {
                await action();
            }
            finally {
                chatLock.Release();
            }
        }
    }
}