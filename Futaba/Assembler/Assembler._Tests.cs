using System.Buffers;
using System.Buffers.Binary;
using System.Drawing;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Futaba;




#if UNITTESTS

#pragma warning disable IDE0079
#pragma warning disable CS1591 // debug only class. not part of the public api
partial class Assembler {


	// TODO
	// need to stress test ; and : for end of command behavior
	// need to stress test multiple variables in a single function
	// test db "string" including functions

	public class UnitTester(Assembler asmtester) {
		private static bool FailsOnly = true;
		private static bool GroupPassing = false;
		private static int TotalTestCount = 0;
		private static int TotalPassCount = 0;

		public static void TestMain() {
			FailsOnly = false;


			Assembler asmblr = new LoromAssembler("Tests/errortests.asm");

			RunErrorTests();
			RunSourceEncodingTest();


			return;
			UnitTester asstest = new(asmblr);

			asstest.RunStandardTests();

			RunDataTests();
			RunPcTests();


			bool allgood = TotalPassCount == TotalTestCount;
			Console.WriteLine();
			Console.WriteLine();

			Console.ForegroundColor = allgood ? ConsoleColor.DarkGreen : ConsoleColor.Red;

			Console.Write($"{TotalPassCount} / {TotalTestCount} tests passed.");

			Console.ResetColor();
			Console.WriteLine();
		}

		private static void WriteColoredLine(string text, ConsoleColor fg, ConsoleColor bg) {
			Console.BackgroundColor = bg;
			Console.ForegroundColor = fg;
			Console.Write(text);
			Console.ResetColor();
			Console.WriteLine();
		}


		private static void RunPcTests() {
			SuiteGroupHeader("Program counter tests");

			Assembler asmblr = new LoromAssembler("Tests/pctests.asm");

			_ = asmblr.Assemble();

			PostGroupResults(asmblr.ErrorCount < 1);
		}

		private static void PostGroupResults() {
			PostGroupResults(GroupPassing);
		}

		private static void PassLine(string text) {
			WriteColoredLine(text, ConsoleColor.White, ConsoleColor.DarkGreen);
		}

		private static void FailLine(string text) {
			WriteColoredLine(text, ConsoleColor.White, ConsoleColor.DarkRed);
		}

		private static void PostGroupResults(bool result) {
			Console.ResetColor();
			Console.WriteLine();

			if (result) {
				PassLine("  All tests passed!  ");
			} else {
				FailLine("  Some tests failed...");
			}

			Console.ResetColor();
			Console.WriteLine();
		}


		private void RunStandardTests() {
			TestAddressHelpers();
			TestMnemonics();
			TestExpresions();
		}


		private static void RunErrorTests() {
			Assembler asmblr = new LoromAssembler("Tests/errortests.asm");
			_ = asmblr.Assemble();

			if (asmblr.TryGetVariable("EXPECTED_ERRORS", out var exp)) {
				var expected = exp.Value.AsInt();

				string txt = $"  errors/expected: {asmblr.ErrorCount}/{expected}  ";

				if (expected == asmblr.ErrorCount) {
					PassLine(txt);
				} else {
					FailLine(txt);
				}

			} else {
				FailLine("                           ");
				FailLine("  Something went wrong...  ");
				FailLine("                           ");
			}
		}


		// Regression test for a bug where SourceFile.Refresh() set Length to the
		// file's raw byte count instead of the actual decoded char count. A UTF-8
		// BOM (3 bytes, 0 decoded chars) plus one non-ASCII character (2 UTF-8
		// bytes, 1 decoded char) makes those two counts diverge, which a
		// plain-ASCII source file never would.
		private static void RunSourceEncodingTest() {
			SuiteGroupHeader("Source file encoding length");

			string tempDir = Path.Combine(Path.GetTempPath(), $"futaba-tests-{Guid.NewGuid()}");
			Directory.CreateDirectory(tempDir);

			try {
				string content = "; café\n";
				string path = Path.Combine(tempDir, "bom.asm");
				File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

				using SourceFile source = new(path);

				PostResult($"Length is the decoded char count, not the byte count ({source.Length} == {content.Length})",
					source.Length == content.Length);

				PostResult("AsSpan() reproduces exactly the decoded text (no overrun into padding)",
					source.AsSpan().SequenceEqual(content));
			} finally {
				Directory.Delete(tempDir, recursive: true);
			}

			PostGroupResults();
		}


		// TODO rewrite this with something like:
/*
 string code =
$"""
org $00_8000
Start: 
{PAYLOAD}
^.end 
"""

and use a temp file as entry point
so that everything can be handle completely programmatically in source
and just reuse assembler while modifying the code in its entry point
 */
		private unsafe static void RunDataTests() {
			SuiteGroupHeader("Data statement tests");


			Assembler dataTests = new LoromAssembler("Tests/datatests.asm");

			byte[] dataOut = dataTests.Assemble();

			bool dataGood;
			int offset = 0;

			var dbTemp = SliceData<byte>(8);
			for (int i = 0; i < 8; i++) {
				dataGood &= dbTemp[i] == i;
			}

			PostResult("db statements", dataGood);

			var dwTemp = SliceData<ushort>(8);
			for (ushort i = 0; i < 8; i++) {
				dataGood &= dwTemp[i] == i;
			}

			PostResult("dw statements", dataGood);

			var dlTemp = SliceData<U24>(8);
			for (int i = 0; i < 8; i++) {
				dataGood &= dlTemp[i].Equals(i);
			}

			PostResult("dl statements", dataGood);

			
			var ddTemp = SliceData<uint>(8);
			for (int i = 0; i < 8; i++) {
				dataGood &= ddTemp[i] == i;
			}

			PostResult("dd statements", dataGood);



			PostResult("fill byte size", !SliceData<byte>(8).ContainsAnyExcept<byte>(0xBE));
			PostResult("fill byte size", !SliceData<byte>(8).ContainsAnyExcept<byte>(0x21));
			PostResult("fill byte count", !SliceData<byte>(8).ContainsAnyExcept<byte>(0x12));

			PostResult("fill word size 8", !SliceData<ushort>(4).ContainsAnyExcept<ushort>(0x1234));
			PostResult("fill word size 7", TestFill(SliceDataSized<byte>(7, 2), 0x5678, 2));
			PostResult("fill word count 7", !SliceData<ushort>(7).ContainsAnyExcept<ushort>(0xBE51));

			PostResult("fill long count 7", !SliceData<U24>(7).ContainsAnyExcept<U24>(0x123456));
			PostResult("fill long size 7", TestFill(SliceDataSized<byte>(7, 3), 0xABCDEF, 3));
			PostResult("fill long size 8", TestFill(SliceDataSized<byte>(8, 3), 0x328197, 3));

			PostResult("fill double count 7", !SliceData<uint>(7).ContainsAnyExcept(0xBE401299));
			PostResult("fill double size 17", TestFill(SliceDataSized<byte>(17, 4), 0xF456CD78, 4));
			PostResult("fill double size 18", TestFill(SliceDataSized<byte>(18, 4), 0x43902155, 4));
			PostResult("fill double size 19", TestFill(SliceDataSized<byte>(19, 4), 0x13243546, 4));



			// TODO pad, arrange, align

			byte[] rawVals = [
				0x91, 0x8B, 0x4F, 0xF2, 0x3B, 0xE6, 0xFF, 0x9C, 0xB2, 0xA3, 0x4B, 0x2E, 0xF3, 0x7B, 0xD2, 0xBC,
				0xEA, 0xE3, 0x0D, 0xAB, 0x20, 0x47, 0xB3, 0x4A, 0x13, 0x3B, 0x4E, 0xCE, 0x53, 0x2A, 0x9C, 0x2F,
				0x74, 0xA6, 0x95, 0x89, 0x46, 0x75, 0xDF, 0xA7, 0xB2, 0x13, 0x0C, 0x2D, 0xBF, 0xDE, 0xCC, 0xF7,
				0xB4, 0x38, 0x67, 0x76, 0x91, 0x21, 0x96, 0xD2, 0x0A, 0x72, 0xB4, 0xC7, 0xA8, 0x0F, 0x64, 0xA3,
			];

			var rawBytes = SliceData<byte>(rawVals.Length);

			for (int i = 0; i < rawVals.Length; i++) {
				if (rawBytes[i] != rawVals[i]) {
					dataGood = false;
				}
			}

			PostResult("raw", dataGood);




			offset = 0x4000;
			PostResult("fill long until", TestFill(SliceDataSized<byte>(32, 3), 0x328197, 3));


			PostGroupResults();

			bool TestFill(Span<byte> spn, uint value, int size) {
				int j = 0;

				Span<byte> vals = stackalloc byte[4];
				BinaryPrimitives.WriteUInt32LittleEndian(vals, value);

				foreach (var wv in spn) {
					if (wv != vals[j]) {
						return false;
					}

					if (++j == size) {
						j = 0;
					}
				}

				return true;
			}

			Span<T> SliceData<T>(int count) where T : unmanaged {
				return SliceDataSized<T>(count, sizeof(T));
			}

			Span<T> SliceDataSized<T>(int count, int wordSize) where T : unmanaged {
				int size = count * sizeof(T);
				dataGood = true;
				var theData = dataOut.AsSpan(offset, size);

				int i = 0;

				if (!FailsOnly) {
					Console.WriteLine();

					foreach (var wv in theData) {
						Console.ResetColor();
						Console.ForegroundColor = ConsoleColor.Black;
						Console.BackgroundColor = ConsoleColor.DarkGray;
						Console.Write($"{wv:X2}");

						if (++i == wordSize) {
							i = 0;
							Console.ResetColor();
						}

						Console.Write(" ");
						Console.ResetColor();
					}

					Console.ResetColor();
					Console.WriteLine();
				}
				

				offset += size;
				return MemoryMarshal.Cast<byte, T>(theData); ;
			}

		}

		[StructLayout(LayoutKind.Explicit)]
		private readonly struct U24 : IEquatable<U24> {
			[FieldOffset(0)] private readonly byte a;
			[FieldOffset(1)] private readonly byte b;
			[FieldOffset(2)] private readonly byte c;

			public U24(int i) {
				a = (byte) i;
				b = (byte) (i >> 8);
				c = (byte) (i >> 16);
			}

			private int AsInt() => a | (b << 8) | (c << 16);

			public bool Equals(int i) {
				return AsInt() == (i & 0xFFFFFF);
			}

			public bool Equals(U24 other) {
				return a == other.a && b == other.b && c == other.c;
			}

			public static implicit operator int(U24 i) => i.AsInt();
			public static implicit operator U24(int i) => new(i);

			public override bool Equals(object? obj) {
				return obj is U24 u && Equals(u);
			}

			public override int GetHashCode() {
				throw new NotImplementedException();
			}
		}

		private static string PrettyAddr(int a) {
			return $"${(byte) (a >> 16):X2}:{(ushort) a:X4}";
		}

		const string BAD = "TEST FAILED";

		readonly Assembler Tester = asmtester;

		private static void SuiteGroupHeader(string groupName) {
			const string ThickBar = "====================================================================================================";

			GroupPassing = true;
			Console.ResetColor();
			Console.WriteLine(ThickBar);
			Console.WriteLine($"= {groupName}");
			Console.WriteLine(ThickBar);
		}

		private static void PostResult(string test, bool passed) {
			TotalTestCount++;

			if (passed) {
				TotalPassCount++;

				if (FailsOnly) {
					return;
				}
			}

			GroupPassing &= passed;

			Console.ForegroundColor = passed ? ConsoleColor.DarkGray : ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.Write(test);

			Console.Write(' ');

			if (passed) {
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Write(" PASS ");
			} else {
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.DarkRed;
				Console.Write(" FAIL ");
			}

			Console.ResetColor();
			Console.WriteLine();
		}

		public static void TestAddressHelpers() {
			(int address, int target, bool expected)[] loromCompatTest = [
				new (0x00_0000, 0x7E_0000, true),
				new (0x00_1000, 0x7E_8000, false),
				new (0x00_9000, 0x7E_2000, false),
				new (0x00_0000, 0x7F_0000, false),
				new (0x00_2000, 0x7F_2000, false),
				new (0x00_8000, 0x7F_8000, false),
				new (0x00_2000, 0x01_2000, true),
				new (0x00_4000, 0x02_4000, true),
				new (0x00_6000, 0x03_6000, true),
				new (0x00_8000, 0x40_8000, true),
			];


			SuiteGroupHeader("Jump target compatibility");

			PrintHeader("Lorom");

			bool allGood = true;
			
			foreach (var (a, t, p) in loromCompatTest) {
				bool compat = SnesHelpers.JumpTargetMakesSenseSmallRom(a,t);
				string disp = $"{PrettyAddr(a)} | {PrettyAddr(t)}   {BoolChar(compat)} ({BoolChar(p)}) ";

				bool good = compat == p;
				allGood &= good;

				PostResult(disp, good);
			}



			PostGroupResults();

			static void PrintHeader(string suite) {
				Console.WriteLine();
				Console.WriteLine($"- {suite} tests");
				Console.WriteLine("LOCATION | TARGET     RESULT (EXPECTED) ");
				Console.WriteLine(LongBar);
			}

		}


		public void TestExpresions() {
			ExpressionTest[] tests = [
				new ("", null),
				new ("1", 1),
				new ("1.9", 1.9M),
				new ("+1.9", +1.9M),
				new ("-1.9", -1.9M),

				new ("2 + 2", 4),
				new ("2 - 1", 1),
				new ("2 * 3", 6),
				new ("2 / 1", 2),
				new ("4 // 3", 1),
				new ("4 % 3", 1),

				new ("2 / 0", null),
				new ("4 // 0", null),
				new ("4 % 0", null),


				new ("~127", ~127L),
				new ("~127.91212", ~127L),



				new ("1 << 5", 1 << 5),
				new ("1.7 << 5.2", 1 << 5),

				new ("27 >> 3", 27 >> 3),
				new ("27.7 >> 3.2", 27 >> 3),
				new ("-27.7 >>> 3.2", -27L >>> 3),

				new ("9 & 3", 9 & 3),
				new ("9.7 & 3.2", 9 & 3),
				new ("4 | 8", 4 | 8),
				new ("4.1 | 8.6", 4 | 8),
				new ("127 ^ 13", 127 ^ 13),
				new ("127.1 ^ 13.2", 127 ^ 13),


				new ("-(1/2?)", -1/2M),
				new ("1 + 2 * 3", 7M),
				new ("(1/0)?", 0),
				new ("(1/0)? + 1", 1),
				new ("(1/0) ?? 7", 7),
				new ("1 ?? 7", 1),


				new ("Cheese ?? 7", 7),
				new ("-----1", -1),
				new ("-+?-1", null),
				new ("1 == 2", 0),
				new ("1 == 1", 1),

				new ("1 > 0", 1),
				new ("1 >= 0", 1),
				new ("1 < 0", 0),
				new ("1 <= 0", 0),

				new ("1 > 2", 0),
				new ("1 >= 2", 0),
				new ("1 < 2", 1),
				new ("1 <= 2", 1),

				new ("1 > 1", 0),
				new ("1 >= 1", 1),
				new ("1 < 1", 0),
				new ("1 <= 1", 1),

				new ("1 ^ 1", 0),
				new ("~1", ~1),

				new ("$04 ][ $B0", 0xB004),
				new ("<$123456", 0x56),
				new (">$123456", 0x34),
				new ("^$123456", 0x12),
				new ("<&$123456", 0x3456),
				new (">&$123456", 0x1234),
				new ("^&$123456", 0x120000),



				new ("!:pi", 3.14159265358979323846264M),
				new ("!:e", 2.71828182845904523536028M),
				new ("!:rad", 0.01745329251994329576923M),
				new ("!:name", 0x3FF),
				new ("!:invalid", 0),

				new ("abc(12983)", null),
				new ("bcd(12983)", 0x12983),
				new ("bcd(12983, 9)", null),
				new ("round(1234.5678, 3)", 1234.568M),
				new ("round(1234.5678, -2)", 1200),
				new ("floor(1234.5678)", 1234),
				new ("ceil(1234.5678)", 1235),
				new ("abs(-1234)", 1234),
				new ("exists(!CHEESE)", 0),
				new ("rebank($123491, $56)", 0x563491),
				new ("bitn(3)", 8),
				new ("streq(\"abc\", \"abc\")", 1),
				new ("streq(\"abc\", \"ABC\")", 0),
				new ("len(\"cheese\")", 6),
				new ("col($F0, $D8, $40)", 0x237E),
				new ("col($F0D840)", 0x237E),
				new ("select(1 == 2, 1, 2)", 2),
				new ("clamp(1, 5, 9)", 5),
				new ("clamp(7, 5, 9)", 7),
				new ("clamp(10, 5, 9)", 9),
				new ("logb(1000, 10)", 3),
				new ("root(1000, 3)", 10),
				new ("pow(10, 3)", 1000),
				new ("root(pow(10, 3), 3)", 10),
				new ("min(5, 9)", 5),
				new ("max(5, 9)", 9),
				new ("vram($8000)", 0x4000),
				new ("rtn($8000)", 0x7FFF),


				new ("'a'", 'a'),
				new ("'\x1234'", ' '),
				new ("'\\\\'", '\\'),
			];

			bool allGood = true;
			SuiteGroupHeader("Expressions");

			Console.WriteLine("                    EXPRESSION                  RESULT  |              EXPECTED");
			Console.WriteLine(LongBar);

			foreach (ExpressionTest e in tests) {
				const string UNRESOLVED = "UNRESOLVED";
				IExpressionReturn xret;
				string xstr = e.Expression;


				unsafe {
					fixed (char* s = xstr) {
						var (ca, cb) = Console.GetCursorPosition();
						Console.ForegroundColor = ConsoleColor.DarkGray;
						xret = Tester.ParseOperand(s, s + xstr.Length, SymbolContext.Default);
						Console.ResetColor();
						Console.SetCursorPosition(ca, cb);
						Console.Write("                                                                                                              ");
						Console.SetCursorPosition(ca, cb);
					}
				}

				const string numformat = "0.######";
				decimal? rv;

				if (xret.Resolved) {
					rv = xret.Value;
				} else {
					rv = null;
				}

				decimal? ev = e.Value;
				string eval = ev?.ToString(numformat) ?? UNRESOLVED;
				string rval = rv?.ToString(numformat) ?? UNRESOLVED;

				bool good = rv == ev;
				allGood &= good;

				PostResult($"{xstr,30} =  {rval,20}  |  {eval,20}", good);
			}

			PostGroupResults();
		}


		public void TestMnemonics() {
			SuiteGroupHeader("65816 mnemonics");

			var opswdc = Enum.GetValues<OpWdc>();

			foreach (var m in opswdc) {
				string mnem = Enum.GetName(m)!;

				unsafe {
					fixed (char* ptr = mnem) {
						var mt = TestFor65816(new (ptr, ptr + mnem.Length));

						string disp = $"{mnem, 10} => {mt,10} | {m,-10} ";
						bool good = mt == m;
						PostResult(disp, good);
					}
				}
			}

			PostGroupResults();

			SuiteGroupHeader("SPC700 mnemonics");

			var opsspc = Enum.GetValues<OpSPC>();

			foreach (var m in opsspc) {
				string mnem = Enum.GetName(m)!;

				unsafe {
					fixed (char* ptr = mnem) {
						var mt = TestForSPC700(new (ptr, ptr + mnem.Length));

						string disp = $"{mnem, 10} => {mt,10} | {m,-10} ";
						bool good = mt == m;
						PostResult(disp, good);
					}
				}
			}

			PostGroupResults();

			SuiteGroupHeader("SuperFX mnemonics");

			var opssfx = Enum.GetValues<OpSFX>();

			foreach (var m in opssfx) {
				string mnem = Enum.GetName(m)!;

				unsafe {
					fixed (char* ptr = mnem) {
						var mt = TestForSuperFX(new (ptr, ptr + mnem.Length));

						string disp = $"{mnem, 10} => {mt,10} | {m,-10} ";
						bool good = mt == m;
						PostResult(disp, good);
					}
				}
			}

			PostGroupResults();



			//(string test, OpWdc opout)[] mnemtests = [
			//	( "ADC", OpWdc.ADC ),
			//	( "AND", OpWdc.AND ),
			//	( "ASL", OpWdc.ASL ),
			//	( "BCC", OpWdc.BCC ),
			//	( "BCS", OpWdc.BCS ),
			//	( "BEQ", OpWdc.BEQ ),
			//	( "BIT", OpWdc.BIT ),
			//	( "BMI", OpWdc.BMI ),
			//	( "BNE", OpWdc.BNE ),
			//	( "BPL", OpWdc.BPL ),
			//	( "BRA", OpWdc.BRA ),
			//	( "BRK", OpWdc.BRK ),
			//	( "BRL", OpWdc.BRL ),
			//	( "BVC", OpWdc.BVC ),
			//	( "BVS", OpWdc.BVS ),
			//	( "CLC", OpWdc.CLC ),
			//	( "CLD", OpWdc.CLD ),
			//	( "CLI", OpWdc.CLI ),
			//	( "CLV", OpWdc.CLV ),
			//	( "CMP", OpWdc.CMP ),
			//	( "COP", OpWdc.COP ),
			//	( "CPX", OpWdc.CPX ),
			//	( "CPY", OpWdc.CPY ),
			//	( "DEC", OpWdc.DEC ),
			//	( "DEX", OpWdc.DEX ),
			//	( "DEY", OpWdc.DEY ),
			//	( "EOR", OpWdc.EOR ),
			//	( "INC", OpWdc.INC ),
			//	( "INX", OpWdc.INX ),
			//	( "INY", OpWdc.INY ),
			//	( "JML", OpWdc.JML ),
			//	( "JMP", OpWdc.JMP ),
			//	( "JSL", OpWdc.JSL ),
			//	( "JSR", OpWdc.JSR ),
			//	( "LDA", OpWdc.LDA ),
			//	( "LDX", OpWdc.LDX ),
			//	( "LDY", OpWdc.LDY ),
			//	( "LSR", OpWdc.LSR ),
			//	( "MVN", OpWdc.MVN ),
			//	( "MVP", OpWdc.MVP ),
			//	( "NOP", OpWdc.NOP ),
			//	( "ORA", OpWdc.ORA ),
			//	( "PEA", OpWdc.PEA ),
			//	( "PEI", OpWdc.PEI ),
			//	( "PER", OpWdc.PER ),
			//	( "PHA", OpWdc.PHA ),
			//	( "PHB", OpWdc.PHB ),
			//	( "PHD", OpWdc.PHD ),
			//	( "PHK", OpWdc.PHK ),
			//	( "PHP", OpWdc.PHP ),
			//	( "PHX", OpWdc.PHX ),
			//	( "PHY", OpWdc.PHY ),
			//	( "PLA", OpWdc.PLA ),
			//	( "PLB", OpWdc.PLB ),
			//	( "PLD", OpWdc.PLD ),
			//	( "PLP", OpWdc.PLP ),
			//	( "PLX", OpWdc.PLX ),
			//	( "PLY", OpWdc.PLY ),
			//	( "REP", OpWdc.REP ),
			//	( "ROL", OpWdc.ROL ),
			//	( "ROR", OpWdc.ROR ),
			//	( "RTI", OpWdc.RTI ),
			//	( "RTL", OpWdc.RTL ),
			//	( "RTS", OpWdc.RTS ),
			//	( "SBC", OpWdc.SBC ),
			//	( "SEC", OpWdc.SEC ),
			//	( "SED", OpWdc.SED ),
			//	( "SEI", OpWdc.SEI ),
			//	( "SEP", OpWdc.SEP ),
			//	( "STA", OpWdc.STA ),
			//	( "STP", OpWdc.STP ),
			//	( "STX", OpWdc.STX ),
			//	( "STY", OpWdc.STY ),
			//	( "STZ", OpWdc.STZ ),
			//	( "TAX", OpWdc.TAX ),
			//	( "TAY", OpWdc.TAY ),
			//	( "TCD", OpWdc.TCD ),
			//	( "TCS", OpWdc.TCS ),
			//	( "TDC", OpWdc.TDC ),
			//	( "TRB", OpWdc.TRB ),
			//	( "TSB", OpWdc.TSB ),
			//	( "TSC", OpWdc.TSC ),
			//	( "TSX", OpWdc.TSX ),
			//	( "TXA", OpWdc.TXA ),
			//	( "TXS", OpWdc.TXS ),
			//	( "TXY", OpWdc.TXY ),
			//	( "TYA", OpWdc.TYA ),
			//	( "TYX", OpWdc.TYX ),
			//	( "WAI", OpWdc.WAI ),
			//	( "WDM", OpWdc.WDM ),
			//	( "XBA", OpWdc.XBA ),
			//	( "XCE", OpWdc.XCE ),
			//	( "lda", OpWdc.LDA ),
			//	( "ldA", OpWdc.LDA ),
			//	( "Cheese", OpWdc.NotGood ),
			//];
			//
			//bool allGood = true;
			//foreach (var (s, m) in mnemtests) {
			//	unsafe {
			//		fixed (char* ptr = s) {
			//			OpWdc mt = TestFor65816(new (ptr, ptr + s.Length));
			//
			//			string disp = $"{s, 10} => {mt,10} | {m,-10} ";
			//			bool good = mt == m;
			//			allGood &= good;
			//			PostResult(disp, good);
			//		}
			//	}
			//}
		}
	}


	private static char BoolChar(bool v) => v ? 'T' : 'F';










}


file record class ExpressionTest(string Expression, decimal? Value);

#pragma warning restore CS1591
#pragma warning restore IDE0079
#endif
