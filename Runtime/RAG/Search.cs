using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LLMUnity
{
    [DefaultExecutionOrder(-2)]
    public abstract class Searchable : MonoBehaviour
    {
        public abstract string Get(int key);
        public abstract Task<int> Add(string inputString, string splitId = "");
        public abstract int Remove(string inputString, string splitId = "");
        public abstract void Remove(int key);
        public abstract int Count(string splitId);
        public abstract int Count();
        public abstract void Clear();
        public abstract Task<int> IncrementalSearch(string queryString, string splitId = "");
        public abstract (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k);
        public abstract void IncrementalSearchComplete(int fetchKey);
        public abstract void Save(ZipArchive archive);
        public abstract void Load(ZipArchive archive);

        public virtual void Save(string filePath)
        {
            ArchiveSaver.Save(filePath, Save);
        }

        public virtual void Load(string filePath)
        {
            ArchiveSaver.Load(filePath, Load);
        }

        public async Task<(string[], float[])> Search(string queryString, int k, string splitId = "")
        {
            int fetchKey = await IncrementalSearch(queryString, splitId);
            (string[] phrases, float[] distances, bool completed) = IncrementalFetch(fetchKey, k);
            if (!completed) IncrementalSearchComplete(fetchKey);
            return (phrases, distances);
        }

        public virtual (string[], float[], bool) IncrementalFetch(int fetchKey, int k)
        {
            (int[] resultKeys, float[] distances, bool completed) = IncrementalFetchKeys(fetchKey, k);
            string[] results = new string[resultKeys.Length];
            for (int i = 0; i < resultKeys.Length; i++) results[i] = Get(resultKeys[i]);
            return (results, distances, completed);
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
    }

    public abstract class SearchMethod : Searchable
    {
        public LLMEmbedder llmEmbedder;

        protected int nextKey = 0;
        protected int nextIncrementalSearchKey = 0;
        protected SortedDictionary<int, string> data = new SortedDictionary<int, string>();
        protected SortedDictionary<string, List<int>> dataSplits = new SortedDictionary<string, List<int>>();

        private LLM llm;

        protected abstract void AddInternal(int key, float[] embedding);
        protected abstract void RemoveInternal(int key);
        protected abstract void ClearInternal();
        public abstract int IncrementalSearch(float[] embedding, string splitId = "");
        protected abstract void SaveInternal(ZipArchive archive);
        protected abstract void LoadInternal(ZipArchive archive);

        public void SetLLM(LLM llm)
        {
            this.llm = llm;
            if (llmEmbedder != null) llmEmbedder.llm = llm;
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

        public override async Task<int> Add(string inputString, string splitId = "")
        {
            int key = nextKey++;
            AddInternal(key, await Encode(inputString));

            data[key] = inputString;
            if (!dataSplits.ContainsKey(splitId)) dataSplits[splitId] = new List<int>(){key};
            else dataSplits[splitId].Add(key);
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

        public override int Remove(string inputString, string splitId = "")
        {
            if (!dataSplits.TryGetValue(splitId, out List<int> dataSplit)) return 0;
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

        public override int Count(string splitId)
        {
            if (!dataSplits.TryGetValue(splitId, out List<int> dataSplit)) return 0;
            return dataSplit.Count;
        }

        public override async Task<int> IncrementalSearch(string queryString, string splitId = "")
        {
            return IncrementalSearch(await Encode(queryString), splitId);
        }

        public override void Save(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, data, "Search_data");
            ArchiveSaver.Save(archive, dataSplits, "Search_dataSplits");
            ArchiveSaver.Save(archive, nextKey, "Search_nextKey");
            ArchiveSaver.Save(archive, nextIncrementalSearchKey, "Search_nextIncrementalSearchKey");
            SaveInternal(archive);
        }

        public override void Load(ZipArchive archive)
        {
            data = ArchiveSaver.Load<SortedDictionary<int, string>>(archive, "Search_data");
            dataSplits = ArchiveSaver.Load<SortedDictionary<string, List<int>>>(archive, "Search_dataSplits");
            nextKey = ArchiveSaver.Load<int>(archive, "Search_nextKey");
            nextIncrementalSearchKey = ArchiveSaver.Load<int>(archive, "Search_nextIncrementalSearchKey");
            LoadInternal(archive);
        }

        public override void UpdateGameObjects()
        {
            if (this == null || llmEmbedder != null) return;
            llmEmbedder = ConstructComponent<LLMEmbedder>(typeof(LLMEmbedder), (previous, current) => current.llm = previous.llm);
        }
    }

    public abstract class SearchPlugin : Searchable
    {
        public SearchMethod search;

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
    }

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
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
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
            using (Stream entryStream = baseEntry.Open())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                T obj = (T)formatter.Deserialize(entryStream);
                return obj;
            }
        }
    }
}
