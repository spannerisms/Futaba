namespace Futaba;

internal readonly struct SpannedLookup<T> where T : notnull {
	private readonly Dictionary<string, T> _dict;
	private readonly Dictionary<string, T>.AlternateLookup<CharSpan> _alt;

	public int Count => _dict.Count;

	public SpannedLookup() : this(0) { }

	public SpannedLookup(int capacity) {
		_dict = new(capacity);
		_alt = _dict.GetAlternateLookup<CharSpan>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T? GetRefOrDefault(string name) {
		return ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, name, out _);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T? GetRefOrDefault(CharSpan name) {
		return ref CollectionsMarshal.GetValueRefOrAddDefault(_alt, name, out _);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ContainsKey(string name) {
		return _dict.ContainsKey(name);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ContainsKey(CharSpan name) {
		return _alt.ContainsKey(name);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetValue(string name, [MaybeNullWhen(false)] out T item) {
		return _dict.TryGetValue(name, out item!);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetValue(CharSpan name, [MaybeNullWhen(false)] out T item) {
		return _alt.TryGetValue(name, out item!);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(string name, T value) {
		_dict[name] = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear() {
		_dict.Clear();
	}

	public void CopyEntries(Dictionary<string, T> source) {
		foreach ((string k, T v) in source) {
			_dict.Add(k, v);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Dictionary<string, T>.Enumerator GetEnumerator() {
		return _dict.GetEnumerator();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Dictionary<string, T> GetCopy() {
		return new(_dict);
	}
}
