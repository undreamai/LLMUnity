using System;
using System.Collections.Generic;
using Cloud.Unum.USearch;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

namespace LLMUnity
{
    [DataContract]
    public class Search : LLMCaller
    {
        public virtual int[] Search(float[] encoding, int k)
        {
            return Search(encoding, k, out float[] distances);
        }

        public virtual async Task<int[]> Search(string queryString, int k)
        {
            return Search((await Embeddings(queryString)).ToArray(), k, out float[] distances);
        }

        public virtual int[] Search(float[] encoding, int k, out float[] distances)
        {
            LLMUnitySetup.LogError("Not implemented");
            distances = default;
            return default;
        }

        public virtual async Task<float[]> Add(int key, string inputString)
        {
            LLMUnitySetup.LogError("Not implemented");
            await Task.CompletedTask;
            return default;
        }

        public virtual bool Remove(int key)
        {
            LLMUnitySetup.LogError("Not implemented");
            return default;
        }

        public virtual int Count()
        {
            LLMUnitySetup.LogError("Not implemented");
            return default;
        }

        //TODO
/*
        public int[] Search(string queryString, int k, out float[] distances)
        {
            return Search(Encode(queryString), k, out distances);
        }
*/

        public static string GetSearchTypePath(string dirname = "")
        {
            return Path.Combine(dirname, "SearchType.txt");
        }

        public static string GetSearchPath(string dirname = "")
        {
            return Path.Combine(dirname, "Search.json");
        }

        public virtual void Save(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(archive, dirname);
            }
        }

        public virtual void Save(ZipArchive archive, string dirname = "")
        {
            ZipArchiveEntry typeEntry = archive.CreateEntry(GetSearchTypePath(dirname));
            using (StreamWriter writer = new StreamWriter(typeEntry.Open()))
            {
                writer.Write(GetType().FullName);
            }

            ZipArchiveEntry mainEntry = archive.CreateEntry(GetSearchPath(dirname));
            using (Stream entryStream = mainEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(GetType());
                serializer.WriteObject(entryStream, this);
            }

            //TODO
            // llm.SaveHashCode(archive, dirname);
        }

        public static T Load<T>(LLM llm, string filePath, string dirname = "") where T : Search
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load<T>(llm, archive, dirname);
            }
        }

        public static T Load<T>(LLM llm, ZipArchive archive, string dirname = "") where T : Search
        {
            ZipArchiveEntry baseEntry = archive.GetEntry(GetSearchPath(dirname));
            T search;
            using (Stream entryStream = baseEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                search = (T)serializer.ReadObject(entryStream);
            }

            // TODO
            // int embedderHash = EmbeddingModel.LoadHashCode(archive, dirname);
            // if (embedder.GetHashCode() != embedderHash)
            //     throw new Exception($"The Search object uses different embedding model than the Search object stored");
            // search.SetLLM(llm);

            return search;
        }
    }

    [DataContract]
    public class BruteForceSearch : Search
    {
        [DataMember]
        protected Dictionary<int, float[]> index = new Dictionary<int, float[]>();

        public override async Task<float[]> Add(int key, string inputString)
        {
            float[] embedding = (await Embeddings(inputString)).ToArray();
            index[key] = embedding;
            return embedding;
        }

        public override bool Remove(int key)
        {
            return index.Remove(key);
        }

        public override int Count()
        {
            return index.Count;
        }

        public static float DotProduct(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vector lengths must be equal for dot product calculation");
            }
            float result = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                result += vector1[i] * vector2[i];
            }
            return result;
        }

        public static float InverseDotProduct(float[] vector1, float[] vector2)
        {
            return 1 - DotProduct(vector1, vector2);
        }

        public static float[] InverseDotProduct(float[] vector1, float[][] vector2)
        {
            float[] results = new float[vector2.Length];
            for (int i = 0; i < vector2.Length; i++)
            {
                results[i] = InverseDotProduct(vector1, vector2[i]);
            }
            return results;
        }

        public override int[] Search(float[] encoding, int k, out float[] distances)
        {
            float[] unsortedDistances = InverseDotProduct(encoding, index.Values.ToArray());

            var sortedLists = index.Keys.Zip(unsortedDistances, (first, second) => new { First = first, Second = second })
                .OrderBy(item => item.Second)
                .ToList();
            int kmax = k == -1 ? sortedLists.Count : Math.Min(k, sortedLists.Count);
            int[] results = new int[kmax];
            distances = new float[kmax];
            for (int i = 0; i < kmax; i++)
            {
                results[i] = sortedLists[i].First;
                distances[i] = sortedLists[i].Second;
            }
            return results;
        }

        public static BruteForceSearch Load(LLM llm, string filePath, string dirname = "")
        {
            return Load<BruteForceSearch>(llm, filePath, dirname);
        }

        public static BruteForceSearch Load(LLM llm, ZipArchive archive, string dirname = "")
        {
            return Load<BruteForceSearch>(llm, archive, dirname);
        }
    }

    [DataContract]
    public class ANNModelSearch : Search
    {
        USearchIndex index;
        public ScalarKind quantization = ScalarKind.Float16;
        public MetricKind metricKind = MetricKind.Cos;
        public ulong connectivity = 32;
        public ulong expansionAdd = 40;
        public ulong expansionSearch = 16;

        public override void Awake()
        {
            if (!enabled) return;
            base.Awake();
            index = new USearchIndex((ulong)llm.embeddingLength, metricKind, quantization, connectivity, expansionAdd, expansionSearch, false);
        }

        public void SetIndex(USearchIndex index)
        {
            this.index = index;
        }

        public override async Task<float[]> Add(int key, string inputString)
        {
            float[] embedding = (await Embeddings(inputString)).ToArray();
            index.Add((ulong)key, embedding);
            return embedding;
        }

        public override bool Remove(int key)
        {
            return index.Remove((ulong)key) > 0;
        }

        public override int Count()
        {
            return (int)index.Size();
        }

        public override int[] Search(float[] encoding, int k, out float[] distances)
        {
            index.Search(encoding, k, out ulong[] keys, out distances);
            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) intKeys[i] = (int)keys[i];
            return intKeys;
        }

        public static string GetIndexPath(string dirname = "")
        {
            return Path.Combine(dirname, "USearch");
        }

        public override void Save(ZipArchive archive, string dirname = "")
        {
            base.Save(archive, dirname);
            index.Save(archive, GetIndexPath(dirname));
        }

        public static ANNModelSearch Load(LLM llm, string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load(llm, archive, dirname);
            }
        }

        public static ANNModelSearch Load(LLM llm, ZipArchive archive, string dirname = "")
        {
            ANNModelSearch search = Load<ANNModelSearch>(llm, archive, dirname);
            USearchIndex index = new USearchIndex(archive, GetIndexPath(dirname));
            search.SetIndex(index);
            return search;
        }
    }
}
