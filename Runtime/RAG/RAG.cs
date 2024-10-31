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
        public LLMEmbedder llmEmbedder;
        public SearchMethods searchClass = SearchMethods.SimpleSearch;
        public SearchMethod search;
        public ChunkingMethods chunkingClass = ChunkingMethods.NoChunking;
        public Chunking chunking;

        SearchMethods preSearchClass;
        ChunkingMethods preChunkingClass;

        public void Initialize(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLMEmbedder llmEmbedder = null)
        {
            searchClass = searchMethod;
            chunkingClass = chunkingMethod;
            this.llmEmbedder = llmEmbedder;
            UpdateGameObjects();
        }

        public void Initialize(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLM llm = null)
        {
            llmEmbedder = (LLMEmbedder)GetOrAddObject(typeof(LLMEmbedder));
            llmEmbedder.llm = llm;
            Initialize(searchMethod, chunkingMethod, llmEmbedder);
        }

        protected Component GetOrAddObject(Type type)
        {
            Component component = gameObject.GetComponent(type);
            if (component == null) component = gameObject.AddComponent(type);
            return component;
        }

        protected Component AddObjectFromEnum(Enum enumeration)
        {
            Type type = Type.GetType("LLMUnity." + enumeration.ToString());
            return GetOrAddObject(type);
        }

        protected void ConstructSearch()
        {
            if (search != null) DestroyImmediate(search);
            search = (SearchMethod)AddObjectFromEnum(searchClass);
            search.llmEmbedder = llmEmbedder;
        }

        protected void ConstructChunking()
        {
            if (chunking != null) DestroyImmediate(chunking);
            if (chunkingClass == ChunkingMethods.NoChunking) return;
            chunking = (Chunking)AddObjectFromEnum(chunkingClass);
            chunking.search = search;
        }

        void UpdateGameObjects()
        {
            if (llmEmbedder == null) llmEmbedder = (LLMEmbedder)GetOrAddObject(typeof(LLMEmbedder));
            if (search == null || preSearchClass != searchClass)
            {
                ConstructSearch();
                preSearchClass = searchClass;
            }
            if (chunking == null || preChunkingClass != chunkingClass)
            {
                ConstructChunking();
                preChunkingClass = chunkingClass;
            }
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            if (!Application.isPlaying) EditorApplication.update += UpdateGameObjects;
        }

        public void OnDestroy()
        {
            if (!Application.isPlaying) EditorApplication.update -= UpdateGameObjects;
        }

#endif

        protected Searchable GetSearcher()
        {
            if (chunking != null) return chunking;
            if (search != null) return search;
            throw new Exception("The search GameObject is null");
        }

        public override string Get(int key) { return GetSearcher().Get(key); }
        public override async Task<int> Add(string inputString, string splitId = "") { return await GetSearcher().Add(inputString, splitId); }
        public override int Remove(string inputString, string splitId = "") { return GetSearcher().Remove(inputString, splitId); }
        public override void Remove(int key) { GetSearcher().Remove(key); }
        public override int Count() { return GetSearcher().Count(); }
        public override int Count(string splitId) { return GetSearcher().Count(splitId); }
        public override void Clear() { GetSearcher().Clear(); }
        public override async Task<int> IncrementalSearch(string queryString, string splitId = "") { return await GetSearcher().IncrementalSearch(queryString, splitId);}
        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k) { return GetSearcher().IncrementalFetchKeys(fetchKey, k);}
        public override void IncrementalSearchComplete(int fetchKey) { GetSearcher().IncrementalSearchComplete(fetchKey);}
        public override void Save(ZipArchive archive) { GetSearcher().Save(archive); }
        public override void Load(ZipArchive archive) { GetSearcher().Load(archive); }
    }
}
