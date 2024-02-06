using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using Cloud.Unum.USearch;


namespace LLMUnity
{
    public abstract class ModelSearchBase
    {
        protected EmbeddingModel embedder;
        protected Dictionary<string, TensorFloat> embeddings;

        public ModelSearchBase(EmbeddingModel embedder)
        {
            embeddings = new Dictionary<string, TensorFloat>();
            this.embedder = embedder;
        }

        public TensorFloat Encode(string inputString)
        {
            return embedder.Encode(inputString);
        }

        public TensorFloat[] Encode(List<string> inputStrings, int batchSize = 64)
        {
            List<TensorFloat> inputEmbeddings = new List<TensorFloat>();
            for (int i = 0; i < inputStrings.Count; i += batchSize)
            {
                int takeCount = Math.Min(batchSize, inputStrings.Count - i);
                List<string> batch = new List<string>(inputStrings.GetRange(i, takeCount));
                inputEmbeddings.AddRange((TensorFloat[])embedder.Split(embedder.Encode(batch)));
            }
            if (inputStrings.Count != inputEmbeddings.Count)
            {
                throw new Exception($"Number of computed embeddings ({inputEmbeddings.Count}) different than inputs ({inputStrings.Count})");
            }
            return inputEmbeddings.ToArray();
        }

        public virtual string[] Search(string queryString, int k)
        {
            TensorFloat queryEmbedding = embedder.Encode(queryString);
            TensorFloat storeEmbedding = embedder.Concat(embeddings.Values.ToArray());
            float[] scores = embedder.SimilarityScores(queryEmbedding, storeEmbedding);

            var sortedLists = embeddings.Keys.Zip(scores, (first, second) => new { First = first, Second = second })
                .OrderByDescending(item => item.Second)
                .ToList();
            List<string> results = new List<string>();
            int kmax = k == -1 ? sortedLists.Count : Math.Min(k, sortedLists.Count);
            for (int i = 0; i < kmax; i++)
            {
                results.Add(sortedLists[i].First);
            }
            return results.ToArray();
        }

        public virtual int Count()
        {
            return embeddings.Count;
        }
    }

    public class ModelSearch : ModelSearchBase
    {
        public ModelSearch(EmbeddingModel embedder) : base(embedder) {}

        protected void Insert(string inputString, TensorFloat encoding)
        {
            embeddings[inputString] = encoding;
        }

        public TensorFloat Add(string inputString)
        {
            TensorFloat embedding = Encode(inputString);
            Insert(inputString, embedding);
            return embedding;
        }

        public TensorFloat[] Add(string[] inputStrings, int batchSize = 64)
        {
            return Add(new List<string>(inputStrings), batchSize);
        }

        public TensorFloat[] Add(List<string> inputStrings, int batchSize = 64)
        {
            TensorFloat[] inputEmbeddings = Encode(inputStrings, batchSize);
            for (int i = 0; i < inputStrings.Count; i++)
            {
                Insert(inputStrings[i], inputEmbeddings[i]);
            }
            return inputEmbeddings.ToArray();
        }
    }

    public class ModelKeySearch : ModelSearchBase
    {
        protected Dictionary<string, int> valueToKey;

        public ModelKeySearch(EmbeddingModel embedder) : base(embedder)
        {
            valueToKey = new Dictionary<string, int>();
        }

        public virtual void Insert(int key, string value, TensorFloat encoding)
        {
            embeddings[value] = encoding;
            valueToKey[value] = key;
        }

        public virtual TensorFloat Add(int key, string inputString)
        {
            TensorFloat embedding = Encode(inputString);
            Insert(key, inputString, embedding);
            return embedding;
        }

        public virtual TensorFloat[] Add(int[] keys, string[] inputStrings, int batchSize = 64)
        {
            return Add(new List<int>(keys), new List<string>(inputStrings), batchSize);
        }

        public virtual TensorFloat[] Add(List<int> keys, List<string> inputStrings, int batchSize = 64)
        {
            TensorFloat[] inputEmbeddings = Encode(inputStrings, batchSize);
            for (int i = 0; i < inputStrings.Count; i++)
            {
                Insert(keys[i], inputStrings[i], inputEmbeddings[i]);
            }
            return inputEmbeddings.ToArray();
        }

        public virtual int[] SearchKey(string queryString, int k)
        {
            string[] results = Search(queryString, k);
            int[] keys = new int[results.Length];
            for (int i = 0; i < results.Length; i++)
            {
                keys[i] = valueToKey[results[i]];
            }
            return keys;
        }
    }

    public class ANNModelSearch : ModelKeySearch
    {
        USearchIndex index;
        protected Dictionary<int, string> keyToValue;

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
            keyToValue = new Dictionary<int, string>();
        }

        public override void Insert(int key, string value, TensorFloat encoding)
        {
            encoding.MakeReadable();
            index.Add((ulong)key, encoding.ToReadOnlyArray());
            keyToValue[key] = value;
        }

        public override string[] Search(string queryString, int k)
        {
            int[] results = SearchKey(queryString, k);
            string[] values = new string[results.Length];
            for (int i = 0; i < results.Length; i++)
            {
                values[i] = keyToValue[results[i]];
            }
            return values;
        }

        public override int[] SearchKey(string queryString, int k)
        {
            TensorFloat encoding = embedder.Encode(queryString);
            encoding.MakeReadable();
            index.Search(encoding.ToReadOnlyArray(), k, out ulong[] keys, out float[] distances);

            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                intKeys[i] = (int)keys[i];
            return intKeys;
        }

        public override int Count()
        {
            return (int)index.Size();
        }
    }
}
