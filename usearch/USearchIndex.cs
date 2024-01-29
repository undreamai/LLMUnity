using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using static Cloud.Unum.USearch.NativeMethods;

namespace Cloud.Unum.USearch
{
    public class USearchIndex : IDisposable
    {
        private IntPtr _index;
        private bool _disposedValue = false;
        private ulong _cachedDimensions;
        private IndexOptions _indexOptions;


        public USearchIndex(
            ulong dimensions,
            MetricKind metricKind = MetricKind.Cos,
            ulong connectivity = 32,
            ulong expansionAdd = 40,
            ulong expansionSearch = 16,
            bool multi = false
        )
        {
            IndexOptions initOptions = new()
            {
                metric_kind = metricKind,
                metric = default,
                quantization = ScalarKind.Float32,
                dimensions = dimensions,
                connectivity = connectivity,
                expansion_add = expansionAdd,
                expansion_search = expansionSearch,
                multi = multi
            };
            Init(initOptions);
        }

        public USearchIndex(string path, string name = "")
        {
            Load(path, name);
        }

        public void Init(IndexOptions options)
        {
            _indexOptions = options;
            this._index = usearch_init(ref options, out IntPtr error);
            HandleError(error);
            this._cachedDimensions = options.dimensions;
        }

        private static string GetIndexFilename(string name = "")
        {
            return name == "" ? "index" : name + ".index";
        }

        private static string GetIndexOptionsFilename(string name = "")
        {
            return name == "" ? "indexOptions" : name + ".indexOptions";
        }

        private void Load(string path, string name = "")
        {
            try
            {
                using (FileStream zipFileStream = new FileStream(path, FileMode.Open))
                using (ZipArchive zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
                {
                    var entry = zipArchive.GetEntry(GetIndexOptionsFilename(name));
                    using (Stream fileStream = entry.Open())
                    {
                        LoadIndexOptions(fileStream);
                    }

                    entry = zipArchive.GetEntry(GetIndexFilename(name));
                    using (Stream entryStream = entry.Open())
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        entryStream.CopyTo(memoryStream);
                        // Access the length and create a buffer
                        byte[] managedBuffer = new byte[memoryStream.Length];
                        memoryStream.Position = 0;  // Reset the position to the beginning
                        memoryStream.Read(managedBuffer, 0, managedBuffer.Length);

                        GCHandle handle = GCHandle.Alloc(managedBuffer, GCHandleType.Pinned);
                        try
                        {
                            IntPtr unmanagedBuffer = handle.AddrOfPinnedObject();
                            usearch_load_buffer(_index, unmanagedBuffer, (UIntPtr)managedBuffer.Length, out IntPtr error);
                            HandleError(error);
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading the search index: {ex.Message}");
            }
        }

        public void Save(string path, string name = "", FileMode mode = FileMode.Create)
        {
            string indexPath = path + GetIndexFilename(name);
            string indexOptionsPath = path + GetIndexOptionsFilename(name);
            usearch_save(_index, indexPath, out IntPtr error);
            HandleError(error);
            SaveIndexOptions(indexOptionsPath);
            try
            {
                using (FileStream zipFileStream = new FileStream(path, mode))
                using (ZipArchive zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                {
                    zipArchive.CreateEntryFromFile(indexPath, GetIndexFilename(name));
                    zipArchive.CreateEntryFromFile(indexOptionsPath, GetIndexOptionsFilename(name));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding file to the zip archive: {ex.Message}");
            }
            File.Delete(indexPath);
            File.Delete(indexOptionsPath);
        }

        public IndexOptions GetIndexOptions()
        {
            return _indexOptions;
        }

        public void SaveIndexOptions(Stream fileStream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(fileStream, _indexOptions);
        }

        public void LoadIndexOptions(Stream fileStream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            IndexOptions indexOptions = (IndexOptions)formatter.Deserialize(fileStream);
            Init(indexOptions);
        }

        public void SaveIndexOptions(string path)
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Create))
                {
                    SaveIndexOptions(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing struct: {ex.Message}");
            }
        }

        public void LoadIndexOptions(string path)
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open))
                {
                    LoadIndexOptions(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing struct: {ex.Message}");
            }
        }

        public ulong Size()
        {
            ulong size = (ulong)usearch_size(this._index, out IntPtr error);
            HandleError(error);
            return size;
        }

        public ulong Capacity()
        {
            ulong capacity = (ulong)usearch_capacity(this._index, out IntPtr error);
            HandleError(error);
            return capacity;
        }

        public ulong Dimensions()
        {
            ulong dimensions = (ulong)usearch_dimensions(this._index, out IntPtr error);
            HandleError(error);
            return dimensions;
        }

        public ulong Connectivity()
        {
            ulong connectivity = (ulong)usearch_connectivity(this._index, out IntPtr error);
            HandleError(error);
            return connectivity;
        }

        public bool Contains(ulong key)
        {
            bool result = usearch_contains(this._index, key, out IntPtr error);
            HandleError(error);
            return result;
        }

        public int Count(ulong key)
        {
            int count = checked((int)usearch_count(this._index, key, out IntPtr error));
            HandleError(error);
            return count;
        }

        private void IncreaseCapacity(ulong size)
        {
            usearch_reserve(this._index, (UIntPtr)(this.Size() + size), out IntPtr error);
            HandleError(error);
        }

        private void CheckIncreaseCapacity(ulong size_increase)
        {
            ulong size_demand = this.Size() + size_increase;
            if (this.Capacity() < size_demand)
            {
                this.IncreaseCapacity(size_increase);
            }
        }

        public void Add(ulong key, float[] vector)
        {
            this.CheckIncreaseCapacity(1);
            usearch_add(this._index, key, vector, ScalarKind.Float32, out IntPtr error);
            HandleError(error);
        }

        public void Add(ulong key, double[] vector)
        {
            this.CheckIncreaseCapacity(1);
            usearch_add(this._index, key, vector, ScalarKind.Float64, out IntPtr error);
            HandleError(error);
        }

        public void Add(ulong[] keys, float[][] vectors)
        {
            this.CheckIncreaseCapacity((ulong)vectors.Length);
            for (int i = 0; i < vectors.Length; i++)
            {
                usearch_add(this._index, keys[i], vectors[i], ScalarKind.Float32, out IntPtr error);
                HandleError(error);
            }
        }

        public void Add(ulong[] keys, double[][] vectors)
        {
            this.CheckIncreaseCapacity((ulong)vectors.Length);
            for (int i = 0; i < vectors.Length; i++)
            {
                usearch_add(this._index, keys[i], vectors[i], ScalarKind.Float64, out IntPtr error);
                HandleError(error);
            }
        }

        public int Get(ulong key, out float[] vector)
        {
            vector = new float[this._cachedDimensions];
            int foundVectorsCount = checked((int)usearch_get(this._index, key, (UIntPtr)1, vector, ScalarKind.Float32, out IntPtr error));
            HandleError(error);
            if (foundVectorsCount < 1)
            {
                vector = null;
            }

            return foundVectorsCount;
        }

        public int Get(ulong key, int count, out float[][] vectors)
        {
            var flattenVectors = new float[count * (int)this._cachedDimensions];
            int foundVectorsCount = checked((int)usearch_get(this._index, key, (UIntPtr)count, flattenVectors, ScalarKind.Float32, out IntPtr error));
            HandleError(error);
            if (foundVectorsCount < 1)
            {
                vectors = null;
            }
            else
            {
                vectors = new float[foundVectorsCount][];
                for (int i = 0; i < foundVectorsCount; i++)
                {
                    vectors[i] = new float[this._cachedDimensions];
                    Array.Copy(flattenVectors, i * (int)this._cachedDimensions, vectors[i], 0, (int)this._cachedDimensions);
                }
            }

            return foundVectorsCount;
        }

        public int Get(ulong key, out double[] vector)
        {
            vector = new double[this._cachedDimensions];
            int foundVectorsCount = checked((int)usearch_get(this._index, key, (UIntPtr)1, vector, ScalarKind.Float64, out IntPtr error));
            HandleError(error);
            if (foundVectorsCount < 1)
            {
                vector = null;
            }

            return foundVectorsCount;
        }

        public int Get(ulong key, int count, out double[][] vectors)
        {
            var flattenVectors = new double[count * (int)this._cachedDimensions];
            int foundVectorsCount = checked((int)usearch_get(this._index, key, (UIntPtr)count, flattenVectors, ScalarKind.Float64, out IntPtr error));
            HandleError(error);
            if (foundVectorsCount < 1)
            {
                vectors = null;
            }
            else
            {
                vectors = new double[foundVectorsCount][];
                for (int i = 0; i < foundVectorsCount; i++)
                {
                    vectors[i] = new double[this._cachedDimensions];
                    Array.Copy(flattenVectors, i * (int)this._cachedDimensions, vectors[i], 0, (int)this._cachedDimensions);
                }
            }

            return foundVectorsCount;
        }

        private int Search<T>(T[] queryVector, int count, out ulong[] keys, out float[] distances, ScalarKind scalarKind)
        {
            keys = new ulong[count];
            distances = new float[count];

            GCHandle handle = GCHandle.Alloc(queryVector, GCHandleType.Pinned);
            int matches = 0;
            try
            {
                IntPtr queryVectorPtr = handle.AddrOfPinnedObject();
                matches = checked((int)usearch_search(this._index, queryVectorPtr, scalarKind, (UIntPtr)count, keys, distances, out IntPtr error));
                HandleError(error);
            }
            finally
            {
                handle.Free();
            }

            if (matches < count)
            {
                Array.Resize(ref keys, (int)matches);
                Array.Resize(ref distances, (int)matches);
            }

            return matches;
        }

        public int Search(float[] queryVector, int count, out ulong[] keys, out float[] distances)
        {
            return this.Search(queryVector, count, out keys, out distances, ScalarKind.Float32);
        }

        public int Search(double[] queryVector, int count, out ulong[] keys, out float[] distances)
        {
            return this.Search(queryVector, count, out keys, out distances, ScalarKind.Float64);
        }

        public int Remove(ulong key)
        {
            int removedCount = checked((int)usearch_remove(this._index, key, out IntPtr error));
            HandleError(error);
            return removedCount;
        }

        public int Rename(ulong keyFrom, ulong keyTo)
        {
            int foundVectorsCount = checked((int)usearch_rename(this._index, keyFrom, keyTo, out IntPtr error));
            HandleError(error);
            return foundVectorsCount;
        }

        private static void HandleError(IntPtr error)
        {
            if (error != IntPtr.Zero)
            {
                throw new USearchException($"USearch operation failed: {Marshal.PtrToStringAnsi(error)}");
            }
        }

        private void FreeIndex()
        {
            if (this._index != IntPtr.Zero)
            {
                usearch_free(this._index, out IntPtr error);
                HandleError(error);
                this._index = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                this.FreeIndex();
                this._disposedValue = true;
            }
        }

        ~USearchIndex() => this.Dispose(false);
    }
}
