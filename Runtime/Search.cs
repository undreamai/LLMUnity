using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using Cloud.Unum.USearch;


namespace LLMUnity
{
    public abstract class Search<T, E> where E : Embedder<T>
    {
        protected E embedder;
        protected Dictionary<string, T> embeddings;

        public abstract void Insert(string inputString, T encoding);
        public abstract List<string> RetrieveSimilar(string queryString, int k);

        public Search(E embedder)
        {
            embeddings = new Dictionary<string, T>();
            this.embedder = embedder;
        }

        public T Add(string inputString)
        {
            T embedding = embedder.Encode(inputString);
            Insert(inputString, embedding);
            return embedding;
        }

        public T[] Add(string[] inputStrings)
        {
            T[] inputEmbeddings = new T[inputStrings.Length];
            for (int i = 0; i < inputStrings.Length; i++)
            {
                inputEmbeddings[i] = Add(inputStrings[i]);
            }
            return inputEmbeddings;
        }

        public List<string> GetKNN(string[] inputStrings, float[] scores, int k = -1)
        {
            var sortedLists = inputStrings.Zip(scores, (first, second) => new { First = first, Second = second })
                .OrderByDescending(item => item.Second)
                .ToList();
            List<string> results = new List<string>();
            int kmax = k == -1 ? sortedLists.Count : Math.Min(k, sortedLists.Count);
            for (int i = 0; i < kmax; i++)
            {
                results.Add(sortedLists[i].First);
            }
            return results;
        }

        public int Count()
        {
            return embeddings.Count;
        }
    }

    public class ModelSearch : Search<TensorFloat, EmbeddingModel>
    {
        public ModelSearch(EmbeddingModel embedder) : base(embedder) {}

        public override void Insert(string inputString, TensorFloat encoding)
        {
            embeddings[inputString] = encoding;
        }

        public TensorFloat[] Add(string[] inputStrings, int batchSize = 64)
        {
            return Add(new List<string>(inputStrings), batchSize);
        }

        public TensorFloat[] Add(List<string> inputStrings, int batchSize = 64)
        {
            List<TensorFloat> inputEmbeddings = new List<TensorFloat>();
            for (int i = 0; i < inputStrings.Count; i += batchSize)
            {
                int takeCount = Math.Min(batchSize, inputStrings.Count - i);
                List<string> batch = new List<string>(inputStrings.GetRange(i, takeCount));
                inputEmbeddings.AddRange((TensorFloat[])embedder.Split(embedder.Encode(batch)));
                Console.WriteLine(takeCount);
            }

            if (inputStrings.Count != inputEmbeddings.Count)
            {
                throw new Exception($"Number of computed embeddings ({inputEmbeddings.Count}) different than inputs ({inputStrings.Count})");
            }
            for (int i = 0; i < inputStrings.Count; i++)
            {
                Insert(inputStrings[i], inputEmbeddings[i]);
            }
            return inputEmbeddings.ToArray();
        }

        public override List<string> RetrieveSimilar(string queryString, int k)
        {
            TensorFloat queryEmbedding = embedder.Encode(queryString);
            TensorFloat storeEmbedding = embedder.Concat(embeddings.Values.ToArray());
            float[] scores = embedder.SimilarityScores(queryEmbedding, storeEmbedding);
            return GetKNN(embeddings.Keys.ToArray(), scores, k);
        }
    }

    public class ANNModelSearch : ModelSearch
    {
        USearchIndex index;
        Dictionary<ulong, string> USearchToText;
        ulong nextKey;
        object insertLock = new object();

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
            USearchToText = new Dictionary<ulong, string>();
            nextKey = 0;
        }

        public override void Insert(string inputString, TensorFloat encoding)
        {
            if (USearchToText.ContainsValue(inputString)) return;
            encoding.MakeReadable();
            lock (insertLock)
            {
                ulong key = nextKey++;
                USearchToText[key] = inputString;
                index.Add(key, encoding.ToReadOnlyArray());
            }
        }

        public override List<string> RetrieveSimilar(string queryString, int k)
        {
            TensorFloat encoding = embedder.Encode(queryString);
            encoding.MakeReadable();
            index.Search(encoding.ToReadOnlyArray(), k, out ulong[] keys, out float[] distances);
            List<string> result = new List<string>();
            for (int i = 0; i < keys.Length; i++)
            {
                result.Add(USearchToText[keys[i]]);
            }
            return result;
        }

        public new int Count()
        {
            return (int)index.Size();
        }
    }
}
