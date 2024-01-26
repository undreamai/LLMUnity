using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace HuggingFace.SharpTransformers.PreTokenizers
{
    /// <summary>
    /// A callable class representing a pre-tokenizer used in tokenization. Subclasses
    /// should implement the `pre_tokenize_text` method to define the specific pre-tokenization logic.
    /// </summary>
    public class PreTokenizer
    {
        public JObject Config;


        public PreTokenizer(JObject config)
        {
            Config = config;
        }

        /// <summary>
        /// Factory method that returns an instance of a subclass of `PreTokenizer` based on the provided configuration.
        /// </summary>
        /// <param name="config">A configuration object for the pre-tokenizer.</param>
        /// <returns>An instance of a subclass of `PreTokenizer`.</returns>
        /// <exception cref="Exception">If the provided configuration object does not correspond to any known pre-tokenizer.</exception>
        public static PreTokenizer FromConfig(JObject config)
        {
            if (config == null)
                return null;

            string configType = config["type"].ToString();
            switch (configType)
            {

                case "BertPreTokenizer":
                    return new BertPreTokenizer(config);
                default:
                    throw new Exception($"Unknown PreTokenizer type: {configType}");
            }
        }

        /// <summary>
        /// Method that should be implemented by subclasses to define the specific pre-tokenization logic.
        /// </summary>
        /// <param name="text">The text to pre-tokenize.</param>
        /// <returns>The pre-tokenized text.</returns>
        /// <exception cref="Exception"></exception>
        public virtual List<string> PreTokenizeText(string text)
        {
            throw new Exception("PreTokenizeText should be implemented in subclass.");
        }

        /// <summary>
        /// Tokenizes the given text into pre-tokens.
        /// </summary>
        /// <param name="text">The text or array of texts to pre-tokenize.</param>
        /// <returns>A list of pre-tokens</returns>
        public List<string> PreTokenize(object text)
        {

            List<string> result = new List<string>();

            if (text is string textString)
            {
                // Else if 'text' is not a list, tokenize it directly using 'pre_tokenize_text'
                //result.Add(PreTokenizeText(textString));
                result.AddRange(PreTokenizeText(textString));
            }
            else if (text is List<string> textList)
            {
                // If 'text' is List select each element and tokenize it using 'pre_tokenize_text'
                //result = textList.Select(x => PreTokenizeText(x)).ToList();
                result = textList.SelectMany(x => PreTokenizeText(x)).ToList();
            }
            else
            {
                throw new ArgumentException("Unsupported parameter type");
            }
            // Flatten the 'result' list of lists and return a flat list
            //return result.SelectMany(x => x).ToList();
            return result;
        }


        /// <summary>
        /// Alias for PreTokenizer.PreTokenize
        /// </summary>
        /// <param name="text">The text or array of texts to pre-tokenize.</param>
        /// <returns>An array of pre-tokens.</returns>
        public List<string> Call(string text)
        {
            return PreTokenize(text);
        }
    }


    public class BertPreTokenizer : PreTokenizer
    {
        private readonly Regex pattern;

        /// <summary>
        /// A PreTokenizer that splits text into wordpieces using a basic tokenization scheme
        /// similar to that used in the original implementation of BERT.
        /// </summary>

        public BertPreTokenizer(JObject config) : base(config)
        {
            // Construct a pattern which matches the rust implementation:
            // https://github.com/huggingface/tokenizers/blob/b4fcc9ce6e4ad5806e82826f816acfdfdc4fcc67/tokenizers/src/pre_tokenizers/bert.rs#L11
            // Equivalent to removing whitespace and splitting on punctuation (both \p{P} and other ASCII characters)
            string punctuationRegex = "\\p{P}";
            this.pattern = new Regex($"[^\\s{punctuationRegex}]+|[{punctuationRegex}]", RegexOptions.Compiled | RegexOptions.Multiline);

        }

        /// <summary>
        /// Tokenizes a single text using the BERT pre-tokenization scheme.
        /// We use Array instead of list (for test purposes)
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>An array of tokens.</returns>
        /// TODO: See how we can optimize it
        public override List<string> PreTokenizeText(string text)
        {
            var matches = this.pattern.Matches(text.Trim());
            var tokens = new List<string>();

            foreach (Match match in matches)
            {
                tokens.Add(match.Value);
            }

            return tokens;
        }
    }
}