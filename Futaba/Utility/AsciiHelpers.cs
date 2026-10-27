namespace Futaba;

internal static unsafe class AsciiHelpers {
	internal const int AsciiCount = 128;

	internal const char LowercaseCharMask = unchecked((char) 0x0020);
	internal const char UppercaseCharMask = unchecked((char) ~LowercaseCharMask);

	extension(char c) {
		internal bool IsADecimalDigit {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => c is >= '0' and <= '9';
		}

		// The difference between this and skippable space is
		// that \r should be considered invalid for separating command parts
		// TODO this needs to be made consistent or abandoned
		/// <summary>
		/// <c>\x20 \t</c>
		/// </summary>
		internal bool IsInlineSpace {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => c is ' ' or '\t';
		}

		/// <summary>
		/// <c>\x20 \t \r</c>
		/// </summary>
		// TODO microbenchmarks for all properties in various methods
		internal bool IsSkippableSpace {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			//get => c is ' ' or '\t' or '\r';
			get => c < 33 && ((1ul << c) & WhiteSpaceMask) != 0;
		}

		/// <summary>
		/// <c>[A-Za-z_0-9]</c>
		/// </summary>
		internal bool IsIdentifierCharacter {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (c < AsciiCount) && IdentifierCharacters[c];
		}

		/// <summary>
		/// <c>[A-Za-z_0-9.]</c>
		/// </summary>
		internal bool IsSublabelSafe {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (c < AsciiCount) && SubLabelSafe[c];
		}

		internal bool IsALetter {
			[DebuggerStepThrough]
			get => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
		}

		internal bool IsLowercase {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => c is >= 'a' and <= 'z';
		}

		internal bool IsUppercase {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => c is >= 'A' and <= 'Z';
		}

		/// <summary>
		/// <c>\n BadChar</c>
		/// </summary>
		internal bool IsLineEnd {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => c is '\n' or BadChar;
		}

		/// <summary>
		/// space <c>\t \r BadChar \n</c>
		/// </summary>
		internal bool IsNiceBoundary {
			[DebuggerStepThrough]
			//[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => c is ' ' or '\t' or '\r' or ';' or BadChar or '\n';
		}

		internal bool IsLetterOrUnderscore {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (c < AsciiCount) && LetterOrUnderscore[c];
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal int GetHexValue() {
			return (c < AsciiCount) ? HexValue[c] : -1;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal char FastUpper() {
			return (char) (c & UppercaseCharMask);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal char FastLower() {
			return (char) (c | LowercaseCharMask);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool CiIs(char c1) {
			return (c | LowercaseCharMask) == (c1 | LowercaseCharMask);
		}
	}
	private const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
	private const string DIGITS = "0123456789";
	private const string UNDERSCORE = "_";
	private const string ALPHANUMERIC = LETTERS + UNDERSCORE + DIGITS;

	internal static readonly SearchValues<char> IdentifierSearch = SearchValues.Create(ALPHANUMERIC);
	internal static readonly SearchValues<char> SublabelSearch = SearchValues.Create(ALPHANUMERIC + ".");

	internal static readonly SearchValues<char> SpaceSearch = SearchValues.Create(" \r\t");

	//                                            =    ~}|{zyxwvutsrqponmlkjihgfedcba`_^]\[ZYXWVUTSRQPONMLKJIHGFEDCBA@"
	internal const ulong LettersMask /*        */ = 0b0000011111111111111111111111111010000111111111111111111111111110;

	//                                            =                                   FEDCBA9876543210FEDCBA9876543210"
	//                                            =   ?>=<;:9876543210/.-,+*)('&%$#"!                   r   t         "
	internal const ulong WhiteSpaceMask /*     */ = 0b0000000000000000000000000000000100000000000000000010001000000000;
	internal const ulong NumbersMask /*        */ = 0b0000001111111111000000000000000000000000000000000000000000000000;
	internal const ulong SublabelsMask /*      */ = 0b0000001111111111010000000000000000000000000000000000000000000000;

	// Tables that end up running significantly faster than other checks
	// some might need further benchmarking
	internal static readonly AsciiTable<bool> LetterOrUnderscore = new();
	internal static readonly AsciiTable<bool> IdentifierCharacters = new();
	internal static readonly AsciiTable<bool> SubLabelSafe = new();
	internal static readonly AsciiTable<sbyte> HexValue = new();

	internal const sbyte AddressMarker_Bar = 0x30;
	internal const sbyte AddressMarker_Colon = 0x31;
	internal const sbyte AddressMarker_Unclosed = 0x32;

	internal static readonly AsciiTable<sbyte> AddressMarkerAction = new();

	static AsciiHelpers() {
		// TODO add validation for the ulong masks
#if DEBUG
		

#endif
		InsertHex(ref AddressMarkerAction);

		AddressMarkerAction['|'] = AddressMarker_Bar;
		AddressMarkerAction[':'] = AddressMarker_Colon;
		AddressMarkerAction[' '] = AddressMarker_Unclosed;
		AddressMarkerAction[';'] = AddressMarker_Unclosed;
		AddressMarkerAction['\r'] = AddressMarker_Unclosed;
		AddressMarkerAction['\n'] = AddressMarker_Unclosed;
		AddressMarkerAction['\t'] = AddressMarker_Unclosed;

		IdentifierCharacters.SetLetters(true);
		LetterOrUnderscore.SetLetters(true);
		SubLabelSafe.SetLetters(true);

		IdentifierCharacters.SetNumbers(true);
		LetterOrUnderscore.SetNumbers(true);
		SubLabelSafe.SetNumbers(true);

		IdentifierCharacters['_'] = true;
		LetterOrUnderscore['_'] = true;

		SubLabelSafe['_'] = true;
		SubLabelSafe['.'] = true;

		InsertHex(ref HexValue);

		static void InsertHex(ref AsciiTable<sbyte> tbl) {
			tbl.Fill(-1);

			tbl['0'] = 0x0;
			tbl['1'] = 0x1;
			tbl['2'] = 0x2;
			tbl['3'] = 0x3;
			tbl['4'] = 0x4;
			tbl['5'] = 0x5;
			tbl['6'] = 0x6;
			tbl['7'] = 0x7;
			tbl['8'] = 0x8;
			tbl['9'] = 0x9;

			tbl['A'] = 0xA;
			tbl['B'] = 0xB;
			tbl['C'] = 0xC;
			tbl['D'] = 0xD;
			tbl['E'] = 0xE;
			tbl['F'] = 0xF;

			tbl['a'] = 0xA;
			tbl['b'] = 0xB;
			tbl['c'] = 0xC;
			tbl['d'] = 0xD;
			tbl['e'] = 0xE;
			tbl['f'] = 0xF;
		}
	}

	[InlineArray(AsciiCount)]
	internal struct AsciiTable<T> where T : unmanaged {
		private T _table;

		public void Fill(T value) {
			((Span<T>) this).Fill(value);
		}

		public readonly T Get(char c) {
			if (c < AsciiCount) {
				return this[c];
			}

			return this[0];
		}

		public void SetLetters(T value) {
			Span<T> tmp = this;

			tmp.Slice('A', 26).Fill(value);
			tmp.Slice('a', 26).Fill(value);
		}

		public void SetNumbers(T value) {
			((Span<T>) this).Slice('0', 10).Fill(value);
		}
	}

	// TODO consider bringing this back with BitConverter.IsLittleEndian
	// not necessary, because performance is fine, but we'll see; why not
	private const ulong LowercaseMask = 0x0020_0020_0020_0020;
	private const ulong UppercaseMask = unchecked((ulong) ~0x0020_0020_0020_0020);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint MergeChars(char c1, char c2) {
		return FixEndianness(c1 | ((uint) c2 << 16));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong MergeChars(char c1, char c2, char c3, char c4) {
		return FixEndianness(c1 | ((ulong) c2 << 16) | ((ulong) c3 << 32) | ((ulong) c4 << 48));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TestLowercase(char* cptr, char c1, char c2) {
		return GetCharAs32Lower(cptr) == MergeChars(c1, c2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TestUppercase(char* cptr, char c1, char c2) {
		return GetCharAs32Upper(cptr) == MergeChars(c1, c2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TestLowercase(char* cptr, char c1, char c2, char c3, char c4) {
		return GetCharAs64Lower(cptr) == MergeChars(c1, c2, c3, c4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FastTest(ulong cptr, char c1, char c2, char c3, char c4) {
		return cptr == MergeChars(c1, c2, c3, c4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FastTest(uint cptr, char c1, char c2) {
		return cptr == MergeChars(c1, c2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FastTest(void* cptr, char c1, char c2) {
		return ReadLittleEndian32(cptr) == MergeChars(c1, c2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint GetCharAs32Lower(void* cptr) {
		return ReadLittleEndian32(cptr) | unchecked((uint) LowercaseMask);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong GetCharAs64Lower(void* cptr) {
		return ReadLittleEndian64(cptr) | LowercaseMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint GetCharAs32Upper(void* cptr) {
		return ReadLittleEndian32(cptr) & unchecked((uint) UppercaseMask);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong GetCharAs64Upper(void* cptr) {
		return ReadLittleEndian64(cptr) & UppercaseMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ReadLittleEndian32(void* ret) {
		if (BitConverter.IsLittleEndian) {
			return *(uint*) ret;
		} else {
			return BinaryPrimitives.ReverseEndianness(*(uint*) ret);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong ReadLittleEndian64(void* ret) {
		if (BitConverter.IsLittleEndian) {
			return *(ulong*) ret;
		} else {
			return BinaryPrimitives.ReverseEndianness(*(ulong*) ret);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong FixEndianness(ulong ret) {
		if (BitConverter.IsLittleEndian) {
			return ret;
		} else {
			return BinaryPrimitives.ReverseEndianness(ret);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint FixEndianness(uint ret) {
		if (BitConverter.IsLittleEndian) {
			return ret;
		} else {
			return BinaryPrimitives.ReverseEndianness(ret);
		}
	}
}
