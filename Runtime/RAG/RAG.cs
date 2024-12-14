/// @file
/// @brief File implementing the Retrieval Augmented Generation (RAG) system.
using System;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    /// <summary>
    /// Search methods implemented in LLMUnity
    /// </summary>
    public enum SearchMethods
    {
        DBSearch,
        SimpleSearch,
    }

    public class NoChunking {}

    /// <summary>
    /// Chunking methods implemented in LLMUnity
    /// </summary>
    public enum ChunkingMethods
    {
        NoChunking,
        TokenSplitter,
        WordSplitter,
        SentenceSplitter
    }

    /// @ingroup rag
    /// <summary>
    /// Class implementing a Retrieval Augmented Generation (RAG) system based on a search method and an optional chunking method.
    /// </summary>
    [Serializable]
    public class RAG : Searchable
    {
        public SearchMethods searchType = SearchMethods.SimpleSearch;
        public SearchMethod search;
        public ChunkingMethods chunkingType = ChunkingMethods.NoChunking;
        public Chunking chunking;

        /// <summary>
        /// Constructs the Retrieval Augmented Generation (RAG) system based on the provided search and chunking method.
        /// </summary>
        /// <param name="searchMethod">search method</param>
        /// <param name="chunkingMethod">chunking method for splitting the search entries</param>
        /// <param name="llm">LLM to use for the search method</param>
        public void Init(SearchMethods searchMethod = SearchMethods.SimpleSearch, ChunkingMethods chunkingMethod = ChunkingMethods.NoChunking, LLM llm = null)
        {
            searchType = searchMethod;
            chunkingType = chunkingMethod;
            UpdateGameObjects();
            search.SetLLM(llm);
        }

        /// <summary>
        /// Set to true to return chunks or the direct input with the Search function
        /// </summary>
        /// <param name="returnChunks">whether to return chunks</param>
        public void ReturnChunks(bool returnChunks)
        {
            if (chunking != null) chunking.ReturnChunks(returnChunks);
        }

        /// \cond HIDE
        protected void ConstructSearch()
        {
            search = ConstructComponent<SearchMethod>(Type.GetType("LLMUnity." + searchType.ToString()), (previous, current) => current.llmEmbedder.llm = previous.llmEmbedder.llm);
            if (chunking != null) chunking.SetSearch(search);
        }

        protected void ConstructChunking()
        {
            Type type = null;
            if (chunkingType != ChunkingMethods.NoChunking) type = Type.GetType("LLMUnity." + chunkingType.ToString());
            chunking = ConstructComponent<Chunking>(type);
            if (chunking != null) chunking.SetSearch(search);
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
        public override async Task<int> Add(string inputString, string group = "") { return await GetSearcher().Add(inputString, group); }
        public override int Remove(string inputString, string group = "") { return GetSearcher().Remove(inputString, group); }
        public override void Remove(int key) { GetSearcher().Remove(key); }
        public override int Count() { return GetSearcher().Count(); }
        public override int Count(string group) { return GetSearcher().Count(group); }
        public override void Clear() { GetSearcher().Clear(); }
        public override async Task<int> IncrementalSearch(string queryString, string group = "") { return await GetSearcher().IncrementalSearch(queryString, group);}
        public override (string[], float[], bool) IncrementalFetch(int fetchKey, int k) { return GetSearcher().IncrementalFetch(fetchKey, k);}
        public override (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k) { return GetSearcher().IncrementalFetchKeys(fetchKey, k);}
        public override void IncrementalSearchComplete(int fetchKey) { GetSearcher().IncrementalSearchComplete(fetchKey);}
        public override void Save(ZipArchive archive) { GetSearcher().Save(archive); }
        public override void Load(ZipArchive archive) { GetSearcher().Load(archive); }
        /// \endcond
    }
}
