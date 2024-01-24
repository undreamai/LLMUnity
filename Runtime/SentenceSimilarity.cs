using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEngine;
using Unity.Sentis;
using Unity.Sentis.Layers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SentenceSimilarityUtils;
using UnityEditor;


public class SentenceSimilarity : MonoBehaviour
{
    ModelAsset modelAsset;
    public Model runtimeModel;
    public IWorker worker;
    public ITensorAllocator allocator;
    public Ops ops;


    /// <summary>
    /// Load the model on awake
    /// </summary>
    public void Awake()
    {
        // Load the ONNX model
        // modelAsset = Resources.Load("StreamingAssets/model.onnx") as ModelAsset;

        // runtimeModel = ModelLoader.Load("Assets/Models/model.onnx");
        runtimeModel = ModelLoader.Load("Assets/StreamingAssets/model.sentis");

        // Create an engine and set the backend as GPU //GPUCompute
        worker = WorkerFactory.CreateWorker(BackendType.CPU, runtimeModel);

        // Create an allocator.
        allocator = new TensorCachingAllocator();

        // Create an operator
        ops = WorkerFactory.CreateOps(BackendType.GPUCompute, allocator);
    }

    void OnDisable()
    {
        // Tell the GPU we're finished with the memory the engine used
        worker.Dispose();
    }

    /// <summary>
    /// Encode the input
    /// </summary>
    /// <param name="input"></param>
    /// <param name="worker"></param>
    /// <param name="ops"></param>
    /// <returns></returns>
    public TensorFloat Encode(List<string> input, IWorker worker, Ops ops)
    {
        // Step 1: Tokenize the sentences
        Dictionary<string, Tensor> inputSentencesTokensTensor = SentenceSimilarityUtils_.TokenizeInput(input);

        // Step 2: Compute embedding and get the output
        worker.Execute(inputSentencesTokensTensor);

        // Step 3: Get the output from the neural network
        TensorFloat outputTensor = worker.PeekOutput("last_hidden_state") as TensorFloat;

        // Step 4: Perform pooling
        TensorFloat MeanPooledTensor = SentenceSimilarityUtils_.MeanPooling(inputSentencesTokensTensor["attention_mask"], outputTensor, ops);

        // Step 5: Normalize the results
        TensorFloat NormedTensor = SentenceSimilarityUtils_.L2Norm(MeanPooledTensor, ops);

        return NormedTensor;
    }

    /// <summary>
    /// We calculate the similarity scores between the input sequence (what the user typed) and the comparison
    /// sequences (the robot action list)
    /// This similarity score is simply the cosine similarity. It is calculated as the cosine of the angle between two vectors.
    /// It is particularly useful when your texts are not the same length
    /// </summary>
    /// <param name="InputSequence"></param>
    /// <param name="ComparisonSequences"></param>
    /// <returns></returns>
    public TensorFloat SentenceSimilarityScores(TensorFloat InputSequence, TensorFloat ComparisonSequences)
    {
        TensorFloat SentenceSimilarityScores_ = ops.MatMul2D(InputSequence, ComparisonSequences, false, true);
        return SentenceSimilarityScores_;
    }

    /// <summary>
    /// Get the most similar action and its index given the player input
    /// </summary>
    /// <param name="inputSentence"></param>
    /// <param name="comparisonSentences"></param>
    /// <returns></returns>
    public Tuple<int, float> RankSimilarityScores(string inputSentence, string[] comparisonSentences)
    {
        // Step 1: Transform string and string[] to lists
        List<string> InputSentences = new List<string>();
        List<string> ComparisonSentences = new List<string>();

        InputSentences.Add(inputSentence);
        ComparisonSentences = comparisonSentences.ToList();

        // Step 2: Encode the input sentences and comparison sentences
        TensorFloat NormEmbedSentences = Encode(InputSentences, worker, ops);
        TensorFloat NormEmbedComparisonSentences = Encode(ComparisonSentences, worker, ops);

        // Calculate the similarity score of the player input with each action
        TensorFloat scores = SentenceSimilarityScores(NormEmbedSentences, NormEmbedComparisonSentences);
        scores.MakeReadable(); // Be able to read this tensor

        // Helper to return only best score and index
        TensorInt scoreIndex = ops.ArgMax(scores, 1, true);
        scoreIndex.MakeReadable();

        int scoreIndexInt = scoreIndex[0];
        scores.MakeReadable();
        float score = scores[scoreIndexInt];

        // Return the similarity score and the action index
        return Tuple.Create(scoreIndexInt, score);
    }
}
