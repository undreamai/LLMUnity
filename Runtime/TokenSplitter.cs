using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMUnity
{
    [Serializable]
    public class TokenSplitter : Chunking
    {
        public int numTokens = 10;

        public override async Task<List<(int, int)>> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            List<int> tokens = await search.Tokenize(input);
            int startIndex = 0;
            for (int i = 0; i < tokens.Count; i += numTokens)
            {
                int batchTokens = Math.Min(tokens.Count, i + numTokens) - i;
                string detokenised = await search.Detokenize(tokens.GetRange(i, batchTokens));
                int endIndex = Math.Min(input.Length - 1, startIndex + detokenised.Length - 1);
                indices.Add((startIndex, endIndex));
                if (endIndex == input.Length - 1) break;
                startIndex = endIndex + 1;
            }
            return indices;
        }
    }
}
