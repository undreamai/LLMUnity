using UnityEngine;

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HuggingFace.SharpTransformers.NormalizersUtils;


namespace HuggingFace.SharpTransformers.Normalizers
{
    /// <summary>
    /// A base class for text normalization.
    /// </summary>
    public abstract class Normalizer
    {
        public JObject Config;

        // Constructor initializes Config property with the value of the config
        // parameter
        public Normalizer(JObject config)
        {
            Config = config;
        }

        /// <summary>
        /// Factory method for creating normalizers from config objects.
        /// <b>Returns</b> A Normalizer object.
        /// </summary>
        /// <param name="config">The configuration object for the normalizer.</param>
        /// <exception cref="Exception"></exception>
        public static Normalizer FromConfig(JObject config)
        {
            if (config == null)
                return null;

            string configType = config["type"].ToString();
            switch (configType)
            {
                case "BertNormalizer":
                    return new BertNormalizer(config);
                case "Sequence":
                    return new NormalizerSequence(config);
                case "Replace":
                    return new Replace(config);
                case "Prepend":
                    return new Prepend(config);
                default:
                    throw new Exception($"Unknown Normalizer type: {configType}");
            }
        }

        /// <summary>
        /// Normalize the input text.
        /// </summary>
        /// <param name="text">The normalized text.</param>
        /// <exception cref="Exception"></exception>
        public virtual string Normalize(string text)
        {
            throw new Exception("Normalize should be implemented in subclass.");
        }

        /// <summary>
        /// Alias for Normalizer.Normalize
        /// </summary>
        /// <param name="text">The normalized text.</param>
        /// <returns></returns>
        public string Call(string text)
        {
            return Normalize(text); // Call the Normalize method
        }
    }


    /// <summary>
    /// A BertNormalizer class (inherited from Normalizer) for text normalization.
    /// </summary>
    public class BertNormalizer : Normalizer
    {
        public BertNormalizer(JObject config) : base(config)
        {
        }

        /// <summary>
        /// Adds whitespace around any CJK (Chinese, Japanese, or Korean) character in the input text.
        /// </summary>
        /// <param name="text">The input text to tokenize.</param>
        /// <returns name="string">The tokenized text with whitespace added around CJK characters.</returns>
        public string TokenizeChineseChars(string text)
        {
            // Adds whitespace around any CJK character.
            StringBuilder output = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                // Used to convert the character to its Unicode code point.
                int unicodeCodePoint = Char.ConvertToUtf32(character.ToString(), 0);

                if (IsChineseChar(unicodeCodePoint))
                {
                    output.Append(" ");
                    output.Append(character);
                    output.Append(" ");
                }
                else
                {
                    output.Append(character);
                }
            }
            return output.ToString();
        }

        /// <summary>
        /// Checks whether the given Unicode codepoint represents a CJK (Chinese, Japanese, or Korean) character.
        ///
        /// A "chinese character" is defined as anything in the CJK Unicode block:
        /// https://en.wikipedia.org/wiki/CJK_Unified_Ideographs_(Unicode_block)
        ///
        /// Note that the CJK Unicode block is NOT all Japanese and Korean characters, despite its name.
        /// The modern Korean Hangul alphabet is a different block, as is Japanese Hiragana and Katakana.
        /// Those alphabets are used to write space-separated words, so they are not treated specially
        /// and are handled like all other languages.
        ///
        /// </summary>
        /// <param name="unicodeCodePoint">The Unicode codepoint to check.</param>
        /// <returns name="bool"> True if the codepoint represents a CJK character, false otherwise. </returns>
        public bool IsChineseChar(int unicodeCodePoint)
        {
            return (unicodeCodePoint >= 0x4E00 && unicodeCodePoint <= 0x9FFF) ||
                (unicodeCodePoint >= 0x3400 && unicodeCodePoint <= 0x4DBF) ||
                (unicodeCodePoint >= 0x20000 && unicodeCodePoint <= 0x2A6DF) ||
                (unicodeCodePoint >= 0x2A700 && unicodeCodePoint <= 0x2B73F) ||
                (unicodeCodePoint >= 0x2B740 && unicodeCodePoint <= 0x2B81F) ||
                (unicodeCodePoint >= 0x2B820 && unicodeCodePoint <= 0x2CEAF) ||
                (unicodeCodePoint >= 0xF900 && unicodeCodePoint <= 0xFAFF) ||
                (unicodeCodePoint >= 0x2F800 && unicodeCodePoint <= 0x2FA1F);
        }

        /// <summary>
        /// Strips accents from the given text.
        /// </summary>
        /// <param name="text">The text to strip accents from.</param>
        /// <returns name="string">The text with accents removed.</returns>
        public string StripAccents(string text)
        {
            // Normalize used is NFD: NFD (Normalization Form Canonical Decomposition):
            // Characters are decomposed into their constituent parts.
            // For example, a character with a diacritic is split into a base character and a
            // separate diacritic character.
            // ## NFD is NormalizationForm.FormD
            // Then we replace accents (regex) with ""
            return Regex.Replace(text.Normalize(NormalizationForm.FormD), @"[\u0300-\u036f]", "");
        }

        /// <summary>
        /// Normalizes the given text based on the configuration.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns name="string">The normalized text.</returns>
        public new string Normalize(string text)
        {
            // TODO use rest of config


            // In C# we need to check first if it's null or not
            bool? handleChineseCharsValue = (bool?)Config["handle_chinese_chars"];
            bool? handleLowercaseValue = (bool?)Config["lowercase"];
            bool? handleStripAccentsValue = (bool?)Config["strip_accents"];

            if (handleChineseCharsValue == true)
                text = this.TokenizeChineseChars(text);

            if (handleLowercaseValue == true)
            {
                // Lowercase
                text = text.ToLower();

                // If not explicitly set to false == true
                if (handleStripAccentsValue == null || handleStripAccentsValue == true)
                    text = this.StripAccents(text);
            }
            // If lowercase is false but strip accents is true
            else if (handleLowercaseValue == true)
            {
                text = this.StripAccents(text);
            }
            return text;
        }
    }


    public class NormalizerSequence : Normalizer
    {
        public List<Normalizer> Normalizers;

        /// <summary>
        /// Create a new instance of NormalizerSequence
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public NormalizerSequence(JObject config) : base(config)
        {
            string jsonString = config.ToString();

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (config["normalizers"] == null)
            {
                throw new ArgumentException("No normalizers in Sequence");
            }

            Normalizers = new List<Normalizer>();

            foreach (JObject normalizerConfig in config["normalizers"])
            {
                var normalizer = Normalizer.FromConfig(normalizerConfig);

                //var normalizer = new Normalizer.FromConfig(normalizerConfig);
                if (normalizer != null)
                {
                    //string normalizerString = normalizerConfig.Value<string>();
                    Normalizers.Add(normalizer);
                }
            }
        }

        /// <summary>
        /// Apply a sequence of Normalizes to the input text.
        /// </summary>
        /// <param name="text">The text to normalize</param>
        /// <returns>The normalized text</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public new string Normalize(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            foreach (var normalizer in Normalizers)
            {
                text = normalizer.Normalize(text);
            }
            return text;
        }
    }

    /// <summary>
    /// A Normalizer that prepends a string to the input string
    /// This is a nice day => _This is a nice day
    /// </summary>
    public class Prepend : Normalizer
    {
        public Prepend(JObject config) : base(config)
        {
        }

        /// <summary>
        /// Prepends the input string
        /// </summary>
        /// <param name="text">The text to normalize</param>
        /// <returns>The normalized text</returns>
        public override string Normalize(string text)
        {
            string text_ = this.Config["prepend"].Value<string>() + text;
            return text_;
        }
    }


    /// <summary>
    /// Replace normalizer that replaces occurrences of a pattern
    /// with a given string or regular expression.
    /// </summary>
    class Replace : Normalizer
    {
        public Replace(JObject config) : base(config)
        {
        }

        /// <summary>
        /// Normalize the input text by replacing the pattern with the content.
        /// For instance in Llama 2 we replace " " with _
        /// </summary>
        /// <param name="text">Input text to be normalized</param>
        /// <returns>The normalized text after replacing the pattern with the content.</returns>
        public override string Normalize(string text)
        {
            string pattern = Utils.createPattern(this.Config["pattern"]);

            if (pattern == null)
            {
                return text;
            }

            text = text.Replace(pattern, this.Config["content"].Value<string>());

            return text;
        }
    }
}
