using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LLMUnitySamples
{
    public class RAGUtils
    {
        public static List<string> SplitText(string text, int chunkSize = 300)
        {
            List<string> chunks = new List<string>();
            int start = 0;
            char[] delimiters = ".!;?\n\r".ToCharArray();

            while (start < text.Length)
            {
                int end = Math.Min(start + chunkSize, text.Length);
                if (end < text.Length)
                {
                    int nearestDelimiter = text.IndexOfAny(delimiters, end);
                    if (nearestDelimiter != -1) end = nearestDelimiter + 1;
                }
                chunks.Add(text.Substring(start, end - start).Trim());
                start = end;
            }
            return chunks;
        }

        public static Dictionary<string, List<string>> ReadGutenbergFile(string text)
        {
            Dictionary<string, List<string>> messages = new Dictionary<string, List<string>>();

            void AddMessage(string message, string name)
            {
                if (name == null) return;
                if (!messages.ContainsKey(name)) messages[name] = new List<string>();
                messages[name].Add(message);
            }

            void AddMessages(string message, string name, string name2)
            {
                foreach (string chunk in SplitText(message))
                {
                    if (chunk == "") continue;
                    AddMessage(chunk, name);
                    AddMessage(chunk, name2);
                }
            }

            // read the Hamlet play from the Gutenberg file
            string skipPattern = @"\[.*?\]";
            string namePattern = "^[A-Z and]+\\.$";
            Regex nameRegex = new Regex(namePattern);

            string name = null;
            string name2 = null;
            string message = "";
            bool add = false;
            int numWords = 0;
            int numLines = 0;

            string[] lines = text.Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("***")) add = !add;
                if (!add) continue;

                line = line.Replace("\r", "");
                line = Regex.Replace(line, skipPattern, "");
                string lineTrim = line.Trim();
                if (lineTrim == "" || lineTrim.StartsWith("Re-enter ") || lineTrim.StartsWith("Enter ") || lineTrim.StartsWith("SCENE")) continue;

                numWords += line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                numLines++;

                if (line.StartsWith("ACT"))
                {
                    AddMessages(message, name, name2);
                    name = null;
                    name2 = null;
                    message = "";
                }
                else if (nameRegex.IsMatch(line))
                {
                    AddMessages(message, name, name2);
                    message = "";
                    name = line.Replace(".", "");
                    if (name.Contains("and"))
                    {
                        string[] names = name.Split(" and ");
                        name = names[0];
                        name2 = names[1];
                    }
                }
                else if (name != null)
                {
                    if (message != "") message += " ";
                    message += line;
                }
            }
            return messages;
        }
    }
}
