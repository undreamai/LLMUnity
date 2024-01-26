using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;

public class EmbeddingManager
{
    private EmbeddingModel embeddingModel;
    private Dictionary<string, TensorFloat> embeddings;

    public EmbeddingManager(EmbeddingModel embeddingModel)
    {
        embeddings = new Dictionary<string, TensorFloat>();
        this.embeddingModel = embeddingModel;
    }

    public TensorFloat Add(string inputString)
    {
        embeddings[inputString] = embeddingModel.Encode(inputString);
        return embeddings[inputString];
    }

    public TensorFloat[] Add(string[] inputStrings)
    {
        TensorFloat[] inputEmbeddings = (TensorFloat[])embeddingModel.Split(embeddingModel.Encode(inputStrings));
        for (int i = 0; i < inputStrings.Length; i++) embeddings[inputStrings[i]] = inputEmbeddings[i];
        return inputEmbeddings;
    }

    public List<string> RetrieveSimilar(string queryString, int numResults)
    {
        TensorFloat queryEmbedding = embeddingModel.Encode(queryString);
        TensorFloat storeEmbedding = embeddingModel.Concat(embeddings.Values.ToArray());
        float[] scores = embeddingModel.SimilarityScores(queryEmbedding, storeEmbedding);
        var sortedLists = embeddings.Keys.Zip(scores, (first, second) => new { First = first, Second = second })
            .OrderByDescending(item => item.Second)
            .ToList();
        List<string> results = new List<string>();
        for (int i = 0; i < Math.Min(numResults, sortedLists.Count); i++)
        {
            results.Add(sortedLists[i].First);
        }
        return results;
    }
}
