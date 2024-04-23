/// @file
/// @brief File implementing the LLM server.
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    /// @ingroup llm
    /// <summary>
    /// Class implementing the LLM server.
    /// </summary>
    public class LLMBase : MonoBehaviour
    {
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> number of threads to use (-1 = all) </summary>
        [Server] public int numThreads = -1;
        /// <summary> number of model layers to offload to the GPU (0 = GPU not used).
        /// Use a large number i.e. >30 to utilise the GPU as much as possible.
        /// If the user's GPU is not supported, the LLM will fall back to the CPU </summary>
        [Server] public int numGPULayers = 0;
        /// <summary> number of prompts that can happen in parallel (-1 = number of LLM/LLMClient objects) </summary>
        [ServerAdvanced] public int parallelPrompts = -1;
        /// <summary> select to log the output of the LLM in the Unity Editor. </summary>
        [ServerAdvanced] public bool debug = false;
        /// <summary> allows to start the server asynchronously.
        /// This is useful to not block Unity while the server is initialised.
        /// For example it can be used as follows:
        /// \code
        /// void Start(){
        ///     StartCoroutine(Loading());
        ///     ...
        /// }
        ///
        /// IEnumerator<string> Loading()
        /// {
        ///     // show loading screen
        ///     while (!llm.serverListening)
        ///     {
        ///         yield return null;
        ///     }
        ///     Debug.Log("Server is ready");
        /// }
        /// \endcode
        /// </summary>
        [ServerAdvanced] public bool asynchronousStartup = false;
        /// <summary> the path of the model being used (relative to the Assets/StreamingAssets folder).
        /// Models with .gguf format are allowed.</summary>
        [Model] public string model = "";
        /// <summary> the path of the LORA model being used (relative to the Assets/StreamingAssets folder).
        /// Models with .bin format are allowed.</summary>
        [ModelAdvanced] public string lora = "";
        /// <summary> Size of the prompt context (0 = context size of the model).
        /// This is the number of tokens the model can take as input when generating responses. </summary>
        [ModelAdvanced] public int contextSize = 512;
        /// <summary> Batch size for prompt processing. </summary>
        [ModelAdvanced] public int batchSize = 512;
        /// <summary> Boolean set to true if the server has started and is ready to receive requests, false otherwise. </summary>
        public bool serverListening { get; protected set; } = false;
        /// <summary> Boolean set to true if the server as well as the client functionality has fully started, false otherwise. </summary>
        public bool serverStarted { get; protected set; } = false;

        /// \cond HIDE
        [HideInInspector] public readonly (string, string)[] modelOptions = new(string, string)[]
        {
            ("Download model", null),
            ("Mistral 7B Instruct v0.2 (medium, best overall)", "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf?download=true"),
            ("OpenHermes 2.5 7B (medium, best for conversation)", "https://huggingface.co/TheBloke/OpenHermes-2.5-Mistral-7B-GGUF/resolve/main/openhermes-2.5-mistral-7b.Q4_K_M.gguf?download=true"),
            ("Phi 2 (small, decent)", "https://huggingface.co/TheBloke/phi-2-GGUF/resolve/main/phi-2.Q4_K_M.gguf?download=true"),
        };
        public int SelectedModel = 0;
        [HideInInspector] public float modelProgress = 1;
        [HideInInspector] public float modelCopyProgress = 1;
        [HideInInspector] public bool modelHide = true;

        public string chatTemplate = ChatTemplate.DefaultTemplate;
        private ChatTemplate template;
        /// \endcond

#if UNITY_EDITOR
        /// \cond HIDE
        public void DownloadModel(int optionIndex)
        {
            // download default model and disable model editor properties until the model is set
            SelectedModel = optionIndex;
            string modelUrl = modelOptions[optionIndex].Item2;
            if (modelUrl == null) return;
            modelProgress = 0;
            string modelName = Path.GetFileName(modelUrl).Split("?")[0];
            string modelPath = LLMUnitySetup.GetAssetPath(modelName);
            Task downloadTask = LLMUnitySetup.DownloadFile(modelUrl, modelPath, false, false, SetModel, SetModelProgress);
        }

        /// \endcond

        void SetModelProgress(float progress)
        {
            modelProgress = progress;
        }

        /// <summary>
        /// Allows to set the model used by the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .gguf format.
        /// </summary>
        /// <param name="path">path to model to use (.gguf format)</param>
        public async Task SetModel(string path)
        {
            // set the model and enable the model editor properties
            modelCopyProgress = 0;
            model = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
            SetTemplate(ChatTemplate.FromGGUF(path));
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

        public void SetTemplate(string templateName)
        {
            chatTemplate = templateName;
            LoadTemplate();
        }

        /// <summary>
        /// Allows to set a LORA model to use in the LLM.
        /// The model provided is copied to the Assets/StreamingAssets folder that allows it to also work in the build.
        /// Models supported are in .bin format.
        /// </summary>
        /// <param name="path">path to LORA model to use (.bin format)</param>
        public async Task SetLora(string path)
        {
            // set the lora and enable the model editor properties
            modelCopyProgress = 0;
            lora = await LLMUnitySetup.AddAsset(path, LLMUnitySetup.GetAssetPath());
            EditorUtility.SetDirty(this);
            modelCopyProgress = 1;
        }

#endif
        private void LoadTemplate()
        {
            template = ChatTemplate.GetTemplate(chatTemplate);
        }

        protected string EscapeSpaces(string input)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                return input.Replace(" ", "\" \"");
            if (input.Contains(" "))
                return $"'{input}'";
            return input;
        }

        protected virtual int GetNumClients()
        {
            return Math.Max(parallelPrompts, 1);
        }

        protected virtual string GetLlamaccpArguments()
        {
            // Start the LLM server in a cross-platform way
            if (model == "")
            {
                Debug.LogError("No model file provided!");
                return null;
            }
            string modelPath = LLMUnitySetup.GetAssetPath(model);
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"File {modelPath} not found!");
                return null;
            }
            string loraPath = "";
            if (lora != "")
            {
                loraPath = LLMUnitySetup.GetAssetPath(lora);
                if (!File.Exists(loraPath))
                {
                    Debug.LogError($"File {loraPath} not found!");
                    return null;
                }
            }

            int slots = GetNumClients();
            string arguments = $"-m {EscapeSpaces(modelPath)} -c {contextSize} -b {batchSize} --log-disable -np {slots}";
            if (numThreads > 0) arguments += $" -t {numThreads}";
            if (loraPath != "") arguments += $" --lora {EscapeSpaces(loraPath)}";
            return arguments;
        }
    }
}
