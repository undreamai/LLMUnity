using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace HuggingFace.SharpTransformers.PostProcessors
{
    public abstract class PostProcessor
    {
        public JObject Config;

        public PostProcessor(JObject config)
        {
            Config = config;
        }

        /// <summary>
        /// Factory method to create a PostProcessor object from a configuration object.
        /// </summary>
        /// <param name="config">Configuration object representing a PostProcessor.</param>
        /// <returns>A PostProcessor object created from the given configuration.</returns>
        /// <exception cref="Exception"></exception>
        public static PostProcessor FromConfig(JObject config)
        {
            if (config == null)
            {
                return null;
            }

            string configType = config["type"].ToString();

            switch (configType)
            {
                case "TemplateProcessing":
                    return new TemplateProcessing(config);
                default:
                    throw new Exception("Unknown PostProcessor type");
            }
        }


        /// <summary>
        /// Method to be implemented in subclass to apply post-processing on the given tokens.
        /// </summary>
        /// <param name="tokens">The input tokens to be post-processed.</param>
        /// <returns>The post-processed tokens.</returns>
        public virtual List<string> PostProcess(List<string> tokens, List<string> tokensPair = null)
        {
            throw new Exception("PostProcess should be implemented in subclass.");
        }


        /// <summary>
        /// Alias for PostProcess
        /// </summary>
        /// <param name="tokens">The text or array of texts to post-process.</param>
        /// <returns>An array of post-processed tokens.</returns>
        public virtual List<string> Call(List<string> tokens, List<string> tokensPair = null)
        {
            return PostProcess(tokens);
        }
    }

    /// <summary>
    /// Post processor that replaces special tokens in a template with actual tokens.
    /// </summary>
    public class TemplateProcessing : PostProcessor
    {
        // The template for a single sequence of tokens.
        public JArray Single;
        
        //public List<SingleItem> Single;

        // The template for a pair of sequences of tokens.
        public JArray Pair;
        
        //public List<PairItem> Pair;
        
        /// <summary>
        /// Creates a new instance of TemplateProcessing
        /// </summary>
        /// <param name="config"></param>
        public TemplateProcessing(JObject config) : base(config)
        {
            Config = config;
            Single = (JArray)config["single"];
            Pair = (JArray)config["pair"];
        }

        // The function's purpose is to replace special tokens and sequence identifiers with actual tokens.
        public override List<string> PostProcess(List<string> tokens, List<string> tokensPair = null)
        {
            // Check the type of sequence (based on if tokensPair is provided or not)
            // If tokensPair is null => assign Single to Type
            // Else assign Pair to Type
            JArray Type = tokensPair == null ? Single : Pair;

            // Create an empty List<string> to store the resulting tokens after processing
            List<string> ToReturn = new List<string>();

            // The function iterates over each item in the Type List
            foreach (JToken item in Type)
            {
                JObject itemJson = (JObject)item;

                // If the curent item has a property called "Special Token"
                // it means that this item is a special token.
                if (itemJson.ContainsKey("SpecialToken"))
                {
                    // We extracts the id of the special token and adds it to the toReturn List.
                    // We need to parse the JSON string and extract the id here
                    string specialTokenId = (string)itemJson["SpecialToken"]["id"];
                    ToReturn.Add(specialTokenId);
                }
                
                // If the current item has a property called "Sequence"  it means that this item
                // represents a sequence identifier (like 'A' or 'B')
                else if (itemJson.ContainsKey("Sequence"))
                {
                    string sequenceId = (string)itemJson["Sequence"]["id"];
                    if (sequenceId == "A")
                    {
                        // Add the elements of another collection to the list
                        // Equivalent to merge in JS
                        // Merge sequence tokens
                        ToReturn.AddRange(tokens);
                    }
                    else if (sequenceId == "B")
                    {
                        // Merge tokens_pair
                        ToReturn.AddRange(tokensPair);
                    }
                }
            }
            return ToReturn;
        }
    }
}