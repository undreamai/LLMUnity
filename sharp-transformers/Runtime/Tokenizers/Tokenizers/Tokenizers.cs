using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HuggingFace.SharpTransformers.Normalizers;
using HuggingFace.SharpTransformers.PostProcessors;
using HuggingFace.SharpTransformers.Decoders;
using HuggingFace.SharpTransformers.PreTokenizers;


namespace HuggingFace.SharpTransformers.Tokenizers
{
    public abstract class TokenizerModel
    {
        public JObject Config;
        public JObject TokenizerData;
        public List<string> Vocab;
        public Dictionary<string, int> TokensToIds;
        public int? UnkTokenId; // Thanks to int?, the variable can hold an integer value or be null
        public string UnkToken;
        public string EndOfWordSuffix;
        public bool FuseUnk;

        public int UnknownTokenId { get; set; } = -1;
        public string UnknownToken { get; set; }
        public bool FuseUnknown { get; set; } = false;

        public TokenizerModel(JObject config)
        {
            Config = config;
            Vocab = new List<string>();

            // Dict of tokens to ids Dict<string, number>
            TokensToIds = new Dictionary<string, int>();

            UnkTokenId = null;
            UnkToken = null;
            EndOfWordSuffix = null;

            // Whether to fuse unknown tokens when encoding (default to false)
            FuseUnk = false;
        }

        // Overload if we have multiple parameters
        public TokenizerModel(JObject config, JObject tokenizerData)
        {
            Config = config;
            TokenizerData = tokenizerData;
            Vocab = new List<string>();

            // Dict of tokens to ids Dict<string, number>
            TokensToIds = new Dictionary<string, int>();

            UnkTokenId = null;
            UnkToken = null;
            EndOfWordSuffix = null;

            // Whether to fuse unknown tokens when encoding (default to false)
            FuseUnk = false;
        }


        /// <summary>
        /// Instantiates a new TokenizerModel instance based on the configuration object provided.
        /// </summary>
        /// <param name="config">The configuration object for the TokenizerModel.</param>
        /// <returns> A new instance of a TokenizerModel.</returns>
        /// <exception cref="Exception"> Will throw an error if the TokenizerModel type in the config is not recognized.</exception>
        public static TokenizerModel FromConfig(JObject config, JObject tokenizerData)
        {
            string configType = config["type"].ToString();
            switch (configType)
            {
                case "WordPiece":
                    return new WordPieceTokenizer(config);
                default:
                    throw new Exception($"Unknown TokenizerModel type: {configType}");
            }
        }

        /// <summary>
        /// Internal function to call the TokenizerModel instance.
        /// </summary>
        /// <param name="tokens">The tokens to encode.</param>
        /// <returns><The encoded token IDs/returns>
        public List<string> Call(List<string> tokens)
        {
            return Encode(tokens);
        }


        /// <summary>
        /// Encodes a list of tokens into a list of token IDs.
        /// </summary>
        /// <param name="tokens">The tokens to encode.</param>
        /// <returns>The encoded tokens.</returns>
        public virtual List<string> Encode(List<string> tokens)
        {
            throw new Exception("Encode should be implemented in subclass");
        }


        /// <summary>
        /// Converts a list of tokens into a list of token IDs.
        /// </summary>
        /// <param name="tokens"> The tokens to convert.</param>
        /// <returns>The converted token IDs.</returns>
        public List<int> ConvertTokensToIds(List<string> tokens)
        {
            // Create an array of token IDs by mapping each token to its corresponding ID
            List<int> ids = new List<int>();

            //Debug.Log("Dictionary Contents:");
            foreach (var kvp in TokensToIds)
            {
                Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
            }

            // token.Select: applies an operation to each token in tokens
            ids = tokens.Select(t => TokensToIds.TryGetValue(t, out int id) ? id : UnknownTokenId).ToList();



            if (FuseUnk == false)
            {
                //ids = Fuse(ids, UnkTokenId);
                ids = TokenizersUtils.Utils.Fuse(ids, UnkTokenId ?? -1);
            }

            return ids;
        }


        /// <summary>
        /// Converts a list of token IDs into a list of tokens.
        /// </summary>
        /// <param name="ids">The token IDs to convert.</param>
        /// <returns>The converted tokens.</returns>
        public List<string> ConvertIdsToTokens(List<int> ids)
        {
            /*
             Select is used instead of map to transform the list of IDs into a list of tokens.
            The conditional operator (? :) is used to check if the ID is within the valid range of the vocab array. If it is, the code retrieves the corresponding token using this.vocab[i]. If the token is null, it uses this.unk_token as the default value.
            The resulting list of tokens is returned.
            */
            List<string> tokens = ids.Select(i => Vocab.Count > i ? Vocab[i] ?? UnkToken : UnkToken).ToList();
            return tokens;
        }


        public List<int> Fuse(List<int> arr, int value)
        {
            List<int> fused = new List<int>();
            int i = 0;
            while (i < arr.Count)
            {
                fused.Add(arr[i]);
                if (arr[i] != value)
                {
                    i++;
                    continue;
                }

                while (i < arr.Count && arr[i] == value)
                {
                    i++;
                }
            }

            return fused;
        }
    }

    public class WordPieceTokenizer : TokenizerModel
    {
        public string ContinuingSubwordPrefix;

        public WordPieceTokenizer(JObject config) : base(config)
        {
            Config = config;

            //  Parse the JSON data located at config["vocab"] into a Dictionary<string, int> named vocab.
            //  Each key-value pair in the JSON will be represented as an entry in the dictionary.
            JObject vocabJson = config["vocab"].ToObject<JObject>();

            // A mapping of tokens to ids.
            TokensToIds = vocabJson.ToObject<Dictionary<string, int>>();

            // Id of the unknown token
            UnkTokenId = (int)config["vocab"]["[UNK]"];

            // The unknown token string.
            UnkToken = (string)config["unk_token"];

            ContinuingSubwordPrefix = (string)config["continuing_subword_prefix"];
        }


        /// <summary>
        /// Encodes an array of tokens using WordPiece encoding.
        /// </summary>
        /// <param name="tokens">The tokens to encode.</param>
        /// <returns>An array of encoded tokens.</returns>
        public new List<string> Encode(List<string> tokens)
        {
            // Initialize a List<string> to store the encoded tokens
            var OutputTokens = new List<string>();

            foreach (var token in tokens)
            {
                // Convert the token into an array of characters
                var Chars = token.ToCharArray();

                // Initialize a flag to track whether the token is unknown
                var IsUnknown = false;

                // Initialize the starting index for substring search
                var Start = 0;

                // Initialize an List<string> to store subtokens of the token
                var SubTokens = new List<string>();

                while (Start < Chars.Length)
                {
                    var End = Chars.Length;

                    // Initialize a variable to store the current substring
                    string CurrentSubstring = null;

                    while (Start < End)
                    {
                        // Get a substring from the character array
                        var Substr = new string(Chars.Skip(Start).Take(End - Start).ToArray());

                        if (Start > 0)
                        {
                            // Add a prefix to the substring if not the first character
                            Substr = ContinuingSubwordPrefix + Substr;
                        }

                        // Check if the substring is in the vocabulary
                        if (TokensToIds.ContainsKey(Substr))
                        {
                            // Store the current substring
                            CurrentSubstring = Substr;
                            break;
                        }
                        // Decrease the end index for substring search
                        --End;
                    }
                    if (CurrentSubstring == null)
                    {
                        // Set the flag to indicate that the token is unknown
                        IsUnknown = true;
                        break;
                    }
                    // Add the current substring to the subtokens List<string>
                    SubTokens.Add(CurrentSubstring);
                    // Move the start index to the end index for the next iteration
                    Start = End;
                }
                if (IsUnknown)
                {
                    // If token is unknown, add the unknown token to the output List<string>
                    OutputTokens.Add(UnkToken);
                }
                else
                {
                    // If token is not unknown, add the subtokens to the output List<string>
                    OutputTokens.AddRange(SubTokens);
                }
            }
            // Return the List<string> of encoded tokens
            return OutputTokens;
        }

        /// <summary>
        /// Converts a list of token IDs into a list of tokens.
        /// </summary>
        /// <param name="ids">The token IDs to convert.</param>
        /// <returns>The converted tokens.</returns>
        public new List<string> ConvertIdsToTokens(List<int> ids)
        {
            /*
             Select is used instead of map to transform the list of IDs into a list of tokens.
            The conditional operator (? :) is used to check if the ID is within the valid range of the vocab array. If it is, the code retrieves the corresponding token using this.vocab[i]. If the token is null, it uses this.unk_token as the default value.
            The resulting list of tokens is returned.
            */
            List<string> tokens = ids.Select(i => Vocab.Count > i ? Vocab[i] ?? UnkToken : UnkToken).ToList();
            return tokens;
        }

        public new List<int> ConvertTokensToIds(List<string> tokens)
        {
            // Create an array of token IDs by mapping each token to its corresponding ID
            List<int> ids = new List<int>();

            // token.Select: applies an operation to each token in tokens
            ids = tokens.Select(t => TokensToIds.TryGetValue(t, out int id) ? id : UnknownTokenId).ToList();



            if (FuseUnk == false)
            {
                //ids = Fuse(ids, UnkTokenId);
                ids = Fuse(ids, UnkTokenId ?? -1);
            }

            return ids;
        }

        public new List<int> Fuse(List<int> arr, int value)
        {
            List<int> fused = new List<int>();
            int i = 0;
            while (i < arr.Count)
            {
                fused.Add(arr[i]);
                if (arr[i] != value)
                {
                    i++;
                    continue;
                }

                while (i < arr.Count && arr[i] == value)
                {
                    i++;
                }
            }

            return fused;
        }
    }
}