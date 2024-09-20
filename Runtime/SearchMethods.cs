using System;
using System.Collections.Generic;
using Cloud.Unum.USearch;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using UnityEngine;
using System.Threading.Tasks;


namespace LLMUnity
{
    [DataContract]
    public class ANNModelSearch
    {
        USearchIndex index;
        [DataMember]
        protected SortedDictionary<int, string> keyToValue;
        [DataMember]
        protected Dictionary<string, float[]> embeddings;
        public LLM llm;

        public ANNModelSearch(LLM llm) : this(llm, ScalarKind.Float16, MetricKind.Cos, 32, 40, 16) {}

        public ANNModelSearch(
            LLM llm,
            ScalarKind quantization = ScalarKind.Float16,
            MetricKind metricKind = MetricKind.Cos,
            ulong connectivity = 32,
            ulong expansionAdd = 40,
            ulong expansionSearch = 16
        ) : this(llm, new USearchIndex((ulong)llm.embeddingLength, metricKind, quantization, connectivity, expansionAdd, expansionSearch, false)) {}

        public ANNModelSearch(
            LLM llm,
            USearchIndex index
        )
        {
            this.llm = llm;
            this.index = index;
            keyToValue = new SortedDictionary<int, string>();
            embeddings = new Dictionary<string, float[]>();
        }

        public void SetIndex(USearchIndex index)
        {
            this.index = index;
        }

        public void SetEmbedder(LLM llm)
        {
            this.llm = llm;
        }

        public void Insert(int key, string value, float[] encoding)
        {
            index.Add((ulong)key, encoding);
            keyToValue[key] = value;
        }

        public async Task<float[]> Add(int key, string inputString)
        {
            float[] embedding = await Encode(inputString);
            Insert(key, inputString, embedding);
            return embedding;
        }

        public bool Remove(int key)
        {
            return index.Remove((ulong)key) > 0 && keyToValue.Remove(key);
        }

        public int Count()
        {
            return (int)index.Size();
        }

        public string[] Search(float[] encoding, int k, out float[] distances)
        {
            int[] results = SearchKey(encoding, k, out distances);
            string[] values = new string[results.Length];
            for (int i = 0; i < results.Length; i++)
            {
                values[i] = keyToValue[results[i]];
            }
            return values;
        }

        public async Task<string[]> Search(string queryString, int k)
        {
            return Search(await Encode(queryString), k, out float[] distances);
        }

        public string[] Search(float[] encoding, int k)
        {
            return Search(encoding, k, out float[] distances);
        }

        public int[] SearchKey(float[] encoding, int k, out float[] distances)
        {
            index.Search(encoding, k, out ulong[] keys, out distances);
            int[] intKeys = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                intKeys[i] = (int)keys[i];
            return intKeys;
        }

        public int[] SearchKey(float[] encoding, int k)
        {
            return SearchKey(encoding, k, out float[] distances);
        }

        public async Task<int[]> SearchKey(string queryString, int k)
        {
            return SearchKey(await Encode(queryString), k, out float[] distances);
        }

        //TODO
/*
        public string[] Search(string queryString, int k, out float[] distances)
        {
            return Search(Encode(queryString), k, out distances);
        }

        public int[] SearchKey(string queryString, int k, out float[] distances)
        {
            return SearchKey(Encode(queryString), k, out distances);
        }
*/

        public static string GetIndexPath(string dirname = "")
        {
            return Path.Combine(dirname, "USearch");
        }

        // TODO use same base as LLMCharacter
        protected Ret ConvertContent<Res, Ret>(string response, ContentCallback<Res, Ret> getContent = null)
        {
            // template function to convert the json received and get the content
            if (response == null) return default;
            response = response.Trim();
            if (response.StartsWith("data: "))
            {
                string responseArray = "";
                foreach (string responsePart in response.Replace("\n\n", "").Split("data: "))
                {
                    if (responsePart == "") continue;
                    if (responseArray != "") responseArray += ",\n";
                    responseArray += responsePart;
                }
                response = $"{{\"data\": [{responseArray}]}}";
            }
            return getContent(JsonUtility.FromJson<Res>(response));
        }

        // TODO use same base as LLMCharacter
        protected List<float> EmbeddingsContent(EmbeddingsResult result)
        {
            // get content from a chat result received from the endpoint
            return result.embedding;
        }

        public async Task<float[]> Encode(string query)
        {
            TokenizeRequest tokenizeRequest = new TokenizeRequest();
            tokenizeRequest.content = query;
            string json = JsonUtility.ToJson(tokenizeRequest);

            string callResult = await llm.Embeddings(json);
            List<float> result = ConvertContent<EmbeddingsResult, List<float>>(callResult, EmbeddingsContent);
            return result.ToArray();
        }

        public static float DotProduct(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vector lengths must be equal for dot product calculation");
            }
            float result = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                result += vector1[i] * vector2[i];
            }
            return result;
        }

        public static float InverseDotProduct(float[] vector1, float[] vector2)
        {
            return 1 - DotProduct(vector1, vector2);
        }

        public static float[] InverseDotProduct(float[] vector1, float[][] vector2)
        {
            float[] results = new float[vector2.Length];
            for (int i = 0; i < vector2.Length; i++)
            {
                results[i] = InverseDotProduct(vector1, vector2[i]);
            }
            return results;
        }

        public static string GetSearchTypePath(string dirname = "")
        {
            return Path.Combine(dirname, "SearchType.txt");
        }

        public static string GetSearchPath(string dirname = "")
        {
            return Path.Combine(dirname, "Search.json");
        }

        public virtual void Save(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(archive, dirname);
            }
        }

        public virtual void Save(ZipArchive archive, string dirname = "")
        {
            ZipArchiveEntry typeEntry = archive.CreateEntry(GetSearchTypePath(dirname));
            using (StreamWriter writer = new StreamWriter(typeEntry.Open()))
            {
                writer.Write(GetType().FullName);
            }

            ZipArchiveEntry mainEntry = archive.CreateEntry(GetSearchPath(dirname));
            using (Stream entryStream = mainEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(GetType());
                serializer.WriteObject(entryStream, this);
            }
            index.Save(archive, GetIndexPath(dirname));

            //TODO
            // llm.SaveHashCode(archive, dirname);
        }

        public static ANNModelSearch Load(LLM llm, string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load(llm, archive, dirname);
            }
        }

        public static ANNModelSearch Load(LLM llm, ZipArchive archive, string dirname = "")
        {
            ZipArchiveEntry baseEntry = archive.GetEntry(GetSearchPath(dirname));
            ANNModelSearch search;
            using (Stream entryStream = baseEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ANNModelSearch));
                search = (ANNModelSearch)serializer.ReadObject(entryStream);
            }
            //TODO
            // int embedderHash = LLM.LoadHashCode(archive, dirname);
            // if (llm.GetHashCode() != embedderHash)
            //     throw new Exception($"The Search object uses different embedding model than the Search object stored");
            search.SetEmbedder(llm);

            USearchIndex index = new USearchIndex(archive, GetIndexPath(dirname));
            search.SetIndex(index);

            return search;
        }
    }
}
