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

/// <summary>
/// Ported from ccl_leveldb.py using pytocs 2.0.0-3150cbcd42
/// Check https://github.com/cclgroupltd/ccl_chrome_indexeddb for the original source
/// 
/// Version: 0.2.1
/// Description: Module for reading the Chromium leveldb sessionstorage format
/// Contact: Alex Caithness
/// </summary>
public static class ccl_chromium_sessionstorage {
    public static string _NAMESPACE_PREFIX = new byte[] { (byte)'n', (byte)'a', (byte)'m', (byte)'e', (byte)'s', (byte)'p', (byte)'a', (byte)'c', (byte)'e', (byte)'-' };
    
    public static string _MAP_ID_PREFIX = new byte[] { (byte)'m', (byte)'a', (byte)'p', (byte)'-' };
    
    public static void log = null;
    
    public class SessionStoreValue {
        
        public object value;
        
        public object guid;
        
        public object leveldb_sequence_number;
    }
    
    public class SessionStoreDb {
        
        public object _deleted_keys;
        
        public Dictionary<object, object> _host_lookup;
        
        public object _ldb;
        
        public object _map_id_to_host;
        
        public List<object> _orphans;
        
        public SessionStoreDb(object in_dir) {
            object host;
            object split_key;
            object key;
            if (!in_dir.is_dir()) {
                throw new IOError("Input directory is not a directory");
            }
            this._ldb = CclLevelDb.RawLevelDb(in_dir);
            // If performance is a concern we should refactor this, but slow and steady for now
            // First collect the namespace (session/tab guid  + host) and map-ids together
            this._map_id_to_host = new Dictionary<object, object> {
            };
            this._deleted_keys = new HashSet<object>();
            foreach (var rec in this._ldb.iterate_records_raw()) {
                if (rec.user_key.startswith(_NAMESPACE_PREFIX)) {
                    if (rec.user_key == _NAMESPACE_PREFIX) {
                        continue;
                    }
                    try {
                        key = rec.user_key.decode("utf-8");
                    } catch (UnicodeDecodeError) {
                        Console.WriteLine($"Invalid namespace key: {rec.user_key}");
                        continue;
                    }
                    split_key = key.split("-", 2);
                    if (split_key.Count != 3) {
                        Console.WriteLine($"Invalid namespace key: {key}");
                        continue;
                    }
                    (_, guid, host) = split_key;
                    if (!host) {
                        continue;
                    }
                    // normalize host to lower just in case
                    host = host.lower();
                    var guid_host_pair = (guid, host);
                    if (rec.state == CclLevelDb.KeyState.Deleted) {
                        this._deleted_keys.add(guid_host_pair);
                    } else {
                        try {
                            var map_id = rec.value.decode("utf-8");
                        } catch (UnicodeDecodeError) {
                            Console.WriteLine($"Invalid namespace value: {key}");
                            continue;
                        }
                        if (!map_id) {
                            continue;
                        }
                        //if map_id in self._map_id_to_host_guid and self._map_id_to_host_guid[map_id] != guid_host_pair:
                        if (this._map_id_to_host.Contains(map_id) && this._map_id_to_host[map_id] != host) {
                            Console.WriteLine("Map ID Collision!");
                            Console.WriteLine($"map_id: {map_id}");
                            Console.WriteLine($"Old host: {self._map_id_to_host[map_id]}");
                            Console.WriteLine($"New host: {guid_host_pair}");
                            throw new ValueError("map_id collision");
                        } else {
                            this._map_id_to_host[map_id] = host;
                        }
                    }
                }
            }
            // freeze stuff
            this._map_id_to_host = MappingProxyType(this._map_id_to_host);
            this._deleted_keys = frozenset(this._deleted_keys);
            this._host_lookup = new Dictionary<object, object> {
            };
            this._orphans = new List<object>();
            foreach (var rec in this._ldb.iterate_records_raw()) {
                if (rec.user_key.startswith(_MAP_ID_PREFIX)) {
                    try {
                        key = rec.user_key.decode("utf-8");
                    } catch (UnicodeDecodeError) {
                        Console.WriteLine($"Invalid map id key: {rec.user_key}");
                        continue;
                    }
                    if (rec.state == CclLevelDb.KeyState.Deleted) {
                        continue;
                    }
                    split_key = key.split("-", 2);
                    if (split_key.Count != 3) {
                        Console.WriteLine($"Invalid map id key: {key}");
                        continue;
                    }
                    (_, map_id, ss_key) = split_key;
                    if (!split_key) {
                        // TODO what does it mean when there is no key here?
                        //      The value will also be a single number (encoded utf-8)
                        continue;
                    }
                    try {
                        var value = rec.value.decode("UTF-16-LE");
                    } catch (UnicodeDecodeError) {
                        Console.WriteLine($"Error decoding value for {key}");
                        Console.WriteLine($"Raw Value: {rec.value}");
                        continue;
                    }
                    host = this._map_id_to_host.get(map_id);
                    if (!host) {
                        this._orphans.append((ss_key, new SessionStoreValue(value, null, rec.seq)));
                    } else {
                        this._host_lookup.setdefault(host, new Dictionary<object, object> {
                        });
                        this._host_lookup[host].setdefault(ss_key, new List<object>());
                        this._host_lookup[host][ss_key].append(new SessionStoreValue(value, null, rec.seq));
                    }
                }
            }
        }
        
        // 
        //         :param item: either the host as a str or a tuple of the host and a key (both str)
        //         :return: if item is a str, returns true if that host is present, if item is a tuple of (str, str), returns True
        //             if that host and key pair are present
        //         
        public virtual bool @__contains__(object item) {
            if (item is str) {
                return this._host_lookup.Contains(item);
            } else if (item is tuple && item.Count == 2) {
                (host, key) = item;
                return this._host_lookup.Contains(host) && this._host_lookup[host].Contains(key);
            } else {
                throw new TypeError("item must be a string or a tuple of (str, str)");
            }
        }
        
        // 
        //         :return: yields the hosts present in this SessionStorage
        //         
        public virtual object iter_hosts() {
            yield return this._host_lookup.keys();
        }
        
        // 
        //         :param host: the host (domain name) for the session storage
        //         :return: a dictionary where the keys are storage keys and the values are tuples of SessionStoreValue objects
        //             for that key. Multiple values may be returned as deleted or old values may be recovered.
        //         
        public virtual object get_all_for_host(string host) {
            if (!this.Contains(host)) {
                return new Dictionary<object, object> {
                };
            }
            var result_raw = new dict(this._host_lookup[host]);
            foreach (var ss_key in result_raw) {
                result_raw[ss_key] = tuple(result_raw[ss_key]);
            }
            return result_raw;
        }
        
        // 
        //         :param host: the host (domain name) for the session storage
        //         :param key: the storage key
        //         :return: a tuple of SessionStoreValue matching the host and key. Multiple values may be returned as deleted or
        //             old values may be recovered.
        //         
        public virtual object get_session_storage_key(object host, object key) {
            if (!this.Contains((host, key))) {
                return tuple();
            }
            return tuple(this._host_lookup[host][key]);
        }
        
        // 
        //         Returns records which have been orphaned from their host (domain name) where it cannot be recovered. The keys
        //             may be named uniquely enough that the host may be inferred.
        //         :return: yields tuples of (session key, SessionStoreValue)
        //         
        public virtual object iter_orphans() {
            yield return this._orphans;
        }
        
        public virtual object @__getitem__(object item) {
            if (!this.Contains(item)) {
                throw new KeyError(item);
            }
            if (item is str) {
                return this.get_all_for_host(item);
            } else if (item is tuple && item.Count == 2) {
                return this.get_session_storage_key(item);
            } else {
                throw new TypeError("item must be a string or a tuple of (str, str)");
            }
        }
        
        // 
        //         iterates the hosts present
        //         
        public virtual object @__iter__() {
            return this.iter_hosts();
        }
    }
}
