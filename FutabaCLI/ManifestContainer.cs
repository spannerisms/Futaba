namespace FutabaCLI;

internal class ManifestContainer(FileInfo manifest) : IDisposable {
	private static readonly Random random = new();

	private readonly string fileName = manifest.FullName;

	private LoadedItem<byte[]>? baseromfile = null;
	private LoadedItem<byte[]>? diffromfile = null;

	private LoadedItem<StreamWriter>? StdOut {
		get;
		set {
			field?.Dispose();
			field = value;
		}
	} = null;

	private LoadedItem<StreamWriter>? ErrOut {
		get;
		set {
			field?.Dispose();
			field = value;
		}
	} = null;


	public string OutputPath {
		get;
		private set;
	} = Path.ChangeExtension(manifest.FullName, Igarashi.RomExtension);


	private LoadedItem<Stream>? OutputStream {
		get;
		set {
			field?.Dispose();
			field = value;
		}
	} = null;

	private Assembler? Assembler {
		get;
		set {
			field?.Dispose();
			field = value;
		}
	} = null;






	private byte[]? baserom = null;
	private int? romsize = 0;
	private int fillstart = 0;

	public bool ManifestGood { get; private set; } = false;

	private DateTime LastUpdated = DateTime.MinValue;

	private bool AssemblerNeedsUpdating = true;

	private bool AssemblerSeeded = false;

	private readonly Dictionary<string, ArgInfo> argVals = [];
	private readonly Dictionary<string, Variable> prevariables = [];
	private readonly Dictionary<string, FutabaSymbol> presymbols = [];


	public Assembler GetAssembler() {
		if (Assembler is null) {
			throw new InvalidOperationException("Assembler is null.");
		}

		Assembler.SeedRNG(random.NextInt64(), random.NextInt64());
		return Assembler;
	}

	internal void Configure() {
		try {
			if (ManifestGood && AssemblerNeedsUpdating) {
				PrepAssembler();
			}
		} catch {
			ManifestGood = false;
		}
	}


	private void PrepAssembler() {
		if (!TryGetArg("entry", out var fileEntry)) {
			ManifestErrorNoLine("No entry file specified.");
			return;
		}

		if (!TryGetArg("mapmode", out var mapMode)) {
			ManifestErrorNoLine("No mapper mode specified.");
			return;
		}

		if (!TryGetString("output", out var outputGet)) {
			outputGet = Path.ChangeExtension(fileName, Igarashi.RomExtension);
		}

		string entryPoint = fileEntry.Value;

		if (RomHeader.TryGetMapperMode(mapMode.Value, out var modeEnum)) {
			if (Assembler is null
				|| Assembler.Mapper != modeEnum
				|| Path.GetFullPath(entryPoint) != Assembler.EntryPoint.FullName) {

				Igarashi.Notice("Creating assembler...");
				Assembler = Assembler.CreateAssemblerWithMapper(modeEnum, entryPoint);
			}
		} else {
			ManifestError($"Invalid mapper mode: {mapMode.Value}", mapMode.Line);
			return;
		}


		Igarashi.Notice("Configuring assembler...");


		Assembler.ClearInitialSymbols();
		Assembler.ClearInitialVariables();

		OutputPath = Path.GetFullPath(outputGet);

		if (OutputStream is null || OutputStream.Reload(OutputPath)) {
			OutputStream?.Dispose();

			OutputStream = new(outputGet, Helpers.WaitForAndCreateStream(outputGet));
		}


		if (TryGetArg("coprocessor", out var coprocessor)
			&& CoprocessorLookup.TryGetValue(coprocessor.Value, out var copval)) {
			Assembler.Coprocessor = copval;
			
		} else if (Assembler.Mapper is not MapperMode.Sa1) {
			Assembler.Coprocessor = Coprocessor.None;
		}

		bool autofill = GetBoolOrDefault("autofill", false, out var _);
		Assembler.AutoPopulateHeader = autofill;

		Assembler.Title = GetStringOrDefault("title");
		Assembler.MakerCode = GetStringOrDefault("makercode");
		Assembler.GameCode = GetStringOrDefault("gamecode");
		
		if (TryGetArg("region", out var regionGet)) {
			if (Region.TryGetRegion(regionGet.Value, out var regionObj)) {
				Assembler.Destination = regionObj;
			} else {
				ManifestWarning($"Invalid region name: '{regionGet.Value}'", regionGet.Line);
			}
		} else {
			Assembler.Destination = Region.Japan;
		}

		Assembler.RomVersion = GetByteOrDefault("version", 0);
		Assembler.SpecialVersion = GetByteOrDefault("specialversion", 0);
		Assembler.CartridgeSubtype = GetByteOrDefault("cartridgetype", 0);


		bool autochecksum = GetBoolOrDefault("checksum", false, out ArgParse parsed);

		if (parsed is ArgParse.Valid && (autofill && !autochecksum)) {
			ManifestWarningNoLine("Ignoring 'autochecksum=false' because header 'autofill=true'.");
			autochecksum = true;
		}
		Assembler.CalculateChecksum = autochecksum;

		Assembler.FastRom = GetBoolOrDefault("fastrom", false, out _);
		Assembler.ExtendedHeader = GetBoolOrDefault("extended", true, out _);

		if (TryGetArg("ramsize", out var ramsizeget)) {
			if (RomHeader.TryGetRamSize(ramsizeget.Value, out int ramsizeval)) {
				Assembler.RamSize = ramsizeval;
			} else {
				ManifestError($"Invalid argument to ramsize: \"{ramsizeget.Value}\"", ramsizeget.Line);
				Assembler.RamSize = 0;
			}
		} else {
			Assembler.RamSize = 0;
		}

		bool setSize = false;
		bool forceNullFill = false;

		if (TryGetArg("romsize", out var romsizestr)) {
			if (RomHeader.TryGetRomSize(romsizestr.Value, out int romsizeget)) {
				romsize = romsizeget;
				setSize = true;
			} else {
				ManifestError($"Invalid argument to romsize: \"{romsizestr.Value}\"", romsizestr.Line);
				romsize = null;
			}
		} else {
			romsize = null;
		}


		if (TryOpenBinary("base", ref baseromfile)) {
			int allocSize;

			setSize = true;

			if (romsize is null) {
				allocSize = baseromfile.Item.Length;
				fillstart = allocSize;

				if (allocSize > Assembler.MaxRomSize) {
					ManifestWarningNoLine("Base ROM file exceeds maximum length. It has been truncated.");
					allocSize = Assembler.MaxRomSize;
					fillstart = allocSize;

				} else if (allocSize < Assembler.MinRomSize) {
					ManifestWarningNoLine("Base ROM file is too small on its own. ROM will be padded to reach minimum length.");
					fillstart = allocSize;
					allocSize = Assembler.MinRomSize;
					forceNullFill = true;

				}
			} else {
				fillstart = (int) long.Min(baseromfile.Item.Length, romsize.Value);

				allocSize = romsize.Value;
			}

			AllocateBaseRom(allocSize);
			Assembler.InitialRomSize = allocSize;
			Array.Copy(baseromfile.Item, baserom!, fillstart);

		} else {
			fillstart = 0;

			if (romsize is null) {
				baserom = null;
			} else {
				AllocateBaseRom(romsize.Value);
				Assembler.InitialRomSize = romsize.Value;
			}
		}

		if (!setSize) {
			ManifestWarningNoLine("With no 'romsize' or 'base' field, initial size has been set to minimum.");
			Assembler.InitialRomSize = Assembler.MinRomSize;
		}

		bool doNullFill = baserom is not null && fillstart < baserom?.Length;


		if (TryGetArg("nullfill", out var nullfillGet)) {
			string nullfillOperand = nullfillGet.Value;
			int nullComma = nullfillOperand.IndexOf(',');

			string? nullFillType, nullFillValue = null;

			if (nullComma < 0) {
				nullFillType = nullfillOperand.Trim();
			} else {
				nullFillType = new(nullfillOperand.AsSpan(0, nullComma).Trim());

				if (++nullComma < nullfillOperand.Length) {
					var tmp = nullfillOperand.AsSpan(nullComma).Trim();

					if (!tmp.IsEmpty) {
						nullFillValue = new(tmp);
					}
				}
			}

			if (!forceNullFill && (romsize is null || baserom is null)) {
				ManifestWarning("Ignoring 'nullfill' while 'romsize' is unspecified.", nullfillGet.Line);
			} else {
				if (string.IsNullOrWhiteSpace(nullFillType)) {
					if (nullFillValue is not null) {
						ManifestWarning("No nullfill type provided.", nullfillGet.Line);
					}
				} else if (nullFillType.EqualsI("zero")) {
					if (nullFillValue is not null) {
						ManifestWarning("Ignoring 'nullfill' value' while type is 'zero'.", nullfillGet.Line);
					}

					baserom.AsSpan(fillstart).Clear();
				} else if (nullFillType.EqualsI("byte")) {
					if (nullFillValue is null) {
						ManifestError("'nullfill=byte' requires a comma-separated argument.", nullfillGet.Line);
					} else {
						var fillspan = Helpers.CleanSplit(nullFillValue, ' ');

						int filllen = fillspan.Length;
						int linenum = nullfillGet.Line;
						byte[] fillarray = new byte[fillspan.Length];
						bool good = true;

						for (int fi = 0; fi < filllen; fi++) {
							string fillatom = fillspan[fi];
							if (Helpers.TryParseByte(fillatom, out byte fillb)) {
								fillarray[fi] = fillb;
							} else {
								ManifestError($"Invalid hex number in 'nullfill' value at index {fi}: {fillatom}", linenum);
								good = false;
							}
						}

						if (good && doNullFill) {
							Helpers.FillArray(fillarray, baserom!, fillstart);
						}
					}
				} else if (nullfillGet.Value.EqualsI("string")) {
					if (nullFillValue is null) {
						ManifestError("'nullfill=string' requires a comma-separated argument.", nullfillGet.Line);
					} else if (doNullFill) {
						Helpers.FillArray(System.Text.Encoding.ASCII.GetBytes(nullFillValue), baserom!, fillstart);
					}
				} else if (nullfillGet.Value.EqualsI("random")) {
					long rngseed1, rngseed2;

					if (nullFillValue is null) {
						rngseed1 = Random.Shared.NextInt64();
						rngseed2 = Random.Shared.NextInt64();
					} else {
						(rngseed1, rngseed2) = Helpers.HashString(nullFillValue);
					}

					if (doNullFill) {
						Helpers.FillRandom((ulong) rngseed1, (ulong) rngseed2, baserom.AsSpan(fillstart));
					}
						
				}
			}
		} else if (doNullFill) {
			baserom?.AsSpan(fillstart).Clear();
		}

		Assembler.BaseRom = baserom;
		AssemblerSeeded = TryGetString("randomseed", out var randomseed);

		if (AssemblerSeeded) {
			var (seedA, seedB) = Helpers.HashString(randomseed);
			Assembler.SeedRNG(seedA, seedB);
		}

		if (TryGetArg("addsymbols", out var symrecord)) {
			var symbolNames = Helpers.CleanSplit(symrecord.Value, ',');

			foreach (string symbolId in symbolNames) {
				switch (symbolId.ToLower()) {
					case "standard":
						Assembler.AddSymbols(Igarashi.SPC);
						Assembler.AddSymbols(Igarashi.DSP);
						Assembler.AddSymbols(Igarashi.DMAalt);
						goto case "snes";

					case "snes":
					case "sfc":
						Assembler.AddSymbols(Igarashi.PPU);
						Assembler.AddSymbols(Igarashi.SCPU);
						Assembler.AddSymbols(Igarashi.Joypad);
						Assembler.AddSymbols(Igarashi.DMA);
						break;

					case "ppu":
						Assembler.AddSymbols(Igarashi.PPU);
						break;

					case "cpu":
						Assembler.AddSymbols(Igarashi.SCPU);
						break;

					case "joypad":
						Assembler.AddSymbols(Igarashi.Joypad);
						break;

					case "dma":
						Assembler.AddSymbols(Igarashi.DMA);
						break;

					case "sound":
					case "apu":
						Assembler.AddSymbols(Igarashi.SPC);
						Assembler.AddSymbols(Igarashi.DSP);
						break;

					case "spc":
						Assembler.AddSymbols(Igarashi.SPC);
						break;

					case "dsp":
						Assembler.AddSymbols(Igarashi.DSP);
						break;

					case "sa1":
					case "sa-1":
						Assembler.AddSymbols(Igarashi.SA1);
						break;

					case "sfx":
					case "gsu":
						Assembler.AddSymbols(Igarashi.SuperFX);
						break;

					default:
						ManifestWarning($"Ignoring invalid symbols group name {symbolId}", symrecord.Line);
						break;
				}
			}
		}

		Assembler.AddSymbols(presymbols.Values);
		Assembler.AddVariables(prevariables.Values);

		Assembler.MaximumErrors = GetInt32OrDefault("maxerrors", int.MaxValue);
		Assembler.WarningsAreErrors = GetBoolOrDefault("warnaserror", false, out _);

		MissingTokenSeverity tokenWarnLevel = MissingTokenSeverity.Ambiguous;

		if (TryGetArg("tokens", out var tokensGet)) {
			if (!MissingTokenLookup.TryGetValue(tokensGet.Value, out tokenWarnLevel)) {
				ManifestError($"Invalid token warning level: '{tokensGet.Value}'", tokensGet.Line);
				tokenWarnLevel = MissingTokenSeverity.Ambiguous;
			}
		}

		Assembler.MissingTokenSeverity = tokenWarnLevel;

		RomOverflowAction overflowAction = RomOverflowAction.Error;

		if (TryGetArg("overflow", out var overflowGet)) {
			if (!OverflowLookup.TryGetValue(overflowGet.Value, out overflowAction)) {
				ManifestError($"Invalid overflow action level: '{overflowGet.Value}'", overflowGet.Line);
				overflowAction = RomOverflowAction.Error;
			}
		}
		Assembler.OverflowAction = overflowAction;


		if (TryGetFileInfo("stdout", out var stdoutGet, out int stdoutLine)) {
			if (StdOut?.Reload(stdoutGet) ?? true) {
				try {
					StdOut?.Dispose();
					StdOut = new(stdoutGet.FullName, new(Helpers.WaitForAndCreateStream(stdoutGet)));
				} catch (Exception e) {
					StdOut = null;
					ProblemLoadingFile(e, stdoutLine);
				}
			}
		} else {
			StdOut = null;
		}

		Assembler.MessageOut = StdOut?.Item ?? Console.Out;

		if (TryGetFileInfo("errorout", out var erroroutGet, out int erroroutLine)) {
			if (ErrOut?.Reload(erroroutGet) ?? true) {
				try {
					ErrOut?.Dispose();
					ErrOut = new(erroroutGet.FullName, new(Helpers.WaitForAndCreateStream(erroroutGet)));
				} catch (Exception e) {
					ErrOut = null;
					ProblemLoadingFile(e, erroroutLine);
				}
			}
		} else {
			ErrOut = null;
		}

		Assembler.ErrorOut = ErrOut?.Item ?? Console.Error;
		AssemblerNeedsUpdating = false;
	}





	internal bool TryGetDiffFile([NotNullWhen(true)] out byte[]? binFile) {
		if (TryOpenBinary("diff", ref diffromfile)) {
			binFile = diffromfile.Item;
			return true;
		}

		binFile = null;
		return false;
	}

	internal bool TryGetOutputStream([NotNullWhen(true)] out Stream? outStream) {
		outStream = OutputStream?.Item;

		return outStream is not null;
	}


	private bool GetBoolOrDefault(string argName, bool defaultValue, out ArgParse parseStatus) {
		if (TryGetArg(argName, out var argvalrecord)) {
			string argvalstr = argvalrecord.Value;
			if (argvalstr.EqualsI("true")) {
				parseStatus = ArgParse.Valid;
				return true;
			} else if (argvalstr.EqualsI("false")) {
				parseStatus = ArgParse.Valid;
				return false;
			} else {
				ManifestError($"Invalid value to {argName}: {argvalstr}. Expected 'true' or 'false'.", argvalrecord.Line);
				parseStatus = ArgParse.Invalid;
			}
		} else {
			parseStatus = ArgParse.Empty;
		}

		return defaultValue;
	}


	internal bool TryGetString(string key, [NotNullWhen(true)] out string? value) {
		if (argVals.TryGetValue(key, out var info)) {
			value = info.Value;
			return true;
		} else {
			value = null;
			return false;
		}
	}

	private string GetStringOrDefault(string key) {
		_ = TryGetString(key, out var ret);
		return ret ?? string.Empty;
	}


	internal bool TryGetFileInfo(string key, [NotNullWhen(true)] out FileInfo? value, out int line) {
		if (argVals.TryGetValue(key, out var info)) {
			line = info.Line;
			value = new(info.Value);
			return true;
		} else {
			line = 0;
			value = null;
			return false;
		}
	}

	private bool TryGetArg(string key, [NotNullWhen(true)] out ArgInfo value) {
		return argVals.TryGetValue(key, out value);
	}





	internal void ParseManifest() {
		var update = File.GetLastWriteTimeUtc(fileName);

		if (update == LastUpdated) {
			return;
		}

		LastUpdated = update;
		AssemblerNeedsUpdating = true;
		char[]? buffer = null;

		try {
			argVals.Clear();
			prevariables.Clear();
			presymbols.Clear();


			var ManifestWordsSpan = ManifestKeywords.GetAlternateLookup<ReadOnlySpan<char>>();

			using var reader = new StreamReader(fileName, ManifestOptions);

			Igarashi.Notice("Parsing manifest file...");

			int len = (int) reader.BaseStream.Length;

			buffer = ArrayPool<char>.Shared.Rent(len * 4); // safe upper bound: UTF-8, never decodes to more chars than input bytes

			reader.BaseStream.Position = 0;

			int didRead = reader.ReadBlock(buffer);

			if (!reader.EndOfStream) {
				Igarashi.Error("Problem parsing manifest file.");
				ManifestGood = false;
				return;
			}


			ManifestGood = true;

			ReadOnlySpan<char> fileBuffer = buffer.AsSpan(0, didRead);

			int lineNum = 0; // 0 to start at 1 because it increments at the beginning of the loop for simplicity

			HashSet<string>.AlternateLookup<ReadOnlySpan<char>>? cmdset = null;

			foreach (var lineElement in fileBuffer.EnumerateLines()) {
				var line = lineElement.TrimEnd();

				lineNum++;

				if (lineNum is 1) {
					if (line.Equals("[FUTABA:assemble]", StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					Igarashi.Error("Manifest files must begin with '[FUTABA:assemble]'");
					ManifestGood = false;
					return;
				}

				line = line.Trim();

				if (line.IsEmpty || line.StartsWith(';')) {
					continue;
				}

				int equalsAt = line.IndexOf('=');

				if (equalsAt >= 0) {
					ReadOnlySpan<char> cmdName = line[..equalsAt].Trim();

					if (cmdName.Length is 0) {
						ManifestError("Missing command name.", lineNum);
						continue;
					}


					var cmdVal = (equalsAt + 1) == line.Length ? [] : line[(equalsAt+1)..].Trim();

					char ftok = cmdName[0];

					if (ftok is '#') { // Symbol assignment
						string symName = new(cmdName[1..]);

						if (symName.IsWhiteSpace()) {
							ManifestError("Missing symbol name", lineNum);
							continue;
						}

						if (cmdVal.IsWhiteSpace()) {
							continue;
						}

						if (presymbols.ContainsKey(symName)) {
							ManifestWarning($"Duplicate declaration for symbol {symName}.", lineNum);
						}

						if (Helpers.TryParseValue(cmdVal, out decimal symval)) {
							if (AssignedSymbol.TryCreate(symName, symval, out var assym, out var symerr)) {
								presymbols.Add(symName, assym);
							} else {
								ManifestError(symerr, lineNum);
							}
						} else {
							ManifestError($"Invalid symbol assignment: {cmdVal}", lineNum);
						}

					} else if (ftok is '!') { // Variable assignment
						string varName = new(cmdName[1..]);

						if (varName.IsWhiteSpace()) {
							ManifestError("Missing symbol name", lineNum);
							continue;
						}

						if (prevariables.ContainsKey(varName)) {
							ManifestWarning($"Duplicate declaration for variable !{varName}.", lineNum);
						}

						if (cmdVal.IsWhiteSpace()) {
							continue;
						}

						if (cmdVal is ['"', .. var contents, '"']) {
							if (Variable.TryCreate(varName, contents, out var addVar, out var varErr)) {
								prevariables.Add(varName, addVar);
							} else {
								ManifestError(varErr, lineNum);
							}
						} else if (Helpers.TryParseValue(cmdVal, out decimal varVal)) {
							if (Variable.TryCreate(varName, varVal, out var addVar, out var varErr)) {
								prevariables.Add(varName, addVar);
							} else {
								ManifestError(varErr, lineNum);
							}
						} else {
							ManifestError("Invalid variable assignment. Must use constant decimal, hex, or binary value, or string enclosed in quotes.", lineNum);
						}

						// Commands
					} else {
						if (cmdset is not HashSet<string>.AlternateLookup<ReadOnlySpan<char>> cmdset2) {
							ManifestError("Not in any category.", lineNum);
						} else {
							if (cmdset2.TryGetValue(cmdName, out var cmdReal)) {
								if (argVals.ContainsKey(cmdReal)) {
									ManifestWarning($"Ignoring duplicate key: {cmdName}", lineNum);
								} else if (cmdVal.IsWhiteSpace()) {
									ManifestWarning($"Ignoring empty key", lineNum);
								} else {
									argVals[cmdReal] = new(lineNum, new string(cmdVal));
								}
							} else {
								ManifestError("Unrecognized command name.", lineNum);
							}
						}
					}
				} else {
					if (line[0] is '#' or '!') {
						ManifestError($"Missing assignment: '{line}'", lineNum);
					} else if (line[^1] is ':') {
						ReadOnlySpan<char> catName = line[..^1];

						if (ManifestWordsSpan.TryGetValue(catName, out var catter)) {
							cmdset = catter.GetAlternateLookup<ReadOnlySpan<char>>();
						} else {
							ManifestError($"Unrecognized category name: '{catName}'", lineNum);
						}

					} else {
						ManifestError($"Syntax error: '{line}'", lineNum);
					}
				}
			}
		} finally {
			if (buffer is not null) {
				ArrayPool<char>.Shared.Return(buffer);
			}
		}
	}

	void AllocateBaseRom(int size) {
		if (baserom is null || (baserom.Length != size)) {
			baserom = GC.AllocateUninitializedArray<byte>(size);
		}
	}





	byte GetByteOrDefault(string key, byte defaultValue) {
		if (TryGetArg(key, out var argGet)) {
			if (Helpers.TryParseByte(argGet.Value, out byte value)) {
				return value;
			} else {
				ManifestWarning($"Unable to parse '{argGet.Value}' to an 8-bit value", argGet.Line);
			}
		}

		return defaultValue;
	}


	int GetInt32OrDefault(string key, int defaultValue) {
		if (TryGetArg(key, out var argGet)) {
			if (Helpers.TryParseByte(argGet.Value, out byte value)) {
				return value;
			} else {
				ManifestWarning($"Unable to parse '{argGet.Value}' to an 8-bit value", argGet.Line);
			}
		}

		return defaultValue;
	}


	bool TryOpenBinary(string key, [NotNullWhen(true)] ref LoadedItem<byte[]>? binFile) {
		if (TryGetArg(key, out var argGet)) {
			FileInfo diffInfo = new(argGet.Value);

			if (binFile?.Reload(diffInfo) ?? true) {
				try {
					var data = Helpers.GetFileAsArray(diffInfo);
					binFile = new(argGet.Value, data);
					return true;
				} catch (Exception e) {
					ManifestError($"Problem loading file: '{argGet.Value}' - {e.Message}", argGet.Line);
				}
			} else {
				return false;
			}
		}
		binFile = null;
		return false;
	}


	void ProblemLoadingFile(Exception e, int line) {
		ManifestError($"Problem loading file: '{e.Message}'", line);
	}

	void ManifestError(string err, int lineNumber) {
		Igarashi.Error($"Manifest error in {fileName}:{lineNumber}: {err}");
		ManifestGood = false;
	}

	void ManifestErrorNoLine(string err) {
		Igarashi.Error($"Manifest error in {fileName}: {err}");
		ManifestGood = false;
	}

	void ManifestWarning(string err, int lineNumber) {
		Igarashi.Warning($"Manifest warning in {fileName}:{lineNumber}: {err}");
	}

	void ManifestWarningNoLine(string err) {
		Igarashi.Warning($"Manifest warning in {fileName}: {err}");
	}

	static readonly Dictionary<string, MissingTokenSeverity> MissingTokenLookup = new(StringComparer.OrdinalIgnoreCase) {
		{ "off", MissingTokenSeverity.Ignore },
		{ "ignore", MissingTokenSeverity.Ignore },
		{ "ambiguous", MissingTokenSeverity.Ambiguous },
		{ "aggressive", MissingTokenSeverity.AbsolutePointers },
		{ "absolute", MissingTokenSeverity.AbsolutePointers },
		{ "all", MissingTokenSeverity.AllInstructions },
	};

	static readonly Dictionary<string, RomOverflowAction> OverflowLookup = new(StringComparer.OrdinalIgnoreCase) {
		{ "error", RomOverflowAction.Error },
		{ "grow", RomOverflowAction.Grow },
		{ "expand", RomOverflowAction.Expand },
	};

	static readonly Dictionary<string, Coprocessor> CoprocessorLookup = new(StringComparer.OrdinalIgnoreCase) {
		{ "none", Coprocessor.None },
		{ "sa1", Coprocessor.SA1 },
		{ "sa-1", Coprocessor.SA1 },
		{ "gsu", Coprocessor.GSU },
		{ "sfx", Coprocessor.SuperFX },
		{ "superfx", Coprocessor.SuperFX },
		{ "dsp", Coprocessor.DSP },
		{ "dsp1", Coprocessor.DSP },
		{ "obc1", Coprocessor.OBC1 },
		{ "sdd1", Coprocessor.SDD1 },
		{ "srtc", Coprocessor.SRTC },
		{ "custom", Coprocessor.Custom },
		{ "other", Coprocessor.Other },
	};


	static readonly Dictionary<string, HashSet<string>> ManifestKeywords = new() {
		{ "assembly", [
			"entry",
			"type",
			"output",
			"addsymbols",
			"nullfill",
			"randomseed",
			"base",
		]},
		{ "header", [
			"autofill",
			"checksum",
			"mapmode",
			"fastrom",
			"title",
			"romsize",
			"ramsize",
			"version",
			"region",
			"coprocessor",
			"extended",
			"gamecode",
			"makercode",
			"specialversion",
			"cartridgetype",
		]},
		{ "debug", [
			"stdout",
			"errorout",
			"overflow",
			"tokens",
			"symbols",
			"warnaserror",
			"maxerrors"
		]},
		{ "disassembly", [
			"crc",
			"md5",
			"sha1",
			"diff",
		]},
	};


	private static readonly FileStreamOptions ManifestOptions = new() {
		Mode = FileMode.Open,
		Access = FileAccess.Read,
		Share = FileShare.Read
	};


	private record LoadedItem<T> : IDisposable {
		public string Name { get; }
		public DateTime Loaded { get; }
		public T Item { get; }

		public LoadedItem(string name, T item) {
			name = Path.GetFullPath(name);
			Name = name;
			Loaded = File.GetLastWriteTimeUtc(name);
			Item = item;
		}

		public bool Reload(FileInfo file) {
			return file.FullName != Name || file.LastWriteTimeUtc != Loaded;
		}

		public bool Reload(string file) {
			file = Path.GetFullPath(file);
			return file != Name || File.GetLastWriteTimeUtc(file) != Loaded;
		}


		private bool disposed = false;

		~LoadedItem() {
			Dispose();
		}

		public void Dispose() {
			if (disposed) {
				return;
			}

			disposed = true;
			if (Item is IDisposable d) {
				d.Dispose();
			}

			GC.SuppressFinalize(this);
		}
	}

	private enum ArgParse {
		Empty = 0,
		Valid,
		Invalid,
	}


	public void TryExportSymbols() {
		if (Assembler is not null) {
			if (TryGetArg("symbols", out var symbolsOutput)) {
				var symtoks = Helpers.CleanSplit(symbolsOutput.Value, ',');

				foreach (var symtype in symtoks) {
					if (SymbolsOutputLookup.TryGetValue(symtype, out var func)) {
						func(OutputPath, Assembler);
					} else {
						ManifestWarning($"Unrecognized symbol output type: '{symtype}", symbolsOutput.Line);
					}
				}
			}
		}
	}



	public void Dispose() {
		StdOut = null;
		ErrOut = null;
		Assembler = null;
		OutputStream = null;
	}

	static readonly Dictionary<string, Action<string, Assembler>> SymbolsOutputLookup = new(StringComparer.OrdinalIgnoreCase) {
		{ "mesen", Igarashi.ExportSymbols_Mlb },
		{ "mlb", Igarashi.ExportSymbols_Mlb },
		{ "wla", Igarashi.ExportSymbols_Wla },
	};
	
	private readonly record struct ArgInfo(int Line, string Value);
}
