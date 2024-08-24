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
        public float weight;

        public LoraAsset(string path, float weight = 1)
        {
            assetPath = LLM.GetLLMManagerAsset(path);
            this.weight = weight;
        }

        public override bool Equals(object obj)
        {
            string RuntimePath(string path) {return LLMUnitySetup.GetFullPath(LLM.GetLLMManagerAssetRuntime(path));}

            if (obj == null || obj.GetType() != this.GetType()) return false;
            LoraAsset other = (LoraAsset)obj;
            return assetPath == other.assetPath || RuntimePath(assetPath) == RuntimePath(other.assetPath);
        }

        public override int GetHashCode()
        {
            return (assetPath + "," + weight.ToString()).GetHashCode();
        }
    }

    public class LoraManager
    {
        List<LoraAsset> loras = new List<LoraAsset>();

        public void Clear()
        {
            loras.Clear();
        }

        public bool Contains(string path)
        {
            LoraAsset lora = new LoraAsset(path);
            return loras.Contains(lora);
        }

        public void Add(string path, float weight = 1)
        {
            LoraAsset lora = new LoraAsset(path, weight);
            if (loras.Contains(lora)) return;
            loras.Add(lora);
        }

        public void Remove(string path)
        {
            loras.Remove(new LoraAsset(path));
        }

        public void SetWeight(string path, float weight)
        {
            LoraAsset lora = new LoraAsset(path);
            int index = loras.IndexOf(lora);
            if (index == -1)
            {
                LLMUnitySetup.LogError($"LoRA {path} not loaded with the LLM");
                return;
            }
            loras[index].weight = weight;
        }

        public void FromStrings(string loraString, string loraWeightsString)
        {
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

        public StringPair ToStrings()
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
            return new StringPair(){source = loraString, target = loraWeightsString};
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
