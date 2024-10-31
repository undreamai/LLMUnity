using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMUnity
{
    [Serializable]
    public class TokenSplitter : Chunking
    {
        public int numTokens = 10;

        protected int DetermineEndIndex(string input, string detokenised, int startIndex, int searchRange = 5, int charsFromEnd = 3)
        {
            int endIndex = Math.Min(input.Length - 1, startIndex + detokenised.Length - 1);
            if (endIndex == input.Length - 1) return endIndex;

            for (int lastCharI = 0; lastCharI < charsFromEnd; lastCharI++)
            {
                int charI = detokenised.Length - 1 - lastCharI;
                if (charI < 0) break;
                char lastChar = detokenised[charI];

                for (int i = 0; i < searchRange; i++)
                {
                    foreach (int mul in new int[] {-1, 1})
                    {
                        int inputCharI = endIndex + mul * i;
                        if (inputCharI < 0 || inputCharI > input.Length - 1) continue;
                        if (input[inputCharI] == lastChar) return inputCharI;
                    }
                }
            }
            return endIndex;
        }

        public override async Task<List<(int, int)>> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            List<int> tokens = await search.llmEmbedder.Tokenize(input);
            if (tokens.Count == 0) return indices;

            int startIndex = 0;
            for (int i = 0; i < tokens.Count; i += numTokens)
            {
                int batchTokens = Math.Min(tokens.Count, i + numTokens) - i;
                string detokenised = await search.llmEmbedder.Detokenize(tokens.GetRange(i, batchTokens));
                int endIndex = DetermineEndIndex(input, detokenised, startIndex);
                indices.Add((startIndex, endIndex));
                startIndex = endIndex + 1;
                if (endIndex == input.Length - 1) break;
            }
            if (startIndex <= input.Length - 1) indices.Add((startIndex, input.Length - 1));
            return indices;
        }
    }
}
