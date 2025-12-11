using UnityEngine;

namespace LLMUnity
{
    public class LLMCharacter : LLMAgent
    {
        public string prompt
        {
            get { return systemPrompt; }
            set { systemPrompt = value; }
        }

        public LLMCharacter()
        {
            Debug.LogWarning("LLMCharacter is deprecated and will be removed from future versions. Please Use LLMAgent instead.");
        }

        public void SetPrompt(string newPrompt, bool clearChat = true)
        {
            systemPrompt = newPrompt;
            if (clearChat) _ = ClearHistory();
        }
    }
}
