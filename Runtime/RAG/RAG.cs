using System;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    public enum SearchMethods
    {
        SimpleSearch,
        DBSearch,
    }

    public class NoChunking {}

    public enum ChunkingMethods
    {
        NoChunking,
        TokenSplitter,
        WordSplitter,
        SentenceSplitter
    }

    [Serializable]
    public class RAG : Searchable
    {
        public SearchMethods searchClass = SearchMethods.SimpleSearch;
        public SearchMethod search;
        public ChunkingMethods chunkingClass = ChunkingMethods.NoChunking;
        public Chunking chunking;

        [SerializeField, HideInInspector] SearchMethods preSearchClass;
        [SerializeField, HideInInspector] ChunkingMethods preChunkingClass;

        public void Construct(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLM llm = null)
        {
            searchClass = searchMethod;
            chunkingClass = chunkingMethod;
            UpdateGameObjects();
            search.SetLLM(llm);
        }

        protected void ConstructSearch()
        {
            search = ConstructComponent<SearchMethod>(Type.GetType("LLMUnity." + searchClass.ToString()), (previous, current) => current.llmEmbedder.llm = previous.llmEmbedder.llm);
            if (chunking != null) chunking.search = search;
        }

        protected void ConstructChunking()
        {
            Type type = null;
            if (chunkingClass != ChunkingMethods.NoChunking) type = Type.GetType("LLMUnity." + chunkingClass.ToString());
            chunking = ConstructComponent<Chunking>(type, (previous, current) => current.search = previous.search);
        }

        public override void UpdateGameObjects()
        {
            if (this == null) return;
            ConstructSearch();
            ConstructChunking();
        }

        protected Searchable GetSearcher()
        {
            if (chunking != null) return chunking;
            if (search != null) return search;
            throw new Exception("The search GameObject is null");
        }

#if UNITY_EDITOR
        private void OnValidateUpdate()
        {
            EditorApplication.delayCall -= OnValidateUpdate;
            UpdateGameObjects();
        }

        public virtual void OnValidate()
        {
            if (!Application.isPlaying) EditorApplication.delayCall += OnValidateUpdate;
        }

#endif

        public override string Get(int key) { return GetSearcher().Get(key); }
        public override async Task<int> Add(string inputString, string splitId = "") { return await GetSearcher().Add(inputString, splitId); }
        public override int Remove(string inputString, string splitId = "") { return GetSearcher().Remove(inputString, splitId); }
        public override void Remove(int key) { GetSearcher().Remove(key); }
        public override int Count() { return GetSearcher().Count(); }
        public override int Count(string splitId) { return GetSearcher().Count(splitId); }
        public override void Clear() { GetSearcher().Clear(); }
        public override async Task<int> IncrementalSearch(string queryString, string splitId = "") { return await GetSearcher().IncrementalSearch(queryString, splitId);}
        public override (string[], float[], bool) IncrementalFetch(int fetchKey, int k) { return GetSearcher().IncrementalFetch(fetchKey, k);}
        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k) { return GetSearcher().IncrementalFetchKeys(fetchKey, k);}
        public override void IncrementalSearchComplete(int fetchKey) { GetSearcher().IncrementalSearchComplete(fetchKey);}
        public override void Save(ZipArchive archive) { GetSearcher().Save(archive); }
        public override void Load(ZipArchive archive) { GetSearcher().Load(archive); }
    }
}
