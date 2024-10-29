using System;
using System.Collections.Generic;
using Cloud.Unum.USearch;
using System.IO.Compression;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public class DBSearch : SearchMethod
    {
        protected USearchIndex index;
        [ModelAdvanced] public ScalarKind quantization = ScalarKind.Float16;
        [ModelAdvanced] public MetricKind metricKind = MetricKind.Cos;
        [ModelAdvanced] public ulong connectivity = 32;
        [ModelAdvanced] public ulong expansionAdd = 40;
        [ModelAdvanced] public ulong expansionSearch = 16;
        private Dictionary<int, (float[], int, List<int>)> incrementalSearchCache = new Dictionary<int, (float[], int, List<int>)>();

        public void Awake()
        {
            if (!enabled) return;
            InitIndex();
        }

        public void InitIndex()
        {
            index = new USearchIndex(metricKind, quantization, (ulong)llmCaller.llm.embeddingLength, connectivity, expansionAdd, expansionSearch, false);
        }

        protected override void AddInternal(int key, float[] embedding)
        {
            index.Add((ulong)key, embedding);
        }

        protected override void RemoveInternal(int key)
        {
            index.Remove((ulong)key);
        }

        protected int[] UlongToInt(ulong[] keys)
        {
            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) intKeys[i] = (int)keys[i];
            return intKeys;
        }

        public override int IncrementalSearch(float[] embedding, int id = 0)
        {
            int key = nextIncrementalSearchKey++;
            incrementalSearchCache[key] = (embedding, id, new List<int>());
            return key;
        }

        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k)
        {
            if (!incrementalSearchCache.ContainsKey(fetchKey)) throw new Exception($"There is no IncrementalSearch cached with this key: {fetchKey}");

            (float[] embedding, int id, List<int> seenKeys) = incrementalSearchCache[fetchKey];

            if (!dataSplits.TryGetValue(id, out List<int> dataSplit)) return (new int[0], new float[0], true);
            if (dataSplit.Count == 0) return (new int[0], new float[0], true);

            index.Search(
                embedding, k, out ulong[] keys, out float[] distances,
                (int key, IntPtr state) => !dataSplit.Contains(key) || seenKeys.Contains(key) ? 0 : 1
            );
            int[] intKeys = UlongToInt(keys);
            incrementalSearchCache[fetchKey].Item3.AddRange(intKeys);

            bool completed = intKeys.Length < k || seenKeys.Count == Count(id);
            if (completed) IncrementalSearchComplete(fetchKey);
            return (intKeys, distances, completed);
        }

        public override void IncrementalSearchComplete(int fetchKey)
        {
            incrementalSearchCache.Remove(fetchKey);
        }

        protected override void SaveInternal(ZipArchive archive)
        {
            index.Save(archive);
        }

        protected override void LoadInternal(ZipArchive archive)
        {
            index.Load(archive);
        }

        protected override void ClearInternal()
        {
            index.Dispose();
            InitIndex();
            incrementalSearchCache.Clear();
        }
    }
}
