namespace Futaba;

/// <summary>
/// 
/// </summary>
internal unsafe readonly struct SourceLine {
	public readonly char* Start;
	public readonly SourceObject Source;
	public readonly int Line;

	public readonly bool IsNull => Start == default;

	public SourceLine(SourceObject source, int line, char* start) {
		Start = start;
		Source = source;
		Line = line;
	}

	public override string ToString() {
		return $"{Source}:{Line}";
	}

	public CharSpan GetLineContents() {
		char* pstart = Start;

		// probably not necessary, but just in case
		if (pstart == default) {
			return string.Empty;
		}

		char* theend = Source.SourceEnd;
		char* read = pstart;

		for (; read < theend; read++) {
			char c = *read;

			if (c is NewLine or CommentChar) {
				break;
			}

			if (FastRead.TestIfColonAndSeparator(read)) {
				break;
			}

			if (c is CharDelim) {
				_ = FastRead.SkipToMatchingApos(ref read, theend);
			}

			if (c is '"') {
				_ = FastRead.SkipToMatchingQuote(ref read, theend);
			}
		}

		FastRead.TrimWhiteSpace(ref pstart, ref read);

		return CharSpanHelpers.CreateSpanUnchecked(pstart, read);
	}

}
