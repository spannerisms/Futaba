namespace Futaba;

internal class SnesHelpers {
	public const int BankSize = 32768;

	public const int MaxRegularSize = BankSize * 128;
	public const int MaxExSize = BankSize * 254;
	public const int MinExSize = MaxRegularSize + BankSize;
	public const int MaxMmcSize = BankSize * 256;

	internal const int AddressMask = 0xFF_FFFF;
	internal const int BankMask = 0xFF_0000;
	internal const int AbsoluteMask = 0xFFFF;
	internal const int MirroredBankMask = 0x7F_FFFF;
	internal const int HalfBankMask = 0x7FFF;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LoromToOffset(int address) => address & 0x7FFF | (address & 0x7F0000) >> 1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int OffsetToLorom(int offset) => (offset & 0x7FFF) | 0x8000 | ((offset & 0x7F8000) << 1);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int HiromToOffset(int address) => address & 0x3F_FFFF;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int OffsetToHirom(int offset) => offset | 0xC0_0000;


	[DebuggerStepThrough]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ushort FixBigEndian(ushort value) {
		if (BitConverter.IsLittleEndian) {
			return value;
		} else {
			return BinaryPrimitives.ReverseEndianness(value);
		}
	}

	[DebuggerStepThrough]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint FixBigEndian(uint value) {
		if (BitConverter.IsLittleEndian) {
			return value;
		} else {
			return BinaryPrimitives.ReverseEndianness(value);
		}
	}

	[DebuggerStepThrough]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int FixBigEndian(int value) {
		if (BitConverter.IsLittleEndian) {
			return value;
		} else {
			return BinaryPrimitives.ReverseEndianness(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void FillBlockB(byte* pointer, byte value, int count) {
		new Span<byte>(pointer, count).Fill(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void FillBlockW(byte* pointer, ushort value, int count) {
		new Span<ushort>(pointer, count).Fill(FixBigEndian(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void FillBlockD(byte* pointer, int value, int count) {
		new Span<int>(pointer, count).Fill(FixBigEndian(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteW(byte* ptr, int value) {
		*(ushort*) ptr = FixBigEndian((ushort) value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteW(byte* ptr, ushort value) {
		*(ushort*) ptr = FixBigEndian(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteL(byte* ptr, int value) {
		*(ushort*) ptr = FixBigEndian((ushort) value);
		ptr[2] = (byte) (value >> 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteD(byte* ptr, int value) {
		*(int*) ptr = FixBigEndian(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FitsIn8(int v) {
		return v is >= sbyte.MinValue and <= byte.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FitsIn16(int v) {
		return v is >= short.MinValue and <= ushort.MaxValue;
	}

	internal static string BranchDistanceError(int distance, int size) {
		if (distance < 0) {
			return $"Branch distance too far: {distance} < {(size == 1 ? sbyte.MinValue : short.MinValue)}";
		} else {
			return $"Branch distance too far: {distance} > {(size == 1 ? sbyte.MaxValue : short.MaxValue)}";
		}
	}

	internal unsafe static void InsertHeaderString(string str, byte* ptr, int len) {
		new Span<byte>(ptr, len).Fill((byte) ' ');

		len = Math.Min(str.Length, len);

		for (int i = 0; i < len; i++) {
			*ptr = (byte) str[i];
		}
	}

	internal static byte GetHeaderSizeByte(int value) {
		return (byte) System.Numerics.BitOperations.Log2(
			System.Numerics.BitOperations.RoundUpToPowerOf2((uint) value));
	}

	internal static (bool valid, int distance) TestGetBranchDistance(int location, int target) {
		int v = target - location;
		int distance = (sbyte) v;
		return (v == distance, distance);
	}

	internal static (bool valid, int distance) TestGetBranchDistanceLong(int location, int target) {
		int v = target - location;
		int distance = (short) v;
		return (v == distance, distance);
	}

	public const int GameCodeLength = 4;
	public const int MakerCodeLength = 2;

	/// <summary>
	/// Sanitizes a given string for header entry by
	/// ensuring it is exactly the given number of characters in length (padded with spaces if necessary)
	/// and removing characters that are not printable ASCII.
	/// </summary>
	internal static string SanitizeString(string contents, int length) {
		// set up temporary space for the new string
		Span<char> newString = length < 32
			? stackalloc char[length]
			: GC.AllocateUninitializedArray<char>(length);

		// initialize with padding character
		newString.Fill(' ');

		// track number of characters added
		int added = 0;

		foreach (char c in contents) {
			if (c is >= ' ' and <= '~') {
				newString[added++] = c;

				if (added == length) {
					break;
				}
			}
		}

		// create a new string from the span on the stack
		return new(newString);
	}

	// TODO validate all of these with unit tests
	// TODO can hirom and lorom be merged like this?
	internal static bool JumpTargetMakesSenseSmallRom(int addrA, int addrB) {
		int bankChange = (addrA ^ addrB) & BankMask;

		if (bankChange is 0) {
			return true;
		}

		// check banks that mirror each other
		if (bankChange is 0x80_0000) {
			// wram has no mirror though
			if ((addrA | 0x80_0000) >= 0xFE_0000) {
				return false;
			}

			// TODO needs tinkering
			return IsInMirroredBank(addrA) || IsHighBank(addrB);
		}

		if (bankChange is 0x40_0000 or 0xC0_0000) {
			// TODO is this actually correct?
			if (IsInMirroredBank(addrA)) {
				return IsMirroredWram(addrB) || (!IsWram(addrB) && IsHighBank(addrB));
			}

			if (IsInMirroredBank(addrA)) return IsHighBank(addrB) || IsMirroredWram(addrB);
			if (IsInMirroredBank(addrB)) return IsHighBank(addrA) || IsMirroredWram(addrA);

			return false;
		}

		if (IsMirroredWram(addrB)) {
			return IsInMirroredBank(addrA);
		}

		// bankA being in 7E/7F means it's assembled in WRAM, which is rarer
		if (IsWram(addrA)) {
			return IsMirroredWram(addrA) && IsInMirroredBank(addrB);
		}

		// weird cases like 01:2000 => 03:4000
		return IsCommonMirror(addrA) && IsCommonMirror(addrB);
	}

	internal static bool JumpTargetMakesSenseMmc(int addrA, int addrB) {
		int bankChange = (addrA ^ addrB) & BankMask;

		if (bankChange is 0) {
			return true;
		}


		if (IsMirroredWram(addrB)) {
			return IsInMirroredBank(addrA);
		}

		// bankA being in 7E/7F means it's assembled in WRAM, which is rarer
		if (IsWram(addrA)) {
			return IsMirroredWram(addrA) && IsInMirroredBank(addrB);
		}

		return false;
	}

	internal static bool JumpTargetMakesSenseExRom(int addrA, int addrB) {
		int bankChange = (addrA ^ addrB) & BankMask;

		if (bankChange is 0) return true;

		if (IsInMirroredBank(addrA)) {
			return IsMirroredWram(addrB) || IsCommonMirror(addrB);
		}

		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsValidBusAddress(int addr) {
		return (uint) addr <= 0xFF_FFFF;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsInMirroredBank(int addr) {
		return (addr & 0x40_0000) is 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsHighBank(int addr) => (addr & 0x8000) is not 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsCommonMirror(int addr) => (addr & 0x40_8000) < 0x8000;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsWramMirror(int addr) => (addr & 0x40_FFFF) < 0x2000;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsMirroredWram(int addr) => (addr ^ 0x7E_0000) < 0x2000;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsWram(int addr) => (addr & 0xFE_0000) >= 0x7E_0000;

	internal static int GetAllocationSize(int desired) {
		return desired switch {
			< 0 => 0,
			< 0x0002_0000 => 0x0002_0000,
			< 0x0004_0000 => 0x0004_0000,
			< 0x0008_0000 => 0x0008_0000,
			< 0x0010_0000 => 0x0010_0000,
			< 0x0020_0000 => 0x0020_0000,
			< 0x0040_0000 => 0x0040_0000,
			<= 0x0080_0000 => 0x0080_0000,
			_ => 0,
		};
	}
}
