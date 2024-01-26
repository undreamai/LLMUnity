using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;

public abstract class Search<T, E> where E : Embedder<T>
{
    protected E embedder;
    protected Dictionary<string, T> embeddings;

    public Search(E embedder)
    {
        embeddings = new Dictionary<string, T>();
        this.embedder = embedder;
    }

    public T Add(string inputString)
    {
        embeddings[inputString] = embedder.Encode(inputString);
        return embeddings[inputString];
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

    public abstract List<string> RetrieveSimilar(string queryString, int k);
}

public class ModelSearch : Search<TensorFloat, EmbeddingModel>
{
    public ModelSearch(EmbeddingModel embedder) : base(embedder) { }

    public new TensorFloat[] Add(string[] inputStrings)
    {
        TensorFloat[] inputEmbeddings = (TensorFloat[])embedder.Split(embedder.Encode(inputStrings));
        for (int i = 0; i < inputStrings.Length; i++) embeddings[inputStrings[i]] = inputEmbeddings[i];
        return inputEmbeddings;
    }

    public override List<string> RetrieveSimilar(string queryString, int k)
    {
        TensorFloat queryEmbedding = embedder.Encode(queryString);
        TensorFloat storeEmbedding = embedder.Concat(embeddings.Values.ToArray());
        float[] scores = embedder.SimilarityScores(queryEmbedding, storeEmbedding);
        return GetKNN(embeddings.Keys.ToArray(), scores, k);
    }
}
