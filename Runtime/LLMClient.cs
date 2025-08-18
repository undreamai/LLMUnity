/// @file
/// @brief File implementing the basic functionality for LLM callers.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UndreamAI.LlamaLib;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.IO;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing calling of LLM functions (local and remote).
    /// </summary>
    public class LLMClient : MonoBehaviour
    {
        /// <summary> show/hide advanced options in the GameObject </summary>
        [Tooltip("show/hide advanced options in the GameObject")]
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> use remote LLM server </summary>
        [Tooltip("use remote LLM server")]
        [LocalRemote, SerializeField] protected bool _remote;
        public bool remote
        {
            get => _remote;
            set { if (_remote != value) { _remote = value; SetupLLMClient(); } }
        }
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
        [Remote, SerializeField] protected string _APIKey;
        public string APIKey
        {
            get => _APIKey;
            set { if (_APIKey != value) { _APIKey = value; SetupLLMClient(); } }
        }
        /// <summary> host of the remote LLM server </summary>
        [Tooltip("host of the remote LLM server")]
        [Remote, SerializeField] protected string _host = "localhost";
        public string host
        {
            get => _host;
            set { if (_host != value) { _host = value; SetupLLMClient(); } }
        }
        /// <summary> port of the remote LLM server </summary>
        [Tooltip("port of the remote LLM server")]
        [Remote, SerializeField] protected int _port;
        public int port
        {
            get => _port;
            set { if (_port != value) { _port = value; SetupLLMClient(); } }
        }

        /// <summary> grammar used by the LLM </summary>
        [Tooltip("grammar used by the LLM")]
        [ModelAdvanced, SerializeField] protected string _grammar;
        public string grammar
        {
            get => _grammar;
            set => SetGrammar(value);
        }
        /// <summary> maximum number of tokens that the LLM will predict (-1 = infinity). </summary>
        [Tooltip("maximum number of tokens that the LLM will predict (-1 = infinity).")]
        [Model] public int numPredict = -1;
        /// <summary> cache the processed prompt to avoid reprocessing the entire prompt every time (default: true, recommended!) </summary>
        [Tooltip("cache the processed prompt to avoid reprocessing the entire prompt every time (default: true, recommended!)")]
        [ModelAdvanced] public bool cachePrompt = true;
        /// <summary> seed for reproducibility (-1 = no reproducibility). </summary>
        [Tooltip("seed for reproducibility (-1 = no reproducibility).")]
        [ModelAdvanced] public int seed = 0;
        /// <summary> LLM temperature, lower values give more deterministic answers. </summary>
        [Tooltip("LLM temperature, lower values give more deterministic answers.")]
        [ModelAdvanced, Float(0f, 2f)] public float temperature = 0.2f;
        /// <summary> Top-k sampling selects the next token only from the top k most likely predicted tokens (0 = disabled).
        /// Higher values lead to more diverse text, while lower value will generate more focused and conservative text.
        /// </summary>
        [Tooltip("Top-k sampling selects the next token only from the top k most likely predicted tokens (0 = disabled). Higher values lead to more diverse text, while lower value will generate more focused and conservative text. ")]
        [ModelAdvanced, Int(-1, 100)] public int topK = 40;
        /// <summary> Top-p sampling selects the next token from a subset of tokens that together have a cumulative probability of at least p (1.0 = disabled).
        /// Higher values lead to more diverse text, while lower value will generate more focused and conservative text.
        /// </summary>
        [Tooltip("Top-p sampling selects the next token from a subset of tokens that together have a cumulative probability of at least p (1.0 = disabled). Higher values lead to more diverse text, while lower value will generate more focused and conservative text. ")]
        [ModelAdvanced, Float(0f, 1f)] public float topP = 0.9f;
        /// <summary> minimum probability for a token to be used. </summary>
        [Tooltip("minimum probability for a token to be used.")]
        [ModelAdvanced, Float(0f, 1f)] public float minP = 0.05f;
        /// <summary> Penalty based on repeated tokens to control the repetition of token sequences in the generated text. </summary>
        [Tooltip("Penalty based on repeated tokens to control the repetition of token sequences in the generated text.")]
        [ModelAdvanced, Float(0f, 2f)] public float repeatPenalty = 1.1f;
        /// <summary> Penalty based on token presence in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled). </summary>
        [Tooltip("Penalty based on token presence in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled).")]
        [ModelAdvanced, Float(0f, 1f)] public float presencePenalty = 0f;
        /// <summary> Penalty based on token frequency in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled). </summary>
        [Tooltip("Penalty based on token frequency in previous responses to control the repetition of token sequences in the generated text. (0.0 = disabled).")]
        [ModelAdvanced, Float(0f, 1f)] public float frequencyPenalty = 0f;
        /// <summary> enable locally typical sampling (1.0 = disabled). Higher values will promote more contextually coherent tokens, while  lower values will promote more diverse tokens. </summary>
        [Tooltip("enable locally typical sampling (1.0 = disabled). Higher values will promote more contextually coherent tokens, while  lower values will promote more diverse tokens.")]
        [ModelAdvanced, Float(0f, 1f)] public float typicalP = 1f;
        /// <summary> last n tokens to consider for penalizing repetition (0 = disabled, -1 = ctx-size). </summary>
        [Tooltip("last n tokens to consider for penalizing repetition (0 = disabled, -1 = ctx-size).")]
        [ModelAdvanced, Int(0, 2048)] public int repeatLastN = 64;
        /// <summary> enable Mirostat sampling, controlling perplexity during text generation (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0). </summary>
        [Tooltip("enable Mirostat sampling, controlling perplexity during text generation (0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0).")]
        [ModelAdvanced, Int(0, 2)] public int mirostat = 0;
        /// <summary> The Mirostat target entropy (tau) controls the balance between coherence and diversity in the generated text. </summary>
        [Tooltip("The Mirostat target entropy (tau) controls the balance between coherence and diversity in the generated text.")]
        [ModelAdvanced, Float(0f, 10f)] public float mirostatTau = 5f;
        /// <summary> The Mirostat learning rate (eta) controls how quickly the algorithm responds to feedback from the generated text. </summary>
        [Tooltip("The Mirostat learning rate (eta) controls how quickly the algorithm responds to feedback from the generated text.")]
        [ModelAdvanced, Float(0f, 1f)] public float mirostatEta = 0.1f;
        /// <summary> if greater than 0, the response also contains the probabilities of top N tokens for each generated token. </summary>
        [Tooltip("if greater than 0, the response also contains the probabilities of top N tokens for each generated token.")]
        [ModelAdvanced, Int(0, 10)] public int nProbs = 0;
        /// <summary> ignore end of stream token and continue generating. </summary>
        [Tooltip("ignore end of stream token and continue generating.")]
        [ModelAdvanced] public bool ignoreEos = false;

        protected LLM _prellm;
        [Local, SerializeField] protected UndreamAI.LlamaLib.LLMClient _llmClient;
        public UndreamAI.LlamaLib.LLMClient llmClient
        {
            get => _llmClient;
            protected set => SetLLMClient(value);
        }

        bool started = false;
        string completionParametersPre = "";

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
            }
        }

        public virtual void Start()
        {
            if (!enabled) return;
            SetupLLMClient();
            started = true;
        }

        protected virtual void SetupLLMClient()
        {
            if (!remote) llmClient = new UndreamAI.LlamaLib.LLMClient(llm.llmService);
            else llmClient = new UndreamAI.LlamaLib.LLMClient(host, port, APIKey);
            SetGrammar(grammar);
            completionParametersPre = "";
        }

        protected virtual LLMLocal GetCaller()
        {
            return _llmClient;
        }

        /// <summary>
        /// Sets the LLM object of the LLMClient
        /// </summary>
        /// <param name="llmSet">LLM object</param>
        protected virtual void SetLLM(LLM llmSet)
        {
            if (llmSet == _llm) return;
            if (remote)
            {
                LLMUnitySetup.LogError("The client is in remote mode");
                return;
            }
            _llm = llmSet;
            _prellm = _llm;
            if (started) SetupLLMClient();
        }

        protected virtual void SetLLMClient(UndreamAI.LlamaLib.LLMClient llmClientSet)
        {
            _llmClient = llmClientSet;
        }

        /// <summary>
        /// Checks if a LLM can be auto-assigned if the LLM of the LLMClient is null
        /// </summary>
        /// <param name="llmSet"LLM object></param>
        /// <returns>bool specifying whether the LLM can be auto-assigned</returns>
        public virtual bool IsAutoAssignableLLM(LLM llmSet)
        {
            return true;
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
                if (IsAutoAssignableLLM(foundllm)) validLLMs.Add(foundllm);
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
        /// Sets the provided grammar (gbnf or json schema format)
        /// </summary>
        /// <param name="grammarString">grammar in gbnf or json schema format</param>
        public virtual void SetGrammar(string grammarString)
        {
            grammar = grammarString;
            GetCaller().SetGrammar(grammarString);
        }

        /// <summary>
        /// Loads a grammar file
        /// </summary>
        /// <param name="path">path to the grammar file</param>
        public virtual void LoadGrammar(string path)
        {
            if (String.IsNullOrEmpty(path)) return;
            SetGrammar(File.ReadAllText(path));
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

        protected virtual void SetCompletionParameters()
        {
            if (llm.embeddingsOnly)
            {
                string error = "LLM can't be used for completion, it is an embeddings only model!";
                LLMUnitySetup.LogError(error);
                throw new Exception(error);
            }

            JObject json = new JObject
            {
                ["temperature"] = temperature,
                ["top_k"] = topK,
                ["top_p"] = topP,
                ["min_p"] = minP,
                ["n_predict"] = numPredict,
                ["typical_p"] = typicalP,
                ["repeat_penalty"] = repeatPenalty,
                ["repeat_last_n"] = repeatLastN,
                ["presence_penalty"] = presencePenalty,
                ["frequency_penalty"] = frequencyPenalty,
                ["mirostat"] = mirostat,
                ["mirostat_tau"] = mirostatTau,
                ["mirostat_eta"] = mirostatEta,
                ["seed"] = seed,
                ["ignore_eos"] = ignoreEos,
                ["n_probs"] = nProbs,
                ["cache_prompt"] = cachePrompt
            };
            string completionParameters = json.ToString();
            if (completionParameters != completionParametersPre)
            {
                GetCaller().SetCompletionParameters(json);
                completionParametersPre = completionParameters;
            }
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
        public virtual string Completion(string prompt, LlamaLib.CharArrayCallback callback = null, int id_slot = -1)
        {
            // handle a completion request by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received

            SetCompletionParameters();
            return llmClient.Completion(prompt, callback, id_slot);
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
        public virtual async Task<string> CompletionAsync(string prompt, LlamaLib.CharArrayCallback callback = null, EmptyCallback completionCallback = null, int id_slot = -1)
        {
            // handle a completion request by the user
            // call the callback function while the answer is received
            // call the completionCallback function when the answer is fully received

            SetCompletionParameters();
            string result = await llmClient.CompletionAsync(prompt, callback, id_slot);
            completionCallback?.Invoke();
            return result;
        }
    }
}
