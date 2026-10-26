using System;
using System.Collections.Generic;

namespace PackageBank.Core;

/// <summary>
/// Table data looked up once per id and kept while its scope (the loaded tables, the language, whatever else it was
/// built from) stays the same; a new scope clears it, so it never answers from another game's or language's tables.
/// </summary>
public sealed class KeyedMemo<TScope, TValue>
{
    private readonly Dictionary<int, TValue> _byId = new();
    private readonly Func<int, TValue> _make;
    private TScope _scope = default!;
    private bool _hasScope;

    public KeyedMemo(Func<int, TValue> make)
    {
        _make = make;
    }

    public int Count => _byId.Count;

    public TValue Get(TScope scope, int id)
    {
        if (!_hasScope || !EqualityComparer<TScope>.Default.Equals(scope, _scope))
        {
            _byId.Clear();
            _scope = scope;
            _hasScope = true;
        }

        if (!_byId.TryGetValue(id, out var value)) _byId[id] = value = _make(id);

        return value;
    }
}

/// <summary>Mixes numbers into one 64-bit print (FNV-1a over each number's bytes), with no allocation.</summary>
public struct ChangePrint
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    private ulong _hash;
    private bool _isStarted;

    public void Add(long number)
    {
        if (!_isStarted)
        {
            _hash = Offset;
            _isStarted = true;
        }

        var bits = unchecked((ulong)number);

        for (var i = 0; i < 8; i++)
        {
            _hash = unchecked((_hash ^ (bits & 0xFF)) * Prime);
            bits >>= 8;
        }
    }

    public void Add(bool flag) => Add(flag ? 1L : 0L);

    public readonly long Value => unchecked((long)(_isStarted ? _hash : Offset));
}
