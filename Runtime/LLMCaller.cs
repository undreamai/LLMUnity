/// @file
/// @brief File implementing the basic functionality for LLM callers.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing calling of LLM functions (local and remote).
    /// </summary>
    public class LLMCaller : MonoBehaviour
    {
        /// <summary> show/hide advanced options in the GameObject </summary>
        [Tooltip("show/hide advanced options in the GameObject")]
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> use remote LLM server </summary>
        [Tooltip("use remote LLM server")]
        [LocalRemote] public bool remote = false;
        /// <summary> LLM GameObject to use </summary>
        [Tooltip("LLM GameObject to use")] // Tooltip: ignore
        [Local, SerializeField] protected LLM _llm;
        public LLM llm
        {
            get => _llm;
            set => SetLLM(value);
        }
        /// <summary> API key for the remote server </summary>
        [Tooltip("API key for the remote server")]
        [Remote] public string APIKey;
        /// <summary> host of the remote LLM server </summary>
        [Tooltip("host of the remote LLM server")]
        [Remote] public string host = "localhost";
        /// <summary> port of the remote LLM server </summary>
        [Tooltip("port of the remote LLM server")]
        [Remote] public int port = 13333;
        /// <summary> number of retries to use for the remote LLM server requests (-1 = infinite) </summary>
        [Tooltip("number of retries to use for the remote LLM server requests (-1 = infinite)")]
        [Remote] public int numRetries = 10;

        protected LLM _prellm;

        [Local, SerializeField] protected LLMClient _llmClient;
        public LLMClient llmClient
        {
            get => _llmClient;
            protected set => SetLLMClient(value);
        }

        /// <summary>
        /// The Unity Awake function that initializes the state before the application starts.
        /// The following actions are executed:
        /// - the corresponding LLM server is defined (if ran locally)
        /// - the grammar is set based on the grammar file
        /// - the prompt and chat history are initialised
        /// - the chat template is constructed
        /// - the number of tokens to keep are based on the system prompt (if setNKeepToPrompt=true)
        /// </summary>
        public virtual void Awake()
        {
            // Start the LLM server in a cross-platform way
            if (!enabled) return;

            if (!remote)
            {
                AssignLLM();
                if (llm == null)
                {
                    string error = $"No LLM assigned or detected for LLMAgent {name}!";
                    LLMUnitySetup.LogError(error);
                    throw new Exception(error);
                }
                llmClient = new LLMClient(llm.llmService);
            }
            else
            {
                llmClient = new LLMClient(host, port, APIKey);
            }
        }

        /// <summary>
        /// Sets the LLM object of the LLMCaller
        /// </summary>
        /// <param name="llmSet">LLM object</param>
        protected virtual void SetLLM(LLM llmSet)
        {
            if (llmSet != null && !IsValidLLM(llmSet))
            {
                LLMUnitySetup.LogError(NotValidLLMError());
                llmSet = null;
            }
            _llm = llmSet;
            _prellm = _llm;
            if (!remote) llmClient = new LLMClient(llm.llmService);
        }

        protected virtual void SetLLMClient(LLMClient llmClientSet)
        {
            llmClient = llmClientSet;
        }

        /// <summary>
        /// Checks if a LLM is valid for the LLMCaller
        /// </summary>
        /// <param name="llmSet">LLM object</param>
        /// <returns>bool specifying whether the LLM is valid</returns>
        public virtual bool IsValidLLM(LLM llmSet)
        {
            return true;
        }

        /// <summary>
        /// Checks if a LLM can be auto-assigned if the LLM of the LLMCaller is null
        /// </summary>
        /// <param name="llmSet"LLM object></param>
        /// <returns>bool specifying whether the LLM can be auto-assigned</returns>
        public virtual bool IsAutoAssignableLLM(LLM llmSet)
        {
            return true;
        }

        protected virtual string NotValidLLMError()
        {
            return $"Can't set LLM {llm.name} to {name}";
        }

        protected virtual void OnValidate()
        {
            if (_llm != _prellm) SetLLM(_llm);
            AssignLLM();
        }

        protected virtual void Reset()
        {
            AssignLLM();
        }

        protected virtual void AssignLLM()
        {
            if (remote || llm != null) return;

            List<LLM> validLLMs = new List<LLM>();
#if UNITY_6000_0_OR_NEWER
            foreach (LLM foundllm in FindObjectsByType(typeof(LLM), FindObjectsSortMode.None))
#else
            foreach (LLM foundllm in FindObjectsOfType<LLM>())
#endif
            {
                if (IsValidLLM(foundllm) && IsAutoAssignableLLM(foundllm)) validLLMs.Add(foundllm);
            }
            if (validLLMs.Count == 0) return;

            llm = SortLLMsByBestMatching(validLLMs.ToArray())[0];
            string msg = $"Assigning LLM {llm.name} to {GetType()} {name}";
            if (llm.gameObject.scene != gameObject.scene) msg += $" from scene {llm.gameObject.scene}";
            LLMUnitySetup.Log(msg);
        }

        protected virtual LLM[] SortLLMsByBestMatching(LLM[] arrayIn)
        {
            LLM[] array = (LLM[])arrayIn.Clone();
            for (int i = 0; i < array.Length - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < array.Length - i - 1; j++)
                {
                    bool sameScene = array[j].gameObject.scene == array[j + 1].gameObject.scene;
                    bool swap = (
                        (!sameScene && array[j + 1].gameObject.scene == gameObject.scene) ||
                        (sameScene && array[j].transform.GetSiblingIndex() > array[j + 1].transform.GetSiblingIndex())
                    );
                    if (swap)
                    {
                        LLM temp = array[j];
                        array[j] = array[j + 1];
                        array[j + 1] = temp;
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
            return array;
        }

        /// <summary>
        /// Allows to cancel the requests in a specific slot of the LLM
        /// </summary>
        /// <param name="id_slot">slot of the LLM</param>
        public void CancelRequest(int id_slot)
        {
            llmClient.Cancel(id_slot);
        }

        /// <summary>
        /// Tokenises the provided query.
        /// </summary>
        /// <param name="query">query to tokenise</param>
        /// <param name="callback">callback function called with the result tokens</param>
        /// <returns>list of the tokens</returns>
        public virtual List<int> Tokenize(string query, Callback<List<int>> callback = null)
        {
            List<int> tokens = llmClient.Tokenize(query);
            callback?.Invoke(tokens);
            return tokens;
        }

        /// <summary>
        /// Detokenises the provided tokens to a string.
        /// </summary>
        /// <param name="tokens">tokens to detokenise</param>
        /// <param name="callback">callback function called with the result string</param>
        /// <returns>the detokenised string</returns>
        public virtual string Detokenize(List<int> tokens, Callback<string> callback = null)
        {
            // handle the detokenization of a message by the user
            string prompt = llmClient.Detokenize(tokens);
            callback?.Invoke(prompt);
            return prompt;
        }

        /// <summary>
        /// Computes the embeddings of the provided input.
        /// </summary>
        /// <param name="tokens">input to compute the embeddings for</param>
        /// <param name="callback">callback function called with the result string</param>
        /// <returns>the computed embeddings</returns>
        public virtual List<float> Embeddings(string query, Callback<List<float>> callback = null)
        {
            // handle the tokenization of a message by the user
            List<float> embeddings = llmClient.Embeddings(query);
            callback?.Invoke(embeddings);
            return embeddings;
        }

        /// <summary>
        /// Completion functionality of the LLM.
        /// It calls the LLM completion based solely on the provided prompt (no formatting by the chat template).
        /// The function allows callbacks when the response is partially or fully received.
        /// </summary>
        /// <param name="prompt">user query</param>
        /// <param name="callback">callback function that receives the response as string</param>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <returns>the LLM response</returns>
        public virtual string CompletionSync(string prompt, LlamaLib.CharArrayCallback callback = null)
        {
            // handle a completion request by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received

            return llmClient.Completion(prompt, callback);
        }

        /// <summary>
        /// Completion functionality of the LLM (async).
        /// It calls the LLM completion based solely on the provided prompt (no formatting by the chat template).
        /// The function allows callbacks when the response is partially or fully received.
        /// </summary>
        /// <param name="prompt">user query</param>
        /// <param name="callback">callback function that receives the response as string</param>
        /// <param name="completionCallback">callback function called when the full response has been received</param>
        /// <returns>the LLM response</returns>
        public virtual async Task<string> Completion(string prompt, LlamaLib.CharArrayCallback callback = null, EmptyCallback completionCallback = null)
        {
            // handle a completion request by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received

            string result = await llmClient.CompletionAsync(prompt, callback);
            completionCallback?.Invoke();
            return result;
        }
    }
}
