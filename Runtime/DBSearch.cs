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
        private Dictionary<int, (float[], List<int>)> incrementalSearchCache = new Dictionary<int, (float[], List<int>)>();

        public override void Awake()
        {
            if (!enabled) return;
            base.Awake();
            InitIndex();
        }

        public void InitIndex()
        {
            index = new USearchIndex(metricKind, quantization, (ulong)llm.embeddingLength, connectivity, expansionAdd, expansionSearch, false);
        }

        protected override void AddInternal(int key, float[] embedding)
        {
            index.Add((ulong)key, embedding);
        }

        protected override void RemoveInternal(int key)
        {
            index.Remove((ulong)key);
        }

        protected override int[] SearchInternal(float[] embedding, int k, out float[] distances)
        {
            index.Search(embedding, k, out ulong[] keys, out distances);
            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) intKeys[i] = (int)keys[i];
            return intKeys;
        }

        public override int IncrementalSearch(float[] embedding)
        {
            int key = nextIncrementalSearchKey++;
            incrementalSearchCache[key] = (embedding, new List<int>());
            return key;
        }

        public override (int[], bool) IncrementalFetchKeys(int fetchKey, int k)
        {
            if (!incrementalSearchCache.ContainsKey(fetchKey)) throw new Exception($"There is no IncrementalSearch cached with this key: {fetchKey}");

            float[] embedding;
            List<int> seenKeys;
            (embedding, seenKeys) = incrementalSearchCache[fetchKey];
            int matches = index.Search(embedding, k, out ulong[] keys, out float[] distances, (int key, IntPtr state) => {return seenKeys.Contains(key) ? 0 : 1;});
            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++) intKeys[i] = (int)keys[i];
            incrementalSearchCache[fetchKey].Item2.AddRange(intKeys);

            bool completed = matches < k || seenKeys.Count == Count();
            if (completed) IncrementalSearchComplete(fetchKey);
            return (intKeys, completed);
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
