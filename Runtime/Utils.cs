using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace LLMUnity
{
    public class Saver
    {
        public static void Save(object saveObject, string filePath, string name, FileMode mode = FileMode.Create)
        {
            using (FileStream stream = new FileStream(filePath, mode))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(saveObject, archive, name);
            }
        }

        public static void Save(object saveObject, ZipArchive archive, string name)
        {
            ZipArchiveEntry mainEntry = archive.CreateEntry(name);
            using (Stream entryStream = mainEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(saveObject.GetType());
                serializer.WriteObject(entryStream, saveObject);
            }
        }

        public static T Load<T>(string filePath, string name)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                return Load<T>(archive, name);
            }
        }

        public static T Load<T>(ZipArchive archive, string name)
        {
            ZipArchiveEntry baseEntry = archive.GetEntry(name);
            using (Stream entryStream = baseEntry.Open())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                T obj = (T)serializer.ReadObject(entryStream);
                return obj;
            }
        }

        public static string EscapeFileName(string fileName)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }
            return fileName;
        }

        public static string RemoveExtension(string name)
        {
            return Path.Combine(Path.GetDirectoryName(name), Path.GetFileNameWithoutExtension(name));
        }

        public static string ComputeMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
