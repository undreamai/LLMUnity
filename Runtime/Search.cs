using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace LLMUnity
{
    public class ArchiveSaver
    {
        public static void Save(object saveObject, string filePath, string name, FileMode mode = FileMode.Create)
        {
            using (FileStream stream = new FileStream(filePath, mode))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(saveObject, archive, name);
            }
        }

        public static void Save(object saveObject, ZipArchive archive, string name)
        {
            ZipArchiveEntry mainEntry = archive.CreateEntry(name);
            using (Stream entryStream = mainEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(saveObject.GetType());
                serializer.WriteObject(entryStream, saveObject);
            }
        }

        public static T Load<T>(string filePath, string name)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var loadMethod = typeof(T).GetMethod("Load", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new[] { typeof(ZipArchive), typeof(string) }, null);
                if (loadMethod != null) return (T)loadMethod.Invoke(null, new object[] { archive, name });
                return Load<T>(archive, name);
            }
        }

        public static T Load<T>(ZipArchive archive, string name)
        {
            ZipArchiveEntry baseEntry = archive.GetEntry(name);
            using (Stream entryStream = baseEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                T obj = (T)serializer.ReadObject(entryStream);
                return obj;
            }
        }
    }

    public interface ISearchable
    {
        public abstract string Get(int key);
        public abstract Task<int> Add(string inputString);
        public abstract int Remove(string inputString);
        public abstract void Remove(int key);
        public abstract int Count();
        public abstract void Clear();
        public abstract Task<string[]> Search(string queryString, int k);
        public abstract void Save(string filePath, string dirname = "");
        public abstract void Save(ZipArchive archive, string dirname = "");
    }

    [DataContract]
    public abstract class SearchMethod : LLMCaller, ISearchable
    {
        [DataMember] protected int nextKey = 0;
        [DataMember] protected SortedDictionary<int, string> data = new SortedDictionary<int, string>();
        [DataMember] protected int nextIncrementalSearchKey = 0;

        public abstract int IncrementalSearch(float[] embedding);
        public abstract (int[], bool) IncrementalFetchKeys(int fetchKey, int k);
        public abstract void IncrementalSearchComplete(int fetchKey);
        protected abstract int[] SearchInternal(float[] encoding, int k, out float[] distances);
        protected abstract void AddInternal(int key, float[] embedding);
        protected abstract void RemoveInternal(int key);
        protected abstract void ClearInternal();

        public virtual async Task<float[]> Encode(string inputString)
        {
            return (await Embeddings(inputString.Trim())).ToArray();
        }

        public virtual string Get(int key)
        {
            return data[key];
        }

        public virtual async Task<int> Add(string inputString)
        {
            int key = nextKey++;
            data[key] = inputString;
            AddInternal(key, await Encode(inputString));
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

        public virtual string[] Search(float[] encoding, int k)
        {
            int[] keys = SearchInternal(encoding, k, out float[] distances);
            string[] result = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++) result[i] = Get(keys[i]);
            return result;
        }

        public virtual async Task<string[]> Search(string queryString, int k)
        {
            return Search(await Encode(queryString), k);
        }

        public virtual async Task<int> IncrementalSearch(string queryString)
        {
            return IncrementalSearch(await Encode(queryString));
        }

        public virtual (string[], bool) IncrementalFetch(int fetchKey, int k)
        {
            int[] resultKeys;
            bool completed;
            (resultKeys, completed) = IncrementalFetchKeys(fetchKey, k);
            string[] results = new string[resultKeys.Length];
            for (int i = 0; i < resultKeys.Length; i++) results[i] = Get(resultKeys[i]);
            return (results, completed);
        }

        public static string GetSavePath(string dirname = "")
        {
            return Path.Combine(dirname, "search.json");
        }

        public virtual void Save(string filePath, string dirname = "")
        {
            ArchiveSaver.Save(this, filePath, GetSavePath(dirname));
        }

        public virtual void Save(ZipArchive archive, string dirname = "")
        {
            ArchiveSaver.Save(this, archive, GetSavePath(dirname));
        }

        public static T Load<T>(string filePath, string dirname = "") where T : SearchMethod
        {
            return ArchiveSaver.Load<T>(filePath, GetSavePath(dirname));
        }

        public static T Load<T>(ZipArchive archive, string dirname = "") where T : SearchMethod
        {
            return ArchiveSaver.Load<T>(archive, GetSavePath(dirname));
        }
    }
}
