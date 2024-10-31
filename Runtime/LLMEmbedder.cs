/// @file
/// @brief File implementing the LLMEmbedder.

using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public class LLMEmbedder : LLMCaller
    {
        protected override void SetLLM(LLM llmSet)
        {
            base.SetLLM(llmSet);
            if (llmSet != null && !llmSet.embeddingsOnly)
            {
                LLMUnitySetup.LogWarning($"The LLM {llmSet.name} set for LLMEmbeddings {gameObject.name} is not an embeddings-only model, accuracy may be sub-optimal");
            }
        }

        public override bool IsAutoAssignableLLM(LLM llmSet)
        {
            return llmSet.embeddingsOnly;
        }
    }
}
