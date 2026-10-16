using System.Buffers.Binary;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace FutabaCLI;

internal static class Helpers {
	private const int DefaultPatience = 1750;

	// why is this backwards? so annoying
	public static byte[] GetCrc32(byte[] data) {
		byte[] ret = new byte[4];
		uint reverse = System.IO.Hashing.Crc32.HashToUInt32(data);
		BinaryPrimitives.WriteUInt32BigEndian(ret, reverse);

		return ret;
	}

	extension(Stream strm) {
		public void Clear() {
			strm.Position = 0;
			strm.SetLength(0);
		}
	}

	public static byte[] GetFileAsArray(FileInfo file) {
		using FileStream stream = file.OpenRead();

		byte[] ret = GC.AllocateUninitializedArray<byte>((int) stream.Length);

		stream.ReadExactly(ret);
		return ret;
	}

	public static FileStream WaitForAndCreateStream(FileInfo o, int patience = DefaultPatience) {
		return WaitForAndCreateStream(o.FullName, patience);
	}

	public static FileStream WaitForAndCreateStream(string p, int patience = DefaultPatience) {
		long started = Stopwatch.GetTimestamp();

		int retries = 0;

		while (true) {
			try {
				var f = new FileStream(p, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

				if (retries > 0) {
					Console.WriteLine();
				}

				return f;
			} catch (IOException) {
				int timeWasted = (int) Stopwatch.GetElapsedTime(started).TotalMilliseconds;

				if (timeWasted > patience) {
					throw;
				}

				if ((++retries & 0x7F) == 1) {
					Console.Write('.');
				}
			}
		}
	}

	public static void WaitForAndMoveFile(FileInfo file, string dest, int patience = DefaultPatience) {
		long started = Stopwatch.GetTimestamp();

		int retries = 0;

		while (true) {
			try {
				file.MoveTo(dest, true);

				if (retries > 0) {
					Console.WriteLine();
				}

				return;
			} catch (IOException) {
				int timeWasted = (int) Stopwatch.GetElapsedTime(started).TotalMilliseconds;

				if (timeWasted > patience) {
					throw;
				}

				if ((++retries & 0x7F) == 1) {
					Console.Write('.');
				}
			}
		}
	}

	internal static void FillArray(byte[] source, byte[] dest, int start) {
		int fillLen = source.Length;
		int remaining = dest.Length - start;

		while (remaining > 0) {
			int fillamt = (fillLen >= remaining) ? remaining : fillLen;

			Array.Copy(source, 0, dest, start, fillamt);
			start += fillamt;
			remaining -= fillamt;
		}
	}

	internal static string[] CleanSplit(string s, char separator) {
		return s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	extension(Command cmd) {
		internal void AddArguments(params Argument[] arguments) {
			foreach (Argument a in arguments) {
				cmd.Add(a);
			}
		}

		internal void AddOptions(params Option[] options) {
			foreach (Option o in options) {
				cmd.Add(o);
			}
		}
	}

	extension(string s) {
		public bool EqualsI(string s2) => s.Equals(s2, StringComparison.OrdinalIgnoreCase);
		public bool EqualsI(Span<char> s2) => s.Equals(s2, StringComparison.OrdinalIgnoreCase);
	}

	extension(ReadOnlySpan<char> s) {
		public bool EqualsI(string s2) => s.Equals(s2, StringComparison.OrdinalIgnoreCase);
		public bool EqualsI(Span<char> s2) => s.Equals(s2, StringComparison.OrdinalIgnoreCase);
	}

	public static void WriteHexAddress(int address, Span<char> destination, int offset) {
		Span<byte> addrBytes = stackalloc byte[4];

		BinaryPrimitives.WriteInt32BigEndian(addrBytes, address);
		_ = Convert.TryToHexString(addrBytes[1..4], destination.Slice(offset, 6), out _);
	}

	public static (long, long) HashString(ReadOnlySpan<char> seedString) {
		Span<long> seed = stackalloc long[2];

		if (System.IO.Hashing.XxHash128.TryHash(MemoryMarshal.AsBytes(seedString), MemoryMarshal.AsBytes(seed), out _, 0xF007ABA)) {
			return (seed[0], seed[1]);
		}

		return (0, 0);
	}

	public static bool TryParseByte(ReadOnlySpan<char> s, out byte value) {
		if (s.Length > 0) {
			if (s.Length > 1) {
				char c = s[0];

				if (c is '$') {
					return byte.TryParse(s[1..], NumberStyles.HexNumber, null, out value);
				} else if (c is '%') {
					return byte.TryParse(s[1..], NumberStyles.BinaryNumber, null, out value);
				}
			}

			return byte.TryParse(s, NumberStyles.AllowLeadingSign, null, out value);
		}

		value = 0;
		return false;
	}

	public static bool TryParseInt32(ReadOnlySpan<char> s, out int value) {
		if (s.Length > 0) {
			if (s.Length > 1) {
				char c = s[0];

				if (c is '$') {
					return int.TryParse(s[1..], NumberStyles.HexNumber, null, out value);
				} else if (c is '%') {
					return int.TryParse(s[1..], NumberStyles.BinaryNumber, null, out value);
				}
			}

			return int.TryParse(s, NumberStyles.AllowLeadingSign, null, out value);
		}

		value = 0;
		return false;
	}

	public static bool TryParseValue(ReadOnlySpan<char> s, out decimal value) {

		if (s.Length > 0) {
			if (s.Length > 1) {
				char c = s[0];

				if (c is '$') {
					return decimal.TryParse(s[1..], NumberStyles.HexNumber, null, out value);
				} else if (c is '%') {
					return decimal.TryParse(s[1..], NumberStyles.BinaryNumber, null, out value);
				}
			}

			return decimal.TryParse(s, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, null, out value);
		}

		value = 0M;
		return false;
	}

	// We want RNG to never change across builds
	// but we also want the benefits of xoshiro
	// so this is a handrolled copy of that algorithm
	// but with custom seeding
	// this is copied from <https://github.com/dotnet/dotnet/blob/main/src/runtime/src/libraries/System.Private.CoreLib/src/System/Random.Xoshiro256StarStarImpl.cs>
	// which is licensed under the MIT license
	//       Copyright (c) .NET Foundation and Contributors
	//
	//       Permission is hereby granted, free of charge, to any person obtaining a copy
	//       of this software and associated documentation files (the "Software"), to deal
	//       in the Software without restriction, including without limitation the rights
	//       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	//       copies of the Software, and to permit persons to whom the Software is
	//       furnished to do so, subject to the following conditions:
	//
	//       The above copyright notice and this permission notice shall be included in all
	//       copies or substantial portions of the Software.
	// Licensed to the .NET Foundation under one or more agreements.
	// The .NET Foundation licenses this file to you under the MIT license.

	internal static unsafe void FillRandom(ulong seed1, ulong seed2, Span<byte> buffer) {
		ulong s0 = 0xBEBE_6368_6565_7365ul; // need not all 0s, and this will guarantee that
		ulong s1 = seed1; // make this the most interesting
		ulong s2 = (uint) seed2;
		ulong s3 = seed2 ^ 0x00_FFFF_FFFF_00;

		while (buffer.Length >= sizeof(ulong)) {
			BinaryPrimitives.WriteUInt64LittleEndian(buffer, BitOperations.RotateLeft(s1 * 5, 7) * 9);

			ulong t = s1 << 17;
			s2 ^= s0;
			s3 ^= s1;
			s1 ^= s2;
			s0 ^= s3;
			s2 ^= t;
			s3 = BitOperations.RotateRight(s3, 19);

			buffer = buffer.Slice(sizeof(ulong));
		}

		if (!buffer.IsEmpty) {
			ulong next = BitOperations.RotateLeft(s1 * 5, 7) * 9;

			if (!BitConverter.IsLittleEndian) {
				next = BinaryPrimitives.ReverseEndianness(next);
			}

			byte* remainingBytes = (byte*) &next;

			Debug.Assert(buffer.Length < sizeof(ulong));

			for (int i = 0; i < buffer.Length; i++) {
				buffer[i] = remainingBytes[i];
			}
		}
	}

}
