using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using Cloud.Unum.USearch;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Reflection;


namespace LLMUnity
{
    [DataContract]
    public class ModelSearchBase
    {
        protected EmbeddingModel embedder;
        [DataMember]
        protected Dictionary<string, float[]> embeddings;

        public ModelSearchBase(EmbeddingModel embedder)
        {
            embeddings = new Dictionary<string, float[]>();
            this.embedder = embedder;
        }

        public void SetEmbedder(EmbeddingModel embedder)
        {
            this.embedder = embedder;
        }

        public float[] Encode(string inputString)
        {
            TensorFloat encoding = embedder.Encode(inputString);
            encoding.MakeReadable();
            return encoding.ToReadOnlyArray();
        }

        public float[][] Encode(List<string> inputStrings, int batchSize = 64)
        {
            List<float[]> inputEmbeddings = new List<float[]>();
            for (int i = 0; i < inputStrings.Count; i += batchSize)
            {
                int takeCount = Math.Min(batchSize, inputStrings.Count - i);
                List<string> batch = new List<string>(inputStrings.GetRange(i, takeCount));
                foreach (TensorFloat tensor in embedder.Split(embedder.Encode(batch)))
                {
                    tensor.MakeReadable();
                    inputEmbeddings.Add(tensor.ToReadOnlyArray());
                }
            }
            if (inputStrings.Count != inputEmbeddings.Count)
            {
                throw new Exception($"Number of computed embeddings ({inputEmbeddings.Count}) different than inputs ({inputStrings.Count})");
            }
            return inputEmbeddings.ToArray();
        }

        public virtual string[] Search(float[] encoding, int k)
        {
            return Search(encoding, k, out float[] distances);
        }

        public virtual string[] Search(string queryString, int k)
        {
            return Search(queryString, k, out float[] distances);
        }

        public static float DotProduct(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vector lengths must be equal for dot product calculation");
            }
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

        public virtual string[] Search(float[] encoding, int k, out float[] distances)
        {
            float[] unsortedDistances = InverseDotProduct(encoding, embeddings.Values.ToArray());

            var sortedLists = embeddings.Keys.Zip(unsortedDistances, (first, second) => new { First = first, Second = second })
                .OrderBy(item => item.Second)
                .ToList();
            int kmax = k == -1 ? sortedLists.Count : Math.Min(k, sortedLists.Count);
            string[] results = new string[kmax];
            distances = new float[kmax];
            for (int i = 0; i < kmax; i++)
            {
                results[i] = sortedLists[i].First;
                distances[i] = sortedLists[i].Second;
            }
            return results;
        }

        public virtual string[] Search(string queryString, int k, out float[] distances)
        {
            return Search(Encode(queryString), k, out distances);
        }

        public virtual int Count()
        {
            return embeddings.Count;
        }

        public virtual void Save(string filePath, string dirname)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(archive, dirname);
            }
        }

        public static string GetSearchTypePath(string dirname)
        {
            return Path.Combine(dirname, "SearchType.txt");
        }

        public static string GetSearchPath(string dirname)
        {
            return Path.Combine(dirname, "Search.json");
        }

        public virtual void Save(ZipArchive archive, string dirname)
        {
            ZipArchiveEntry typeEntry = archive.CreateEntry(GetSearchTypePath(dirname));
            using (StreamWriter writer = new StreamWriter(typeEntry.Open()))
            {
                writer.Write(GetType().FullName);
            }
            Saver.Save(this, archive, GetSearchPath(dirname));
            embedder.Save(archive, dirname);
        }

        public static ModelSearchBase CastLoad(string filePath, string dirname)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return CastLoad(archive, dirname);
            }
        }

        public static ModelSearchBase CastLoad(ZipArchive archive, string dirname)
        {
            ZipArchiveEntry typeEntry = archive.GetEntry(GetSearchTypePath(dirname));
            Type modelSearchType;
            using (StreamReader reader = new StreamReader(typeEntry.Open()))
            {
                modelSearchType = Type.GetType(reader.ReadLine());
            }
            MethodInfo methodInfo = modelSearchType.GetMethod("Load", new Type[] { typeof(ZipArchive), typeof(string) });
            return (ModelSearchBase)Convert.ChangeType(methodInfo.Invoke(null, new object[] { archive, dirname }), modelSearchType);
        }

        public static T Load<T>(string filePath, string dirname) where T : ModelSearchBase
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load<T>(archive, dirname);
            }
        }

        public static T Load<T>(ZipArchive archive, string dirname) where T : ModelSearchBase
        {
            T search = Saver.Load<T>(archive, GetSearchPath(dirname));
            EmbeddingModel embedder = EmbeddingModel.Load(archive, dirname);
            search.SetEmbedder(embedder);
            return search;
        }
    }

    [DataContract]
    public class ModelSearch : ModelSearchBase
    {
        public ModelSearch(EmbeddingModel embedder) : base(embedder) {}

        protected void Insert(string inputString, float[] encoding)
        {
            embeddings[inputString] = encoding;
        }

        public float[] Add(string inputString)
        {
            float[] embedding = Encode(inputString);
            Insert(inputString, embedding);
            return embedding;
        }

        public float[][] Add(string[] inputStrings, int batchSize = 64)
        {
            return Add(new List<string>(inputStrings), batchSize);
        }

        public float[][] Add(List<string> inputStrings, int batchSize = 64)
        {
            float[][] inputEmbeddings = Encode(inputStrings, batchSize);
            for (int i = 0; i < inputStrings.Count; i++)
            {
                Insert(inputStrings[i], inputEmbeddings[i]);
            }
            return inputEmbeddings;
        }

        public bool Remove(string inputString)
        {
            return embeddings.Remove(inputString);
        }

        public static ModelSearch Load(string filePath, string dirname)
        {
            return Load<ModelSearch>(filePath, dirname);
        }

        public static ModelSearch Load(ZipArchive archive, string dirname)
        {
            return Load<ModelSearch>(archive, dirname);
        }
    }


    [DataContract]
    public class ANNModelSearch : ModelSearchBase
    {
        USearchIndex index;
        [DataMember]
        protected SortedDictionary<int, string> keyToValue;

        public ANNModelSearch(EmbeddingModel embedder) : this(embedder, MetricKind.Cos, 32, 40, 16) {}

        public ANNModelSearch(
            EmbeddingModel embedder,
            MetricKind metricKind = MetricKind.Cos,
            ulong connectivity = 32,
            ulong expansionAdd = 40,
            ulong expansionSearch = 16,
            bool multi = false
        ) : this(embedder, new USearchIndex((ulong)embedder.Dimensions, metricKind, connectivity, expansionAdd, expansionSearch, multi)) {}

        public ANNModelSearch(
            EmbeddingModel embedder,
            USearchIndex index
        ) : base(embedder)
        {
            this.index = index;
            keyToValue = new SortedDictionary<int, string>();
        }

        public void SetIndex(USearchIndex index)
        {
            this.index = index;
        }

        public void Insert(int key, string value, float[] encoding)
        {
            index.Add((ulong)key, encoding);
            keyToValue[key] = value;
        }

        public virtual float[] Add(int key, string inputString)
        {
            float[] embedding = Encode(inputString);
            Insert(key, inputString, embedding);
            return embedding;
        }

        public virtual float[][] Add(int[] keys, string[] inputStrings, int batchSize = 64)
        {
            return Add(new List<int>(keys), new List<string>(inputStrings), batchSize);
        }

        public virtual float[][] Add(List<int> keys, List<string> inputStrings, int batchSize = 64)
        {
            float[][] inputEmbeddings = Encode(inputStrings, batchSize);
            for (int i = 0; i < inputStrings.Count; i++)
            {
                Insert(keys[i], inputStrings[i], inputEmbeddings[i]);
            }
            return inputEmbeddings.ToArray();
        }

        public bool Remove(int key)
        {
            return index.Remove((ulong)key) > 0 && keyToValue.Remove(key);
        }

        public override string[] Search(float[] encoding, int k)
        {
            return Search(encoding, k, out float[] distances);
        }

        public override string[] Search(string queryString, int k)
        {
            return Search(queryString, k, out float[] distances);
        }

        public override string[] Search(float[] encoding, int k, out float[] distances)
        {
            int[] results = SearchKey(encoding, k, out distances);
            string[] values = new string[results.Length];
            for (int i = 0; i < results.Length; i++)
            {
                values[i] = keyToValue[results[i]];
            }
            return values;
        }

        public override string[] Search(string queryString, int k, out float[] distances)
        {
            return Search(Encode(queryString), k, out distances);
        }

        public int[] SearchKey(float[] encoding, int k)
        {
            return SearchKey(encoding, k, out float[] distances);
        }

        public int[] SearchKey(string queryString, int k)
        {
            return SearchKey(queryString, k, out float[] distances);
        }

        public int[] SearchKey(float[] encoding, int k, out float[] distances)
        {
            index.Search(encoding, k, out ulong[] keys, out distances);
            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                intKeys[i] = (int)keys[i];
            return intKeys;
        }

        public int[] SearchKey(string queryString, int k, out float[] distances)
        {
            return SearchKey(Encode(queryString), k, out distances);
        }

        public override int Count()
        {
            return (int)index.Size();
        }

        public static string GetIndexPath(string dirname)
        {
            return Path.Combine(dirname, "USearch");
        }

        public override void Save(ZipArchive archive, string dirname)
        {
            base.Save(archive, dirname);
            index.Save(archive, GetIndexPath(dirname));
        }

        public static ANNModelSearch Load(string filePath, string dirname)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load(archive, dirname);
            }
        }

        public static ANNModelSearch Load(ZipArchive archive, string dirname)
        {
            ANNModelSearch search = Load<ANNModelSearch>(archive, dirname);
            USearchIndex index = new USearchIndex(archive, GetIndexPath(dirname));
            search.SetIndex(index);
            return search;
        }
    }
}
