/// @file
/// @brief File implementing the search functionality
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// @defgroup rag RAG
namespace LLMUnity
{
    /// @ingroup rag
    /// <summary>
    /// Class implementing the search template
    /// </summary>
    [DefaultExecutionOrder(-2)]
    public abstract class Searchable : MonoBehaviour
    {
        /// <summary>
        /// Retrieves the phrase with the specific id
        /// </summary>
        /// <param name="key">phrase id</param>
        /// <returns>phrase</returns>
        public abstract string Get(int key);

        /// <summary>
        /// Adds a phrase to the search.
        /// </summary>
        /// <param name="inputString">input phrase</param>
        /// <param name="group">data group to add it to </param>
        /// <returns>phrase id</returns>
        public abstract Task<int> Add(string inputString, string group = "");

        /// <summary>
        /// Removes a phrase from the search.
        /// </summary>
        /// <param name="inputString">input phrase</param>
        /// <param name="group">data group to remove it from </param>
        /// <returns>number of removed entries/returns>
        public abstract int Remove(string inputString, string group = "");

        /// <summary>
        /// Removes a phrase from the search.
        /// </summary>
        /// <param name="key">phrase id</param>
        public abstract void Remove(int key);

        /// <summary>
        /// Returns a count of the phrases
        /// </summary>
        /// <returns>phrase count</returns>
        public abstract int Count();

        /// <summary>
        /// Returns a count of the phrases in a specific data group
        /// </summary>
        /// <param name="group">data group</param>
        /// <returns>phrase count</returns>
        public abstract int Count(string group);

        /// <summary>
        /// Clears the search object
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Allows to do search and retrieve results in batches (incremental search).
        /// </summary>
        /// <param name="queryString">search query</param>
        /// <param name="group">data group to search in</param>
        /// <returns>incremental search key</returns>
        public abstract Task<int> IncrementalSearch(string queryString, string group = "");

        /// <summary>
        /// Retrieves the most similar search results in batches (incremental search).
        /// The phrase keys and distances are retrieved, as well as a parameter that dictates whether the search is exhausted.
        /// </summary>
        /// <param name="fetchKey">incremental search key</param>
        /// <param name="k">number of results to retrieve</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Array of retrieved keys (`int[]`).</description></item>
        /// <item><description>Array of distances for each result (`float[]`).</description></item>
        /// <item><description>`bool` indicating if the search is exhausted.</description></item>
        /// </list>
        /// </returns>
        public abstract ValueTuple<int[], float[], bool> IncrementalFetchKeys(int fetchKey, int k);

        /// <summary>
        /// Completes the search and clears the cached results for an incremental search
        /// </summary>
        /// <param name="fetchKey">incremental search key</param>
        public abstract void IncrementalSearchComplete(int fetchKey);

        /// <summary>
        /// Search for similar results to the provided query.
        /// The most similar results and their distances (dissimilarity) to the query are retrieved.
        /// </summary>
        /// <param name="queryString">query</param>
        /// <param name="k">number of results to retrieve</param>
        /// <param name="group">data group to search in</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Array of retrieved results (`string[]`).</description></item>
        /// <item><description>Array of distances for each result (`float[]`).</description></item>
        /// <item><description>`bool` indicating if the search is exhausted.</description></item>
        /// </list>
        /// </returns>
        public async Task<(string[], float[])> Search(string queryString, int k, string group = "")
        {
            int fetchKey = await IncrementalSearch(queryString, group);
            (string[] phrases, float[] distances, bool completed) = IncrementalFetch(fetchKey, k);
            if (!completed) IncrementalSearchComplete(fetchKey);
            return (phrases, distances);
        }

        /// <summary>
        /// Retrieves the most similar search results in batches (incremental search).
        /// The most similar results and their distances (dissimilarity) to the query are retrieved as well as a parameter that dictates whether the search is exhausted.
        /// </summary>
        /// <param name="fetchKey">incremental search key</param>
        /// <param name="k">number of results to retrieve</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Array of retrieved results (`string[]`).</description></item>
        /// <item><description>Array of distances for each result (`float[]`).</description></item>
        /// <item><description>`bool` indicating if the search is exhausted.</description></item>
        /// </list>
        /// </returns>
        public virtual ValueTuple<string[], float[], bool> IncrementalFetch(int fetchKey, int k)
        {
            (int[] resultKeys, float[] distances, bool completed) = IncrementalFetchKeys(fetchKey, k);
            string[] results = new string[resultKeys.Length];
            for (int i = 0; i < resultKeys.Length; i++) results[i] = Get(resultKeys[i]);
            return (results, distances, completed);
        }

        /// <summary>
        /// Saves the state of the search object.
        /// </summary>
        /// <param name="archive">file to save to</param>
        public void Save(string filePath)
        {
            try
            {
                string path = LLMUnitySetup.GetAssetPath(filePath);
                ArchiveSaver.Save(path, Save);
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"File {filePath} could not be saved due to {e.GetType()}: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the state of the search object.
        /// </summary>
        /// <param name="archive">file to load from</param>
        public async Task<bool> Load(string filePath)
        {
            try
            {
                await LLMUnitySetup.AndroidExtractAsset(filePath, true);
                string path = LLMUnitySetup.GetAssetPath(filePath);
                if (!File.Exists(path)) return false;
                ArchiveSaver.Load(path, Load);
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"File {filePath} could not be loaded due to {e.GetType()}: {e.Message}");
                return false;
            }
            return true;
        }

        /// \cond HIDE
        public abstract void Save(ZipArchive archive);
        public abstract void Load(ZipArchive archive);
        public virtual string GetSavePath(string name)
        {
            return Path.Combine(GetType().Name, name);
        }

        public virtual void UpdateGameObjects() {}

        protected T ConstructComponent<T>(Type type, Action<T, T> copyAction = null) where T : Component
        {
            T Construct(Type type)
            {
                if (type == null) return null;
                T newComponent = (T)gameObject.AddComponent(type);
                if (newComponent is Searchable searchable) searchable.UpdateGameObjects();
                return newComponent;
            }

            T component = (T)gameObject.GetComponent(typeof(T));
            T newComponent;
            if (component == null)
            {
                newComponent = Construct(type);
            }
            else
            {
                if (component.GetType() == type)
                {
                    newComponent = component;
                }
                else
                {
                    newComponent = Construct(type);
                    if (type != null) copyAction?.Invoke(component, newComponent);
#if UNITY_EDITOR
                    DestroyImmediate(component);
#else
                    Destroy(component);
#endif
                }
            }
            return newComponent;
        }

        public virtual void Awake()
        {
            UpdateGameObjects();
        }

#if UNITY_EDITOR
        public virtual void Reset()
        {
            if (!Application.isPlaying) EditorApplication.update += UpdateGameObjects;
        }

        public virtual void OnDestroy()
        {
            if (!Application.isPlaying) EditorApplication.update -= UpdateGameObjects;
        }

#endif
        /// \endcond
    }

    /// @ingroup rag
    /// <summary>
    /// Class implementing the search method template
    /// </summary>
    public abstract class SearchMethod : Searchable
    {
        public LLMEmbedder llmEmbedder;

        protected int nextKey = 0;
        protected int nextIncrementalSearchKey = 0;
        protected SortedDictionary<int, string> data = new SortedDictionary<int, string>();
        protected SortedDictionary<string, List<int>> dataSplits = new SortedDictionary<string, List<int>>();

        protected LLM llm;

        protected abstract void AddInternal(int key, float[] embedding);
        protected abstract void RemoveInternal(int key);
        protected abstract void ClearInternal();
        protected abstract void SaveInternal(ZipArchive archive);
        protected abstract void LoadInternal(ZipArchive archive);

        /// <summary>
        /// Sets the LLM for encoding the search entries
        /// </summary>
        /// <param name="llm"></param>
        public void SetLLM(LLM llm)
        {
            this.llm = llm;
            if (llmEmbedder != null) llmEmbedder.llm = llm;
        }

        /// <summary>
        /// Orders the entries in the searchList according to their similarity to the provided query.
        /// The entries and distances (dissimilarity) to the query are returned in decreasing order of similarity.
        /// </summary>
        /// <param name="queryString">query</param>
        /// <param name="searchList">entries to order based on similarity</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>Array of entries (`string[]`).</description></item>
        /// <item><description>Array of distances for each result (`float[]`).</description></item>
        /// </list>
        /// </returns>
        public async Task<(string[], float[])> SearchFromList(string query, string[] searchList)
        {
            float[] embedding = await Encode(query);
            float[][] embeddingsList = new float[searchList.Length][];
            for (int i = 0; i < searchList.Length; i++) embeddingsList[i] = await Encode(searchList[i]);

            float[] unsortedDistances = InverseDotProduct(embedding, embeddingsList);
            List<(string, float)> sortedLists = searchList.Zip(unsortedDistances, (first, second) => (first, second))
                .OrderBy(item => item.Item2)
                .ToList();

            string[] results = new string[sortedLists.Count];
            float[] distances = new float[sortedLists.Count];
            for (int i = 0; i < sortedLists.Count; i++)
            {
                results[i] = sortedLists[i].Item1;
                distances[i] = sortedLists[i].Item2;
            }
            return (results.ToArray(), distances.ToArray());
        }

        /// \cond HIDE
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

        public virtual async Task<float[]> Encode(string inputString)
        {
            return (await llmEmbedder.Embeddings(inputString)).ToArray();
        }

        public virtual async Task<List<int>> Tokenize(string query, Callback<List<int>> callback = null)
        {
            return await llmEmbedder.Tokenize(query, callback);
        }

        public async Task<string> Detokenize(List<int> tokens, Callback<string> callback = null)
        {
            return await llmEmbedder.Detokenize(tokens, callback);
        }

        public override string Get(int key)
        {
            if (data.TryGetValue(key, out string result)) return result;
            return null;
        }

        public override async Task<int> Add(string inputString, string group = "")
        {
            int key = nextKey++;
            AddInternal(key, await Encode(inputString));

            data[key] = inputString;
            if (!dataSplits.ContainsKey(group)) dataSplits[group] = new List<int>(){key};
            else dataSplits[group].Add(key);
            return key;
        }

        public override void Clear()
        {
            data.Clear();
            dataSplits.Clear();
            ClearInternal();
            nextKey = 0;
            nextIncrementalSearchKey = 0;
        }

        protected bool RemoveEntry(int key)
        {
            bool removed = data.Remove(key);
            if (removed) RemoveInternal(key);
            return removed;
        }

        public override void Remove(int key)
        {
            if (RemoveEntry(key))
            {
                foreach (var dataSplit in dataSplits.Values) dataSplit.Remove(key);
            }
        }

        public override int Remove(string inputString, string group = "")
        {
            if (!dataSplits.TryGetValue(group, out List<int> dataSplit)) return 0;
            List<int> removeIds = new List<int>();
            foreach (int key in dataSplit)
            {
                if (Get(key) == inputString) removeIds.Add(key);
            }
            foreach (int key in removeIds)
            {
                if (RemoveEntry(key)) dataSplit.Remove(key);
            }
            return removeIds.Count;
        }

        public override int Count()
        {
            return data.Count;
        }

        public override int Count(string group)
        {
            if (!dataSplits.TryGetValue(group, out List<int> dataSplit)) return 0;
            return dataSplit.Count;
        }

        public override async Task<int> IncrementalSearch(string queryString, string group = "")
        {
            return IncrementalSearch(await Encode(queryString), group);
        }

        public override void Save(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, data, GetSavePath("data"));
            ArchiveSaver.Save(archive, dataSplits, GetSavePath("dataSplits"));
            ArchiveSaver.Save(archive, nextKey, GetSavePath("nextKey"));
            ArchiveSaver.Save(archive, nextIncrementalSearchKey, GetSavePath("nextIncrementalSearchKey"));
            SaveInternal(archive);
        }

        public override void Load(ZipArchive archive)
        {
            data = ArchiveSaver.Load<SortedDictionary<int, string>>(archive, GetSavePath("data"));
            dataSplits = ArchiveSaver.Load<SortedDictionary<string, List<int>>>(archive, GetSavePath("dataSplits"));
            nextKey = ArchiveSaver.Load<int>(archive, GetSavePath("nextKey"));
            nextIncrementalSearchKey = ArchiveSaver.Load<int>(archive, GetSavePath("nextIncrementalSearchKey"));
            LoadInternal(archive);
        }

        public override void UpdateGameObjects()
        {
            if (this == null || llmEmbedder != null) return;
            llmEmbedder = ConstructComponent<LLMEmbedder>(typeof(LLMEmbedder), (previous, current) => current.llm = previous.llm);
        }

        public abstract int IncrementalSearch(float[] embedding, string group = "");
        /// \endcond
    }

    /// @ingroup rag
    /// <summary>
    /// Class implementing the search plugin template used e.g. in chunking
    /// </summary>
    public abstract class SearchPlugin : Searchable
    {
        protected SearchMethod search;

        /// <summary>
        /// Sets the search method of the plugin
        /// </summary>
        /// <param name="llm"></param>
        public void SetSearch(SearchMethod search)
        {
            this.search = search;
        }

        /// \cond HIDE
        protected abstract void SaveInternal(ZipArchive archive);
        protected abstract void LoadInternal(ZipArchive archive);

        public override void Save(ZipArchive archive)
        {
            search.Save(archive);
            SaveInternal(archive);
        }

        public override void Load(ZipArchive archive)
        {
            search.Load(archive);
            LoadInternal(archive);
        }

        /// \endcond
    }

    /// \cond HIDE
    public class ArchiveSaver
    {
        public delegate void ArchiveSaverCallback(ZipArchive archive);

        public static void Save(string filePath, ArchiveSaverCallback callback)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                callback(archive);
            }
        }

        public static void Load(string filePath, ArchiveSaverCallback callback)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                callback(archive);
            }
        }

        public static void Save(ZipArchive archive, object saveObject, string name)
        {
            ZipArchiveEntry mainEntry = archive.CreateEntry(name);
            using (Stream entryStream = mainEntry.Open())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(entryStream, saveObject);
            }
        }

        public static T Load<T>(ZipArchive archive, string name)
        {
            ZipArchiveEntry baseEntry = archive.GetEntry(name);
            if (baseEntry == null) throw new Exception($"No entry with name {name} was found");
            using (Stream entryStream = baseEntry.Open())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(entryStream);
            }
        }
    }
    /// \endcond
}
