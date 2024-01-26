using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HuggingFace.SharpTransformers.NormalizersUtils;


namespace HuggingFace.SharpTransformers.Decoders
{
    class Decoder_
    {
        public JObject Config;
        public List<string> AddedTokens;
        public string EndOfWordSuffix;
        public string TrimOffsets;


        /// <summary>
        /// Creates an instance of `Decoder`.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        public Decoder_(JObject config)
        {
            Config = config;
            AddedTokens = new List<string>();
            EndOfWordSuffix = null;
            TrimOffsets = null; // config.trim_offsets
        }


        /// <summary>
        /// Creates a decoder instance based on the provided configuration.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns> A decoder instance.</returns>
        /// <exception cref="Exception">If an unknown decoder type is provided.</exception>
        public static Decoder_ FromConfig(JObject config)
        {
            string configType = config["type"].ToString();

            switch (configType)
            {
                /*case "WordPiece":
                    return new WordPieceDecoder(config);*/
                case "Sequence":
                    return new DecoderSequence(config);
                default:
                    throw new Exception("Unknown Decoder type");
            }
        }


        /// <summary>
        /// Apply the decoder to a list of tokens.
        /// </summary>
        /// <param name="tokens">The list of tokens.</param>
        /// <returns>The decoded list of tokens.</returns>
        /// <exception cref="Exception">If the `decode_chain` method is not implemented in the subclass.</exception>
        public virtual List<string> DecodeChain(List<string> tokens)
        {
            throw new Exception("`decode_chain` should be implemented in subclass.");
        }

        /// <summary>
        /// Method to be implemented in subclass to apply post-processing on the given tokens.
        /// </summary>
        /// <param name="tokens">The list of tokens.</param>
        /// <returns> The decoded string.</returns>
        public virtual List<string> Decode(List<string> tokens)
        {
            return tokens; //string.Join("", DecodeChain(tokens));
        }


        /// <summary>
        /// Alias for Decode method
        /// </summary>
        /// <param name="tokens">The list of tokens.</param>
        /// <returns>The decoded string.</returns>
        public List<string> Call(List<string> tokens)
        {
            return Decode(tokens);
        }
    }



    /// <summary>
    /// A decoder that decodes a list of WordPiece tokens into a single string.
    /// </summary>
    /*class WordPieceDecoder : Decoder
    {

        public JObject Config;
        public string Prefix;
        public bool Cleanup;

        /// <summary>
        /// Creates a new instance of WordPieceDecoder.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        public WordPieceDecoder(JObject config, string prefix) : base(config)
        {
            Config = config;
            Prefix = prefix;
            // Whether to cleanup the decoded string.
            Cleanup = (bool)config["cleanup"];

        /*Clean up a list of simple English tokenization artifacts like spaces before punctuations and abbreviated forms
        AddedTokens = new List<string>();
        EndOfWordSuffix = null;
        TrimOffsets = null; // config.trim_offsets
        
    }


    /// <summary>
    /// DecodeChain
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public List<string> DecodeChain(List<string> tokens)
    {
        List<string> decodedTokens = new List<string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            string token = tokens[i];

            if (i != 0)
            {
                if (token.StartsWith(this.config.prefix)) // You will need to replace 'config.prefix' with the appropriate value
                {
                    // NOTE: .Replace() is intended; only replace first occurrence
                    token = token.Replace(this.config.prefix, "");
                }
                else
                {
                    token = " " + token;
                }
            }

            if (this.Cleanup)
            {
                token = CleanUpTokenization(token);
            }

            decodedTokens.Add(token);
        }

        return decodedTokens;
    }

}*/

    class ReplaceDecoder : Decoder_
    {
        public ReplaceDecoder(JObject config) : base(config)
        {
        }
        public override List<string> DecodeChain(List<string> tokens)
        {
            string pattern = Utils.createPattern(this.Config["pattern"]);

            if (pattern == null)
            {
                return tokens;
            }

            // Iterate through each token in tokens array. Replacing all occurences of specified pattern
            // with the content defined the configuration and returning a new array with modified tokens.
            // We use LINQ's Select method to transform each element of the tokens list.
            return tokens.Select(token => token.Replace(pattern, this.Config["content"].Value<string>())).ToList();
        }
    }


    /// <summary>
    /// Fuse all tokens into one big string
    /// Usually it's the last decoding step but this
    /// decoder exist in case some decoders need to happen after that step
    /// </summary>
    class FuseDecoder : Decoder_
    {
        public FuseDecoder(JObject config) : base(config)
        {
        }

        public override List<string> DecodeChain(List<string> tokens)
        {
            return new List<string> { string.Concat(tokens) };
        }
    }


    /// <summary>
    /// Handle tokens that represent bytes in hexadecimal format 
    /// and convert them back into their corresponding string representations
    /// </summary>
    class ByteFallback : Decoder_
    {
        public ByteFallback(JObject config) : base(config)
        {
        }

        private readonly UTF8Encoding uTF8Encoding = new UTF8Encoding();

        public override List<string> DecodeChain(List<string> tokens)
        {
            var newTokens = new List<string>();
            var previousByteTokens = new List<byte>();

            foreach (var token in tokens)
            {
                byte? bytes = null;
                // We check if a token is in the <0xXX> format (where XX is a hexadecimal byte) and try to parse it to a byte
                if (token.Length == 6 && token.StartsWith("<0x") && token.EndsWith(">"))
                {
                    if (byte.TryParse(token.Substring(3, 2), System.Globalization.NumberStyles.HexNumber, null, out byte byteValue))
                    {
                        bytes = byteValue;
                    }
                }
                // If successful we add it to previousByteTokens
                if (bytes != null)
                {
                    previousByteTokens.Add(bytes.Value);
                }
                else
                {
                    // If a token is not in the byte format, we check if there are any bytes in previousByteTokens, decode them into a string, add it to newTokens, and clear previousByteTokens.
                    if (previousByteTokens.Count > 0)
                    {
                        var decodedString = uTF8Encoding.GetString(previousByteTokens.ToArray());
                        newTokens.Add(decodedString);
                        previousByteTokens.Clear();
                    }

                    newTokens.Add(token);
                }
            }

            if (previousByteTokens.Count > 0)
            {
                var decodedString = uTF8Encoding.GetString(previousByteTokens.ToArray());
                newTokens.Add(decodedString);
                previousByteTokens.Clear();
            }

            return newTokens;
        }
        
    }

    /// <summary>
    /// Strip character from the beginning and end of tokens.
    /// </summary>
    class StripDecoder : Decoder_
    {
        private readonly string content;
        private readonly int start;
        private readonly int stop;

        public StripDecoder(JObject config) : base(config)
        {

            this.content = this.Config["content"].Value<string>();
            this.start = this.Config["start"].Value<int>(); 
            this.stop = this.Config["stop"].Value<int>(); 
        }

        public override List<string> Decode(List<string> tokens)
        {
            return tokens.Select(token =>
            {
                int startCut = 0;
                for (int i = 0; i < start; ++i)
                {
                    if (token[i] == content[0])
                    {
                        startCut = i + 1;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                int stopCut = token.Length;
                for (int i = 0; i < stop; ++i)
                {
                    int index = token.Length - i - 1;
                    if (token[index] == content[0])
                    {
                        stopCut = index;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                return token.Substring(startCut, stopCut - startCut);
            }).ToList();
        }
}


    /// <summary>
    /// Apply a sequence of decoders
    /// </summary>
    class DecoderSequence : Decoder_
    {
        public List<Decoder_> Decoders;

        public DecoderSequence(JObject config) : base(config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (config["decoders"] == null)
            {
                throw new ArgumentException("No decoders in Sequence");
            }

            Decoders = new List<Decoder_>();

            foreach (JObject decoderConfig in config["decoders"])
            {
               
                var decoder = Decoder_.FromConfig(decoderConfig);

                if (decoder != null)
                {
                    Decoders.Add(decoder);
                }
            }
        }

        /// <summary>
        /// Use this method to apply each decoder to the tokens
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public new List<string> DecodeChain(List<string> tokens)
        {
            return this.Decoders.Aggregate(tokens, (currentTokens, decoder) => decoder.DecodeChain(currentTokens));
        }
    }
}
