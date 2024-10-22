using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public class LLMCaller : MonoBehaviour
    {
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> toggle to use remote LLM server or local LLM </summary>
        [LocalRemote] public bool remote = false;
        /// <summary> the LLM object to use </summary>
        [Local] public LLM llm;
        /// <summary> option to receive the reply from the model as it is produced (recommended!).
        /// If it is not selected, the full reply from the model is received in one go </summary>
        [Chat] public bool stream = true;
        /// <summary> allows to use a server with API key </summary>
        [Remote] public string APIKey;
        /// <summary> specify which slot of the server to use for computation (affects caching) </summary>
        [ModelAdvanced] public int slot = -1;

        /// <summary> host to use for the LLM server </summary>
        [Remote] public string host = "localhost";
        /// <summary> port to use for the LLM server </summary>
        [Remote] public int port = 13333;
        /// <summary> number of retries to use for the LLM server requests (-1 = infinite) </summary>
        [Remote] public int numRetries = 10;

        private List<(string, string)> requestHeaders;
        private List<UnityWebRequest> WIPRequests = new List<UnityWebRequest>();

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

            requestHeaders = new List<(string, string)> { ("Content-Type", "application/json") };
            if (!remote)
            {
                AssignLLM();
                if (llm == null)
                {
                    LLMUnitySetup.LogError($"No LLM assigned or detected for LLMCharacter {name}!");
                    return;
                }
                int slotFromServer = llm.Register(this);
                if (slot == -1) slot = slotFromServer;
            }
            else
            {
                if (!String.IsNullOrEmpty(APIKey)) requestHeaders.Add(("Authorization", "Bearer " + APIKey));
            }
        }

        protected virtual void OnValidate()
        {
            AssignLLM();
            if (llm != null && llm.parallelPrompts > -1 && (slot < -1 || slot >= llm.parallelPrompts)) LLMUnitySetup.LogError($"The slot needs to be between 0 and {llm.parallelPrompts-1}, or -1 to be automatically set");
        }

        protected virtual void Reset()
        {
            AssignLLM();
        }

        protected virtual void AssignLLM()
        {
            if (remote || llm != null) return;

            LLM[] existingLLMs = FindObjectsOfType<LLM>();
            if (existingLLMs.Length == 0) return;

            SortBySceneAndHierarchy(existingLLMs);
            llm = existingLLMs[0];
            string msg = $"Assigning LLM {llm.name} to LLMCharacter {name}";
            if (llm.gameObject.scene != gameObject.scene) msg += $" from scene {llm.gameObject.scene}";
            LLMUnitySetup.Log(msg);
        }

        protected virtual void SortBySceneAndHierarchy(LLM[] array)
        {
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
        }

        protected string ChatContent(ChatResult result)
        {
            // get content from a chat result received from the endpoint
            return result.content.Trim();
        }

        protected string MultiChatContent(MultiChatResult result)
        {
            // get content from a chat result received from the endpoint
            string response = "";
            foreach (ChatResult resultPart in result.data)
            {
                response += resultPart.content;
            }
            return response.Trim();
        }

        protected async Task<string> CompletionRequest(string json, Callback<string> callback = null)
        {
            string result = "";
            if (stream)
            {
                result = await PostRequest<MultiChatResult, string>(json, "completion", MultiChatContent, callback);
            }
            else
            {
                result = await PostRequest<ChatResult, string>(json, "completion", ChatContent, callback);
            }
            return result;
        }

        protected string TemplateContent(TemplateResult result)
        {
            // get content from a char result received from the endpoint in open AI format
            return result.template;
        }

        protected List<int> TokenizeContent(TokenizeResult result)
        {
            // get the tokens from a tokenize result received from the endpoint
            return result.tokens;
        }

        protected string DetokenizeContent(TokenizeRequest result)
        {
            // get content from a chat result received from the endpoint
            return result.content;
        }

        protected List<float> EmbeddingsContent(EmbeddingsResult result)
        {
            // get content from a chat result received from the endpoint
            return result.embedding;
        }

        protected string SlotContent(SlotResult result)
        {
            // get the tokens from a tokenize result received from the endpoint
            return result.filename;
        }

        protected virtual Ret ConvertContent<Res, Ret>(string response, ContentCallback<Res, Ret> getContent = null)
        {
            // template function to convert the json received and get the content
            if (response == null) return default;
            response = response.Trim();
            if (response.StartsWith("data: "))
            {
                string responseArray = "";
                foreach (string responsePart in response.Replace("\n\n", "").Split("data: "))
                {
                    if (responsePart == "") continue;
                    if (responseArray != "") responseArray += ",\n";
                    responseArray += responsePart;
                }
                response = $"{{\"data\": [{responseArray}]}}";
            }
            return getContent(JsonUtility.FromJson<Res>(response));
        }

        protected virtual void CancelRequestsLocal()
        {
            if (slot >= 0) llm.CancelRequest(slot);
        }

        protected virtual void CancelRequestsRemote()
        {
            foreach (UnityWebRequest request in WIPRequests)
            {
                request.Abort();
            }
            WIPRequests.Clear();
        }

        /// <summary>
        /// Cancel the ongoing requests e.g. Chat, Complete.
        /// </summary>
        // <summary>
        public void CancelRequests()
        {
            if (remote) CancelRequestsRemote();
            else CancelRequestsLocal();
        }

        protected virtual async Task<Ret> PostRequestLocal<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            string callResult = null;
            bool callbackCalled = false;
            while (!llm.failed && !llm.started) await Task.Yield();
            switch (endpoint)
            {
                case "tokenize":
                    callResult = await llm.Tokenize(json);
                    break;
                case "detokenize":
                    callResult = await llm.Detokenize(json);
                    break;
                case "embeddings":
                    callResult = await llm.Embeddings(json);
                    break;
                case "slots":
                    callResult = await llm.Slot(json);
                    break;
                case "completion":
                    if (llm.embeddingsOnly) LLMUnitySetup.LogError("The LLM can't be used for completion, only for embeddings");
                    else
                    {
                        Callback<string> callbackString = null;
                        if (stream && callback != null)
                        {
                            if (typeof(Ret) == typeof(string))
                            {
                                callbackString = (strArg) =>
                                {
                                    callback(ConvertContent(strArg, getContent));
                                };
                            }
                            else
                            {
                                LLMUnitySetup.LogError($"wrong callback type, should be string");
                            }
                            callbackCalled = true;
                        }
                        callResult = await llm.Completion(json, callbackString);
                    }
                    break;
                default:
                    LLMUnitySetup.LogError($"Unknown endpoint {endpoint}");
                    break;
            }

            Ret result = ConvertContent(callResult, getContent);
            if (!callbackCalled) callback?.Invoke(result);
            return result;
        }

        protected virtual async Task<Ret> PostRequestRemote<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            if (endpoint == "slots")
            {
                LLMUnitySetup.LogError("Saving and loading is not currently supported in remote setting");
                return default;
            }

            Ret result = default;
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            UnityWebRequest request = null;
            string error = null;
            int tryNr = numRetries;

            while (tryNr != 0)
            {
                using (request = UnityWebRequest.Put($"{host}:{port}/{endpoint}", jsonToSend))
                {
                    WIPRequests.Add(request);

                    request.method = "POST";
                    if (requestHeaders != null)
                    {
                        for (int i = 0; i < requestHeaders.Count; i++)
                            request.SetRequestHeader(requestHeaders[i].Item1, requestHeaders[i].Item2);
                    }

                    // Start the request asynchronously
                    var asyncOperation = request.SendWebRequest();
                    float lastProgress = 0f;
                    // Continue updating progress until the request is completed
                    while (!asyncOperation.isDone)
                    {
                        float currentProgress = request.downloadProgress;
                        // Check if progress has changed
                        if (currentProgress != lastProgress && callback != null)
                        {
                            callback?.Invoke(ConvertContent(request.downloadHandler.text, getContent));
                            lastProgress = currentProgress;
                        }
                        // Wait for the next frame
                        await Task.Yield();
                    }
                    WIPRequests.Remove(request);
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        result = ConvertContent(request.downloadHandler.text, getContent);
                        error = null;
                        break;
                    }
                    else
                    {
                        result = default;
                        error = request.error;
                        if (request.responseCode == (int)System.Net.HttpStatusCode.Unauthorized) break;
                    }
                }
                tryNr--;
                if (tryNr > 0) await Task.Delay(200 * (numRetries - tryNr));
            }

            if (error != null) LLMUnitySetup.LogError(error);
            callback?.Invoke(result);
            return result;
        }

        protected virtual async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            if (remote) return await PostRequestRemote(json, endpoint, getContent, callback);
            return await PostRequestLocal(json, endpoint, getContent, callback);
        }

        /// <summary>
        /// Tokenises the provided query.
        /// </summary>
        /// <param name="query">query to tokenise</param>
        /// <param name="callback">callback function called with the result tokens</param>
        /// <returns>list of the tokens</returns>
        public async Task<List<int>> Tokenize(string query, Callback<List<int>> callback = null)
        {
            // handle the tokenization of a message by the user
            TokenizeRequest tokenizeRequest = new TokenizeRequest();
            tokenizeRequest.content = query;
            string json = JsonUtility.ToJson(tokenizeRequest);
            return await PostRequest<TokenizeResult, List<int>>(json, "tokenize", TokenizeContent, callback);
        }

        /// <summary>
        /// Detokenises the provided tokens to a string.
        /// </summary>
        /// <param name="tokens">tokens to detokenise</param>
        /// <param name="callback">callback function called with the result string</param>
        /// <returns>the detokenised string</returns>
        public async Task<string> Detokenize(List<int> tokens, Callback<string> callback = null)
        {
            // handle the detokenization of a message by the user
            TokenizeResult tokenizeRequest = new TokenizeResult();
            tokenizeRequest.tokens = tokens;
            string json = JsonUtility.ToJson(tokenizeRequest);
            return await PostRequest<TokenizeRequest, string>(json, "detokenize", DetokenizeContent, callback);
        }

        /// <summary>
        /// Computes the embeddings of the provided input.
        /// </summary>
        /// <param name="tokens">input to compute the embeddings for</param>
        /// <param name="callback">callback function called with the result string</param>
        /// <returns>the computed embeddings</returns>
        public async Task<List<float>> Embeddings(string query, Callback<List<float>> callback = null)
        {
            // handle the tokenization of a message by the user
            TokenizeRequest tokenizeRequest = new TokenizeRequest();
            tokenizeRequest.content = query;
            string json = JsonUtility.ToJson(tokenizeRequest);
            return await PostRequest<EmbeddingsResult, List<float>>(json, "embeddings", EmbeddingsContent, callback);
        }
    }
}
