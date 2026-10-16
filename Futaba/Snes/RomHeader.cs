using System.Numerics;
using System.Runtime.Intrinsics;

namespace Futaba.Snes;

/// <summary>
/// This class contains helper functions for creating a well-formatted ROM registration header.
/// <para>
/// Implemented as per the specifications outlined in
/// Super Nintendo Entertainment System DEVELOPMENT MANUAL BOOK 1,
/// pages 1-2-10 through 1-2-22.
/// </para>
/// </summary>
public static class RomHeader {
	internal const int MaxRamSize = 524288;

	/// <summary>
	/// The SNES system bus address of the first byte of the extended header.
	/// </summary>
	public const int ExtendedHeaderLocation = 0x00FFB0;

	/// <summary>
	/// The SNES system bus address of the first byte of the title.
	/// <para>
	/// This is also the first byte of the non-extended header.
	/// </para>
	/// </summary>
	public const int TitleLocation = ExtendedHeaderLocation + 0x10;

	/// <summary>
	/// The exact length of the title, in bytes.
	/// </summary>
	public const int TitleLength = 21;

	/// <summary>
	/// Sanitizes a given string for game title entry by
	/// ensuring it is exactly 21 characters in length (padded with spaces if necessary)
	/// and removing characters that are not printable ASCII.
	/// </summary>
	/// <param name="title">The game title to sanitize.</param>
	/// <returns>A header-compliant title.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string SanitizeTitle(string title) {
		Debug.Assert(TitleLength == 21);

		return SnesHelpers.SanitizeString(title, TitleLength);
	}

	/// <summary>
	/// Attempts to get a <see cref="MapperMode"/> enum by name.
	/// </summary>
	/// <param name="name">A case-insensitive string that identifies the mapper mode. See: <see cref="MapperMode"/> remarks for valid names.</param>
	/// <param name="mode">The enum value corresponding to the given mode, or <see cref="MapperMode.Invalid"/> if an invalid name is passed.</param>
	/// <returns><see langword="true"/> if <paramref name="name"/> identifies a mapper mode; otherwise <see langword="false"/></returns>
	public static bool TryGetMapperMode(string name, out MapperMode mode) {
		name = name.Trim();

		if (name.Length < 10) {
			Span<char> tspan = stackalloc char[name.Length];
			name.FastLower(tspan);

			mode = tspan switch {
				"none" => /*                */ MapperMode.None,
				"lorom" => /*               */ MapperMode.Lorom,
				"exlorom" => /*             */ MapperMode.ExLorom,
				"hirom" => /*               */ MapperMode.Hirom,
				"exhirom" => /*             */ MapperMode.ExHirom,
				"sa1" => /*                 */ MapperMode.Sa1,
				_ => /*                     */ MapperMode.Invalid,
			};
		} else {
			mode = MapperMode.Invalid;
		}

		return mode != MapperMode.Invalid;
	}

	/// <summary>
	/// Returns a new array containing all valid ROM size tokens in lowercase form.
	/// </summary>
	public static string[] GetValidRomSizeTokens() => [.. RomSizeLookup.Keys];

	/// <summary>
	/// Returns a new array containing all valid RAM size tokens in lowercase form.
	/// </summary>
	public static string[] GetValidRamSizeTokens() => [.. RamSizeLookup.Keys];

	/// <summary>
	/// Checks a given size for being one of the values expected of a SNES ROM binary file.
	/// </summary>
	/// <param name="size">size in bytes</param>
	/// <returns><see langword="true"/> if <paramref name="size"/> is a standard SNES ROM size; otherwise <see langword="false"/></returns>
	public static bool IsExpectedRomSize(int size) {
		return RomSizeLookup.ContainsValue(size);
	}

	private static readonly Dictionary<string, int> RomSizeLookup = new(StringComparer.OrdinalIgnoreCase) {
		{ "32k", 0x0001_0000 },
		{ "32kb", 0x0001_0000 },

		{ "64k", 0x0001_0000 },
		{ "64kb", 0x0001_0000 },

		{ "128k", 0x0002_0000 },
		{ "128kb", 0x0002_0000 },

		{ "256k", 0x0004_0000 },
		{ "256kb", 0x0004_0000 },

		{ "512k", 0x0008_0000 },
		{ "512kb", 0x0008_0000 },

		{ "1024k", 0x0010_0000 },
		{ "1024kb", 0x0010_0000 },
		{ "1m", 0x0010_0000 },
		{ "1mb", 0x0010_0000 },

		{ "1.5m", 0x0018_0000 },
		{ "1.5mb", 0x0018_0000 },

		{ "2048k", 0x0020_0000 },
		{ "2048kb", 0x0020_0000 },
		{ "2m", 0x0020_0000 },
		{ "2mb", 0x0020_0000 },

		{ "3m", 0x0030_0000 },
		{ "3mb", 0x0030_0000 },

		{ "4096k", 0x0040_0000 },
		{ "4096kb", 0x0040_0000 },
		{ "4m", 0x0040_0000 },
		{ "4mb", 0x0040_0000 },

		{ "6m", 0x0060_0000 },
		{ "6mb", 0x0060_0000 },

		{ "8192k", SnesHelpers.MaxExSize },
		{ "8192kb", SnesHelpers.MaxExSize },
		{ "8m", SnesHelpers.MaxExSize },
		{ "8mb", SnesHelpers.MaxExSize },
	};

	private static readonly Dictionary<string, int> RamSizeLookup = new(StringComparer.OrdinalIgnoreCase) {
		{ "0", 0 },
		{ "0k", 0 },
		{ "0kb", 0 },
		{ "none", 0 },

		{ "2k", 0x0000_0800 },
		{ "2kb", 0x0000_0800 },

		{ "4k", 0x0000_1000 },
		{ "4kb", 0x0000_1000 },

		{ "8k", 0x0000_2000 },
		{ "8kb", 0x0000_2000 },

		{ "16k", 0x0000_4000 },
		{ "16kb", 0x0000_4000 },

		{ "32k", 0x0000_8000 },
		{ "32kb", 0x0000_8000 },

		{ "64k", 0x0001_0000 },
		{ "64kb", 0x0001_0000 },

		{ "128k", 0x0002_0000 },
		{ "128kb", 0x0002_0000 },

		{ "256k", 0x0004_0000 },
		{ "256kb", 0x0004_0000 },
	};

	/// <inheritdoc cref="IsValidRomSize(string)"/>
	/// <param name="token">string to test</param>
	/// <param name="size">will contain the size corresponding to the input token</param>
	/// <returns><see langword="true"/> if <paramref name="token"/> identifies a valid ROM size; otherwise <see langword="false"/></returns>
	public static bool TryGetRomSize(string token, out int size) {
		return RomSizeLookup.TryGetValue(token, out size);
	}

	/// <inheritdoc cref="IsValidRamSize(string)"/>
	public static bool TryGetRamSize(string token, out int size) {
		return RamSizeLookup.TryGetValue(token, out size);
	}

	/// <summary>
	/// Tries parsing a token as a RAM size.
	/// </summary>
	/// <remarks>
	/// Legal values are (case-insensitive):
	/// <list type="bullet">
	/// <item><c>0</c>, <c>0k</c>, <c>0kb</c>, <c>none</c></item>
	/// <item><c>2k</c>, <c>2kb</c></item>
	/// <item><c>32k</c>, <c>32kb</c></item>
	/// <item><c>4k</c>, <c>4kb</c></item>
	/// <item><c>8k</c>, <c>8kb</c></item>
	/// <item><c>16k</c>, <c>16kb</c></item>
	/// <item><c>64k</c>, <c>64kb</c></item>
	/// <item><c>128k</c>, <c>128kb</c></item>
	/// <item><c>256k</c>, <c>256kb</c></item>
	/// </list>
	/// </remarks>
	/// <param name="token">The case-insensitive name to test</param>
	/// <returns><see langword="true"/> if <paramref name="token"/> identifies a valid size; otherwise <see langword="false"/></returns>
	public static bool IsValidRamSize(string token) => RamSizeLookup.ContainsKey(token);

	/// <summary>
	/// Tries parsing a token as a ROM size.
	/// </summary>
	/// <remarks>
	/// Legal values are (case-insensitive):
	/// <list type="bullet">
	/// <item><c>32k</c>, <c>32kb</c></item>
	/// <item><c>64k</c>, <c>64kb</c></item>
	/// <item><c>128k</c>, <c>128kb</c></item>
	/// <item><c>256k</c>, <c>256kb</c></item>
	/// <item><c>512k</c>, <c>512kb</c></item>
	/// <item><c>1024k</c>, <c>1024kb</c>, <c>1m</c>, <c>1mb</c></item>
	/// <item><c>2048k</c>, <c>2048kb</c>, <c>2m</c>, <c>2mb</c></item>
	/// <item><c>3m</c>, <c>3mb</c></item>
	/// <item><c>4096k</c>, <c>4096kb</c>, <c>4m</c>, <c>4mb</c></item>
	/// <item><c>6m</c>, <c>6mb</c></item>
	/// <item><c>8192k</c>, <c>8192kb</c>, <c>8m</c>, <c>8mb</c></item>
	/// </list>
	/// </remarks>
	/// <inheritdoc cref="IsValidRamSize(string)" path="/param"/>
	/// <returns><see langword="true"/> if <paramref name="token"/> identifies a valid size; otherwise <see langword="false"/></returns>
	public static bool IsValidRomSize(string token) => RomSizeLookup.ContainsKey(token);

	/// <summary>
	/// Tests the given value for being a valid range size.
	/// </summary>
	/// <remarks>
	/// Valid RAM sizes are in the inclusive range [0, 524288].
	/// </remarks>
	/// <param name="size">The integer value to test</param>
	/// <returns><see langword="true"/> if <paramref name="size"/> is within the range of RAM sizes; otherwise <see langword="false"/></returns>
	public static bool IsValidRamSize(int size) {
		return size is >= 0 and <= MaxRamSize;
	}

	/// <summary>
	/// Calculates the checksum over a given collection of <see langword="byte"/> values
	/// by adding every byte together and truncating to 16-bits.
	/// </summary>
	/// <remarks>
	/// Note that this method does not insert complementary values anywhere
	/// to ensure the checksum remains valid if the value is reinserted.
	/// </remarks>
	/// <param name="data">The collection of bytes to checksum</param>
	/// <returns>A 16-bit checksum</returns>
	public static short CalculateChecksum(byte[] data) {
		unsafe {
			fixed (byte* baseptr = data) {
				return (short) CalculateChecksum(baseptr, data.Length);
			}
		}
	}

	/// <inheritdoc cref="CalculateChecksum(byte[])"/>
	public static short CalculateChecksum(ReadOnlySpan<byte> data) {
		unsafe {
			fixed (byte* baseptr = data) {
				return (short) CalculateChecksum(baseptr, data.Length);
			}
		}
	}

	internal static unsafe ushort CalculateChecksum(byte* baseptr, int fullsize) {
		Debug.Assert(fullsize >= 0);

		if (fullsize < 1) {
			return 0;
		}

		// get the nearest power of 2 less than or equal to the full size
		int mainChunkSize = 1 << BitOperations.Log2((uint) fullsize);

		ushort sum = SumChunk(baseptr, mainChunkSize);

		// if we've summed every byte, we're done
		if (mainChunkSize == fullsize) {
			return sum;
		}

		// otherwise, we need to repeatedly sum over the final segment
		// until we've summed a number of bytes equal to the main chunk size again
		byte* remainderPointer = baseptr + mainChunkSize;

		int remaining = fullsize - mainChunkSize;
		var (remQ, remR) = Math.DivRem(mainChunkSize, remaining);

		// sum of the remaining chunk, with as many copies as needed
		sum += (ushort) (remQ * SumChunk(remainderPointer, remaining));

		// last tiny slice if needed
		if (remR > 0) {
			sum += SumChunk(remainderPointer, remR);
		}

		return sum;

		//////////////////////////////////////////////////////////////////

		static ushort SumChunk(byte* ptr, int sumCount) {
			ushort retsum = 0;

			if (Vector256.IsHardwareAccelerated) {
				for (; sumCount >= Vector256<byte>.Count; sumCount -= Vector256<byte>.Count) {
					var chunk = Vector256.Load(ptr);

					var (chunk1, chunk2) = Vector256.Widen(chunk);
					retsum += Vector256.Sum(chunk1);
					retsum += Vector256.Sum(chunk2);

					ptr += Vector256<byte>.Count;
				}
			}

			if (Vector128.IsHardwareAccelerated) {
				for (; sumCount >= Vector128<byte>.Count; sumCount -= Vector128<byte>.Count) {
					var chunk = Vector128.Load(ptr);

					var (chunk1, chunk2) = Vector128.Widen(chunk);
					retsum += Vector128.Sum(chunk1);
					retsum += Vector128.Sum(chunk2);

					ptr += Vector128<byte>.Count;
				}
			}

			if (Vector64.IsHardwareAccelerated) {
				for (; sumCount >= Vector64<byte>.Count; sumCount -= Vector64<byte>.Count) {
					var chunk = Vector64.Load(ptr);

					var (chunk1, chunk2) = Vector64.Widen(chunk);
					retsum += Vector64.Sum(chunk1);
					retsum += Vector64.Sum(chunk2);

					ptr += Vector64<byte>.Count;
				}
			}

			// vector fall back and also covers any remainder
			for (; sumCount > 0; sumCount--) {
				retsum += *ptr;
				ptr++;
			}

			return retsum;
		}
	}
}
