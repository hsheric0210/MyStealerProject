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
using Serilog;
using Snappy;
using System.Runtime.Remoting.Messaging;
using System.Collections.Immutable;
using MyStealer.Utils.Chromium.LevelDb;

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

    // See: https://github.com/google/leveldb/blob/master/doc/table_format.md
    //     A BlockHandle contains an offset and length of a block in an ldb table file
    public class BlockHandle
    {
        public int Offset { get; }

        public int Length { get; }

        public BlockHandle(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }

        public static BlockHandle FromStream(Stream stream) => new BlockHandle(stream.ReadVarInt(), stream.ReadVarInt());

        public static BlockHandle FromBytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
                return FromStream(stream);
        }
    }

    // Raw key, value for a record in a LDB file Block, along with the offset within the block from which it came from
    //     See: https://github.com/google/leveldb/blob/master/doc/table_format.md
    public class RawBlockEntry
    {
        public byte[] Key { get; }

        public byte[] Value { get; }

        public long BlockOffset { get; }

        public RawBlockEntry(byte[] key, byte[] value, long blockOffset)
        {
            Key = key;
            Value = value;
            BlockOffset = blockOffset;
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
        public byte[] Key { get; }

        public byte[] Value { get; }

        public ulong Seq { get; }

        public KeyState State { get; }

        public FileType FileType { get; }

        public string OriginFile { get; }

        public long Offset { get; }

        public bool IsCompressed { get; }

        public Record(byte[] key, byte[] value, ulong seq, KeyState state, FileType fileType, string originFile, long offset, bool isCompressed)
        {
            Key = key;
            Value = value;
            Seq = seq;
            State = state;
            FileType = fileType;
            OriginFile = originFile;
            Offset = offset;
            IsCompressed = isCompressed;
        }

        // Returns the "userkey" which omits the metadata bytes which may reside at the end of the raw key
        public byte[] UserKey
        {
            get
            {
                if (FileType == FileType.Ldb && Key.Length >= 8)
                {
                    var buffer = new byte[Key.Length - 8];
                    Buffer.BlockCopy(Key, 0, buffer, 0, buffer.Length);
                    return buffer;
                }

                return Key;
            }
        }

        public static Record LdbRecord(byte[] key, byte[] value, string originFile, long offset, bool isCompressed)
        {
            var state = KeyState.Unknown;

            // seq = (struct.unpack("<Q", key[-8:])[0]) >> 8
            var seq = key.ToLeUInt64(key.Length - 8) >> 8;

            if (key.Length > 8)
                state = key[key.Length - 8] == 0 ? KeyState.Deleted : KeyState.Live;

            return new Record(key, value, seq, state, FileType.Ldb, originFile, offset, isCompressed);
        }

        public static Record LogRecord(byte[] key, byte[] value, ulong seq, KeyState state, string origin_file, long offset)
            => new Record(key, value, seq, state, FileType.Log, origin_file, offset, false);
    }

    // Block from an .lldb (table) file. See: https://github.com/google/leveldb/blob/master/doc/table_format.md
    public class Block : IEnumerable<RawBlockEntry>
    {
        public byte[] RawData { get; }

        internal int RestartArrayOffset { get; }

        internal int RestartArrayCount { get; }

        public int Offset { get; }

        public LdbFile Origin { get; }

        public bool IsCompressed { get; }

        public Block(byte[] rawdata, bool isCompressed, LdbFile origin, int offset)
        {
            RawData = rawdata;
            IsCompressed = isCompressed;
            Origin = origin;
            Offset = offset;
            RestartArrayCount = (int)RawData.ToLeUInt32(RawData.Length - 4);
            RestartArrayOffset = RawData.Length - (RestartArrayCount + 1) * 4;
        }

        public int GetRestartOffset(int index) => RawData.ToLeInt32(RestartArrayOffset + index * 4);

        public int GetFirstEntryOffset() => GetRestartOffset(0);

        public IEnumerator<RawBlockEntry> GetEnumerator()
        {
            var offset = GetFirstEntryOffset();
            using (var stream = new MemoryStream(RawData))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                var key = Array.Empty<byte>();
                while (stream.Position < RestartArrayOffset)
                {
                    var start_offset = stream.Position;
                    var shared_length = stream.ReadVarInt(is_google_32bit: true);
                    var non_shared_length = stream.ReadVarInt(is_google_32bit: true);
                    var value_length = stream.ReadVarInt(is_google_32bit: true);

                    // sense check
                    if (offset >= RestartArrayOffset)
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
    public class LdbFile : DbFile<Record>
    {
        private readonly ImmutableArray<(byte[], BlockHandle)> index;

        private readonly BlockHandle indexHandle;

        private readonly BlockHandle metaIndexHandle;

        public static readonly int BLOCK_TRAILER_SIZE = 5;

        public static readonly long FOOTER_SIZE = 48;

        public static readonly ulong MAGIC = 0xdb4775248b80fb57;

        public LdbFile(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);

            FilePath = file;

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!int.TryParse(fileName, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out var fileNumber))
                throw new ArgumentException("Invalid ldb file name: " + fileName);
            FileNumber = fileNumber;

            Stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Stream.Seek(-FOOTER_SIZE, SeekOrigin.End);

            metaIndexHandle = BlockHandle.FromStream(Stream);
            indexHandle = BlockHandle.FromStream(Stream);

            Stream.Seek(-8, SeekOrigin.End);

            var magic = Stream.ReadLeUInt64();
            if (magic != MAGIC)
                throw new Exception($"Invalid magic number in {file}");
            index = ReadIndex();
        }

        private Block ReadBlock(BlockHandle handle)
        {
            // block is the size in the blockhandle plus the trailer
            // the trailer is 5 bytes long.
            // idx  size  meaning
            // 0    1     CompressionType (0 = none, 1 = snappy)
            // 1    4     CRC32
            Stream.Seek(handle.Offset, SeekOrigin.Begin);

            var rawBlock = Stream.ReadBytes(handle.Length);
            var trailer = Stream.ReadBytes(BLOCK_TRAILER_SIZE);
            if (rawBlock.Length != handle.Length || trailer.Length != BLOCK_TRAILER_SIZE)
                throw new Exception($"Could not read all of the block at offset {handle.Offset} in file {FilePath}");

            var isCompressed = trailer[0] != 0;
            if (isCompressed)
            {
                var buffer = new byte[SnappyCodec.GetUncompressedLength(rawBlock, 0, rawBlock.Length)];
                var written = SnappyCodec.Uncompress(rawBlock, 0, rawBlock.Length, buffer, 0);
                if (written != buffer.Length)
                    throw new Exception("Snappy decompression length mismatched");

                rawBlock = buffer;
            }

            return new Block(rawBlock, isCompressed, this, handle.Offset);
        }

        private ImmutableArray<(byte[], BlockHandle)> ReadIndex()
        {
            var index_block = ReadBlock(indexHandle);

            // key is earliest key, value is BlockHandle to that data block
            return (from entry in index_block
                    select (entry.Key, BlockHandle.FromBytes(entry.Value))).ToImmutableArray();
        }

        // Iterate Records in this Table file
        public override IEnumerator<Record> GetEnumerator()
        {
            foreach (var blockEntry in index)
            {
                var block = ReadBlock(blockEntry.Item2);
                foreach (var entry in block)
                    yield return Record.LdbRecord(entry.Key, entry.Value, FilePath, block.IsCompressed ? block.Offset : block.Offset + entry.BlockOffset, block.IsCompressed);
            }
        }

        public override void Dispose() => Stream.Dispose();
    }

    public enum LogEntryType
    {
        Zero = 0,

        Full = 1,

        First = 2,

        Middle = 3,

        Last = 4
    }

    public abstract class DbFile<T> : IDisposable, IEnumerable<T>
    {
        public const int LOG_ENTRY_HEADER_SIZE = 7;

        public const int LOG_BLOCK_SIZE = 32768;

        protected Stream Stream { get; set; }

        public int FileNumber { get; protected set; }

        public string FilePath { get; protected set; }

        public abstract void Dispose();

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected IEnumerable<byte[]> GetRawBlocks()
        {
            byte[] chunk;
            Stream.Seek(0, SeekOrigin.Begin);
            while ((chunk = Stream.ReadBytes(LOG_BLOCK_SIZE)).Length == LOG_BLOCK_SIZE)
                yield return chunk;
        }

        protected IEnumerable<(long, byte[])> GetBatches()
        {
            var in_record = false;
            var start_block_offset = 0L;
            var block = new byte[] { };
            var idx = -1;
            foreach (var chunk in GetRawBlocks())
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
                                throw new Exception($"Full block whilst still building a block at offset {idx * LOG_BLOCK_SIZE + buff.Position} in {FilePath}");

                            in_record = false;
                            yield return (idx * LOG_BLOCK_SIZE + buff.Position, buff.ReadBytes(length));
                        }
                        else if (block_type == LogEntryType.First)
                        {
                            if (in_record)
                                throw new Exception($"First block whilst still building a block at offset {idx * LOG_BLOCK_SIZE + buff.Position} in {FilePath}");

                            start_block_offset = idx * LOG_BLOCK_SIZE + buff.Position;
                            block = buff.ReadBytes(length);
                            in_record = true;
                        }
                        else if (block_type == LogEntryType.Middle)
                        {
                            if (!in_record)
                                throw new Exception($"Middle block whilst not building a block at offset {idx * LOG_BLOCK_SIZE + buff.Position} in {FilePath}");

                            block.Append(buff.ReadBytes(length));
                        }
                        else if (block_type == LogEntryType.Last)
                        {
                            if (!in_record)
                                throw new Exception($"Last block whilst not building a block at offset {idx * LOG_BLOCK_SIZE + buff.Position} in {FilePath}");

                            block.Append(buff.ReadBytes(length));
                            in_record = false;
                            yield return (start_block_offset * LOG_BLOCK_SIZE, block);
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
    }

    // A levelDb log (.log) file
    public class LogFile : DbFile<Record>
    {
        public LogFile(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);
            FilePath = file;

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!int.TryParse(fileName, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out var fileNumber))
                throw new ArgumentException("Invalid file name: " + fileName);
            FileNumber = fileNumber;

            Stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        // Iterate Records in this Log file
        public override IEnumerator<Record> GetEnumerator()
        {
            foreach (var entry in GetBatches())
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
                        var key_length = buff.ReadVarInt(is_google_32bit: true);
                        var key = buff.ReadBytes(key_length);
                        byte[] value;
                        // print(key)
                        if (state != KeyState.Deleted)
                        {
                            var value_length = buff.ReadVarInt(is_google_32bit: true);
                            value = buff.ReadBytes(value_length);
                        }
                        else
                        {
                            value = Array.Empty<byte>();
                        }
                        yield return Record.LogRecord(key, value, seq + (ulong)i, state, FilePath, start_offset);
                    }
                }
            }
        }

        public override void Dispose() => Stream.Dispose();
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
        public readonly struct CompactionPointer
        {
            public int Level { get; }
            public byte[] Pointer { get; }

            public CompactionPointer(int level, byte[] pointer)
            {
                Level = level;
                Pointer = pointer;
            }
        }

        public readonly struct DeletedFile
        {
            public int Level { get; }
            public int FileNumber { get; }

            public DeletedFile(int level, int fileNumber)
            {
                Level = level;
                FileNumber = fileNumber;
            }
        }

        public readonly struct NewFile
        {
            public int Level { get; }
            public int FileNumber { get; }
            public int FileSize { get; }
            public byte[] SmallestKey { get; }
            public byte[] LargestKey { get; }

            public NewFile(int level, int file_no, int file_size, byte[] smallest_key, byte[] largest_key)
            {
                Level = level;
                FileNumber = file_no;
                FileSize = file_size;
                SmallestKey = smallest_key;
                LargestKey = largest_key;
            }
        }

        public string Comparator { get; }

        public int LogNumber { get; }

        public int PrevLogNumber { get; }

        public int LastSequence { get; }

        public int NextFileNumber { get; }

        public IImmutableList<CompactionPointer> CompactionPointers { get; }

        public IImmutableList<DeletedFile> DeletedFiles { get; }

        public IImmutableList<NewFile> NewFiles { get; }

        public VersionEdit(
            string comparator,
            int logNumber,
            int prevLogNumber,
            int lastSequence,
            int nextFileNumber,
            IImmutableList<CompactionPointer> compactionPointers,
            IImmutableList<DeletedFile> deletedFiles,
            IImmutableList<NewFile> newFiles)
        {
            Comparator = comparator;
            LogNumber = logNumber;
            PrevLogNumber = prevLogNumber;
            LastSequence = lastSequence;
            NextFileNumber = nextFileNumber;
            CompactionPointers = compactionPointers;
            DeletedFiles = deletedFiles;
            NewFiles = newFiles;
        }

        public static VersionEdit FromBuffer(byte[] buffer)
        {
            string comparator = null;
            var log_number = 0;
            var prev_log_number = 0;
            var last_sequence = 0;
            var next_file_number = 0;
            var compaction_pointers = ImmutableList.CreateBuilder<CompactionPointer>();
            var deleted_files = ImmutableList.CreateBuilder<DeletedFile>();
            var new_files = ImmutableList.CreateBuilder<NewFile>();

            using (var b = new MemoryStream(buffer))
            {
                while (b.Position < buffer.Length - 1)
                {
                    var tag = (VersionEditTag)b.ReadVarInt(is_google_32bit: true);
                    if (tag == VersionEditTag.Comparator)
                    {
                        comparator = Encoding.UTF8.GetString(b.ReadPrefixedBlob());
                    }
                    else if (tag == VersionEditTag.LogNumber)
                    {
                        log_number = b.ReadVarInt();
                    }
                    else if (tag == VersionEditTag.PrevLogNumber)
                    {
                        prev_log_number = b.ReadVarInt();
                    }
                    else if (tag == VersionEditTag.NextFileNumber)
                    {
                        next_file_number = b.ReadVarInt();
                    }
                    else if (tag == VersionEditTag.LastSequence)
                    {
                        last_sequence = b.ReadVarInt();
                    }
                    else if (tag == VersionEditTag.CompactPointer)
                    {
                        var level = b.ReadVarInt(is_google_32bit: true);
                        var compaction_pointer = b.ReadPrefixedBlob();
                        compaction_pointers.Add(new CompactionPointer(level, compaction_pointer));
                    }
                    else if (tag == VersionEditTag.DeletedFile)
                    {
                        var level = b.ReadVarInt(is_google_32bit: true);
                        var file_no = b.ReadVarInt();
                        deleted_files.Add(new DeletedFile(level, file_no));
                    }
                    else if (tag == VersionEditTag.NewFile)
                    {
                        var level = b.ReadVarInt(is_google_32bit: true);
                        var fileNo = b.ReadVarInt();
                        var fileSize = b.ReadVarInt();
                        var smallest = b.ReadPrefixedBlob();
                        var largest = b.ReadPrefixedBlob();
                        new_files.Add(new NewFile(level, fileNo, fileSize, smallest, largest));
                    }
                }
            }

            return new VersionEdit(comparator, log_number, prev_log_number, last_sequence, next_file_number, compaction_pointers.ToImmutable(), deleted_files.ToImmutable(), new_files.ToImmutable());
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
    public class ManifestFile : DbFile<VersionEdit>
    {
        public static readonly Regex MANIFEST_FILENAME_PATTERN = new Regex("MANIFEST-([0-9A-F]{6})", RegexOptions.Compiled);

        public IImmutableDictionary<int, int> FileToLevel { get; }

        public ManifestFile(string path)
        {
            FilePath = path;

            var fileName = Path.GetFileNameWithoutExtension(path);
            Match match;
            if ((match = MANIFEST_FILENAME_PATTERN.Match(fileName)).Success && int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out var fileNumber))
                FileNumber = fileNumber;
            else
                throw new Exception("Invalid name for Manifest: " + fileName);

            Stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var fileToLevel = ImmutableDictionary.CreateBuilder<int, int>();
            foreach (var edit in this)
            {
                foreach (var nf in edit.NewFiles)
                    fileToLevel[nf.FileNumber] = nf.Level;
            }

            FileToLevel = fileToLevel.ToImmutable();
        }

        public override IEnumerator<VersionEdit> GetEnumerator()
        {
            foreach (var entry in GetBatches())
            {
                yield return VersionEdit.FromBuffer(entry.Item2);
            }
        }

        public override void Dispose() => Stream.Dispose();
    }

    public class RawLevelDb : IDisposable, IEnumerable<Record>
    {
        public static readonly Regex DATA_FILE_PATTERN = new Regex(@"[0-9]{6}\.(ldb|log|sst)", RegexOptions.Compiled);

        private readonly IImmutableList<DbFile<Record>> recordFiles;

        public string Directory { get; }

        public ManifestFile Manifest { get; }

        public RawLevelDb(string directory)
        {
            if (!File.GetAttributes(directory).HasFlag(FileAttributes.Directory))
                throw new IOException(directory + " is not a directory");

            this.Directory = directory;

            var recordFiles = ImmutableList.CreateBuilder<DbFile<Record>>();

            var latest_manifest = (0, "");
            foreach (var file in System.IO.Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(file);
                if (DATA_FILE_PATTERN.IsMatch(fileName))
                {
                    var fileExt = Path.GetExtension(file).ToLowerInvariant();
                    if (fileExt == ".log")
                    {
                        recordFiles.Add(new LogFile(file));
                    }
                    else if (fileExt == ".ldb" || fileExt == ".sst")
                    {
                        recordFiles.Add(new LdbFile(file));
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

            this.recordFiles = recordFiles.ToImmutable();
            Manifest = !string.IsNullOrEmpty(latest_manifest.Item2) ? new ManifestFile(latest_manifest.Item2) : null;
        }

        public IEnumerator<Record> GetEnumerator()
        {
            foreach (var file_containing_records in recordFiles.OrderBy(x => x.FileNumber))
            {
                foreach (var record in file_containing_records)
                    yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            foreach (var file in recordFiles)
                file.Dispose();

            Manifest?.Dispose();
        }
    }
}
