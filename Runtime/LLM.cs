/// @file
/// @brief File implementing the LLM server.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    public class LLMException : Exception
    {
        public int ErrorCode { get; private set; }

        public LLMException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    [DefaultExecutionOrder(-1)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM server.
    /// </summary>
    public class LLM : LLMBase
    {
        IntPtr LLMObject;
        List<LLMClient> clients = new List<LLMClient>();

        public async void Awake()
        {
            if (!gameObject.activeSelf) return;
            if (asynchronousStartup) await StartLLMServer();
            else _ = StartLLMServer();
        }

        private async Task StartLLMServer()
        {
            for (int i = 0; i < 2; i++)
            {
                string arguments = GetLlamaccpArguments();
                Debug.Log($"Server command: {arguments}");
                try
                {
                    if (!asynchronousStartup)
                    {
                        LLMObject = LLMLib.LLM_Construct(arguments);
                    }
                    else
                    {
                        LLMObject = await Task.Run(() => LLMLib.LLM_Construct(arguments));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    continue;
                }

                try
                {
                    CheckLLMStatus();
                    serverStarted = true;
                    serverListening = true;
                    Debug.Log("LLM service started");
                    break;
                }
                catch (LLMException e)
                {
                    LLMLib.LLM_Delete(LLMObject);
                    if (numGPULayers > 0) numGPULayers = 0;
                    else break;
                }
            }
        }

        public int Register(LLMClient llmClient)
        {
            clients.Add(llmClient);
            return clients.IndexOf(llmClient);
        }

        protected override int GetNumClients()
        {
            return Math.Max(parallelPrompts == -1 ? clients.Count : parallelPrompts, 1);
        }

        public delegate void LLMStatusCallback(IntPtr LLMObject, IntPtr stringWrapper);
        public delegate void LLMReplyCallback(IntPtr LLMObject, string json_data, IntPtr stringWrapper);
        public delegate void StreamingCallback();
        public delegate void LLMReplyStreamingCallback(IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer);

        static string GetStringWrapperResult(IntPtr stringWrapper)
        {
            string result;
            int bufferSize = LLMLib.StringWrapper_GetStringSize(stringWrapper);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                LLMLib.StringWrapper_GetString(stringWrapper, buffer, bufferSize);
                result = Marshal.PtrToStringAnsi(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return result;
        }

        class StreamWrapper
        {
            IntPtr stringWrapper;
            Callback<string> streamCallback;
            LLM llm;

            public StreamWrapper(LLM llm, IntPtr stringWrapper, Callback<string> streamCallback)
            {
                this.stringWrapper = stringWrapper;
                this.streamCallback = streamCallback;
                this.llm = llm;
            }

            public void Call()
            {
                string result = GetStringWrapperResult(stringWrapper);
                llm.EnqueueAsync(() =>
                {
                    streamCallback(result);
                });
            }
        }

        void CheckLLMStatus()
        {
            IntPtr stringWrapper = LLMLib.StringWrapper_Construct();
            int status = LLMLib.LLM_Status(LLMObject, stringWrapper);
            string result = GetStringWrapperResult(stringWrapper);
            LLMLib.StringWrapper_Delete(stringWrapper);
            string message = $"LLM {status}: {result}";
            if (status > 0)
            {
                Debug.LogError(message);
                throw new LLMException(message, status);
            }
            else if (status < 0)
            {
                Debug.LogWarning(message);
            }
        }

        async Task<string> LLMReply(LLMReplyCallback callback, string json)
        {
            // CheckLLMStatus();
            IntPtr stringWrapper = LLMLib.StringWrapper_Construct();
            await Task.Run(() => callback(LLMObject, json, stringWrapper));
            string result = GetStringWrapperResult(stringWrapper);
            LLMLib.StringWrapper_Delete(stringWrapper);
            CheckLLMStatus();
            return result;
        }

        async Task<string> LLMStreamReply(LLMReplyStreamingCallback callback, string json, Callback<string> streamCallback = null)
        {
            // CheckLLMStatus();
            IntPtr stringWrapper = LLMLib.StringWrapper_Construct();
            IntPtr streamCallbackPointer = IntPtr.Zero;
            if (streamCallback != null)
            {
                StreamWrapper streamWrapper = new StreamWrapper(this, stringWrapper, streamCallback);
                StreamingCallback callbackDelegate = streamWrapper.Call;
                streamCallbackPointer = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
            }
            await Task.Run(() => callback(LLMObject, json, stringWrapper, streamCallbackPointer));
            string result = GetStringWrapperResult(stringWrapper);
            LLMLib.StringWrapper_Delete(stringWrapper);
            CheckLLMStatus();
            return result;
        }

        public async Task<string> Tokenize(string json)
        {
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                LLMLib.LLM_Tokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
            // return await LLMReply(LLMLib.LLM_Tokenize, json);
        }

        public async Task<string> Detokenize(string json)
        {
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                LLMLib.LLM_Detokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
            // return await LLMReply(LLMLib.LLM_Detokenize, json);
        }

        public async Task<string> Completion(string json, Callback<string> streamCallback = null)
        {
            LLMReplyStreamingCallback callback = (IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer) =>
            {
                LLMLib.LLM_Completion(LLMObject, json_data, stringWrapper, streamCallbackPointer);
            };
            return await LLMStreamReply(callback, json, streamCallback);

            // return await LLMStreamReply(LLMLib.LLM_Completion, json, streamCallback);
        }

        public void CancelRequest(int id_slot)
        {
            // CheckLLMStatus();
            LLMLib.LLM_Cancel(LLMObject, id_slot);
            CheckLLMStatus();
        }

        /// <summary>
        /// The Unity OnDestroy function called when the onbject is destroyed.
        /// The function StopProcess is called to stop the LLM server.
        /// </summary>
        public void OnDestroy()
        {
            CheckLLMStatus();
            LLMLib.LLM_Delete(LLMObject);
        }

//================================ UnityMainThreadDispatcher ================================//
// The following is copied from https://github.com/PimDeWitte/UnityMainThreadDispatcher
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        public new void Update()
        {
            lock (_executionQueue) {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        /// <summary>
        /// Locks the queue and adds the IEnumerator to the queue
        /// </summary>
        /// <param name="action">IEnumerator function that will be executed from the main thread.</param>
        public void Enqueue(IEnumerator action)
        {
            lock (_executionQueue) {
                _executionQueue.Enqueue(() => {
                    StartCoroutine(action);
                });
            }
        }

        /// <summary>
        /// Locks the queue and adds the Action to the queue
        /// </summary>
        /// <param name="action">function that will be executed from the main thread.</param>
        public void Enqueue(Action action)
        {
            Enqueue(ActionWrapper(action));
        }

        /// <summary>
        /// Locks the queue and adds the Action to the queue, returning a Task which is completed when the action completes
        /// </summary>
        /// <param name="action">function that will be executed from the main thread.</param>
        /// <returns>A Task that can be awaited until the action completes</returns>
        public Task EnqueueAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            void WrappedAction()
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            Enqueue(ActionWrapper(WrappedAction));
            return tcs.Task;
        }

        IEnumerator ActionWrapper(Action a)
        {
            a();
            yield return null;
        }
    }
//================================ UnityMainThreadDispatcher ================================//
}
