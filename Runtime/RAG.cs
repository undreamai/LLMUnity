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
        public LLMCaller llmCaller;
        public SearchMethods searchClass = SearchMethods.SimpleSearch;
        public SearchMethod search;
        public ChunkingMethods chunkingClass = ChunkingMethods.NoChunking;
        public Chunking chunking;

        SearchMethods preSearchClass;
        ChunkingMethods preChunkingClass;

        public void Initialize(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLMCaller llmCaller = null)
        {
            searchClass = searchMethod;
            chunkingClass = chunkingMethod;
            this.llmCaller = llmCaller;
            UpdateGameObjects();
        }

        public void Initialize(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLM llm = null)
        {
            llmCaller = (LLMCaller)GetOrAddObject(typeof(LLMCaller));
            llmCaller.llm = llm;
            Initialize(searchMethod, chunkingMethod, llmCaller);
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
            search.llmCaller = llmCaller;
        }

        protected void ConstructChunking()
        {
            if (chunking != null) DestroyImmediate(chunking);
            if (chunkingClass == ChunkingMethods.NoChunking) return;
            chunking = (Chunking)AddObjectFromEnum(chunkingClass);
            chunking.search = search;
        }

        protected virtual void Reset()
        {
            if (!Application.isPlaying) EditorApplication.update += UpdateGameObjects;
        }

        public void OnDestroy()
        {
            if (!Application.isPlaying) EditorApplication.update -= UpdateGameObjects;
        }

        void UpdateGameObjects()
        {
            if (llmCaller == null) llmCaller = (LLMCaller)GetOrAddObject(typeof(LLMCaller));
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

        protected Searchable GetSearcher()
        {
            if (chunking != null) return chunking;
            if (search != null) return search;
            throw new Exception("The search GameObject is null");
        }

        public override string Get(int key) { return GetSearcher().Get(key); }
        public override async Task<int> Add(string inputString) { return await GetSearcher().Add(inputString); }
        public override int Remove(string inputString) { return GetSearcher().Remove(inputString); }
        public override void Remove(int key) { GetSearcher().Remove(key); }
        public override int Count() { return GetSearcher().Count(); }
        public override void Clear() { GetSearcher().Clear(); }
        public override async Task<(string[], float[])> Search(string queryString, int k) { return await GetSearcher().Search(queryString, k); }
        public override async Task<int> IncrementalSearch(string queryString) { return await GetSearcher().IncrementalSearch(queryString);}
        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k) { return GetSearcher().IncrementalFetchKeys(fetchKey, k);}
        public override void IncrementalSearchComplete(int fetchKey) { GetSearcher().IncrementalSearchComplete(fetchKey);}

        public override void Save(string filePath) { ArchiveSaver.Save(filePath, Save); }
        public override void Load(string filePath) { ArchiveSaver.Load(filePath, Load); }

        public override void Save(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, JsonUtility.ToJson(this, true), "RAG_object");
            GetSearcher().Save(archive);
        }

        public override void Load(ZipArchive archive)
        {
            JsonUtility.FromJsonOverwrite(ArchiveSaver.Load<string>(archive, "RAG_object"), this);
            GetSearcher().Load(archive);
        }
    }
}
