/// @file
/// @brief File implementing LLM helper code.
using System;
using System.Collections.Generic;

namespace LLMUnity
{
    /// \cond HIDE
    public class LLMException : Exception
    {
        public int ErrorCode { get; private set; }

        public LLMException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class DestroyException : Exception {}

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

    public class LoraManager
    {
        List<LoraAsset> loras = new List<LoraAsset>();

        public void Clear()
        {
            loras.Clear();
        }

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

        public bool Contains(string path)
        {
            return IndexOf(path) != -1;
        }

        public void Add(string path, float weight = 1)
        {
            if (Contains(path)) return;
            loras.Add(new LoraAsset(path, weight));
        }

        public void Remove(string path)
        {
            int index = IndexOf(path);
            if (index != -1) loras.RemoveAt(index);
        }

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

        public void FromStrings(string loraString, string loraWeightsString)
        {
            if (string.IsNullOrEmpty(loraString) && string.IsNullOrEmpty(loraWeightsString))
            {
                Clear();
                return;
            }

            try
            {
                List<string> loraStringArr = new List<string>(loraString.Split(" "));
                List<string> loraWeightsStringArr = new List<string>(loraWeightsString.Split(" "));
                if (loraStringArr.Count != loraWeightsStringArr.Count) throw new Exception($"LoRAs number ({loraString}) doesn't match the number of weights ({loraWeightsString})");

                List<LoraAsset> lorasNew = new List<LoraAsset>();
                for (int i = 0; i < loraStringArr.Count; i++) lorasNew.Add(new LoraAsset(loraStringArr[i], float.Parse(loraWeightsStringArr[i])));
                loras = lorasNew;
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"Loras not set: {e.Message}");
            }
        }

        public (string, string) ToStrings()
        {
            string loraString = "";
            string loraWeightsString = "";
            for (int i = 0; i < loras.Count; i++)
            {
                if (i > 0)
                {
                    loraString += " ";
                    loraWeightsString += " ";
                }
                loraString += loras[i].assetPath;
                loraWeightsString += loras[i].weight;
            }
            return (loraString, loraWeightsString);
        }

        public float[] GetWeights()
        {
            float[] weights = new float[loras.Count];
            for (int i = 0; i < loras.Count; i++) weights[i] = loras[i].weight;
            return weights;
        }
    }
    /// \endcond
}
