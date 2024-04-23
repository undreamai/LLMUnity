/// @file
/// @brief File implementing the LLM client.
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public class LLMClient : LLMClientBase
    {
        [Model] public LLM llm;

        public new void Awake()
        {
            id_slot = llm.Register(this);
            base.Awake();
        }

        // <summary>
        // Cancel the ongoing requests e.g. Chat, Complete.
        // </summary>
        public new void CancelRequests()
        {
            if (id_slot >= 0) llm.CancelRequest(id_slot);
        }

        protected override async Task<Ret> PostRequest<Res, Ret>(string json, string endpoint, ContentCallback<Res, Ret> getContent, Callback<Ret> callback = null)
        {
            // send a post request to the server and call the relevant callbacks to convert the received content and handle it
            // this function has streaming functionality i.e. handles the answer while it is being received
            string callResult = null;
            switch (endpoint)
            {
                case "tokenize":
                    callResult = await llm.Tokenize(json);
                    break;
                case "detokenize":
                    callResult = await llm.Detokenize(json);
                    break;
                case "completion":
                    Callback<string> callbackString = null;
                    if (callback != null)
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
                            Debug.LogError($"wrong callback type, should be string");
                        }
                    }
                    callResult = await llm.Completion(json, callbackString);
                    break;
                default:
                    Debug.LogError($"Unknown endpoint {endpoint}");
                    break;
            }

            Ret result = ConvertContent(callResult, getContent);
            callback?.Invoke(result);
            return result;
        }
    }
}
