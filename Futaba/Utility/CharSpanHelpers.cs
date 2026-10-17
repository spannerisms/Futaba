namespace Futaba;

internal static unsafe class CharSpanHelpers {
	extension(CharSpan charspan) {
		internal bool MatchesIgnoreCase(string test) {
			return MemoryExtensions.Equals(charspan, test, StringComparison.OrdinalIgnoreCase);
		}

		internal void FastLower(Span<char> dest) {
			_ = Ascii.ToLower(charspan, dest, out _);
		}

		internal bool BeginsWithIgnoreSpace(string test) {
			if (charspan.Length >= test.Length) {
				return charspan[..test.Length].MatchesIgnoreCase(test);
			}

			return false;
		}

		internal bool BeginsWithIgnoreCaseAndSpace(string test) {
			int len = test.Length;

			if (charspan.Length + 1 >= len) {
				if (charspan[len].IsSkippableSpace) {
					return charspan.Slice(0, len).MatchesIgnoreCase(test);
				}
			}

			return false;
		}
	}

	internal static CharSpan ClampForMessage(CharSpan span) {
		int len = Math.Min(span.Length, 20);

		return span[..len];
	}

	// NEVER CHANGE THIS
	// We want deterministic output across builds
	internal static UInt128 HashString(CharSpan spn) {
		return System.IO.Hashing.XxHash128.HashToUInt128(MemoryMarshal.AsBytes(spn), 0xBEBE_F007ABA);
	}

	private const char PrettyAddressSpecifier = 'A';
	private const char PrettyAddressSpecifierLower = (char) (PrettyAddressSpecifier | AsciiHelpers.LowercaseCharMask);

	private static string PrettyLongUpper(long value) => $"${(value >> 16) & 0xFF:X2}:{(ushort) value:X4}";
	private static string PrettyLongLower(long value) => $"${(value >> 16) & 0xFF:x2}:{(ushort) value:x4}";

	internal static string ExtendedStringFormat(decimal value, string? format) {
		if (format is null || format.Length < 1) {
			return value.ToString();
		}

		return format[0] switch {
			'X' or 'x' or 'B' or 'b' /*        */ => value.AsLong().ToString(format),
			PrettyAddressSpecifier /*          */ => PrettyLongUpper(value.AsLong()),
			PrettyAddressSpecifierLower /*     */ => PrettyLongLower(value.AsLong()),
			_ /*                               */ => value.ToString(format),
		};
	}

	[DebuggerStepThrough]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CharSpan CreateSpan(char* start, char* end) {
		return (start < end) ? new(start, (int) (end - start)) : default;
	}

	[DebuggerStepThrough]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CharSpan CreateSpanUnchecked(char* start, char* end) {
		return new(start, (int) (end - start));
	}
}
