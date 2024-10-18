
using System;
using System.Collections.Generic;
using Cloud.Unum.USearch;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace LLMUnity
{
    [DataContract]
    public class DBSearch : SearchMethod
    {
        USearchIndex index;
        [DataMember] public ScalarKind quantization = ScalarKind.Float16;
        [DataMember] public MetricKind metricKind = MetricKind.Cos;
        [DataMember] public ulong connectivity = 32;
        [DataMember] public ulong expansionAdd = 40;
        [DataMember] public ulong expansionSearch = 16;
        private Dictionary<int, (float[], List<int>)> incrementalSearchCache = new Dictionary<int, (float[], List<int>)>();

        public override void Awake()
        {
            if (!enabled) return;
            base.Awake();
            InitIndex();
        }

        public void InitIndex()
        {
            index = new USearchIndex((ulong)llm.embeddingLength, metricKind, quantization, connectivity, expansionAdd, expansionSearch, false);
        }

        public void SetIndex(USearchIndex index)
        {
            this.index = index;
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

        public static string GetIndexSavePath(string dirname = "")
        {
            return Path.Combine(dirname, "USearch");
        }

        public override void Save(ZipArchive archive, string dirname = "")
        {
            ((ISearchable)this).Save(archive, dirname);
            index.Save(archive, GetIndexSavePath(dirname));
        }

        public static DBSearch Load(ZipArchive archive, string dirname = "")
        {
            DBSearch search = SearchMethod.Load<DBSearch>(archive, dirname);
            USearchIndex index = new USearchIndex(archive, GetIndexSavePath(dirname));
            search.SetIndex(index);
            return search;
        }

        protected override void ClearInternal()
        {
            index.FreeIndex();
            InitIndex();
            incrementalSearchCache.Clear();
        }
    }
}