using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace HuggingFace.SharpTransformers.NormalizersUtils
{
    public static class Utils
    {
        public static string createPattern(JToken pattern, bool invert = true)
        {
            // Execute when pattern.Regex is defined
            if (pattern["Regex"] != null)
            {
                // Todo
                return null;
            }
            else if (pattern["String"] != null)
            {
                return pattern["String"].Value<string>();
            }
            else
            {
                throw new Exception($"Unknown pattern type: {pattern}");
            }
        }
    }
}
