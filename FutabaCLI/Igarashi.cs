global using System.Buffers;
global using System.Diagnostics.CodeAnalysis;
global using Futaba;
global using Futaba.Snes;
global using Futaba.Symbols;

// System.CommandLine has its own Symbol type
global using FutabaSymbol = Futaba.Symbols.Symbol;

using System.CommandLine;
using System.Diagnostics;
using System.Text;

namespace FutabaCLI;


// TODO add a template command
internal static partial class Igarashi {
	public static readonly Version Version;

	public const string RomExtension = ".sfc";
	public const string ManifestExtension = ".futaba";

	public const int Exit_Good = 0;
	const int Exit_Error = 1;

	// Windows registry identifiers
	const string AppHandle = "futaba_assembler";
	const string DefaultOpen = "Assemble";

	// Linux/XDG identifiers
	const string MimeType = "text/x-futaba";
	const string DesktopFileId = "futaba.desktop";
	const string IconName = "futaba";
	const string MimeIconName = "text-x-futaba"; // MimeType with '/' -> '-'

	static Igarashi() {
		Version = System.Reflection.Assembly.GetAssembly(typeof(Igarashi))?.GetName().Version ?? new(0, 0);

		Arg_Entry.AcceptExistingOnly();
		Arg_PatchBase.AcceptExistingOnly();
		Arg_MapMode.AcceptOnlyFromAmong("none", "lorom", "hirom", "exlorom", "exhirom", "loromsa1", "hiromsa1");

		Option_Size.Validators.Add(res => {
			if (res.GetValue(Option_Size) is string s) {
				if (s.EqualsI("auto")) {

				} else if (!RomHeader.IsValidRomSize(s)) {
					res.AddError($"Argument '{s}' not recognized. Must be one of:\n'auto', {ValidRomSizeStrings}");
				}
			}
		});
	}

	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	static int Main(string[] args) {
#if UNITTESTS
		Assembler.UnitTester.TestMain();
		FutabaCliTests.RunAll();

		return 0;
#else

		// heh
		RootCommand rootaba = new("Futaba Assembler\r\nFor full documentation, see <https://spannerisms.github.io/futaba#cli>");

		Command manifestBuild = new("build", "Assemble using a manifest file");

		manifestBuild.Arguments.Add(Arg_ManifestFile);
		manifestBuild.AddOptions(Option_AssembleOnce, Option_PreserveConsole, Option_PauseAfter, Option_Timer);
		manifestBuild.SetAction(Assemble);


		Command registerCommand = new("register", "Register as a system command and enable direct opening of .futaba files");
		registerCommand.SetAction(RegisterFutaba);

		Command unregisterCommand = new("unregister", "Remove the .futaba file association and app shortcut");
		unregisterCommand.SetAction(UnregisterFutaba);

		Command quicky = new("quicky", "Quick assembly without a manifest file") {
			Arg_Entry, Arg_BaseRom, Arg_OutputRom,
		};

		Command patch = new("patch", "Quick patch assembly without a manifest file") {
			Arg_Entry, Arg_PatchBase, Arg_OutputRom
		};

		Command checksums = new("checksum", "Calculate various checksums for a file") {
			Arg_TargetRom
		};

		checksums.SetAction(ChecksumFile);


		quicky.AddOptions(Arg_MapMode, Option_FixChecksum, Option_Size, Option_Timer);
		patch.AddOptions(Arg_MapMode, Option_FixChecksum, Option_Size, Option_Timer);

		quicky.SetAction(res => AssembleQuick(res, Arg_BaseRom));
		patch.SetAction(res => AssembleQuick(res, Arg_PatchBase));

		Command futaba = new("igarashi") { Hidden = true };
		futaba.SetAction(PrintFutaba);
		rootaba.Add(futaba);


		VersionOption cmdversion = new("--version") {
			Action = new ShowVersionThing(() => {
				{
					Console.WriteLine($"Futaba CLI version:       {Assembler.Version}");
					Console.WriteLine($"Futaba Assembler version: {Version}");
				}
			})
		};

		rootaba.Add(manifestBuild);
		rootaba.Add(quicky);
		rootaba.Add(patch);
		rootaba.Add(checksums);
		rootaba.Add(registerCommand);
		rootaba.Add(unregisterCommand);
		rootaba.Options.Remove(rootaba.Options.First(o => o is VersionOption));

		rootaba.Add(cmdversion);

		return rootaba.Parse(args).Invoke();
#endif
	}


	static int ChecksumFile(ParseResult res) {
		try {
			if (res.GetValue(Arg_TargetRom) is not FileInfo entry || !entry.Exists) {
				Error("Unable to locate file.");
				return Exit_Error;
			}

			byte[] dat = Helpers.GetFileAsArray(entry);
			int len = dat.Length;

			if (len is 0) {
				Console.WriteLine("File is empty.");
				return Exit_Error;
			}

			if (!RomHeader.IsExpectedRomSize(len)) {
				Warning($"This file is {len} bytes, which is weird. Are you sure it's a SNES ROM?");
			}

			ushort cksm = (ushort) RomHeader.CalculateChecksum(dat);

			Console.WriteLine($"Internal checksum (complement): {cksm:X4} ({(ushort) ~cksm:X4})");

			// TODO add something that tries to find the checksum in the file itself to verify it matches.

			EmitHashInfo("CRC32", Helpers.GetCrc32(dat));
			EmitHashInfo("MD5", System.Security.Cryptography.MD5.HashData(dat));
			EmitHashInfo("SHA1", System.Security.Cryptography.SHA1.HashData(dat));

			return Exit_Good;

			static void EmitHashInfo(string name, byte[] vals) {
				string valStr = Convert.ToHexString(vals);
				Console.WriteLine($"{name}: {valStr}");
			}
		} catch (Exception e) {
			Error(e.Message);
			return e.HResult;
		}
	}

	static int RegisterFutaba(ParseResult _) => RunRegistrar(static r => r.Register());

	static int UnregisterFutaba(ParseResult _) => RunRegistrar(static r => r.Unregister());

	static int RunRegistrar(Func<IAssociationRegistrar, int> action) {
		try {
			return action(GetRegistrar());
		} catch (Exception e) {
			Error(e.Message);
			return Exit_Error;
		}
	}

	static IAssociationRegistrar GetRegistrar() {
		if (OperatingSystem.IsWindows()) {
			return new WindowsAssociationRegistrar();
		}

		if (OperatingSystem.IsLinux()) {
			return new LinuxAssociationRegistrar();
		}

		if (OperatingSystem.IsMacOS()) {
			return new RefusalAssociationRegistrar("MacOS is not supported due to certificate fees.");
		}

		return new RefusalAssociationRegistrar("Unsupported platform.");
	}

	// covers both a deliberately-unsupported OS (MacOS) and a genuinely
	// unrecognized one; neither has any work to reverse on Unregister either
	sealed class RefusalAssociationRegistrar(string message) : IAssociationRegistrar {
		public int Register() => Refuse();
		public int Unregister() => Refuse();

		int Refuse() {
			Error(message);
			return Exit_Error;
		}
	}

	static int AssembleQuick(ParseResult args, Argument<FileInfo> baseRomOption) {
		var entry = args.GetValue(Arg_Entry);

		if (entry is null) {
			Error("No entry file provided.");
			return Exit_Error;
		}

		try {
			var baseRom = args.GetValue(baseRomOption);
			var outputFile = args.GetValue(Arg_OutputRom);

			if (outputFile is null) {
				if (entry.Extension.EqualsI(RomExtension)) {
					Error("Either provide an output file path or use an .asm file entry point.");
					return Exit_Error;
				}

				string outputName = Path.ChangeExtension(entry.FullName, RomExtension);
				outputFile = new(outputName);
			}

			MapperMode mapper = args.GetValue(Arg_MapMode);

			using Assembler assembler = Assembler.CreateAssemblerWithMapper(mapper, entry);

			if (baseRom is not null) {
				assembler.BaseRom = Helpers.GetFileAsArray(baseRom);
			}

			string romSizeStr = args.GetValue(Option_Size) ?? "auto";

			if (romSizeStr.EqualsI("auto")) {
				int size = assembler.BaseRom?.Length ?? 0;

				if (size < assembler.MinRomSize) {
					assembler.InitialRomSize = assembler.MinRomSize;
				} else if (size > assembler.MaxRomSize) {
					assembler.InitialRomSize = assembler.MaxRomSize;
				} else {
					assembler.InitialRomSize = size;
				}
			} else {
				if (RomHeader.TryGetRomSize(romSizeStr, out int romSizeInt)) {
					assembler.InitialRomSize = romSizeInt;
				} else {
					Error($"Invalid ROM size: {romSizeStr}");
					return Exit_Error;
				}
			}

			assembler.CalculateChecksum = args.GetResult(Option_FixChecksum) is not null;
			assembler.OverflowAction = RomOverflowAction.Grow;

			bool showTimer = args.GetValue(Option_Timer);

			Notice(StartedAssembly);

			long started = Stopwatch.GetTimestamp();
			assembler.AssembleFile(outputFile);
			long finished = Stopwatch.GetTimestamp();

			Notice(FinishedAssembly);
			PostErrors(assembler);

			if (showTimer) {
				WriteTimer(started, finished);
			}

			return Exit_Good;
		} catch (Exception e) {
			FatalError(e);
			return e.HResult;
		}
	}

		

	static void DrainKeyBuffer() {
		while (Console.KeyAvailable) {
			Console.ReadKey(true);
		}
	}

	private const string StartedAssembly = "Assembly started...";
	private const string FinishedAssembly = "Assembly complete!";
	private const string StarSep = "****************************************************************************************************";
	private const string LineSep = "----------------------------------------------------------------------------------------------------";

	static int Assemble(ParseResult args) {
		if (args.GetValue(Arg_ManifestFile) is not FileInfo manifestFile) {
			return Exit_Error;
		}

		bool redoAssembly = !args.GetValue(Option_AssembleOnce);
		bool clearConsole = !args.GetValue(Option_PreserveConsole);
		bool showTimer = args.GetValue(Option_Timer);
		bool pauseAfter = args.GetValue(Option_PauseAfter);


		int retcode = 0;

		string fileName = manifestFile.FullName;

		if (manifestFile.DirectoryName is string dir) {
			Directory.SetCurrentDirectory(dir);
		}

		using ManifestContainer manifest = new(manifestFile);

		do {
			retcode = AssembleOne(manifest);

			DrainKeyBuffer();

			if (redoAssembly) {
				Console.WriteLine();
				Notice("Press ENTER to rerun assembly. Press any other key to exit.");

				var key = Console.ReadKey(false).Key;
				redoAssembly = key == ConsoleKey.Enter;

				if (redoAssembly) {
					if (clearConsole) {
						Console.Clear();
					} else {
						Console.WriteLine();
					}
				}
			} else if (pauseAfter) {
				Console.ReadKey(false);
			}
		} while (redoAssembly);

		return retcode;

		int AssembleOne(ManifestContainer manifest) {
			try {
				manifest.ParseManifest();
				manifest.Configure();

				if (!manifest.ManifestGood) {
					BadNotice("Aborting assembly while errors are present.");
					return Exit_Error;
				}

				Assembler assembler = manifest.GetAssembler();

				Notice(StartedAssembly);

				long started = Stopwatch.GetTimestamp();
				byte[] rom = assembler.Assemble();
				long finished = Stopwatch.GetTimestamp();

				Notice(FinishedAssembly);

				if (showTimer) {
					WriteTimer(started, finished);
				}

				// Write the ROM
				if (!manifest.TryGetOutputStream(out var romstream)) {
					BadNotice("Problem loading output stream.");
					return Exit_Error;
				}

				romstream.Clear();
				romstream.Write(rom);

				bool didAnalysisSep = false;

				if (manifest.TryGetString("crc", out var crcGet)) {
					AddOutputHeader();
					EmitManifestHashInfo("CRC32", Helpers.GetCrc32(rom), crcGet);
				}

				if (manifest.TryGetString("md5", out var md5Get)) {
					AddOutputHeader();
					EmitManifestHashInfo("MD5", System.Security.Cryptography.MD5.HashData(rom), md5Get);
				}

				if (manifest.TryGetString("sha1", out var sha1Get)) {
					AddOutputHeader();
					EmitManifestHashInfo("SHA-1", System.Security.Cryptography.SHA1.HashData(rom), sha1Get);
				}

				if (manifest.TryGetDiffFile(out var diffRom)) {
					AddOutputHeader();
					CreateDifferenceFile(rom, diffRom, fileName, assembler.OffsetToAddress);
				}

				manifest.TryExportSymbols();

				PostErrors(assembler);

				return manifest.ManifestGood ? Exit_Good : Exit_Error;


				//////////////////////////////////////////////////////////////////



				void AddOutputHeader() {
					if (!didAnalysisSep) {
						Console.WriteLine();
						Notice(LineSep);
						Notice("-- Output analysis");
						Notice(LineSep);
						didAnalysisSep = true;
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e.GetType());
				FatalError(e);
				return e.HResult;
			}
		}
	}

	static void EmitManifestHashInfo(string name, byte[] hashBytes, string expectedVal) {
		int spanLen = hashBytes.Length * 2;

		Span<char> realVal = spanLen < 128
			? stackalloc char[spanLen]
			: new char[spanLen];

		Convert.TryToHexString(hashBytes, realVal, out _);

		const int padlen = 15;
		string outputName = $"Output {name}";

		if (expectedVal.EqualsI(realVal)) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write($"{outputName,padlen}: ");
			Console.WriteLine(realVal);
		} else {
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.Write($"{outputName,padlen}: ");
			Console.WriteLine(realVal);
			//             "123456789ABCDEF" <-- padlen
			Console.Write($"       Expected: ");
			Console.WriteLine(expectedVal);
		}

		Console.ResetColor();
	}

	static void CreateDifferenceFile(byte[] rom, byte[] diffRom, string fileName, Func<int, int> offsetToAddress) {
		FileInfo diffText = new(Path.GetTempFileName());

		bool hasDiff = false;

		using (var diffWriter = diffText.CreateText()) {
			int romlength = rom.Length;

			if (romlength != diffRom.Length) {
				diffWriter.WriteLine($"File size mismatch: {romlength} | expected: {diffRom.Length}");
				hasDiff = true;
				romlength = int.Min(romlength, diffRom.Length);
			}

			var romWhole = rom.AsSpan(0, romlength);
			var nomWhole = diffRom.AsSpan(0, romlength);

			if (!romWhole.SequenceEqual(nomWhole)) {
				hasDiff = true;

				unsafe {
					const string errSep = "==================================================================";
					const int bytesPerRow = 16;
					const int addressLineLength = 19;
					const int hexCharSpanLength = bytesPerRow * 2;
					const int diffCharSpanLength = hexCharSpanLength + bytesPerRow - 1;
					const int fullAddressLineLength = addressLineLength + diffCharSpanLength;

					char* hexCharPtr = stackalloc char[hexCharSpanLength];
					char* addressCharPtr = stackalloc char[fullAddressLineLength];


					char* diffWriteBase = addressCharPtr + addressLineLength;

					Span<char> diffCharSpan = new(diffWriteBase, diffCharSpanLength);
					Span<char> hexCharSpan = new(hexCharPtr, hexCharSpanLength);
					Span<char> addressLineSpan = new(addressCharPtr, fullAddressLineLength);

					//  0123456789012345678
					// "000000 ($000000) | "
					addressLineSpan.Fill(' ');
					addressLineSpan[7] = '(';
					addressLineSpan[8] = '$';
					addressLineSpan[15] = ')';
					addressLineSpan[17] = '|';

					for (int i = 0; i < romlength; i += bytesPerRow) {
						int rem = int.Min(romlength - i, bytesPerRow);

						var romSlice = romWhole.Slice(i, rem);
						var nomSlice = nomWhole.Slice(i, rem);

						if (romSlice.SequenceEqual(nomSlice)) {
							continue;
						}

						diffWriter.WriteLine(errSep);

						diffCharSpan.Fill(' ');

						Convert.TryToHexString(romSlice, hexCharSpan, out _);

						// reformat the converted hex string to have spaces between each hex number
						char* diffWritePtr = diffWriteBase;
						char* hexWritePtr = hexCharPtr;
						for (int k = 0; k < romSlice.Length; k++, hexWritePtr += 2, diffWritePtr += 3) {
							*(uint*) diffWritePtr = *(uint*) hexWritePtr;
						}


						Helpers.WriteHexAddress(i, addressLineSpan, 0);
						Helpers.WriteHexAddress(offsetToAddress(i), addressLineSpan, 9);
						diffWriter.WriteLine(addressLineSpan[..(addressLineLength + (rem * 3 - 1))]);

						diffCharSpan.Fill(' ');
						Convert.TryToHexString(nomSlice, hexCharSpan, out _);

						// reformat the converted hex string to have spaces between each hex number

						diffWritePtr = diffWriteBase;
						hexWritePtr = hexCharPtr;

						for (int k = 0; k < romSlice.Length; k++, hexWritePtr += 2, diffWritePtr += 3) {
							if (romSlice[k] != nomSlice[k]) {
								*(uint*) diffWritePtr = *(uint*) hexWritePtr;
							}
						}

						diffWriter.Write("                 | ");
						diffWriter.WriteLine(diffCharSpan[..(rem * 3 - 1)]);

					}
				}
			}
		}

		if (hasDiff) {
			string diffFileName = $"{Path.GetFileNameWithoutExtension(fileName)}-diff.txt";
			Helpers.WaitForAndMoveFile(diffText, diffFileName);
			Console.WriteLine();
			Console.WriteLine($"Differences found between output and nominal. Diff log created at '{diffFileName}'");
		} else {
			Console.WriteLine("No differences found between output and nominal.");
		}
	} 

	static void FatalError(Exception e) {
		DrainKeyBuffer();

		Error(e.Message);

		Console.BackgroundColor = ConsoleColor.Red;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("Print trace? (Y/N) ");
		Console.ResetColor();

		while (true) {
			var key = Console.ReadKey(false).Key;

			if (key is ConsoleKey.Y) {
				Console.Error.WriteLine();
				Error(e.StackTrace ?? "\tStack trace not available.");
			} else if (key is ConsoleKey.N or ConsoleKey.Enter) {
				break;
			}
		}
	}


	internal static void ExportSymbols_Wla(string outputPath, Assembler assembler) {
		FileInfo symfile = new(Path.ChangeExtension(outputPath, ".sym"));

		using var w = new StreamWriter(Helpers.WaitForAndCreateStream(symfile));
		w.BaseStream.Clear();

		var list = assembler.Symbols.Values;

		w.WriteLine("[labels]");

		Span<char> symaddress = stackalloc char[7];
		symaddress[2] = ':';
		symaddress[6] = ' ';


		foreach (var s in list) {
			int wval = s.DefaultValue;
			symaddress[0] = GetCharFromNum(wval >> 20);
			symaddress[1] = GetCharFromNum(wval >> 16);
			symaddress[3] = GetCharFromNum(wval >> 12);
			symaddress[4] = GetCharFromNum(wval >> 8);
			symaddress[5] = GetCharFromNum(wval >> 4);
			symaddress[6] = GetCharFromNum(wval >> 0);

			w.Write(symaddress);
			w.Write(' ');
			w.WriteLine(s.Name);
		}


		static char GetCharFromNum(int n) {
			n &= 0xF;

			int offset = n < 10 ? '0' : ('A' - 10);

			return (char) (offset + n);
		}

	}

	internal static void ExportSymbols_Mlb(string outputPath, Assembler assembler) {
		FileInfo symfile = new(Path.ChangeExtension(outputPath, ".mlb"));

		using var w = new StreamWriter(Helpers.WaitForAndCreateStream(symfile));
		w.BaseStream.Clear();

		var list = assembler.Symbols.Values;

		// set up some stack space for hex conversion
		Span<char> addrHex = stackalloc char[13];

		Span<char> addrHexShort = addrHex.Slice(0, 6);

		addrHex[6] = '-';

		foreach (var s in list) {
			int size;
			int addr;

			switch (s) {
				case Label or AssignedSymbol:
					size = 1;
					addr = s.DefaultValue;
					break;

				case AllocatedSymbol a:
					size = a.Size;
					addr = s.DefaultValue;
					break;

				case Buoy:
					size = 1;
					addr = s.Address;
					break;

				default:
					continue;
			}

			int writeOffset;

			if (assembler.AddressIsRom(addr)) {
				if (assembler.Coprocessor is Coprocessor.SA1 && AddressIsBwram(addr, out writeOffset)) {
					w.Write("SnesSaveRam:");
				} else {
					w.Write("SnesPrgRom:");
					writeOffset = s.BinaryOffset;
				}

			} else if (AddressIsWram(addr, out writeOffset)) {
				w.Write("SnesWorkRam:");

			} else if (assembler.Coprocessor is Coprocessor.SA1 && AddressIsIram(addr, out writeOffset)) {
				w.Write("Sa1InternalRam:");

			} else {
				w.Write("SnesMemory:");
				writeOffset = s.DefaultValue;
			}

			Helpers.WriteHexAddress(writeOffset, addrHex, 0);

			if (size < 2) {
				w.Write(addrHexShort);
			} else {
				Helpers.WriteHexAddress(writeOffset + size - 1, addrHex, 7);
				w.Write(addrHex);
			}

			w.Write(':');
			w.WriteLine(s.Name);
			
		}
	}


	static bool AddressIsBwram(int addr, out int offset) {
		if (((uint) addr & 0xFFF0_0000u) is 0x40_0000) {
			offset = addr & 0x0F_FFFF;
			return true;
		}

		offset = 0;
		return false;
	}

	static bool AddressIsWram(int addr, out int offset) {
		// test for WRAM
		// including full int test elides bounds checking
		if (((uint) addr & 0xFFFE_0000u) is 0x7E_0000) {
			offset = addr & 0x1_FFFF;
			return true;
		}

		// limit to mirrored banks
		// ditto re:bounds
		addr &= unchecked((int) 0xFF40_FFFF);

		if ((uint) addr < 0x2000) {
			offset = addr;
			return true;
		}

		offset = 0;
		return false;
	}

	static bool AddressIsIram(int addr, out int offset) {
		// test for WRAM
		// including full int test elides bounds checking
		addr &= unchecked((int) 0xFF40_FFFF);

		if ((uint) addr is >= 0x3000 and < 0x4000) {
			offset = addr & 0x1FFF;
			return true;
		}

		offset = 0;
		return false;
	}



	static int PrintFutaba(ParseResult _) {
		string w =
			@"R27 L05.';:cR02oR03xR0AkR03xR03oR02cL04;,'.&R14 L09.;clo=o:.R05 L05." +
			@",:loR1D=L02olR02kL05oc;'.&R11 L05.c=kOR02kR02=L0AkO=,.,c=kOR02kR25=L" +
			@"06k=oc,.&R0F L04.ckOR09=R020R02kR23=L01kR02OR08=L05kO=l'&R0E L03,=kR" +
			@"09=L04O0OkR26=L01kR020L01kR09=R02kL03=:.&R0D L01:R02kR08=L01kR02OL01" +
			@"kR29=L01kR020L01kR0B=L04k=l'&R0C L01:R02kR08=L03O0kR2D=R020R0D=R02kL" +
			@"02o,&R0B L01:R02kR07=L03k0OR03=L01kR2B=L01kR02OR0F=L03ko'&R0A L01;R0" +
			@"2kR07=L03O0kR02=L03kOkR13=R02OR17=L03kOkR11=L02c.&R09 L03.=0R07=L03O" +
			@"0kR02=R02OR14=L03k0OR18=L03k0OR10=L03k='&R09 L03;OkR06=L03O0kR02=R02" +
			@"OR0E=R02kR05=L03k0OR18=L02kOR03kR11=&R09 L02c0R06=L03O0kR02=R02OR0E=" +
			@"R02kR06=L030KOR18=R02kR02;R02kR11=&R09 L02l0R05=L03O0kR02=R02OR0E=R0" +
			@"2kR06=L04O=kOR19=L06O; '=OR11=&R09 L02c0R04=L03O0kR02=R02OR0F=R02kR0" +
			@"5=L05Ol.cOR19=L02k;R02 L03.=OR10=L02l.&R09 L03;0kR02=L03k0OR02=L03kO" +
			@"kR0E=R02kR04=R02kL01cR02 L02:OR18=R02kL01,R03 L03.okR10=&R09 L03'kOR" +
			@"02=L02O0R03=R02OR0F=L01kR04=R02kL01:R03 L01,R02kR17=L03k='R04 L03.ok" +
			@"R0F=&R09 L07.=O=k0OR02=L02kOR0F=R02kR03=L03k=,R04 L03.=OR03=R02kR12=" +
			@"L03Oo.R05 L03.=kR0E=L02o.&R0A L05l0=O0R03=R02OR0F=L01kR03=R02OL01lR0" +
			@"6xL02=0R02kL03OK0R10=R02kL030l.R06xL03ckOR0E=&R0A L03:O=R02OR03=L02O" +
			@"kR0E=R04kL03O=,R07 L08,kO=kO0OR0D=R02kL04=O=.R08 L03;OkR0D=&R0A L01," +
			@"R02kR02OR02=L02kOR0F=R02kL04=kl.R09 L07lO=k=okR0A=L01kR05=L02k;R0A L" +
			@"02ckR0D=&R0A L02.=R020L01kR02=L02kOR0E=R02kR02=L02c.R0A L08'=O0o,oOR" +
			@"08=L02O0R04=L03kl.R0A L02.oR0D=L02l.&R0A L01cR02OL020kR02=L02kOR0E=L" +
			@"04kO=:R0D L07:=Ok,;kR07=L01kR02OR03=L03kl.R0C L01,R0C=L03k=.&R08 L02" +
			@".lR02OL03k0OR02=L02kOR0E=L03k=,R0F L07.c0:.lOR05=L04kolkR03=L02c.R0D" +
			@" L03.okR09=R02kL02=;&R07 L04.oOkR02=L02O0R03=L01OR0E=L02k;R12 L06cl." +
			@"'=kR03=L08kl.ck=o,R10 L02:kR0A=L03';o&R05 L04,l=kR05=L020OR02=R02kL0" +
			@"3=OkR05=L01kR03=L03Ok'R02 L05.';:lR02oL05lc;'.R05 L01.R02 L02:kR03=L" +
			@"07c. lkc.R02 L04.':lR03=L04:,;.R04 L01,R09=L02kcR02 &R02 L04.:dkR08=" +
			@"L04k0O=R02OL01kR02OR05=R02kR02dL07O=:;c NR08WL02O:R02,L02;,R04 L05.l" +
			@"Od,R02 L03.l:R02.L04:d0NR04WL0BK; .,:;. .dR08=L02d'&L05 '=OdR0A=L08O" +
			@"K0=oO=oR05=L02kOR02=L030k:R02 L010R09WL02d.R03 L03':.R03 L03.:'R04 R" +
			@"02.L03;kNR07WL02K,R04 L02;cR02.L02=kR07=&L03 =OR0D=L04O=o;R03'R06=L0" +
			@"1OR02kL030o'R02 L01kR04NR04WL03KodR04 L02:.R0B L01xR02 R08KL02kcR04 " +
			@"L07;:.'=OkR02=L01kR02=L02l.&L02 0R0F=L02O/R03 L02;cR06=L01OR020L01lR" +
			@"14.R0D R12.R02 L03,0KR02=L05kOko.&L02 0R06=L03k0kR06=L03O=\R02 L02;l" +
			@"R07=L030K:R34 L04.kKkR02OL03=l.&L01 R02kR05=L03kocR07=L06Oo\ 'cR07=L" +
			@"03kO,R34 L03'k0R02=L02kc&L03 .lR02kR04=L04; 'oR06=L05O=, ;R07=R02kL0" +
			@"1,R34 L07;O=o=k;&R03 L03.:lR04=L01:R02 L01cR06=L01;R02 L02'oR07=L02O" +
			@";R34 L07cO=o=k;&R06 L06.',;:.R03 L06':l=k;R03 L01,R07=L02O:R0F L01,R" +
			@"12xL03c:.R0F L07cO=o=k;&R12 L01.R02;L01.R03 L01:R06=L02O:R0E L03.l\R" +
			@"02 L01/R0E L02dcR0F L03.=kR02oR02=&R19 L02.lR05=L03Oo.R0D L05.l \/R0" +
			@"E L03.l.R0E L04'=O=R02oR02=&R1A L01,R05=L05OKOl'R0C R02cR0E L01.R02:" +
			@"L01.R0D L0B':=k=o=o=l.&R1A L02.lR04=L01OR02=L05Olc:'R09 L01'R02oR0C " +
			@"L03.c;R0C L04';:;R02.L02l=R02oR02=&R18 L03.,lR02OR02=L08kO;.oc',R03:" +
			@"L02,.R06 L01'R02lL01,R08 L04.,:;R08 R02.L01,R03;L01.R04 L06.o=o=:&R1" +
			@"6 L03.;dR02=L040KkdR02kL06' .co'R02 L03.;:R03;L01,R02.R02 L01.R02:R0" +
			@"6.L03',;R05 R02.L02,;R03,L02'.R08 L05'd=:.&R12 L04.,cdR02OL01kR02dL0" +
			@"1kR020R02OL02=.R03 L04'c:.R04 L04.';:R04cL0Ald=c,;:;,'R02,R03'R02,L0" +
			@"1'R02.R0E L02p'&R10 L05'oO0OR03=R02dL02o=R02OL030=oR06 L04,c:.R09 L0" +
			@"B;0='.lK0=odR02kL02l.&R0F L01,R03=R02OL03=doR02dL08odkOk0k,R07 L05.;" +
			@":,.R05 L04.ok'R02 L05:K0kdR03oL03O=.&R0E L03'd=R03oL05=OkdoR04dR02OL" +
			@"04k0k:R08 L05.';,'R02.L04'lc.R03 L05cKOk=R02oL04dkO:&R0E L03:kdR04oL" +
			@"03dOkR02dR03oL02=OR02kL03Ok:R0B L05.';,.R04 L04.o0kR02=L03do=R02k&@";

		int x = 0;
		List<string> l = [];
		StringBuilder m = new();
		while (true) {
			char c;
			if ((c = w[x++]) is '@') {
				break;
			}

			switch (--c) {
				case '%':
					l.Add(m.ToString());
					m.Clear();
					break;

				case 'K':
					int o = int.Parse(w[x++..++x], System.Globalization.NumberStyles.HexNumber);
					int y = x + o;
					m.Append(w[x..y]);
					x = y;
					break;

				case 'Q':
					o = int.Parse(w[x++..++x], System.Globalization.NumberStyles.HexNumber);
					m.Append(new string(w[x++], o));
					break;
			}
		}

		foreach (var k in l) Console.WriteLine(k);

		return 251689658;
	}


	static void WriteTimer(long started, long finished) {
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Write($"Completed in {Stopwatch.GetElapsedTime(started, finished).TotalMilliseconds:#}ms");
		Console.ResetColor();
		Console.WriteLine();
	}

	internal static void Error(string s) {
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.Write(s);
		Console.ResetColor();
		Console.WriteLine();
	}

	internal static void Warning(string s) {
		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.Error.Write(s);
		Console.ResetColor();
		Console.WriteLine();
	}

	internal static void Notice(string s) {
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.Write(s);
		Console.ResetColor();
		Console.WriteLine();
	}

	internal static void PostErrors(Assembler assembler) {
		int wc = assembler.WarningCount;
		int ec = assembler.ErrorCount;

		if ((wc, ec) is (0, 0)) {
			return;
		}

		Console.WriteLine();
		Console.ForegroundColor = ec > 0 ? ConsoleColor.Red : ConsoleColor.DarkYellow;

		string ws = wc == 1 ? "warning" : "warnings";
		string es = ec == 1 ? "error" : "errors";

		Console.Write($"Assembly completed with {wc} {ws} and {ec} {es}.");

		if (ec > 0) {
			Console.Write(" Output may be invalid.");
		}

		Console.ResetColor();
		Console.WriteLine();
	}

	internal static void BadNotice(string s) {
		Console.ForegroundColor = ConsoleColor.White;
		Console.BackgroundColor = ConsoleColor.Red;
		Console.Write(s);
		Console.ResetColor();
		Console.WriteLine();
	}


	// lazy load these to minimize allocations
	internal static string ValidRomSizeStrings => field ??= string.Join(", ", RomHeader.GetValidRomSizeTokens().Select(o => $"'{o}'"));
	internal static Register[] DMA => field ??= Register.DmaRegisters;
	internal static Register[] DMAalt => field ??= Register.DmaAltNameRegisters;
	internal static Register[] DSP => field ??= Register.SoundDspRegisters;
	internal static Register[] Joypad => field ??= Register.JoypadRegisters;
	internal static Register[] PPU => field ??= Register.PpuRegisters;
	internal static Register[] SA1 => field ??= Register.Sa1Registers;
	internal static Register[] SCPU => field ??= Register.SnesCpuRegisters;
	internal static Register[] SPC => field ??= Register.Spc700Registers;
	internal static Register[] SuperFX => field ??= Register.SuperFxRegisters;





	static readonly Argument<FileInfo> Arg_ManifestFile = new("manifest"){
		Description = "manifest file with assembly configuration",
		Arity = ArgumentArity.ExactlyOne,
	};

	static readonly Option<bool> Option_PreserveConsole = new("--keep-old-logs", "-k") {
		Description = "Disables clearing the console on each assembly",
		Arity = ArgumentArity.Zero,
	};

	static readonly Option<bool> Option_AssembleOnce = new("--no-retry", "-1") {
		Description = "Skips the retry prompt after assembly",
		Arity = ArgumentArity.Zero,
	};

	static readonly Option<bool> Option_PauseAfter = new("--pause") {
		Description = "Waits for input after assembly. Only applicable with --no-retry",
		Arity = ArgumentArity.Zero,
	};

	static readonly Option<bool> Option_Timer = new("--show-time", "-t") {
		Description = "Enable a timer",
		Arity = ArgumentArity.Zero,
#if DEBUG
		DefaultValueFactory = _ => true,
#endif
		Hidden = true,
	};



	static readonly Argument<FileInfo> Arg_Entry = new("entry"){
		Description = "assembly entry",
		Arity = ArgumentArity.ExactlyOne,
	};

	static readonly Argument<FileInfo> Arg_BaseRom = new("baserom") {
		Description = "base rom binary",
		Arity = ArgumentArity.ZeroOrOne,
	};

	static readonly Argument<FileInfo> Arg_PatchBase = new("baserom") {
		Description = "base rom binary",
		Arity = ArgumentArity.ExactlyOne,
	};

	static readonly Argument<FileInfo?> Arg_OutputRom = new("output") {
		Description = "output file name",
		DefaultValueFactory = _ => null,
		Arity = ArgumentArity.ZeroOrOne,
	};

	static readonly Argument<FileInfo> Arg_TargetRom = new("rom") {
		Description = "target rom file name",
		Arity = ArgumentArity.ExactlyOne,
	};

	static readonly Option<MapperMode> Arg_MapMode = new("--mapmode", "-m") {
		Description = "Mapper mode used for assembly",
		Arity = ArgumentArity.ExactlyOne,
	};


	static readonly Option<string> Option_Size = new("--size", "-s") {
		Description = "Output ROM size",
		DefaultValueFactory = _ => "auto",
		Arity = ArgumentArity.ZeroOrOne,
	};

	static readonly Option<bool> Option_FixChecksum = new("--fix-checksum", "-c") {
		Description = "Automatically adjusts the checksum",
		Arity = ArgumentArity.Zero,
	};

}

file sealed class ShowVersionThing : System.CommandLine.Invocation.SynchronousCommandLineAction {
	private readonly Action _syncAction;

	internal ShowVersionThing(Action action) => _syncAction = action;

	/// <inheritdoc />
	public override int Invoke(ParseResult parseResult) {
		_syncAction();
		return 1;
	}
}
