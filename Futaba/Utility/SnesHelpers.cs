namespace Futaba;

internal static class SnesHelpers {
	public const int BankSize = 32768;

	public const int GameCodeLength = 4;
	public const int MakerCodeLength = 2;

	public const int MaxRegularSize = BankSize * 128;
	public const int MaxExSize = BankSize * 254;
	public const int MinExSize = MaxRegularSize + BankSize;
	public const int MaxMmcSize = BankSize * 256;

	internal const int AddressMask = 0xFF_FFFF;
	internal const int BankMask = 0xFF_0000;
	internal const int AbsoluteMask = 0xFFFF;
	internal const int MirroredBankMask = 0x7F_FFFF;
	internal const int HalfBankMask = 0xFF_8000;

	internal const int MmcBankingSize = 0x10_0000;
	internal const int MmcBankingMask = 0xF_FFFF;
	internal const int MmcBankShift = 20;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LoromToOffset(int address) {
		return address & 0x7FFF | (address & 0x7F0000) >> 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int OffsetToLorom(int offset) {
		return (offset & 0x7FFF) | 0x8000 | ((offset & 0x7F8000) << 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int HiromToOffset(int address) {
		return address & 0x3F_FFFF;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int OffsetToHirom(int offset) {
		return offset | 0xC0_0000;
	}

	internal static int Sa1ToOffset(int address) {
		if ((uint) address > 0xFF_FFFF) {
			return -1;
		}

		if (address is >= 0x00_0000 and < 0x40_0000) {
			if ((address & 0x8000) is not 0) {
				return LoromToOffset(address);
			} else {
				return -1;
			}
		}

		if (address is >= 0x80_0000 and < 0xC0_0000) {
			if ((address & 0x8000) is not 0) {
				return LoromToOffset(address & 0x7F_FFFF) + MmcBankingSize;
			} else {
				return -1;
			}
		}

		if (address is >= 0xC0_0000) {
			return address & ~0xC0_0000;
		}

		return -1;
	}

	internal static int OffsetToSa1(int offset) {
		if ((uint) offset >= MaxMmcSize) {
			return -1;
		}

		if (offset < (MmcBankingSize * 2)) {
			return OffsetToLorom(offset);
		}

		if (offset < (MmcBankingSize * 4)) {
			return OffsetToLorom(offset & 0x1F_FFFF) + (MmcBankingSize * 2);
		}

		return offset + 0xC0_0000;
	}



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
	internal static unsafe void FillBlockB(byte* pointer, byte value, int count) {
		new Span<byte>(pointer, count).Fill(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void FillBlockW(byte* pointer, ushort value, int count) {
		new Span<ushort>(pointer, count).Fill(FixBigEndian(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void FillBlockD(byte* pointer, int value, int count) {
		new Span<int>(pointer, count).Fill(FixBigEndian(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void WriteW(byte* ptr, int value) {
		*(ushort*) ptr = FixBigEndian((ushort) value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void WriteW(byte* ptr, ushort value) {
		*(ushort*) ptr = FixBigEndian(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void WriteL(byte* ptr, int value) {
		*(ushort*) ptr = FixBigEndian((ushort) value);
		ptr[2] = (byte) (value >> 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void WriteD(byte* ptr, int value) {
		*(int*) ptr = FixBigEndian(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FitsIn8(int value) {
		return value is >= sbyte.MinValue and <= byte.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool FitsIn16(int value) {
		return value is >= short.MinValue and <= ushort.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool OffsetChangedMmcBank(int addrA, int addrB) {
		return ((addrA ^ addrB) & ~MmcBankingMask) is not 0;
	}

	internal static string BranchDistanceError(int distance, int size) {
		if (distance < 0) {
			return $"Branch distance too far: {distance} < {(size == 1 ? sbyte.MinValue : short.MinValue)}";
		} else {
			return $"Branch distance too far: {distance} > {(size == 1 ? sbyte.MaxValue : short.MaxValue)}";
		}
	}

	internal static unsafe void InsertHeaderString(string text, byte* pointer, int length) {
		new Span<byte>(pointer, length).Fill((byte) ' ');

		length = Math.Min(text.Length, length);

		for (int i = 0; i < length; i++) {
			*pointer++ = (byte) text[i];
		}
	}

	internal static byte GetHeaderSizeByte(int value) {
		return (byte) System.Numerics.BitOperations.Log2(
			System.Numerics.BitOperations.RoundUpToPowerOf2((uint) value));
	}

	internal static (bool Valid, int Distance) TestGetBranchDistance(int location, int target) {
		int v = target - location;
		int distance = (sbyte) v;
		return (v == distance, distance);
	}

	internal static (bool Valid, int Distance) TestGetBranchDistanceLong(int location, int target) {
		int v = target - location;
		int distance = (short) v;
		return (v == distance, distance);
	}

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
	 
	// TODO can hirom and lorom be merged like this?
	internal static bool JumpTargetMakesSenseSmallRom(int address, int destination) {
		int bankChange = (address ^ destination) & BankMask;

		if (bankChange is 0) {
			return true;
		}

		// check banks that mirror each other
		if (bankChange is 0x80_0000) {
			// wram has no mirror
			// since the change is 0x80_0000
			// this captures either address being WRAM
			if ((address | 0x80_0000) >= 0xFE_0000) {
				return false;
			}

			// otherwise it's probably fine
			return true;
		}

		// this means we also crossed into a region with a different layout
		if (bankChange is 0x40_0000 or 0xC0_0000) {
			if (IsInMirroredBank(address)) {
				if (IsWram(destination)) {
					return IsMirroredWram(destination);  // mirrored WRAM is allowed

				} else {
					return IsInUpperBankHalf(destination); // only valid if upper bank (otherwise we went to mirrored memory)
				}

			} else {
				return !(IsWram(address) || IsWram(destination)); // reject WRAM
			}
		}

		return JumpTargetMakesSenseEdgeCases(address, destination);
	}

	internal static bool JumpTargetMakesSenseExRom(int address, int destination) {
		int bankChange = (address ^ destination) & BankMask;

		if (bankChange is 0) return true;

		return JumpTargetMakesSenseEdgeCases(address, destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsValidBusAddress(int addr) {
		return (uint) addr <= AddressMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsInMirroredBank(int addr) {
		return (addr & 0x40_0000) is 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInUpperBankHalf(int addr) {
		return (addr & 0x8000) is not 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsCommonMirror(int addr) {
		return (addr & 0x40_8000) < 0x8000;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsMirroredWram(int addr) {
		return (addr ^ 0x7E_0000) < 0x00_2000;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsWram(int addr) {
		// ^ has JIT synergy with mirror wram check
		return (addr ^ 0x7E_0000) < 0x02_0000;
	}

	internal static bool JumpTargetMakesSenseEdgeCases(int address, int destination) {
		// if the destination is mirrored WRAM, we can allow it for mirrored banks or common mirrors
		if (IsMirroredWram(destination)) {
			return IsInMirroredBank(address);
		}

		// address being in 7E/7F means it's assembled in WRAM, which is rarer
		if (IsWram(address)) {
			return IsMirroredWram(address) && IsInMirroredBank(destination);
		}

		// handles weird cases like 01:2000 => 03:4000
		return IsCommonMirror(address) || IsCommonMirror(destination);
	}

	internal static int GetAllocationSize(int desired) {
		return desired switch {
			<= 0 /*     */ => 0,
			<= 0x0002_0000 => 0x0002_0000,
			<= 0x0004_0000 => 0x0004_0000,
			<= 0x0008_0000 => 0x0008_0000,
			<= 0x0010_0000 => 0x0010_0000,
			<= 0x0020_0000 => 0x0020_0000,
			<= 0x0040_0000 => 0x0040_0000,
			<= 0x0080_0000 => 0x0080_0000,
			_ /*       */  => 0,
		};
	}
}
