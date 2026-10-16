namespace Futaba;

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe readonly struct OperandString {
	private readonly char* _start;
	private readonly char* _end;

	public readonly char* Start => _start;
	public readonly char* End => _end;

	public readonly int Length => (int) (_end - _start);

	public readonly char OnlyChar {
		get {
			char* test = _end - 1;
			return test == _start ? *test : BadChar;
		}
	}

	/// <summary>
	/// Returns the character at the given index
	/// or <see cref="BadChar"/> if the index is not valid.
	/// </summary>
	public readonly char this[int i] {
		get {
			if ((uint) i >= (uint) Length) {
				return BadChar;
			} else {
				return _start[i];
			}
		}
	}

	/// <inheritdoc cref="this[int]"/>
	public readonly char this[Index i] {
		get {
			int len = Length;
			int offset = i.GetOffset(len);

			if ((uint) offset >= (uint) len) {
				return BadChar;
			} else {
				return _start[offset];
			}
		}
	}

	public readonly char First => _start >= _end ? BadChar : *_start;

	public readonly char Last => _start >= _end ? BadChar : _end[-1];

	public readonly bool IsEmpty => _start >= _end;

	public readonly bool IsNull => _start == default;

	public OperandString() {
		_start = (char*) null;
		_end = (char*) null;
	}

	/// <summary>
	/// No bounds check. This should be done by other routines
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal OperandString(char* start, char* end) {
		_start = start;
		_end = end;
	}

	/// <summary>
	/// No bounds check. This should be done by other routines
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal OperandString(char* start, int length) {
		_start = start;
		_end = start + length;
	}

	/// <summary>
	/// No bounds checking. Assumes it's already been done.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public char GetUnchecked(int i) => _start[i];

	public readonly bool OnlyCharIs(char c) => ((_end - 1) == _start) && _start[0] == c;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void FastLower(scoped Span<char> dest) {
		_ = Ascii.ToLower(this, dest, out _);
	}

	public static OperandString CreateTrimmed(char* start, char* end) {
		if (start >= end) {
			return default;
		}

		FastRead.TrimWhiteSpace(ref start, ref end);

		return new(start, end);
	}

	public bool SplitByComma(out OperandString left, out OperandString right) {
		var splitter = new StringSplitter(_start, _end);

		// we want more commas for the second operand
		if (splitter.GetNextComma(out left)) {
			// now the opposite
			// if there's more commas, that's bad
			return !splitter.GetNextComma(out right);
		} else {
			right = default;
			return false;
		}
	}

	public bool HasIndexer(char token, out char register, out OperandString newSlice) {
		char* search = _end;
		char* thestart = _start;

		while (--search > thestart && search->IsSkippableSpace) ;

		if (search <= thestart) {
			newSlice = default;
			register = BadChar;
			return false;
		}

		register = *search;

		while (--search > thestart && search->IsSkippableSpace) ;

		if (*search != token) {
			newSlice = default;
			register = BadChar;
			return false;
		}

		while (--search > thestart && search->IsSkippableSpace) ;

		newSlice = new(thestart, search + 1);
		return true;

	}

	public bool HasSpecificIndexer(char token, char register, out OperandString newSlice) {
		char* search = _end;
		char* thestart = _start;

		while (--search > thestart && search->IsSkippableSpace) ;

		if (search <= thestart || !search->CiIs(register)) {
			newSlice = default;
			return false;
		}

		while (--search > thestart && search->IsSkippableSpace) ;

		if (search < thestart || *search != token) {
			newSlice = default;
			return false;
		}

		while (--search > thestart && search->IsSkippableSpace) ;

		newSlice = new(thestart, search + 1);
		return true;

	}

	public readonly OperandString Slice(int start, int length) {
		char* newStart = _start + start;
		char* newEnd = newStart + length;

		Debug.Assert(start >= 0);
		Debug.Assert(length >= 0);

		return new OperandString(newStart, newEnd);
	}

	public readonly OperandString Slice(int start) {
		char* newStart = _start + start;
		char* newEnd = _end;

		Debug.Assert(start >= 0);

		if (newStart >= newEnd) {
			return default;
		} else {
			return new OperandString(newStart, newEnd);
		}
	}

	public readonly OperandString SliceEnd(int trunc) {
		char* newStart = _start;
		char* newEnd = _end - trunc;

		Debug.Assert(trunc >= 0);

		if (newStart >= newEnd) {
			return default;
		} else {
			return new OperandString(newStart, newEnd);
		}
	}

	public readonly OperandString SliceBothEnds(int truncStart, int truncEnd) {
		char* newStart = _start + truncStart;
		char* newEnd = _end - truncEnd;

		Debug.Assert(truncStart >= 0);
		Debug.Assert(truncEnd >= 0);

		if (newStart >= newEnd) {
			return default;
		} else {
			return new OperandString(newStart, newEnd);
		}
	}

	public OperandString SliceEndsAndTrim(int truncStart, int truncEnd) {
		return CreateTrimmed(_start + truncStart, _end - truncEnd);
	}

	public readonly CharSpan AsSpan() {
		return CharSpanHelpers.CreateSpanUnchecked(_start, _end);
	}

	public readonly CharSpan AsSpan(int start) {
		char* newStart = _start + start;

		Debug.Assert(start >= 0);
		Debug.Assert(newStart <= _end);

		return CharSpanHelpers.CreateSpanUnchecked(newStart, _end);
	}

	public bool TryReadInlineDecimalInteger(ref int index, out int value) {
		Debug.Assert(index >= 0);

		char* start = _start + index;
		char* end = _end;

		Debug.Assert(start <= end);

		if (FastRead.TryReadInlineDecimalInteger(ref start, end, out value)) {
			index = (int) (start - _start);
			return true;
		} else {
			return false;
		}
	}

	public bool TestIfVariable() {
		return _start[0] is '!' && FastRead.IsVariableName(_start + 1, _end);
	}

	public void Deconstruct(out char* start, out char* end) {
		start = _start;
		end = _end;
	}

	public override string ToString() => new(_start, 0, Length);

	public static implicit operator CharSpan(OperandString a) => CharSpanHelpers.CreateSpanUnchecked(a._start, a._end);
	public static implicit operator string(OperandString a) => new(a._start, 0, a.Length);

	public StringSplitter GetEnumerator() => new(_start, _end);
}