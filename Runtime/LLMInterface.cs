/// @file
/// @brief File implementing the LLM server interfaces.
using System;
using System.Collections.Generic;

/// \cond HIDE
namespace LLMUnity
{
    [Serializable]
    public struct ChatRequest
    {
        public string prompt;
        public int id_slot;
        public float temperature;
        public int top_k;
        public float top_p;
        public float min_p;
        public int n_predict;
        public int n_keep;
        public bool stream;
        public List<string> stop;
        public float tfs_z;
        public float typical_p;
        public float repeat_penalty;
        public int repeat_last_n;
        public bool penalize_nl;
        public float presence_penalty;
        public float frequency_penalty;
        public string penalty_prompt;
        public int mirostat;
        public float mirostat_tau;
        public float mirostat_eta;
        public string grammar;
        public int seed;
        public bool ignore_eos;
        public Dictionary<int, string> logit_bias;
        public int n_probs;
        public bool cache_prompt;
        public List<ChatMessage> messages;
    }

    [Serializable]
    public struct SystemPromptRequest
    {
        public string prompt;
        public string system_prompt;
        public int n_predict;
    }

    [Serializable]
    public struct ChatResult
    {
        public int id_slot;
        public string content;
        public bool stop;
        public string generation_settings;
        public string model;
        public string prompt;
        public bool stopped_eos;
        public bool stopped_limit;
        public bool stopped_word;
        public string stopping_word;
        public string timings;
        public int tokens_cached;
        public int tokens_evaluated;
        public bool truncated;
        public bool cache_prompt;
        public bool system_prompt;
    }

    [Serializable]
    public struct MultiChatResult
    {
        public List<ChatResult> data;
    }

    [Serializable]
    public struct ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public struct TokenizeRequest
    {
        public string content;
    }

    [Serializable]
    public struct TokenizeResult
    {
        public List<int> tokens;
    }

    [Serializable]
    public struct EmbeddingsResult
    {
        public List<float> embedding;
    }

    [Serializable]
    public struct LoraWeightRequest
    {
        public int id;
        public float scale;
    }

    [Serializable]
    public struct LoraWeightRequestList
    {
        public List<LoraWeightRequest> loraWeights;
    }

    [Serializable]
    public struct LoraWeightResult
    {
        public int id;
        public string path;
        public float scale;
    }

    [Serializable]
    public struct LoraWeightResultList
    {
        public List<LoraWeightResult> loraWeights;
    }

    [Serializable]
    public struct TemplateResult
    {
        public string template;
    }

    [Serializable]
    public struct SlotRequest
    {
        public int id_slot;
        public string action;
        public string filepath;
    }

    [Serializable]
    public struct SlotResult
    {
        public int id_slot;
        public string filename;
    }
}
/// \endcond
