using System.Collections.Generic;
using System.Threading.Tasks;
using LLMUnity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LLMUnityTests
{
    public class TestLLMChatHistory
    {
        private GameObject _gameObject;
        private LLMChatHistory _chatHistory;


        [SetUp]
        public void Setup()
        {
            // Create a new GameObject
            _gameObject = new GameObject("TestObject");

            // Add the component X to the GameObject
            _chatHistory = _gameObject.AddComponent<LLMChatHistory>();
        }

        [Test]
        public async void TestSaveAndLoad()
        {
            // 1. ARRANGE
            // Add a few messages to save
            await _chatHistory.AddMessage("user", "hello");
            await _chatHistory.AddMessage("ai", "hi");

            // Save them off and grab the generated filename (since we didn't supply one)
            await _chatHistory.Save();
            string filename = _chatHistory.ChatHistoryFilename;

            // 2. ACT
            // Destroy the current chat history
            Object.Destroy(_gameObject);

            // Recreate the chat history and load from the same file
            Setup();
            _chatHistory.ChatHistoryFilename = filename;
            await _chatHistory.Load();

            // 3. ASSERT
            // Validate the messages were loaded
            List<ChatMessage> loadedMessages = _chatHistory.GetChatMessages();
            Assert.AreEqual(loadedMessages.Count, 2);
            Assert.AreEqual(loadedMessages[0].role, "user");
            Assert.AreEqual(loadedMessages[0].content, "hello");
            Assert.AreEqual(loadedMessages[1].role, "ai");
            Assert.AreEqual(loadedMessages[1].content, "hi");
        }

        [TearDown]
        public void Teardown()
        {
            // Cleanup the GameObject after the test
            Object.Destroy(_gameObject);
        }
    }
}