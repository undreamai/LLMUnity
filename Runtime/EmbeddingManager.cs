using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum SimilarityType
{
    Cosine,
    L2
}

public class EmbeddingManager
{
    private List<(string, float[])> embeddings;
    private Func<string, Task<float[]>> embeddingFunction;
    private SimilarityType similarityType;

    public EmbeddingManager(Func<string, Task<float[]>> embeddingFunction, SimilarityType similarityType = SimilarityType.L2)
    {
        this.embeddings = new List<(string, float[])>();
        this.embeddingFunction = embeddingFunction;
        this.similarityType = similarityType;
    }

    public async Task AddString(string inputString)
    {
        embeddings.Add((inputString, await embeddingFunction(inputString)));
    }

    public async Task<List<string>> RetrieveMostSimilarStrings(string queryString, int numResults)
    {
        float[] queryEmbedding = await embeddingFunction(queryString);
        List<Tuple<(string, float[]), float>> similarities = new List<Tuple<(string, float[]), float>>();

        for (int i = 0; i < embeddings.Count; i++)
        {
            float similarity = CalculateSimilarity(queryEmbedding, embeddings[i].Item2);
            similarities.Add(new Tuple<(string, float[]), float>(embeddings[i], similarity));
        }

        similarities.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        List<string> results = new List<string>();
        for (int i = 0; i < Math.Min(numResults, similarities.Count); i++)
        {
            results.Add(similarities[i].Item1.Item1);
        }

        return results;
    }

    private float CalculateSimilarity(float[] vectorA, float[] vectorB)
    {
        if (similarityType == SimilarityType.Cosine)
        {
            return CalculateCosineSimilarity(vectorA, vectorB);
        }
        else if (similarityType == SimilarityType.L2)
        {
            return CalculateL2Distance(vectorA, vectorB);
        }
        else
        {
            throw new NotSupportedException("Unsupported similarity type");
        }
    }

    private float CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        // Assuming vectors are of the same length
        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        float similarity = dotProduct / (float)(Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        return similarity;
    }

    private float CalculateL2Distance(float[] vectorA, float[] vectorB)
    {
        float sumOfSquares = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            float diff = vectorA[i] - vectorB[i];
            sumOfSquares += diff * diff;
        }

        return Mathf.Sqrt(sumOfSquares);
    }

    public void SaveEmbeddingsToFile(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            for (int i = 0; i < embeddings.Count; i++)
            {
                writer.WriteLine($"{embeddings[i].Item1}|{string.Join(",", embeddings[i].Item2)}");
            }
        }
    }

    public void LoadEmbeddingsFromFile(string filePath)
    {
        embeddings.Clear();

        using (StreamReader reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] parts = line.Split('|');
                embeddings.Add((parts[0], parts[1].Split(',').Select(float.Parse).ToArray()));
            }
        }
    }
}
