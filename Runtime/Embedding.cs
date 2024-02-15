using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Unity.Sentis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEditor;
using HuggingFace.SharpTransformers.Normalizers;
using HuggingFace.SharpTransformers.PreTokenizers;
using HuggingFace.SharpTransformers.Tokenizers;
using HuggingFace.SharpTransformers.PostProcessors;
using System.IO;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace LLMUnity
{
    [DataContract]
    public class EmbeddingModel
    {
        [DataMember]
        public string ModelPath { get; protected set; }
        [DataMember]
        public string TokenizerPath { get; protected set; }
        [DataMember]
        public BackendType Backend { get; protected set; }
        [DataMember]
        public string OutputLayerName { get; protected set; }
        [DataMember]
        public bool UseMeanPooling { get; protected set; }
        [DataMember]
        public int Dimensions { get; protected set; }

        Model runtimeModel;
        IWorker worker;
        ITensorAllocator allocator;
        Ops ops;
        JObject tokenizerJsonData;
        BertNormalizer bertNorm;
        BertPreTokenizer bertPreTok;
        WordPieceTokenizer wordPieceTokenizer;
        TemplateProcessing templateProcessing;
        int? hashcode = null;

        public EmbeddingModel(string modelPath, string tokenizerPath, BackendType backend = BackendType.CPU, string outputLayerName = "last_hidden_state", bool useMeanPooling = true, int dimensions = 384)
        {
            ModelPath = modelPath;
            TokenizerPath = tokenizerPath;
            Backend = backend;
            OutputLayerName = outputLayerName;
            UseMeanPooling = useMeanPooling;
            Dimensions = dimensions;

            runtimeModel = ModelLoader.Load(modelPath);
            string tokenizerJsonContent = File.ReadAllText(tokenizerPath);
            tokenizerJsonData = JsonConvert.DeserializeObject<JObject>(tokenizerJsonContent);

            worker = WorkerFactory.CreateWorker(backend, runtimeModel);
            allocator = new TensorCachingAllocator();
            ops = WorkerFactory.CreateOps(backend, allocator);

            bertNorm = new BertNormalizer(JObject.FromObject(tokenizerJsonData["normalizer"]));
            bertPreTok = new BertPreTokenizer(JObject.FromObject(tokenizerJsonData["pre_tokenizer"]));
            wordPieceTokenizer = new WordPieceTokenizer(JObject.FromObject(tokenizerJsonData["model"]));
            templateProcessing = new TemplateProcessing(JObject.FromObject(tokenizerJsonData["post_processor"]));
        }

        public override int GetHashCode()
        {
            if (hashcode == null)
            {
                string hash = "";
                hash += Saver.ComputeMD5(ModelPath);
                hash += Saver.ComputeMD5(TokenizerPath);
                // hash += Backend;
                hash += OutputLayerName;
                hash += UseMeanPooling;
                hash += Dimensions;
                hashcode = hash.GetHashCode();
            }
            return hashcode.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return GetHashCode() == ((EmbeddingModel)obj).GetHashCode();
        }

        public static string GetHashCodePath(string dirname)
        {
            return Path.Combine(dirname, "EmbeddingModelHashCode.txt");
        }

        public virtual void SaveHashCode(string path, string dirname = "")
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                SaveHashCode(archive, dirname);
            }
        }

        public virtual void SaveHashCode(ZipArchive archive, string dirname = "")
        {
            ZipArchiveEntry modelEntry = archive.CreateEntry(GetHashCodePath(dirname));
            using (StreamWriter writer = new StreamWriter(modelEntry.Open()))
            {
                writer.WriteLine(GetHashCode());
            }
        }

        public static int LoadHashCode(string path, string dirname = "")
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return LoadHashCode(archive, dirname);
            }
        }

        public static int LoadHashCode(ZipArchive archive, string dirname = "")
        {
            ZipArchiveEntry modelEntry = archive.GetEntry(GetHashCodePath(dirname));
            using (StreamReader reader = new StreamReader(modelEntry.Open()))
            {
                return int.Parse(reader.ReadLine());
            }
        }

        public void Destroy()
        {
            allocator.Dispose();
            ops.Dispose();
            worker.Dispose();
        }

        public Tensor Concat(Tensor[] tensors, int axis = 0)
        {
            return ops.Concat(tensors, axis);
        }

        public TensorFloat Concat(TensorFloat[] tensors, int axis = 0)
        {
            return (TensorFloat)ops.Concat(tensors, axis);
        }

        public Tensor[] Split(Tensor tensor, int axis = 0)
        {
            if (tensor.shape.rank != 2) throw new Exception("Tensor does not have a rank of 2.");
            TensorFloat[] tensors = new TensorFloat[tensor.shape[0]];
            for (int i = 0; i < tensor.shape[axis]; i++)
                tensors[i] = (TensorFloat)ops.Split(tensor, axis, i, i + 1);
            return tensors;
        }

        public TensorFloat Encode(string input)
        {
            return Encode(new List<string> { input });
        }

        public TensorFloat Encode(string[] input)
        {
            return Encode(new List<string>(input));
        }

        public TensorFloat Encode(List<string> input)
        {
            // Step 1: Tokenize the sentences
            Dictionary<string, Tensor> inputSentencesTokensTensor = TokenizeInput(input);
            // Step 2: Compute embedding and get the output
            worker.Execute(inputSentencesTokensTensor);
            // Step 3: Get the output from the neural network
            TensorFloat outputTensor = worker.PeekOutput(OutputLayerName) as TensorFloat;
            // Step 4: Perform pooling
            TensorFloat MeanPooledTensor = outputTensor;
            if (UseMeanPooling)
            {
                MeanPooledTensor = MeanPooling(inputSentencesTokensTensor["attention_mask"], outputTensor, ops);
            }
            // Step 5: Normalize the results
            TensorFloat NormedTensor = L2Norm(MeanPooledTensor, ops);
            // Cleanup
            foreach (var val in inputSentencesTokensTensor.Values) val.Dispose();
            inputSentencesTokensTensor.Clear();
            return NormedTensor;
        }

        public float[] SimilarityScores(TensorFloat InputSequence, TensorFloat ComparisonSequences)
        {
            TensorFloat scoresTensor = ops.MatMul2D(InputSequence, ComparisonSequences, false, true);
            scoresTensor.MakeReadable();
            int cols = ComparisonSequences.shape[0];
            float[] scores = new float[cols];
            for (int j = 0; j < cols; j++)
                scores[j] = scoresTensor[0, j];
            return scores;
        }

        public float[][] SimilarityScoresMultiple(TensorFloat InputSequence, TensorFloat ComparisonSequences)
        {
            TensorFloat scoresTensor = ops.MatMul2D(InputSequence, ComparisonSequences, false, true);
            scoresTensor.MakeReadable();
            int rows = InputSequence.shape[0];
            int cols = ComparisonSequences.shape[0];
            float[][] scores = new float[rows][];
            for (int i = 0; i < rows; i++)
            {
                scores[i] = new float[cols];
                for (int j = 0; j < cols; j++)
                    scores[i][j] = scoresTensor[i, j];
            }
            return scores;
        }

        public float[] SimilarityDistances(TensorFloat InputSequence, TensorFloat ComparisonSequences)
        {
            float[] scores = SimilarityScores(InputSequence, ComparisonSequences);
            float[] distances = new float[scores.Length];
            for (int i = 0; i < scores.Length; i++) distances[i] = 1 - scores[i];
            return distances;
        }

        Tuple<List<List<int>>, List<List<int>>, List<List<int>>> Tokenize(List<string> candidates)
        {
            List<List<int>> sentences = new List<List<int>>();
            List<List<int>> tokenIds = new List<List<int>>();
            List<List<int>> attentionMask = new List<List<int>>();
            foreach (string text in candidates)
            {
                // Normalize the sentence
                string normalized = bertNorm.Normalize(text);
                // Pretokenize the sentence
                List<string> pretokenized = bertPreTok.PreTokenize(normalized);
                // Tokenize with WordPieceTokenizer
                List<string> tokenized = wordPieceTokenizer.Encode(pretokenized);
                // Template Process
                List<string> processed = templateProcessing.PostProcess(tokenized);
                // Tokens to Ids
                List<int> ids = wordPieceTokenizer.ConvertTokensToIds(processed);
                // Add ids to List<List<int>>
                sentences.Add(ids);
            }

            int max_length = (int)tokenizerJsonData["truncation"]["max_length"];
            // Tuple<List<List<int>>, List<List<int>>> tuple_ = PaddingOrTruncate(true, true, sentences, max_length, JObject.FromObject(tokenizerJsonData["padding"]));
            Tuple<List<List<int>>, List<List<int>>> tuple_ = PaddingOrTruncate(true, true, sentences, max_length);
            attentionMask = tuple_.Item1;
            tokenIds = tuple_.Item2;

            List<List<int>> TokenTypeIds = new List<List<int>>();
            TokenTypeIds = AddTokenTypes(tokenIds);
            return Tuple.Create(tokenIds, attentionMask, TokenTypeIds);
        }

        public static TensorFloat MeanPooling(Tensor AttentionMaskTensor, TensorFloat outputTensor, Ops ops)
        {
            if (AttentionMaskTensor == null || outputTensor == null)
            {
                Debug.LogError("AttentionMaskTensor or outputTensor is null.");
            }

            // Create an attention mask and
            // add a new dimension (to make the mask compatible for element wise multiplication with token embeddings)
            TensorFloat AttentionMaskTensorFloat = ops.Cast(AttentionMaskTensor, DataType.Float) as TensorFloat;
            Tensor InputMaskExpanded = AttentionMaskTensorFloat.ShallowReshape(AttentionMaskTensorFloat.shape.Unsqueeze(-1));
            TensorFloat InputMaskExpandedFloat = ops.Cast(InputMaskExpanded, DataType.Float) as TensorFloat;

            TensorShape outputShape = outputTensor.shape;
            // Expand to 384 => [2, 6, 384]
            InputMaskExpandedFloat = ops.Expand(InputMaskExpandedFloat, outputShape) as TensorFloat;
            // torch.sum(token_embeddings * input_mask_expanded, 1) / torch.clamp(input_mask_expanded.sum(1), min=1e-9)
            TensorFloat temp_ = ops.Mul(outputTensor, InputMaskExpandedFloat);
            TensorFloat MeanPooledTensor = ops.ReduceMean(temp_, new int[] { 1 }, false);

            return MeanPooledTensor;
        }

        public static TensorFloat L2Norm(TensorFloat MeanPooledTensor, Ops ops)
        {
            // L2 NORMALIZATION
            // Compute L2 norm along axis 1 (dim=1)
            TensorFloat l2Norms = ops.ReduceL2(MeanPooledTensor, new int[] { 1 }, true);
            // Broadcast the L2 norms to the original shape
            TensorFloat l2NormsBroadcasted = ops.Expand(l2Norms, MeanPooledTensor.shape) as TensorFloat;
            // Divide sentence_embeddings by their L2 norms to achieve normalization
            TensorFloat NormalizedEmbeddings = ops.Div(MeanPooledTensor, l2NormsBroadcasted);
            return NormalizedEmbeddings;
        }

        public Dictionary<string, Tensor> TokenizeInput(List<string> sentences)
        {
            List<List<int>> TokenIds = new List<List<int>>();
            List<List<int>> AttentionMask = new List<List<int>>();
            List<List<int>> TokenTypeIds = new List<List<int>>();

            Tuple<List<List<int>>, List<List<int>>, List<List<int>>> FinalTuple = Tokenize(sentences);
            TokenIds = FinalTuple.Item1;
            AttentionMask = FinalTuple.Item2;
            TokenTypeIds = FinalTuple.Item3;

            // Flatten the nested list into a one-dimensional array
            List<int> flattenedData = new List<int>();
            foreach (var innerList in TokenIds)
            {
                flattenedData.AddRange(innerList);
            }
            int[] data = flattenedData.ToArray();
            // Create a 3D tensor shape
            TensorShape shape = new TensorShape(TokenIds.Count, TokenIds[0].Count);
            // Create a new tensor from the array
            TensorInt TokenIdsTensor = new TensorInt(shape, data);
            // Flatten the nested list into a one-dimensional array
            List<int> flattenedData2 = new List<int>();
            foreach (var innerList in AttentionMask)
            {
                flattenedData2.AddRange(innerList);
            }
            int[] data2 = flattenedData2.ToArray();
            // Create a 3D tensor shape
            TensorShape shape2 = new TensorShape(AttentionMask.Count, AttentionMask[0].Count);
            // Create a new tensor from the array
            TensorInt AttentionMaskTensor = new TensorInt(shape2, data2);
            // Flatten the nested list into a one-dimensional array
            List<int> flattenedData3 = new List<int>();
            foreach (var innerList in TokenTypeIds)
            {
                flattenedData3.AddRange(innerList);
            }
            int[] data3 = flattenedData3.ToArray();
            // Create a 3D tensor shape
            TensorShape shape3 = new TensorShape(TokenTypeIds.Count, TokenTypeIds[0].Count);
            // Create a new tensor from the array
            TensorInt TokenTypeIdsTensor = new TensorInt(shape3, data3);
            Dictionary<string, Tensor> inputTensors = new Dictionary<string, Tensor>()
            {
                { "input_ids", TokenIdsTensor },
                {"token_type_ids", TokenTypeIdsTensor },
                { "attention_mask", AttentionMaskTensor }
            };
            return inputTensors;
        }

        static Tuple<List<List<int>>, List<List<int>>> PaddingOrTruncate(bool padding, bool truncation, List<List<int>> tokens, int max_length)
        {
            // TODO allow user to change
            string padding_side = "right";
            int pad_token_id = 0;     // TODO Change (int)config["pad_token"]
            List<List<int>> attentionMask = new List<List<int>>();
            int maxLengthOfBatch = tokens.Max(x => x.Count);
            max_length = maxLengthOfBatch;

            // TODO Check the logic
            /*if (max_length == null)
            {
                max_length = maxLengthOfBatch;
            }
            max_length = Math.Min(max_length.Value, model_max_length);*/

            if (padding || truncation)
            {
                for (int i = 0; i < tokens.Count; ++i)
                {
                    if (tokens[i].Count == max_length)
                    {
                        attentionMask.Add(Enumerable.Repeat(1, tokens[i].Count).ToList());
                        continue;
                    }
                    else if (tokens[i].Count > max_length)
                    {
                        if (truncation)
                        {
                            tokens[i] = tokens[i].Take(max_length).ToList();
                        }
                        attentionMask.Add(Enumerable.Repeat(1, tokens[i].Count).ToList());
                    }
                    else
                    {
                        if (padding)
                        {
                            int diff = max_length - tokens[i].Count;

                            if (padding_side == "right")
                            {
                                attentionMask.Add(Enumerable.Repeat(1, tokens[i].Count)
                                    .Concat(Enumerable.Repeat(0, diff)).ToList());
                                tokens[i].AddRange(Enumerable.Repeat(pad_token_id, diff));
                            }
                            else
                            {
                                attentionMask.Add(Enumerable.Repeat(0, diff)
                                    .Concat(Enumerable.Repeat(1, tokens[i].Count)).ToList());
                                tokens[i].InsertRange(0, Enumerable.Repeat(pad_token_id, diff));
                            }
                        }
                        else
                        {
                            attentionMask.Add(Enumerable.Repeat(1, tokens[i].Count).ToList());
                        }
                    }
                }
            }
            else
            {
                attentionMask = tokens.Select(x => Enumerable.Repeat(1, x.Count).ToList()).ToList();
            }
            return Tuple.Create(attentionMask, tokens);
        }

        static List<List<int>> AddTokenTypes(List<List<int>> inputIds)
        {
            return inputIds.Select(innerList => innerList.Select(_ => 0).ToList()).ToList();
        }
    }

    public class ModelDownloader
    {
        public static async Task DownloadAndUnzip(string modelUrl, string dirname, Callback<float> progresscallback = null, bool async = true)
        {
            if (!Directory.Exists(dirname))
            {
#if UNITY_EDITOR
                string modelZip = Path.GetTempFileName() + ".zip";
                await LLMUnitySetup.DownloadFile(modelUrl, modelZip, true, false, null, progresscallback, async);
                LLMUnitySetup.ExtractZip(modelZip, dirname);
                File.Delete(modelZip);
#else
                throw new Exception($"Can't download file {modelUrl} from play mode!");
#endif
            }
        }

        public static string ModelExtractDir(string modelUrl)
        {
            string modelBasename = Path.GetFileName(modelUrl).Split("?")[0];
            string dirname = LLMUnitySetup.GetAssetPath(modelBasename.Replace(".zip", ""));
            return dirname;
        }

        public static async Task<(string, string)> DownloadUndreamAIAsync(string modelUrl, Callback<float> progresscallback = null)
        {
            string dirname = ModelExtractDir(modelUrl);
            string modelName = Path.GetFileName(dirname);
            await DownloadAndUnzip(modelUrl, dirname, progresscallback, true);
            return (Path.Combine(dirname, modelName + ".sentis"), Path.Combine(dirname, modelName + ".tokenizer.json"));
        }

        public static (string, string) DownloadUndreamAI(string modelUrl, Callback<float> progresscallback = null)
        {
            string dirname = ModelExtractDir(modelUrl);
            string modelName = Path.GetFileName(dirname);
            _ = DownloadAndUnzip(modelUrl, dirname, progresscallback, false);
            return (Path.Combine(dirname, modelName + ".sentis"), Path.Combine(dirname, modelName + ".tokenizer.json"));
        }
    }

    public class EmbeddingModels
    {
        public static EmbeddingModel BGEModel(string modelPath, string tokenizerPath, BackendType backend = BackendType.CPU)
        {
            return new EmbeddingModel(modelPath, tokenizerPath, backend, "sentence_embedding", false, 384);
        }

        public static EmbeddingModel MiniLMModel(string modelPath, string tokenizerPath, BackendType backend = BackendType.CPU)
        {
            return new EmbeddingModel(modelPath, tokenizerPath, backend, "last_hidden_state", true, 384);
        }

        public static async Task<EmbeddingModel> BGESmallModel(BackendType backend = BackendType.CPU, Callback<float> progresscallback = null)
        {
            string modelUrl = "https://huggingface.co/undreamai/bge-small-en-v1.5-sentis/resolve/main/bge-small-en-v1.5.zip?download=true";
            (string modelPath, string tokenizerPath) = await ModelDownloader.DownloadUndreamAIAsync(modelUrl, progresscallback);
            return BGEModel(modelPath, tokenizerPath, backend);
        }

        public static async Task<EmbeddingModel> BGEBaseModel(BackendType backend = BackendType.CPU, Callback<float> progresscallback = null)
        {
            string modelUrl = "https://huggingface.co/undreamai/bge-base-en-v1.5-sentis/resolve/main/bge-base-en-v1.5.zip?download=true";
            (string modelPath, string tokenizerPath) = await ModelDownloader.DownloadUndreamAIAsync(modelUrl, progresscallback);
            return BGEModel(modelPath, tokenizerPath, backend);
        }

        public static async Task<EmbeddingModel> MiniLMModel(BackendType backend = BackendType.CPU, Callback<float> progresscallback = null)
        {
            string modelUrl = "https://huggingface.co/undreamai/all-MiniLM-L6-v2-sentis/resolve/main/all-MiniLM-L6-v2.zip?download=true";
            (string modelPath, string tokenizerPath) = await ModelDownloader.DownloadUndreamAIAsync(modelUrl, progresscallback);
            return MiniLMModel(modelPath, tokenizerPath, backend);
        }
    }

    public class Embedding : MonoBehaviour
    {
        public int SelectedOption = 0;

        [HideInInspector] public float downloadProgress = 1;
        [HideInInspector] public EmbeddingModel embeddingModel = null;
        public readonly (string, string)[] options = new (string, string)[]{
            ("Download model", null),
            ("bge-small-en-v1.5 (small, best)", "BGESmallModel"),
            ("bge-base-en-v1.5 (medium, best)", "BGEBaseModel"),
            ("all-MiniLM-L6-v2 (small, standard)", "MiniLMModel")
        };

        public void Awake()
        {
            SelectModel(SelectedOption);
        }

        public async void SelectModel(int optionIndex)
        {
            SelectedOption = optionIndex;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            string methodName = options[SelectedOption].Item2;
            if (methodName != null)
            {
                Type type = typeof(EmbeddingModels);
                MethodInfo method = type.GetMethod(methodName);
                object[] arguments = { BackendType.CPU, (Callback<float>)SetDownloadProgress };
                embeddingModel = await (Task<EmbeddingModel>)method.Invoke(null, arguments);
            }
        }

        public void SetDownloadProgress(float progress)
        {
            downloadProgress = progress;
        }

        public EmbeddingModel GetModel()
        {
            return embeddingModel;
        }

        public void OnDestroy()
        {
            embeddingModel?.Destroy();
        }
    }
}
