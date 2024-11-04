/// @file
/// @brief File implementing the vector database search.
using System;
using System.Collections.Generic;
using Cloud.Unum.USearch;
using System.IO.Compression;
using UnityEngine;

namespace LLMUnity
{
    /// @ingroup rag
    /// <summary>
    /// Class implementing a search with a vector database.
    /// The search results are retrieved with Approximate Nearest Neighbor (ANN) which is much faster that SimpleSearch.
    /// </summary>
    [DefaultExecutionOrder(-2)]
    public class DBSearch : SearchMethod
    {
        protected USearchIndex index;
        /// <summary> toggle to show/hide advanced options in the GameObject </summary>
        [HideInInspector] public bool advancedOptions = false;
        /// <summary> The quantisation type used for vector data during indexing. </summary>
        [ModelAdvanced] public ScalarKind quantization = ScalarKind.Float16;
        /// <summary> The metric kind used for distance calculation between vectors. </summary>
        [ModelAdvanced] public MetricKind metricKind = MetricKind.Cos;
        /// <summary> The connectivity parameter limits the connections-per-node in the graph. </summary>
        [ModelAdvanced] public ulong connectivity = 32;
        /// <summary> The expansion factor used for index construction when adding vectors. </summary>
        [ModelAdvanced] public ulong expansionAdd = 40;
        /// <summary> The expansion factor used for index construction during search operations. </summary>
        [ModelAdvanced] public ulong expansionSearch = 16;

        private Dictionary<int, (float[], string, List<int>)> incrementalSearchCache = new Dictionary<int, (float[], string, List<int>)>();

        /// \cond HIDE
        public new void Awake()
        {
            base.Awake();
            if (!enabled) return;
            InitIndex();
        }

        public void InitIndex()
        {
            index = new USearchIndex(metricKind, quantization, (ulong)llmEmbedder.llm.embeddingLength, connectivity, expansionAdd, expansionSearch, false);
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

        public override int IncrementalSearch(float[] embedding, string group = "")
        {
            int key = nextIncrementalSearchKey++;
            incrementalSearchCache[key] = (embedding, group, new List<int>());
            return key;
        }

        public override ValueTuple<int[], float[], bool> IncrementalFetchKeys(int fetchKey, int k)
        {
            if (!incrementalSearchCache.ContainsKey(fetchKey)) throw new Exception($"There is no IncrementalSearch cached with this key: {fetchKey}");

            (float[] embedding, string group, List<int> seenKeys) = incrementalSearchCache[fetchKey];

            if (!dataSplits.TryGetValue(group, out List<int> dataSplit)) return (new int[0], new float[0], true);
            if (dataSplit.Count == 0) return (new int[0], new float[0], true);

            Func<int, int> filter = (int key) => !dataSplit.Contains(key) || seenKeys.Contains(key) ? 0 : 1;
            index.Search(embedding, k, out ulong[] keys, out float[] distances, filter);
            int[] intKeys = UlongToInt(keys);
            incrementalSearchCache[fetchKey].Item3.AddRange(intKeys);

            bool completed = intKeys.Length < k || seenKeys.Count == Count(group);
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

        /// \endcond
    }
}
