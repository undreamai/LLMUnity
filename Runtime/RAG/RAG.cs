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

        SearchMethods preSearchClass;
        ChunkingMethods preChunkingClass;

        public void Construct(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLM llm = null)
        {
            searchClass = searchMethod;
            chunkingClass = chunkingMethod;
            UpdateGameObjects();
            search.SetLLM(llm);
        }

        protected Component AddObjectFromEnum(Enum enumeration)
        {
            Type type = Type.GetType("LLMUnity." + enumeration.ToString());
            return GetOrAddObject(gameObject, type);
        }

        protected void ConstructSearch()
        {
            SearchMethod newSearch = (SearchMethod)AddObjectFromEnum(searchClass);
            newSearch.UpdateGameObjects();
            if (search != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(search);
#else
                Destroy(search);
#endif
            }
            search = newSearch;
        }

        protected void ConstructChunking()
        {
            Chunking newChunking = null;
            if (chunkingClass != ChunkingMethods.NoChunking)
            {
                newChunking = (Chunking)AddObjectFromEnum(chunkingClass);
                newChunking.UpdateGameObjects();
                newChunking.search = search;
            }
            if (chunking != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(chunking);
#else
                Destroy(chunking);
#endif
            }
            chunking = newChunking;
        }

        public override void UpdateGameObjects()
        {
            if (this == null) return;
            bool constructSearch = search == null;
            bool constructChunking = chunking == null;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                constructSearch = constructSearch || preSearchClass != searchClass;
                constructChunking = constructChunking || preChunkingClass != chunkingClass;
            }
#endif
            if (constructSearch)
            {
                ConstructSearch();
                preSearchClass = searchClass;
            }
            if (constructChunking)
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
