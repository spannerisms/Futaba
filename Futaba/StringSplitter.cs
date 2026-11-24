namespace Futaba;

internal enum StringSplitResult {
	Invalid = -1,
	ReachedEnd = 0,
	MoreContent,
}

internal unsafe struct StringSplitter {
	private readonly char* _start;
	private readonly char* _end;
	private char* _current;
	public bool AtEnd => _current >= _end;

	internal StringSplitter(char* start, char* end) {
		_start = start;
		_end = end;
		_current = start;
	}

	internal StringSplitter(OperandString item) {
		_start = item.Start;
		_end = item.End;
		_current = _start;
	}

	public void Reset() {
		_current = _start;
	}

	public StringSplitResult GetNextComma(out OperandString next) {
		char* theend = _end;

		if (!FastRead.SkipInlineWhitespaceWithEndCheck(ref _current, theend)) {
			char* read = _current;
			char* chunkStart = read;

			for (; read < theend; read++) {
				char nextChar = *read;

				if (nextChar is '"') {
					if (!FastRead.SkipToMatchingQuote(ref read, theend)) {
						goto Fail;
					}
				} else if (nextChar is CharDelim) {
					if (!FastRead.SkipToMatchingApos(ref read, theend)) {
						goto Fail;
					}
				} else if (nextChar is '(') {
					if (!FastRead.SkipToMatchingParenthesis(ref read, theend)) {
						goto Fail;
					}
				} else if (nextChar is ',') {
					next = OperandString.CreateTrimmed(chunkStart, read);
					read++;
					_current = read;

					return StringSplitResult.MoreContent;
				}
			}

			_current = read;
			next = OperandString.CreateTrimmed(chunkStart, read);
			return StringSplitResult.ReachedEnd;
		}

		next = default;
		return StringSplitResult.ReachedEnd;

	Fail:
		next = default;
		return StringSplitResult.Invalid;
	}

	public StringSplitResult GetNextChar(char delim, out OperandString next) {
		char* theend = _end;

		if (!FastRead.SkipInlineWhitespaceWithEndCheck(ref _current, theend)) {
			char* read = _current;
			char* chunkStart = read;

			for (; read < theend; read++) {
				char nextChar = *read;

				if (nextChar is '"') {
					if (!FastRead.SkipToMatchingQuote(ref read, theend)) {
						goto Fail;
					}
				} else if (nextChar is CharDelim) {
					if (!FastRead.SkipToMatchingApos(ref read, theend)) {
						goto Fail;
					}
				} else if (nextChar is '(') {
					if (!FastRead.SkipToMatchingParenthesis(ref read, theend)) {
						goto Fail;
					}
				} else if (nextChar == delim) {
					next = OperandString.CreateTrimmed(chunkStart, read);
					read++;
					_current = read;

					return StringSplitResult.MoreContent;
				}
			}

			_current = read;
			next = OperandString.CreateTrimmed(chunkStart, read);
			return StringSplitResult.ReachedEnd;
		}

		next = default;
		return StringSplitResult.ReachedEnd;

	Fail:
		next = default;
		return StringSplitResult.Invalid;
	}

	public StringSplitResult GetNextDoubleChar(char delim, out OperandString next) {
		char* theend = _end;

		if (!FastRead.SkipInlineWhitespaceWithEndCheck(ref _current, theend)) {
			char* read = _current;
			char* chunkStart = read;

			for (; read < theend; read++) {
				char c = *read;

				if (c is '"') {
					if (!FastRead.SkipToMatchingQuote(ref read, theend)) {
						goto Fail;
					}
				} else if (c is CharDelim) {
					if (!FastRead.SkipToMatchingApos(ref read, theend)) {
						goto Fail;
					}
				} else if (c is '(') {
					if (!FastRead.SkipToMatchingParenthesis(ref read, theend)) {
						goto Fail;
					}
				} else if (c == delim) {
					char* chk = read + 1;
					if (chk < theend && *chk == delim) {
						next = OperandString.CreateTrimmed(chunkStart, read);
						read += 2;
						_current = read;

						return StringSplitResult.MoreContent;
					}
				}
			}

			_current = read;
			next = OperandString.CreateTrimmed(chunkStart, read);
			return StringSplitResult.ReachedEnd;
		}

		next = default;
		return StringSplitResult.ReachedEnd;

	Fail:
		next = default;
		return StringSplitResult.Invalid;
	}

	// This type also behaves as OperandString's enumerator,
	// because it has everything required already
	public readonly char Current => *_current;
	public bool MoveNext() {
		char* read = _current + 1;

		if (read < _end) {
			_current = read;
			return true;
		} else {
			return false;
		}
	}
}
