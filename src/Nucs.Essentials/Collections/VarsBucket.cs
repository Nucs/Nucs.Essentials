using System;
using System.Collections.Generic;

namespace Nucs.Collections;

/// <summary>
///     A dynamic bucket of values wrapping around Dictionary&lt;string, object&gt;.
/// </summary>
public sealed class VarsBucket : IDisposable {
    private readonly Dictionary<string, object> _data;

    public VarsBucket() {
        _data = new(0);
    }

    public VarsBucket(int capacity) {
        _data = new(capacity);
    }

    public object this[string key] {
        get {
            _data.TryGetValue(key, out var value);
            return value;
        }
        set => _data[key] = value;
    }

    public object Get(string key) {
        _data.TryGetValue(key, out var value);
        return value;
    }

    public bool Has(string key) {
        return _data.ContainsKey(key);
    }

    public bool Remove(string key) {
        return _data.Remove(key, out var value);
    }

    public bool Remove(string key, out object? value) {
        return _data.Remove(key, out value);
    }

    public object? TryGet(string key) {
        _data.TryGetValue(key, out var res);
        return res;
    }

    public T? Get<T>(string key) {
        _data.TryGetValue(key, out var value);
        return (T?) value;
    }

    public T? Get<T>(string key, T? @default) {
        if (!_data.TryGetValue(key, out var value))
            return @default;
        return (T?) value;
    }

    public T? TryGet<T>(string key) {
        _data.TryGetValue(key, out var res);
        return (T?) res;
    }

    public void OnEndOfDay() {
        _data.Clear();
    }

    public void Dispose() {
        _data.Clear();
    }

    public void AddOrSet(VarsBucket values) {
        foreach (var o in values._data) {
            _data[o.Key] = o.Value;
        }
    }

    public void Add(VarsBucket values) {
        foreach (var o in values._data) {
            _data.Add(o.Key, o.Value);
        }
    }
}