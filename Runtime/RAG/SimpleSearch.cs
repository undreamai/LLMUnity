using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public class SimpleSearch : SearchMethod
    {
        protected SortedDictionary<int, float[]> embeddings = new SortedDictionary<int, float[]>();
        protected Dictionary<int, List<(int, float)>> incrementalSearchCache = new Dictionary<int, List<(int, float)>>();

        protected override void AddInternal(int key, float[] embedding)
        {
            embeddings[key] = embedding;
        }

        protected override void RemoveInternal(int key)
        {
            embeddings.Remove(key);
        }

        public static float DotProduct(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null) throw new ArgumentNullException("Vectors cannot be null");
            if (vector1.Length != vector2.Length) throw new ArgumentException("Vector lengths must be equal for dot product calculation");
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

        public override int IncrementalSearch(float[] embedding, string splitId = "")
        {
            int key = nextIncrementalSearchKey++;

            List<(int, float)> sortedLists = new List<(int, float)>();
            if (dataSplits.TryGetValue(splitId, out List<int> dataSplit))
            {
                if (dataSplit.Count >= 0)
                {
                    float[][] embeddingsSplit = new float[dataSplit.Count][];
                    for (int i = 0; i < dataSplit.Count; i++) embeddingsSplit[i] = embeddings[dataSplit[i]];

                    float[] unsortedDistances = InverseDotProduct(embedding, embeddingsSplit);
                    sortedLists = dataSplit.Zip(unsortedDistances, (first, second) => (first, second))
                        .OrderBy(item => item.Item2)
                        .ToList();
                }
            }
            incrementalSearchCache[key] = sortedLists;
            return key;
        }

        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k)
        {
            if (!incrementalSearchCache.ContainsKey(fetchKey)) throw new Exception($"There is no IncrementalSearch cached with this key: {fetchKey}");

            bool completed;
            List<(int, float)> sortedLists;
            if (k == -1)
            {
                sortedLists = incrementalSearchCache[fetchKey];
                completed = true;
            }
            else
            {
                int getK = Math.Min(k, incrementalSearchCache[fetchKey].Count);
                sortedLists = incrementalSearchCache[fetchKey].GetRange(0, getK);
                incrementalSearchCache[fetchKey].RemoveRange(0, getK);
                completed = incrementalSearchCache[fetchKey].Count == 0;
            }
            if (completed) IncrementalSearchComplete(fetchKey);

            int[] results = new int[sortedLists.Count];
            float[] distances = new float[sortedLists.Count];
            for (int i = 0; i < sortedLists.Count; i++)
            {
                results[i] = sortedLists[i].Item1;
                distances[i] = sortedLists[i].Item2;
            }
            return (results.ToArray(), distances.ToArray(), completed);
        }

        public override void IncrementalSearchComplete(int fetchKey)
        {
            incrementalSearchCache.Remove(fetchKey);
        }

        protected override void ClearInternal()
        {
            embeddings.Clear();
            incrementalSearchCache.Clear();
        }

        protected override void SaveInternal(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, embeddings, "SimpleSearch_embeddings");
            ArchiveSaver.Save(archive, incrementalSearchCache, "SimpleSearch_incrementalSearchCache");
        }

        protected override void LoadInternal(ZipArchive archive)
        {
            embeddings = ArchiveSaver.Load<SortedDictionary<int, float[]>>(archive, "SimpleSearch_embeddings");
            incrementalSearchCache = ArchiveSaver.Load<Dictionary<int, List<(int, float)>>>(archive, "SimpleSearch_incrementalSearchCache");
        }
    }
}
