namespace Futaba;

internal unsafe struct StringSplitter {
	private readonly char* _start;
	private readonly char* _end;
	private char* _current;

	internal StringSplitter(char* start, char* end) {
		_start = start;
		_end = end;
		_current = start;
	}

	public void Reset() {
		_current = _start;
	}

	/// <returns><see langword="true"/>If there are potentially more strings to find</returns>
	public bool GetNextComma(out OperandString next) {
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
				} else if (c is '(') {
					if (!FastRead.SkipToMatchingParenthesis(ref read, theend)) {
						goto Fail;
					}
				} else if (c is ',') {
					next = OperandString.CreateTrimmed(chunkStart, read);
					read++;
					_current = read;
					return true;
				}
			}

			_current = read;
			next = OperandString.CreateTrimmed(chunkStart, read);
			return false;
		}

		Fail:
		next = default;
		return false;
	}

	public bool GetNextChar(char delim, out OperandString next) {
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
				} else if (c is '(') {
					if (!FastRead.SkipToMatchingParenthesis(ref read, theend)) {
						goto Fail;
					}
				} else if (c == delim) {
					next = OperandString.CreateTrimmed(chunkStart, read);
					read++;
					_current = read;
					return read < theend;
				}
			}

			_current = read;
			next = OperandString.CreateTrimmed(chunkStart, read);
			return false;
		}

		Fail:
		next = default;
		return false;
	}

	public bool GetNextDoubleChar(char delim, out OperandString next) {
		char* theend = _end;

		if (FastRead.SkipInlineWhitespaceWithEndCheck(ref _current, theend)) {
			char* read = _current;
			char* chunkStart = read;

			for (; read < theend; read++) {
				char c = *read;

				if (c is '"') {
					if (!FastRead.SkipToMatchingQuote(ref read, theend)) {
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
						return read < theend;
					} else {
						break;
					}
				}
			}

			_current = read;
			next = OperandString.CreateTrimmed(chunkStart, read);
			return false;
		}

		Fail:
		next = default;
		return false;
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
