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
            if (asynchronousStartup) await StartLLMServer();
            else _ = StartLLMServer();
        }

        private async Task StartLLMServer()
        {
            string arguments = GetLlamaccpArguments();
            Debug.Log($"Server command: {arguments}");
            if (!asynchronousStartup)
            {
                LLMObject = LLMLib.LLM_Construct(arguments);
            }
            else
            {
                LLMObject = await Task.Run(() => LLMLib.LLM_Construct(arguments));
            }
            serverStarted = true;
            serverListening = true;
            Debug.Log("server started");
        }

        public int Register(LLMClient llmClient)
        {
            clients.Add(llmClient);
            return clients.IndexOf(llmClient);
        }

        protected override int GetNumClients()
        {
            return parallelPrompts == -1? clients.Count: base.GetNumClients();
        }

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

        async Task<string> LLMReply(LLMReplyCallback callback, string json)
        {
            IntPtr stringWrapper = LLMLib.StringWrapper_Construct();
            await Task.Run(() => callback(LLMObject, json, stringWrapper));
            string result = GetStringWrapperResult(stringWrapper);
            LLMLib.StringWrapper_Delete(stringWrapper);
            return result;
        }

        async Task<string> LLMStreamReply(LLMReplyStreamingCallback callback, string json, Callback<string> streamCallback = null)
        {
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
            return result;
        }

        public async Task<string> Tokenize(string json)
        {
            return await LLMReply(LLMLib.LLM_Tokenize, json);
        }

        public async Task<string> Detokenize(string json)
        {
            return await LLMReply(LLMLib.LLM_Detokenize, json);
        }

        public async Task<string> Completion(string json, Callback<string> streamCallback = null)
        {
            return await LLMStreamReply(LLMLib.LLM_Completion, json, streamCallback);
        }

        public void CancelRequest(int id_slot)
        {
            LLMLib.LLM_Cancel(LLMObject, id_slot);
        }

        /// <summary>
        /// The Unity OnDestroy function called when the onbject is destroyed.
        /// The function StopProcess is called to stop the LLM server.
        /// </summary>
        public void OnDestroy()
        {
            LLMLib.LLM_Delete(LLMObject);
        }

//================================ UnityMainThreadDispatcher ================================//
// The following is copied from https://github.com/PimDeWitte/UnityMainThreadDispatcher
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        public void Update()
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
