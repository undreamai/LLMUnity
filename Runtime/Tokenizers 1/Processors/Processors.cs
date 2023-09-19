using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public class Processors : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    
}

/*
/// <summary>
/// Base class for feature extractors.
/// </summary>
public abstract class FeatureExtractor
{
    public JObject Config;

    /// <summary>
    /// Constructs a new FeatureExtractor instance.
    /// </summary>
    /// <param name="config"></param>
    public FeatureExtractor(JObject config)
    {
        Config = config;
    }
}

class WhisperFeatureExtractor: FeatureExtractor
{
    public JObject config;

    public WhisperFeatureExtractor(JObject config) : base(config) 
    {
        Config = config;

    }
}*/