using System;
using System.IO;
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

using HuggingFace.SharpTransformers.Normalizers;
using HuggingFace.SharpTransformers.PreTokenizers;
using HuggingFace.SharpTransformers.Tokenizers;
using HuggingFace.SharpTransformers.PostProcessors;


namespace SentenceSimilarityUtils
{
    public static class SentenceSimilarityUtils_
    {
        /// <summary>
        /// Load the Tokenizer JSON
        /// This JSON contains the information about how the input should be processed
        /// before being feed to the Transformer model
        /// </summary>
        /// <returns></returns>
        public static JObject LoadTokenizerJson()
        {            
            string tokenizerJsonContent = File.ReadAllText("Assets/StreamingAssets/tokenizer.json");

            JObject tokenizerJsonData = JsonConvert.DeserializeObject<JObject>(tokenizerJsonContent);

            // We don't need config so we just return tokenizer json data
            return tokenizerJsonData;
        }


        /// <summary>
        /// Tokenize the sentences
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns>Tuple of 3 lists:
        /// - TokenIds
        /// - Attention Mask
        /// - TokenTypeIds </returns>
        static Tuple<List<List<int>>, List<List<int>>, List<List<int>>> Tokenize(List<string> candidates)
        {
            List<List<int>> sentences = new List<List<int>>();
            List<List<int>> tokenIds = new List<List<int>>();
            List<List<int>> attentionMask = new List<List<int>>();

            /// Step 1: Get the tokenizer and tokenizerConfig JSON
            /// Load JSON files and put elements in a two config objects
            JObject tokenizerJsonData = LoadTokenizerJson();

            /// Step 2: Create the Normalizer Object
            BertNormalizer bertNorm = new BertNormalizer(JObject.FromObject(tokenizerJsonData["normalizer"]));

            /// Step 3: Create the PreTokenization Object
            BertPreTokenizer bertPreTok = new BertPreTokenizer(JObject.FromObject(tokenizerJsonData["pre_tokenizer"]));

            /// Step 4: Create the WordPieceTokenizer
            WordPieceTokenizer wordPieceTokenizer = new WordPieceTokenizer(JObject.FromObject(tokenizerJsonData["model"]));

            /// Step 5: Create the TemplateProcessing
            TemplateProcessing templateProcessing = new TemplateProcessing(JObject.FromObject(tokenizerJsonData["post_processor"]));

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

            Tuple<List<List<int>>, List<List<int>>> tuple_ = PaddingOrTruncate(true, true, sentences, max_length, JObject.FromObject(tokenizerJsonData["padding"]));

            attentionMask = tuple_.Item1;
            tokenIds = tuple_.Item2;

            List<List<int>> TokenTypeIds = new List<List<int>>();
            TokenTypeIds = AddTokenTypes(tokenIds);

            return Tuple.Create(tokenIds, attentionMask, TokenTypeIds);
        }


        /// <summary>
        /// Perform Mean Pooling
        /// </summary>
        /// <param name="AttentionMaskTensor">Attention Mask Tensor</param>
        /// <param name="outputTensor">Output Tensor</param>
        /// <param name="ops">Ops on tensor</param>
        /// <returns></returns>
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


        /// <summary>
        /// L2 Normalization
        /// </summary>
        /// <param name="MeanPooledTensor"></param>
        /// <param name="ops">Ops on tensor</param>
        /// <returns></returns>
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


        /// <summary>
        /// Tokenize the input to be fed to the Transformer model
        /// </summary>
        /// <param name="sentences"></param>
        /// <returns></returns>
        public static Dictionary<string, Tensor> TokenizeInput(List<string> sentences)
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

            Dictionary<string, Tensor> inputTensors = new Dictionary<string, Tensor>() {
                { "input_ids", TokenIdsTensor },
                {"token_type_ids", TokenTypeIdsTensor },
                { "attention_mask", AttentionMaskTensor }
                };

            return inputTensors;
        }


        /// <summary>
        /// Padding or Truncate Tensor
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="truncation"></param>
        /// <param name="tokens"></param>
        /// <param name="max_length"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        static Tuple<List<List<int>>, List<List<int>>> PaddingOrTruncate(bool padding, bool truncation, List<List<int>> tokens, int max_length, JObject config)
        {
            // TODO allow user to change
            string padding_side = "right";

            int pad_token_id = 0; // TODO Change (int)config["pad_token"]

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


        /// <summary>
        /// For now it's just copy inputIds and set all elements to 0
        /// </summary>
        /// <returns></returns>
        static List<List<int>> AddTokenTypes(List<List<int>> inputIds)
        {
            return inputIds.Select(innerList => innerList.Select(_ => 0).ToList()).ToList();

        }
    }
}
