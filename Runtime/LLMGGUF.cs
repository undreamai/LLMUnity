/// @file
/// @brief File implementing the GGUF reader.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace LLMUnity
{
    /// \cond HIDE
    public enum GGUFValueType
    {
        UINT8 = 0,
        INT8 = 1,
        UINT16 = 2,
        INT16 = 3,
        UINT32 = 4,
        INT32 = 5,
        FLOAT32 = 6,
        BOOL = 7,
        STRING = 8,
        ARRAY = 9,
        UINT64 = 10,
        INT64 = 11,
        FLOAT64 = 12
    }

    public class ReaderField
    {
        public int offset;
        public string name;
        public List<Array> parts = new List<Array>();
        public List<int> data = new List<int>();
        public List<GGUFValueType> types = new List<GGUFValueType>();
    }

    public class ReaderTensor
    {
        public string name;
        public GGUFValueType tensor_type;
        public uint[] shape;
        public int n_elements;
        public int n_bytes;
        public int data_offset;
        public Array data;
        public ReaderField field;
    }
    /// \endcond

    /// @ingroup utils
    /// <summary>
    /// Class implementing the GGUF reader.
    /// </summary>
    public class GGUFReader
    {
        private const uint GGUF_MAGIC = 0x46554747; // "GGUF"
        private const int GGUF_VERSION = 3;
        private readonly List<int> READER_SUPPORTED_VERSIONS = new List<int> { 2, GGUF_VERSION };
        private Dictionary<GGUFValueType, Type> gguf_scalar_to_np = new Dictionary<GGUFValueType, Type>
        {
            { GGUFValueType.UINT8, typeof(byte) },
            { GGUFValueType.INT8, typeof(sbyte) },
            { GGUFValueType.UINT16, typeof(ushort) },
            { GGUFValueType.INT16, typeof(short) },
            { GGUFValueType.UINT32, typeof(uint) },
            { GGUFValueType.INT32, typeof(int) },
            { GGUFValueType.FLOAT32, typeof(float) },
            { GGUFValueType.UINT64, typeof(ulong) },
            { GGUFValueType.INT64, typeof(long) },
            { GGUFValueType.FLOAT64, typeof(double) },
            { GGUFValueType.BOOL, typeof(bool) }
        };

        // private MemoryStream data;
        private FileStream data;
        /// <summary> Dictionary of GGUF fields to location info </summary>
        public Dictionary<string, ReaderField> fields = new Dictionary<string, ReaderField>();

        /// <summary>
        /// Constructor of the GGUF reader that parses a GGUF file and retrieves the fields.
        /// </summary>
        /// <param name="path">GGUF file path to read</param>
        public GGUFReader(string path)
        {
            // data = new MemoryStream(File.ReadAllBytes(path));
            data = new FileStream(path, FileMode.Open, FileAccess.Read);
            int offs = 0;

            if (BitConverter.ToUInt32(ReadBytes(offs, 4), 0) != GGUF_MAGIC)
                throw new ArgumentException("GGUF magic invalid");
            offs += 4;

            uint temp_version = BitConverter.ToUInt32(ReadBytes(offs, 4));
            if ((temp_version & 65535) == 0)
            {
                byte[] tempBytes = ReadBytes(offs, 4);
                Array.Reverse(tempBytes);
                temp_version = BitConverter.ToUInt32(tempBytes, 0);
            }
            uint version = temp_version;

            if (!READER_SUPPORTED_VERSIONS.Contains((int)version))
                throw new ArgumentException($"Sorry, file appears to be version {version} which we cannot handle");

            offs += PushField(new ReaderField { offset = offs, name = "GGUF.version", parts = new List<Array> { new uint[] { temp_version } }, data = new List<int> { 0 }, types = new List<GGUFValueType> { GGUFValueType.UINT32 } });
            ulong[] temp_counts = new ulong[2];
            Buffer.BlockCopy(ReadBytes(offs, 16), 0, temp_counts, 0, 16);
            offs += PushField(new ReaderField { offset = offs, name = "GGUF.tensor_count", parts = new List<Array> { new ulong[] { temp_counts[0] } }, data = new List<int> { 0 }, types = new List<GGUFValueType> { GGUFValueType.UINT64 } });
            offs += PushField(new ReaderField { offset = offs, name = "GGUF.kv_count", parts = new List<Array> { new ulong[] { temp_counts[1] } }, data = new List<int> { 0 }, types = new List<GGUFValueType> { GGUFValueType.UINT64 } });
            ulong tensor_count = temp_counts[0];
            ulong kv_count = temp_counts[1];
            offs = BuildFields(offs, (int)kv_count);
            data.Close();
        }

        /// <summary>
        /// Allows to retrieve location info for a GGUF field.
        /// </summary>
        /// <param name="key"> GGUF field to retrieve </param>
        /// <returns> Retrieved location info as ReaderField </returns>
        public ReaderField GetField(string key)
        {
            if (fields.TryGetValue(key, out ReaderField value))
                return value;
            return null;
        }

        /// <summary>
        /// Allows to retrieve a single-valued GGUF field.
        /// </summary>
        /// <param name="key"> GGUF field to retrieve </param>
        /// <returns> Retrieved location info as ReaderField </returns>
        public byte[] GetGenericField(string key)
        {
            ReaderField field = GetField(key);
            if (field == null || field.parts.Count == 0) return null;
            return (byte[])field.parts[field.parts.Count - 1];
        }

        /// <summary>
        /// Allows to retrieve a string GGUF field.
        /// </summary>
        /// <param name="key"> GGUF field to retrieve </param>
        /// <returns> Retrieved GGUF value </returns>
        public string GetStringField(string key)
        {
            byte[] value = GetGenericField(key);
            if (value == null) return null;
            return System.Text.Encoding.UTF8.GetString(value);
        }

        /// <summary>
        /// Allows to retrieve an integer GGUF field.
        /// </summary>
        /// <param name="key"> GGUF field to retrieve </param>
        /// <returns> Retrieved GGUF value </returns>
        public int GetIntField(string key)
        {
            byte[] value = GetGenericField(key);
            if (value == null) return -1;
            return BitConverter.ToInt32(value, 0);
        }

        private byte[] ReadBytes(int offset, int count)
        {
            byte[] buffer = new byte[count];
            data.Seek(offset, SeekOrigin.Begin);
            data.Read(buffer, 0, count);
            return buffer;
        }

        private int PushField(ReaderField field, bool skip_sum = false)
        {
            if (fields.ContainsKey(field.name))
                throw new ArgumentException($"Duplicate {field.name} already in list at offset {field.offset}");
            fields[field.name] = field;
            if (skip_sum)
                return 0;
            int sum = 0;
            for (int i = 0; i < field.parts.Count; i++)
            {
                Type partType = gguf_scalar_to_np[field.types[i]];
                sum += Marshal.SizeOf(partType) *  field.parts[i].Length;
            }
            return sum;
        }

        private (ulong[], byte[]) GetStr(int offset)
        {
            ulong slen = BitConverter.ToUInt64(ReadBytes(offset, 8));
            byte[] sdata = ReadBytes(offset + 8, (int)slen);
            return (new ulong[] { slen }, sdata);
        }

        private (int, List<Array>, List<int>, List<GGUFValueType>) GetFieldParts(int orig_offs, int raw_type)
        {
            int offs = orig_offs;
            List<GGUFValueType> types = new List<GGUFValueType>();
            types.Add((GGUFValueType)raw_type);
            // Handle strings.
            if ((GGUFValueType)raw_type == GGUFValueType.STRING)
            {
                (ulong[] slen, byte[] sdata) = GetStr(offs);
                List<Array> sparts = new List<Array> { slen, sdata };
                int size = slen.Length * sizeof(ulong) + sdata.Length;
                return (size, sparts, new List<int> { 1 }, types);
            }

            // Check if it's a simple scalar type.
            if (gguf_scalar_to_np.TryGetValue((GGUFValueType)raw_type, out Type nptype))
            {
                Array val = ReadBytes(offs, Marshal.SizeOf(nptype));
                int size = nptype == typeof(bool) ? 1 : Marshal.SizeOf(nptype);
                return (size, new List<Array> { val }, new List<int> { 0 }, types);
            }

            // Handle arrays.
            if ((GGUFValueType)raw_type == GGUFValueType.ARRAY)
            {
                int raw_itype = BitConverter.ToInt32(ReadBytes(offs, 4));
                offs += Marshal.SizeOf(typeof(int));

                ulong alen = BitConverter.ToUInt64(ReadBytes(offs, 8));
                offs += Marshal.SizeOf(typeof(ulong));

                List<Array> aparts = new List<Array> { BitConverter.GetBytes(raw_itype), BitConverter.GetBytes(alen) };
                List<int> data_idxs = new List<int>();

                for (int idx = 0; idx < (int)alen; idx++)
                {
                    (int curr_size, List<Array> curr_parts, List<int> curr_idxs, List<GGUFValueType> curr_types) = GetFieldParts(offs, raw_itype);
                    if (idx == 0)
                        types.AddRange(curr_types);

                    int idxs_offs = aparts.Count;
                    aparts.AddRange(curr_parts);
                    data_idxs.AddRange(new List<int>(curr_idxs.ConvertAll(i => i + idxs_offs)));
                    offs += curr_size;
                }
                return (offs - orig_offs, aparts, data_idxs, types);
            }
            // We can't deal with this one.
            throw new ArgumentException($"Unknown/unhandled field type {(GGUFValueType)raw_type}");
        }

        private int BuildFields(int offs, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int orig_offs = offs;
                (ulong[] kv_klen, byte[] kv_kdata) = GetStr(offs);
                offs += Marshal.SizeOf(typeof(ulong)) + kv_kdata.Length;

                int raw_kv_type = BitConverter.ToInt32(ReadBytes(offs, 4));
                offs += Marshal.SizeOf(typeof(int));
                List<Array> parts = new List<Array> { kv_klen, kv_kdata, BitConverter.GetBytes(raw_kv_type) };
                List<int> idxs_offs = new List<int> { parts.Count };

                (int field_size, List<Array> field_parts, List<int> field_idxs, List<GGUFValueType> field_types) = GetFieldParts(offs, raw_kv_type);
                if (field_size == -1)
                    continue;

                parts.AddRange(field_parts);
                ReaderField readerField = new ReaderField
                {
                    offset = orig_offs,
                    name = System.Text.Encoding.UTF8.GetString(kv_kdata),
                    parts = parts,
                    data = new List<int>(field_idxs.ConvertAll(idx => idx + idxs_offs[0])),
                    types = field_types
                };
                PushField(readerField, skip_sum: true);
                offs += field_size;
            }
            return offs;
        }
    }
}
