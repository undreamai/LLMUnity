using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
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

    public interface ISearchable
    {
        public string Get(int key);
        public Task<int> Add(string inputString);
        public int Remove(string inputString);
        public void Remove(int key);
        public int Count();
        public void Clear();
        public Task<(string[], float[])> Search(string queryString, int k);
        public void Save(string filePath);
        public void Save(ZipArchive archive);
        public void Load(string filePath);
        public void Load(ZipArchive archive);
    }

    [DefaultExecutionOrder(-2)]
    public abstract class SearchMethod : LLMCaller, ISearchable
    {
        [HideInInspector, SerializeField] protected int nextKey = 0;
        [HideInInspector, SerializeField] protected int nextIncrementalSearchKey = 0;

        protected SortedDictionary<int, string> data = new SortedDictionary<int, string>();

        public abstract int IncrementalSearch(float[] embedding);
        public abstract (int[], float[], bool) IncrementalFetchKeys(int fetchKey, int k);
        public abstract void IncrementalSearchComplete(int fetchKey);
        protected abstract (int[], float[]) SearchInternal(float[] encoding, int k);
        protected abstract void AddInternal(int key, float[] embedding);
        protected abstract void RemoveInternal(int key);
        protected abstract void ClearInternal();
        protected abstract void SaveInternal(ZipArchive archive);
        protected abstract void LoadInternal(ZipArchive archive);

        public virtual async Task<float[]> Encode(string inputString)
        {
            return (await Embeddings(inputString)).ToArray();
        }

        public virtual string Get(int key)
        {
            return data[key];
        }

        public virtual async Task<int> Add(string inputString)
        {
            int key = nextKey++;
            AddInternal(key, await Encode(inputString));
            data[key] = inputString;
            return key;
        }

        public virtual void Remove(int key)
        {
            data.Remove(key);
            RemoveInternal(key);
        }

        public virtual void Clear()
        {
            data.Clear();
            ClearInternal();
            nextKey = 0;
            nextIncrementalSearchKey = 0;
        }

        public virtual int Remove(string inputString)
        {
            List<int> removeIds = new List<int>();
            foreach (var entry in data)
            {
                if (entry.Value == inputString) removeIds.Add(entry.Key);
            }
            foreach (int id in removeIds) Remove(id);
            return removeIds.Count;
        }

        public virtual int Count()
        {
            return data.Count;
        }

        public virtual (string[], float[]) Search(float[] encoding, int k)
        {
            (int[] keys, float[] distances) = SearchInternal(encoding, k);
            string[] result = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++) result[i] = Get(keys[i]);
            return (result, distances);
        }

        public virtual async Task<(string[], float[])> Search(string queryString, int k)
        {
            return Search(await Encode(queryString), k);
        }

        public virtual async Task<int> IncrementalSearch(string queryString)
        {
            return IncrementalSearch(await Encode(queryString));
        }

        public virtual (string[], float[], bool) IncrementalFetch(int fetchKey, int k)
        {
            (int[] resultKeys, float[] distances, bool completed) = IncrementalFetchKeys(fetchKey, k);
            string[] results = new string[resultKeys.Length];
            for (int i = 0; i < resultKeys.Length; i++) results[i] = Get(resultKeys[i]);
            return (results, distances, completed);
        }

        public virtual void Save(string filePath)
        {
            ArchiveSaver.Save(filePath, Save);
        }

        public virtual void Load(string filePath)
        {
            ArchiveSaver.Load(filePath, Load);
        }

        public virtual void Save(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, JsonUtility.ToJson(this), "Search_object");
            ArchiveSaver.Save(archive, data, "Search_data");
            SaveInternal(archive);
        }

        public virtual void Load(ZipArchive archive)
        {
            JsonUtility.FromJsonOverwrite(ArchiveSaver.Load<string>(archive, "Search_object"), this);
            data = ArchiveSaver.Load<SortedDictionary<int, string>>(archive, "Search_data");
            LoadInternal(archive);
        }
    }

    public abstract class SearchPlugin : MonoBehaviour, ISearchable
    {
        public SearchMethod search;

        public abstract string Get(int key);
        public abstract Task<int> Add(string inputString);
        public abstract int Remove(string inputString);
        public abstract void Remove(int key);
        public abstract int Count();
        public abstract void Clear();
        public abstract Task<(string[], float[])> Search(string queryString, int k);
        protected abstract void SaveInternal(ZipArchive archive);
        protected abstract void LoadInternal(ZipArchive archive);

        public virtual void Save(string filePath)
        {
            ArchiveSaver.Save(filePath, Save);
        }

        public virtual void Load(string filePath)
        {
            ArchiveSaver.Load(filePath, Load);
        }

        public virtual void Save(ZipArchive archive)
        {
            ArchiveSaver.Save(archive, JsonUtility.ToJson(this, true), "SearchPlugin_object");
            SaveInternal(archive);
        }

        public virtual void Load(ZipArchive archive)
        {
            JsonUtility.FromJsonOverwrite(ArchiveSaver.Load<string>(archive, "SearchPlugin_object"), this);
            LoadInternal(archive);
        }
    }
}
