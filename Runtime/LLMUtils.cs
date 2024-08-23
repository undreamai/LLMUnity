/// @file
/// @brief File implementing LLM helper code.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

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
            Clear();
            List<string> loraStringArr = new List<string>(loraString.Split(" "));
            List<string> loraWeightsStringArr = new List<string>(loraWeightsString.Split(" "));
            if (loraStringArr.Count != loraWeightsStringArr.Count)
            {
                LLMUnitySetup.LogError($"LoRAs number ({loraString}) doesn't match the number of weights ({loraWeightsString})");
                return;
            }
            for (int i = 0; i < loraStringArr.Count; i++)
            {
                Add(loraStringArr[i], float.Parse(loraWeightsStringArr[i]));
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
