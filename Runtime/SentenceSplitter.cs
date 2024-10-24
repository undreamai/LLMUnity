using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LLMUnity
{
    [Serializable]
    public class SentenceSplitter : Chunking
    {
        public const string DefaultDelimiters = ".!:;?\n\r";
        public char[] delimiters = DefaultDelimiters.ToCharArray();

        public override async Task<List<(int, int)>> Split(string input)
        {
            List<(int, int)> indices = new List<(int, int)>();
            await Task.Run(() => {
                int startIndex = 0;
                bool seenChar = false;
                for (int i = 0; i < input.Length; i++)
                {
                    bool isDelimiter = delimiters.Contains(input[i]);
                    if (isDelimiter)
                    {
                        while ((i < input.Length - 1) && (delimiters.Contains(input[i + 1]) || char.IsWhiteSpace(input[i + 1]))) i++;
                    }
                    else
                    {
                        if (!seenChar) seenChar = !char.IsWhiteSpace(input[i]);
                    }
                    if ((i == input.Length - 1) || (isDelimiter && seenChar))
                    {
                        indices.Add((startIndex, i));
                        startIndex = i + 1;
                    }
                }
            });
            return indices;
        }
    }
}
