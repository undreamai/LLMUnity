/// @file
/// @brief File implementing LLM helper code.
using System;
using System.Collections.Generic;

namespace LLMUnity
{
    /// @ingroup utils
    /// <summary>
    /// Class implementing a basic LLM Exception
    /// </summary>
    public class LLMException : Exception
    {
        public int ErrorCode { get; private set; }

        public LLMException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// @ingroup utils
    /// <summary>
    /// Class implementing a basic LLM Destroy Exception
    /// </summary>
    public class DestroyException : Exception {}

    /// @ingroup utils
    /// <summary>
    /// Class representing a LORA asset
    /// </summary>
    public class LoraAsset
    {
        public string assetPath;
        public string fullPath;
        public float weight;

        public LoraAsset(string path, float weight = 1)
        {
            assetPath = LLM.GetLLMManagerAsset(path);
            fullPath = RuntimePath(path);
            this.weight = weight;
        }

        public static string RuntimePath(string path)
        {
            return LLMUnitySetup.GetFullPath(LLM.GetLLMManagerAssetRuntime(path));
        }
    }

    /// @ingroup utils
    /// <summary>
    /// Class representing the LORA manager allowing to convert and retrieve LORA assets to string (for serialisation)
    /// </summary>
    public class LoraManager
    {
        List<LoraAsset> loras = new List<LoraAsset>();
        public string delimiter = ",";

        /// <summary>
        /// Clears the LORA assets
        /// </summary>
        public void Clear()
        {
            loras.Clear();
        }

        /// <summary>
        /// Searches for a LORA based on the path
        /// </summary>
        /// <param name="path">LORA path</param>
        /// <returns>LORA index</returns>
        public int IndexOf(string path)
        {
            string fullPath = LoraAsset.RuntimePath(path);
            for (int i = 0; i < loras.Count; i++)
            {
                LoraAsset lora = loras[i];
                if (lora.assetPath == path || lora.fullPath == fullPath) return i;
            }
            return -1;
        }

        /// <summary>
        /// Checks if the provided LORA based on a path exists already in the LORA manager
        /// </summary>
        /// <param name="path">LORA path</param>
        /// <returns>whether the LORA manager contains the LORA</returns>
        public bool Contains(string path)
        {
            return IndexOf(path) != -1;
        }

        /// <summary>
        /// Adds a LORA with the defined weight
        /// </summary>
        /// <param name="path">LORA path</param>
        /// <param name="weight">LORA weight</param>
        public void Add(string path, float weight = 1)
        {
            if (Contains(path)) return;
            loras.Add(new LoraAsset(path, weight));
        }

        /// <summary>
        /// Removes a LORA based on its path
        /// </summary>
        /// <param name="path">LORA path</param>
        public void Remove(string path)
        {
            int index = IndexOf(path);
            if (index != -1) loras.RemoveAt(index);
        }

        /// <summary>
        /// Modifies the weight of a LORA
        /// </summary>
        /// <param name="path">LORA path</param>
        /// <param name="weight">LORA weight</param>
        public void SetWeight(string path, float weight)
        {
            int index = IndexOf(path);
            if (index == -1)
            {
                LLMUnitySetup.LogError($"LoRA {path} not loaded with the LLM");
                return;
            }
            loras[index].weight = weight;
        }

        /// <summary>
        /// Converts strings with the lora paths and weights to entries in the LORA manager
        /// </summary>
        /// <param name="loraString">lora paths</param>
        /// <param name="loraWeightsString">lora weights</param>
        public void FromStrings(string loraString, string loraWeightsString)
        {
            if (string.IsNullOrEmpty(loraString) && string.IsNullOrEmpty(loraWeightsString))
            {
                Clear();
                return;
            }

            try
            {
                List<string> loraStringArr = new List<string>(loraString.Split(delimiter));
                List<string> loraWeightsStringArr = new List<string>(loraWeightsString.Split(delimiter));
                if (loraStringArr.Count != loraWeightsStringArr.Count) throw new Exception($"LoRAs number ({loraString}) doesn't match the number of weights ({loraWeightsString})");

                List<LoraAsset> lorasNew = new List<LoraAsset>();
                for (int i = 0; i < loraStringArr.Count; i++) lorasNew.Add(new LoraAsset(loraStringArr[i].Trim(), float.Parse(loraWeightsStringArr[i])));
                loras = lorasNew;
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"Loras not set: {e.Message}");
            }
        }

        /// <summary>
        /// Converts the entries of the LORA manager to strings with the lora paths and weights
        /// </summary>
        /// <returns>strings with the lora paths and weights</returns>
        public (string, string) ToStrings()
        {
            string loraString = "";
            string loraWeightsString = "";
            for (int i = 0; i < loras.Count; i++)
            {
                if (i > 0)
                {
                    loraString += delimiter;
                    loraWeightsString += delimiter;
                }
                loraString += loras[i].assetPath;
                loraWeightsString += loras[i].weight;
            }
            return (loraString, loraWeightsString);
        }

        /// <summary>
        /// Gets the weights of the LORAs in the manager
        /// </summary>
        /// <returns>LORA weights</returns>
        public float[] GetWeights()
        {
            float[] weights = new float[loras.Count];
            for (int i = 0; i < loras.Count; i++) weights[i] = loras[i].weight;
            return weights;
        }

        /// <summary>
        /// Gets the paths of the LORAs in the manager
        /// </summary>
        /// <returns>LORA paths</returns>
        public string[] GetLoras()
        {
            string[] loraPaths = new string[loras.Count];
            for (int i = 0; i < loras.Count; i++) loraPaths[i] = loras[i].assetPath;
            return loraPaths;
        }
    }
    /// \endcond
}
