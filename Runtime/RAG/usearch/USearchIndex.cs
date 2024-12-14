using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using static Cloud.Unum.USearch.NativeMethods;

namespace Cloud.Unum.USearch
{
    /// <summary>
    /// USearchIndex class provides a managed wrapper for the USearch library's index functionality.
    /// </summary>
    public class USearchIndex : IDisposable
    {
        private IntPtr _index;
        private bool _disposedValue = false;
        private ulong _cachedDimensions;

        public USearchIndex(
            MetricKind metricKind,
            ScalarKind quantization,
            ulong dimensions,
            ulong connectivity = 0,
            ulong expansionAdd = 0,
            ulong expansionSearch = 0,
            bool multi = false
                //CustomDistanceFunction? customMetric = null
        )
        {
            IndexOptions initOptions = new()
            {
                metric_kind = metricKind,
                metric = default,
                quantization = quantization,
                dimensions = dimensions,
                connectivity = connectivity,
                expansion_add = expansionAdd,
                expansion_search = expansionSearch,
                multi = multi
            };

            this._index = usearch_init(ref initOptions, out IntPtr error);
            HandleError(error);
            this._cachedDimensions = dimensions;
        }

        public USearchIndex(IndexOptions options)
        {
            this._index = usearch_init(ref options, out IntPtr error);
            HandleError(error);
            this._cachedDimensions = options.dimensions;
        }

        public USearchIndex(string path, bool view = false)
        {
            IndexOptions initOptions = new();
            this._index = usearch_init(ref initOptions, out IntPtr error);
            HandleError(error);

            if (view)
            {
                usearch_view(this._index, path, out error);
            }
            else
            {
                usearch_load(this._index, path, out error);
            }

            HandleError(error);

            this._cachedDimensions = this.Dimensions();
        }

        public void Save(string path)
        {
            usearch_save(this._index, path, out IntPtr error);
            HandleError(error);
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

        //========================== Additional methods from LLMUnity ==========================//

        public static Func<int, int> FilterFunction;

        private static readonly object filterLock = new object();

        [MonoPInvokeCallback(typeof(NativeMethods.FilterCallback))]
        public static int StaticFilter(int key, System.IntPtr filterState)
        {
            if (FilterFunction != null) return FilterFunction(key);
            return 1;
        }

        private int Search<T>(T[] queryVector, int count, out ulong[] keys, out float[] distances, ScalarKind scalarKind, Func<int, int> filter = null)
        {
            keys = new ulong[count];
            distances = new float[count];

            GCHandle handle = GCHandle.Alloc(queryVector, GCHandleType.Pinned);
            int matches = 0;
            try
            {
                IntPtr queryVectorPtr = handle.AddrOfPinnedObject();
                IntPtr error;
                if (filter == null)
                {
                    matches = checked((int)usearch_search(this._index, queryVectorPtr, scalarKind, (UIntPtr)count, keys, distances, out error));
                }
                else
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        lock (filterLock)
                        {
                            FilterFunction = filter;
                            matches = checked((int)usearch_filtered_search(this._index, queryVectorPtr, scalarKind, (UIntPtr)count, StaticFilter, IntPtr.Zero, keys, distances, out error));
                        }
                    }
                    else
                    {
                        matches = checked((int)usearch_filtered_search(this._index, queryVectorPtr, scalarKind, (UIntPtr)count, (int key, IntPtr state) => filter(key), IntPtr.Zero, keys, distances, out error));
                    }
                }
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

        public int Search(float[] queryVector, int count, out ulong[] keys, out float[] distances, Func<int, int> filter = null)
        {
            return this.Search(queryVector, count, out keys, out distances, ScalarKind.Float32, filter);
        }

        public int Search(double[] queryVector, int count, out ulong[] keys, out float[] distances, Func<int, int> filter = null)
        {
            return this.Search(queryVector, count, out keys, out distances, ScalarKind.Float64, filter);
        }

        protected virtual string GetIndexFilename()
        {
            return "usearch/index";
        }

        public void Save(ZipArchive zipArchive)
        {
            string indexPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Save(indexPath);
            try
            {
                zipArchive.CreateEntryFromFile(indexPath, GetIndexFilename());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding file to the zip archive: {ex.Message}");
            }
            File.Delete(indexPath);
        }

        public void Load(ZipArchive zipArchive)
        {
            IndexOptions initOptions = new();
            this._index = usearch_init(ref initOptions, out IntPtr error);
            HandleError(error);

            try
            {
                ZipArchiveEntry entry = zipArchive.GetEntry(GetIndexFilename());
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
                        usearch_load_buffer(_index, unmanagedBuffer, (UIntPtr)managedBuffer.Length, out error);
                        HandleError(error);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading the search index: {ex.Message}");
            }

            this._cachedDimensions = this.Dimensions();
        }
    }
}
