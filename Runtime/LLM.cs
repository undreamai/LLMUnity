/// @file
/// @brief File implementing the LLM server.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
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
        LLMLib llmlib;
        StreamWrapper logStreamWrapper;
        Thread llmThread;

        public void Awake()
        {
            if (!gameObject.activeSelf) return;
            llmThread = new Thread(StartLLMServer);
            llmThread.Start();
        }

        private void LLMLibLog(string log)
        {
            if (!debug) return;
            Debug.LogWarning(log);
        }

        private void SetupLogging()
        {
            logStreamWrapper = new StreamWrapper(this, LLMLibLog);
            llmlib?.Logging(logStreamWrapper.GetStringWrapper(), logStreamWrapper.GetCallPointer());
        }

        private void StopLogging()
        {
            llmlib?.StopLogging();
            logStreamWrapper.Destroy();
        }

        private void StartLLMServer()
        {
            string arguments = GetLlamaccpArguments();
            bool useGPU = numGPULayers > 0;
            Debug.Log($"Server command: {arguments}");

            foreach (string arch in LLMLib.PossibleArchitectures(useGPU))
            {
                llmlib = new LLMLib(arch);
                string error;
                try
                {
                    SetupLogging();
                    LLMObject = llmlib.LLM_Construct(arguments);
                    CheckLLMStatus(false);
                    Debug.Log($"Using architecture: {arch}");
                    break;
                }
                catch (LLMException e)
                {
                    error = e.Message;
                    Destroy();
                }
                catch (Exception e)
                {
                    error = $"{e.GetType()}: {e.Message}";
                }
                Debug.Log($"Tried architecture: {arch}, " + error);
            }
            if (llmlib == null)
            {
                Debug.LogError("LLM service couldn't be created");
                return;
            }
            Debug.Log("LLM service created");
            serverStarted = true;
            serverListening = true;
            llmlib.LLM_Start(LLMObject);
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

        class StreamWrapper
        {
            LLM llm;
            IntPtr stringWrapper = IntPtr.Zero;
            Callback<string> streamCallback;

            public StreamWrapper(LLM llm, Callback<string> streamCallback)
            {
                this.llm = llm;
                this.streamCallback = streamCallback;
                if (llm.llmlib == null) Debug.LogError("LLM service not started");
                else stringWrapper = llm.llmlib.StringWrapper_Construct();
            }

            public void Call()
            {
                string result = llm.llmlib?.GetStringWrapperResult(stringWrapper);
                if (result != null) llm.EnqueueAsync(() => {streamCallback(result);});
            }

            public IntPtr GetStringWrapper()
            {
                return stringWrapper;
            }

            public IntPtr GetCallPointer()
            {
                return Marshal.GetFunctionPointerForDelegate((StreamingCallback)Call);
            }

            public void Destroy()
            {
                if (stringWrapper != IntPtr.Zero) llm.llmlib?.StringWrapper_Delete(stringWrapper);
            }
        }

        void CheckLLMStatus(bool log = true)
        {
            if (llmlib == null) {Debug.LogError("LLM service not started"); return;}

            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            int status = llmlib.LLM_Status(LLMObject, stringWrapper);
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            llmlib.StringWrapper_Delete(stringWrapper);
            string message = $"LLM {status}: {result}";
            if (status > 0)
            {
                if (log) Debug.LogError(message);
                throw new LLMException(message, status);
            }
            else if (status < 0)
            {
                if (log) Debug.LogWarning(message);
            }
        }

        async Task<string> LLMReply(LLMReplyCallback callback, string json)
        {
            if (llmlib == null) {Debug.LogError("LLM service not started"); return null;}

            IntPtr stringWrapper = llmlib.StringWrapper_Construct();
            await Task.Run(() => callback(LLMObject, json, stringWrapper));
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            llmlib.StringWrapper_Delete(stringWrapper);
            CheckLLMStatus();
            return result;
        }

        async Task<string> LLMStreamReply(LLMReplyStreamingCallback callback, string json, Callback<string> streamCallback = null)
        {
            if (llmlib == null) {Debug.LogError("LLM service not started"); return null;}

            IntPtr stringWrapper = IntPtr.Zero;
            IntPtr streamCallbackPointer = IntPtr.Zero;
            StreamWrapper streamWrapper = null;
            if (streamCallback != null)
            {
                streamWrapper = new StreamWrapper(this, streamCallback);
                streamCallbackPointer = streamWrapper.GetCallPointer();
                stringWrapper = streamWrapper.GetStringWrapper();
            }
            await Task.Run(() => callback(LLMObject, json, stringWrapper, streamCallbackPointer));
            string result = llmlib.GetStringWrapperResult(stringWrapper);
            if (streamWrapper != null) streamWrapper.Destroy();
            CheckLLMStatus();
            return result;
        }

        public async Task<string> Tokenize(string json)
        {
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Tokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        public async Task<string> Detokenize(string json)
        {
            LLMReplyCallback callback = (IntPtr LLMObject, string jsonData, IntPtr strWrapper) =>
            {
                llmlib.LLM_Detokenize(LLMObject, jsonData, strWrapper);
            };
            return await LLMReply(callback, json);
        }

        public async Task<string> Completion(string json, Callback<string> streamCallback = null)
        {
            LLMReplyStreamingCallback callback = (IntPtr LLMObject, string json_data, IntPtr stringWrapper, IntPtr streamCallbackPointer) =>
            {
                llmlib.LLM_Completion(LLMObject, json_data, stringWrapper, streamCallbackPointer);
            };
            return await LLMStreamReply(callback, json, streamCallback);
        }

        public void CancelRequest(int id_slot)
        {
            llmlib?.LLM_Cancel(LLMObject, id_slot);
            CheckLLMStatus();
        }

        private void Destroy()
        {
            if (llmlib != null)
            {
                StopLogging();
                llmlib.LLM_Stop(LLMObject);
                llmlib.LLM_Delete(LLMObject);
            }
            llmlib = null;
        }

        /// <summary>
        /// The Unity OnDestroy function called when the onbject is destroyed.
        /// The function StopProcess is called to stop the LLM server.
        /// </summary>
        public void OnDestroy()
        {
            Destroy();
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
