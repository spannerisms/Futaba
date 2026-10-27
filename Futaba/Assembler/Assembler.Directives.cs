namespace Futaba;

unsafe partial class Assembler {
	private void GenerateAddressMarker() {
		// try reading hex digits until colon or bar
		char* read = _reading + 1;

		char* theend = _fileEnd; // force end into a register

		int action = AsciiHelpers.AddressMarker_Unclosed;
		bool invalid = false;
		int address = 0;

		while (read < theend) {
			char c = *read;

			if (c < AsciiHelpers.AsciiCount) {
				action = AsciiHelpers.AddressMarkerAction[c];

				if ((uint) action < 0x10) {
					address <<= 4;
					address |= action;
				} else {
					if (action < 0) {
						invalid = true;
					} else if (action == AsciiHelpers.AddressMarker_Unclosed) {
						goto Unclosed;
					} else {
						goto ClosedAddress;
					}
				}
			} else {
				if (c is BadChar) {
					goto Unclosed;
				}

				invalid = true;
			}

			read++;
		}

		goto Unclosed;

		ClosedAddress:

		if (invalid) {
			Warning(MsgInfo.BadAddressMarker, MsgInfo.InvalidHex);
		} else if (_pc != address) {
			Warning("Disagreement between program counter and address marker", $"{_pc:X6} != {address:X6}");
		}

		read++;

		if (action is AsciiHelpers.AddressMarker_Bar) {
			if (_inProvenance) {
				Warning("No provenance provided in address marker.");
			}
			_reading = read;
			return;
		}

		// provenance
		address = 0;
		action = AsciiHelpers.AddressMarker_Unclosed;
		invalid = false;

		while (read < theend) {
			char c = *read;

			if (c < AsciiHelpers.AsciiCount) {
				action = AsciiHelpers.AddressMarkerAction[c];

				if ((uint) action < 0x10) {
					address <<= 4;
					address |= action;
				} else {
					if (action < 0) {
						invalid = true;
					} else if (action == AsciiHelpers.AddressMarker_Unclosed) {
						goto Unclosed;
					} else {
						goto ClosedProvenance;
					}
				}
			} else {
				if (c is BadChar) {
					goto Unclosed;
				}

				invalid = true;
			}

			read++;
		}

		goto Unclosed;

		ClosedProvenance:

		read++;

		if (action == AsciiHelpers.AddressMarker_Colon) {
			_reading = read - 1;
			UnexpectedCharacter();
			AbortCommand();
			return;
		} else if (invalid) {
			Warning(MsgInfo.BadAddressMarker, MsgInfo.InvalidHex);
		} else if (!_inProvenance) {
			Warning("Provenance provided in address marker when not relevant.");
		} else if (Provenance != address) {
			Warning("Disagreement between provenance and address marker", $"{Provenance:X6} != {address:X6}");
		}

		_reading = read;
		return;

		Unclosed:
		Error(MsgInfo.BadAddressMarker, "unclosed address marker.");

		_reading = read;

		if (*read is ';') {
			FastRead.ReadUntilLineEnd(ref _reading, _fileEnd);
		} else {
			AbortCommand();
		}
	}

	private void DoDataItems(int size) {
		if (!_reading->IsSkippableSpace) {
			UnexpectedCharacter();
			AbortCommand();
			return;
		}

		if (AdvanceOverWhitespace()) {
			Error("Empty data statement");
			return;
		}

		bool more;

		do {
			more = ReadToNextComma(out var next);

			// special SPLAT
			if (next.OnlyCharIs(SPLAT)) {
				Skip(size);
			} else if (next.TestIfVariable()) {
				if (TryGetVariable(next.AsSpan(1), out var vari)) {
					switch (vari.VarType) {
						case VariableType.String:
							WriteString(vari.Contents, _currentEncoder);
							break;

						case VariableType.Value:
							IExpressionReturn datval = ParseOperand(next);
							WriteData(datval, size);
							break;
					}

				} else {
					Error(MsgInfo.VariableNotFound(next));
				}
			} else if (TryReadStringWithParams(next, out var opstr, out var openc)) {
				WriteString(opstr, openc);
			} else {
				IExpressionReturn datval = ParseOperand(next);
				WriteData(datval, size);
			}

		} while (more);

		void WriteString(string str, SnesEncoder encoder) {
			int len = str.Length * size;

			if (TryAdvanceProgramCounter(len)) {
				InsertEncodedString(_offset, size, str, encoder);
				TrackAndStep(len);
			}
		}

	}

	private bool TryGetAllocatedDataStatement(string directiveName, out int size) {
		if (!AdvanceOverWhitespace() && *_reading++ is 'd' or 'D' && !AtEndOfCommand()) {
			switch (*_reading++) {
				case 'b' or 'B':
					size = 1;
					return TestDataStatement();

				case 'w' or 'W':
					size = 2;
					return TestDataStatement();

				case 'l' or 'L':
					size = 3;
					return TestDataStatement();

				case 'd' or 'D':
					size = 4;
					return TestDataStatement();
			}
		}

		size = 0;
		Error($"{directiveName} must begin with a data definition token: db, dw, dl, dd");

		AbortCommand();
		return false;

		bool TestDataStatement() {
			if (AdvanceOverWhitespace()) {
				Error("Empty data statement");
				return false;
			} else {
				return true;
			}
		}

	}

	private void Directive_PILE() {
		if (!DemandSpaceWithError()) return;

		if (!TryReadInlineSymbolReference(out string? pileName)) {
			AbortCommand();
			return;
		}

		if (!TryGetSymbol(pileName, out var pileObj)) {
			Error($"Cannot find piling: {pileName}");
			AbortCommand();
			return;
		}

		if (pileObj is not Piling pile) {
			Error($"Only {nameof(Piling)}s can be piled on.");
			AbortCommand();
			return;
		}

		if (AdvanceOverWhitespace() || !FastRead.IsAllocator(_reading)) {
			Error(MsgInfo.MissingAllocator);

		} else {
			_reading += 2;
			if (TryGetAllocatedDataStatement(PILE, out int size)) {
				bool more;

				bool pileOverflow = false;

				do {
					more = ReadToNextComma(out var next);

					// special SPLAT
					if (next.OnlyCharIs(SPLAT)) {
						AdvancePile(size);
					} else if (next.TestIfVariable()) {
						if (TryGetVariable(next.AsSpan(1), out var vari)) {
							switch (vari.VarType) {
								case VariableType.String:
									WriteString(vari.Contents, _currentEncoder);
									break;

								case VariableType.Value:
									WriteData(pile.PileOffset, vari.Value.AsInt(), size);
									AdvancePile(size);
									break;
							}
						} else {
							Error(MsgInfo.VariableNotFound(next));
						}
					} else if (TryReadStringWithParams(next, out var opstr, out var openc)) {
						WriteString(opstr, openc);
					} else {
						IExpressionReturn datval = ParseOperand(next);
						WriteData(pile.PileOffset, datval, size);
						AdvancePile(size);
					}
				} while (more);

				void WriteString(string str, SnesEncoder encoder) {
					int len = str.Length * size;

					if (TestOffsetBounds(pile.PileOffset + len)) {
						InsertEncodedString(pile.PileOffset, size, str, encoder);
						AdvancePile(len);
					}
				}

				void AdvancePile(int step) {
					pile.PileOffset += step;

					if (!pileOverflow) {
						if (pile.PileOffset > pile.MaxOffset) {
							pileOverflow = true;
							Error("Pile capacity exceeded");
						}
					}
				}
			}
		}
	}

	private void Directive_PLOP() {
		if (!DemandSpaceWithError()) return;

		if (!ReadUntilAllocator(out var plopAddressArg)) {
			Error(MsgInfo.MissingAllocator);
			AbortCommand();
			return;
		}

		IExpressionReturn plopAddress = ParseOperand(plopAddressArg);

		if (!plopAddress.Resolved) {
			Error(MsgInfo.MustBeResolvedNow(PLOP));
			AbortCommand();
			return;
		}

		if (TryGetAllocatedDataStatement(PLOP, out int size)) {
			int plopOffset = AddressToOffset(plopAddress.ValueInt32);

			bool more;
			do {
				more = ReadToNextComma(out var next);

				// special SPLAT
				if (next.OnlyCharIs(SPLAT)) {
					AdvancePile(size);
				} else if (next.TestIfVariable()) {
					if (TryGetVariable(next.AsSpan(1), out var vari)) {
						switch (vari.VarType) {
							case VariableType.String:
								WriteString(vari.Contents, _currentEncoder);
								break;

							case VariableType.Value:
								WriteData(plopOffset, vari.Value.AsInt(), size);
								AdvancePile(size);
								break;
						}
					} else {
						Error(MsgInfo.VariableNotFound(next));
					}
				} else if (TryReadStringWithParams(next, out var opstr, out var openc)) {
					WriteString(opstr, openc);
				} else {
					IExpressionReturn datval = ParseOperand(next);
					WriteData(plopOffset, datval, size);
					AdvancePile(size);
				}
			} while (more);

			void WriteString(string str, SnesEncoder encoder) {
				int len = str.Length * size;

				if (TestOffsetBounds(plopOffset + len)) {
					InsertEncodedString(plopOffset, size, str, encoder);
					AdvancePile(len);
				}
			}

			void AdvancePile(int step) {
				plopOffset += step;
			}
		}
	}

	private void Directive_INCSRC() {
		if (!DemandSpaceWithError()) return;

		// these need to be linked in reverse order so that the first file is assembled first
		Stack<SourceFile> srcstack = new();

		bool moreFiles;

		do {
			moreFiles = ReadToNextComma(out OperandString incPath);

			if (TryGetQuotedStringExploded(incPath, out var incPathStr)) {
				if (TryGetSource(incPathStr, out var incAdd)) {
					srcstack.Push(incAdd);
				}
			}
		} while (moreFiles);

		while (srcstack.TryPop(out var addSrc)) {
			TransferSourceControl(addSrc);
		}
	}

	private void Directive_INCBIN() {
		if (!DemandSpaceWithError()) return;

		string? labelName = null;
		int binsize = 0;
		int labelOffset = _offset;
		int binaddress = _pc;
		int binsite = Provenance;

		if (*_reading is '[') {
			_reading++;

			if (!AdvanceOverWhitespace()
				&& TryReadAnyLabelName(out var fullName, out var chunkName, out int depth)
				&& !AdvanceOverWhitespace()
				&& *_reading is ']') {
				labelName = fullName;
				_reading++;
			} else {
				UnexpectedCharacter();
				AbortCommand();
				return;
			}
		}

		if (AdvanceOverWhitespace()) {
			Error($"Missing argument for {INCBIN}");
			AbortCommand();
			return;
		}

		bool moreFiles;

		// TODO needs stress testing
		do {
			moreFiles = ReadToNextComma(out OperandString incPath);

			var (pstart, pend) = incPath;
			var pread = pstart;

			char c = *pread;

			string pathString;

			// test for string
			if (c is '"') {
				if (!FastRead.SkipToMatchingQuote(ref pread, pend)) {
					Error(MsgInfo.UnclosedString); // honestly I don't see how this could happen
					continue;
				}

				pathString = ExplodeStringContents(pstart + 1, pread);
				pread++;
			} else {
				UnexpectedCharacter(*pread);
				continue;
			}

			FastRead.SkipInlineWhitespace(ref pread, pend);

			if (!TryGetBinary(pathString, out var binFile)) {
				break;
			}

			int binlen = binFile.Length;
			int binstart = 0; // static analysis can't seem to figure out these are guaranteed to be initialized
			int binend = binlen;

			// only thing that should be here now is [], if anything
			if (pread >= pend) {
				binstart = 0;
				binend = binlen;
			} else {
				if (*pread is '[' && ((pread + 1) < pend)) {
					char* pcheck = pend - 1;

					if (*pcheck is ']') {
						var splitter = new StringSplitter(pread + 1, pcheck);

						// we want more commas for the second operand
						if (splitter.GetNextDoubleChar(':', out var rangeL)) {
							if (!splitter.GetNextComma(out var rangeR)) {
								bool rangeGood = true;

								if (rangeL.IsEmpty) {
									binstart = 0;
								} else {
									IExpressionReturn valL = ParseOperand(rangeL);

									if (valL.Resolved) {
										binstart = valL.ValueInt32;
									} else {
										rangeGood = false;
										Error_BadResolve(valL);
									}
								}

								if (rangeR.IsEmpty) {
									binend = binlen;
								} else {
									IExpressionReturn valR = ParseOperand(rangeR);

									if (valR.Resolved) {
										binstart = valR.ValueInt32;
									} else {
										rangeGood = false;
										Error_BadResolve(valR);
									}
								}

								if (!rangeGood) {
									continue;
								}

							} else {
								Error("Too many allocators.");
								continue;
							}
						} else {
							Error(MsgInfo.MissingAllocator);
							continue;
						}
					} else {
						UnexpectedCharacter(*pcheck);
						continue;
					}
				} else {
					UnexpectedCharacter(*pread);
					continue;
				}

			}

			if ((uint) binstart >= binlen || (uint) binend > binlen || binstart >= binend) {
				const string InvalidRange = "Invalid range";

				if (binstart < 0) {
					Error(InvalidRange, "Range start cannot be negative.");
				} else if (binstart >= binlen) {
					Error(InvalidRange, "Range start cannot be past the end of the file.");
				} else if (binstart >= binend) {
					Error(InvalidRange, "Range start must be before range end.");
				}

				if (binend < 0) {
					Error(InvalidRange, "Range end cannot be negative.");
				} else if (binend > binlen) {
					Error(InvalidRange, "Range end cannot be past the end of the file.");
				}

				continue;
			} else {
				if (TryAdvanceProgramCounter(binlen)) {
					Span<byte> filler = new(_pcPtr, binlen);

					if (binFile.TryGetSpan(binstart, binend - binstart, out var binSpan)) {
						binSpan.CopyTo(filler);
					}

					TrackAndStep(binlen);
				}
				binsize += binlen;
			}
		} while (moreFiles);
		if (labelName is not null) {
			DataBlockLabel binlabel = new(labelName, binsite, binaddress, labelOffset, binsize);
			AddOrReplaceSymbol(binlabel);
		}

		FinishCommand();
	}

	private void Directive_LANG() {
		if (!DemandSpaceWithError()) return;

		var langname = FastRead.ReadAlphaNumeric(ref _reading, _fileEnd);

		if (langname.IsEmpty) {
			Error("Missing architecture name.");
			AbortCommand();
		} else {
			if (langname.Length < 12) {
				Span<char> langlower = stackalloc char[langname.Length];
				langname.FastLower(langlower);

				switch (langlower) {
					case "wdc":
					case "65816":
					case "wdc65816":
					case "65c816":
					case "wdc65c816":
						_architecture = SnesArchitecture.WDC65816;
						FinishCommand();
						return;

					case "spc":
					case "spc700":
						_architecture = SnesArchitecture.SPC700;
						FinishCommand();
						return;

					case "sfx":
					case "superfx":
						_architecture = SnesArchitecture.SuperFX;
						FinishCommand();
						return;
				}
			}

			Error("Invalid architecture name.");
			FinishCommand();
		}
	}

	private void Directive_RAW() {
		if (DemandSpaceWithError() && DemandRestOfCommand(out OperandString arg)) {
			int len = arg.Length;

			bool good = (len & 1) is 0;

			len >>= 1;

			if (TryAdvanceProgramCounter(len)) {
				if (good) {
					byte* writeTo = _pcPtr;
					char* rawread = arg.Start;

					for (int i = 0; i < len; i++, writeTo++) {
						int m = rawread->GetHexValue();
						rawread++;
						m <<= 4;
						m |= rawread->GetHexValue();
						rawread++;

						if ((uint) m > 255) {
							Error("Invalid raw string");
							break;
						}

						*writeTo = (byte) m;
					}
				} else {
					Error("Invalid raw string");
				}

				TrackAndStep(len);
			}
		}
	}

	private void Directive_REBANK() {
		if (DemandSpaceWithError() && DemandRestOfCommand(out OperandString arg)) {
			if (arg.OnlyCharIs(SPLAT)) {
				_inProvenance = false;
			} else {
				int address = Provenance & SnesHelpers.AbsoluteMask;

				switch (arg.First) {
					case '>':
						address |= 0x8000;
						arg = arg.Slice(1);
						break;

					case '<':
						address &= 0x7FFF;
						arg = arg.Slice(1);
						break;
				}

				IExpressionReturn newBank = ParseOperand(arg);

				if (newBank.Resolved) {
					Provenance = (((byte) newBank.ValueInt32) << 16) | address;
				} else {
					Error(MsgInfo.MustBeResolvedNow(REBANK));
				}

				FinishCommand();
			}
		}
	}

	private void Directive_FILL() {
		const string BadFillCommand = "Malformatted fill command";
		const string BadFillModeExpr = "Fill expression must be resolvable immediately.";
		const string BadFillSize = "Size must be > 0.";
		const string BadAlignSize = "Alignment value must be > 0.";

		if (!DemandSpaceWithError()) return;

		var filltype = GetNextKeyword();

		int fillwordsize;
		bool random = false;

		switch (filltype) {
			case BYTE:
				fillwordsize = 1;
				break;

			case WORD:
				fillwordsize = 2;
				break;

			case LONG:
				fillwordsize = 3;
				break;

			case DOUBLE:
				fillwordsize = 4;
				break;

			case RANDOM:
				fillwordsize = 1;
				random = true;
				break;

			case "":
				Error(BadFillCommand, "missing fill type.");
				AbortCommand();
				return;

			default:
				Error(BadFillCommand, $"\"{filltype}\" is not a valid fill type. Legal values are: {BYTE}, {WORD}, {LONG}, {DOUBLE}, {RANDOM}");
				AbortCommand();
				return;
		}

		if (!DemandSpaceWithError()) return;

		var fillmode = GetNextKeyword();

		if (!DemandSpaceWithError()) return;

		bool foundAllocator = ReadUntilAllocator(out OperandString fillModeExpr);

		IExpressionReturn fillModeExpression;

		// use a bool to track the validity of the fill expression
		// that way we can parse both arguments for potential issues
		bool validFill;
		int blockSize = 0;

		switch (fillmode) {
			case SIZE:
				fillModeExpression = ParseOperand(fillModeExpr, SymbolContext.Default);

				if (validFill = fillModeExpression.Resolved) {
					blockSize = fillModeExpression.ValueInt32;

					if (blockSize < 1) {
						validFill = false;
						Error(BadFillSize);
					}
				} else {
					Error_BadResolve(fillModeExpression, BadFillModeExpr);
				}

				break;

			case COUNT:
				fillModeExpression = ParseOperand(fillModeExpr, SymbolContext.Default);

				if (validFill = fillModeExpression.Resolved) {
					blockSize = fillModeExpression.ValueInt32 * fillwordsize;

					if (blockSize < 1) {
						validFill = false;
						Error(BadFillSize);
					}
				} else {
					Error_BadResolve(fillModeExpression, BadFillModeExpr);
				}

				break;

			case UNTIL:
				fillModeExpression = ParseOperand(fillModeExpr, SymbolContext.Provenance);

				if (validFill = fillModeExpression.Resolved) {
					blockSize = fillModeExpression.ValueInt32;

					if (blockSize < 1) {
						validFill = false;
						Error(BadFillSize);
					} else {
						blockSize -= Provenance;

						if (blockSize < 1) {
							validFill = false;

							if (blockSize < 0) {
								Warning($"Target for {UNTIL} has already been passed.");
							}
						}
					}
				} else {
					Error_BadResolve(fillModeExpression, BadFillModeExpr);
				}

				break;

			case ALIGN:
				fillModeExpression = ParseOperand(fillModeExpr, SymbolContext.RomAddress);

				if (validFill = fillModeExpression.Resolved) {
					int alignVal = fillModeExpression.ValueInt32;

					if (alignVal < 1) {
						validFill = false;
						Error(BadAlignSize);
					} else {
						blockSize = _pc % alignVal;

						if (blockSize is 0) {
							validFill = false;
						} else {
							// get inverse for correct number of bytes
							blockSize = alignVal - blockSize;
						}
					}
				} else {
					Error_BadResolve(fillModeExpression, BadFillModeExpr);
				}

				break;

			case ARRANGE:
				fillModeExpression = ParseOperand(fillModeExpr, SymbolContext.RomAddress);

				if (validFill = fillModeExpression.Resolved) {
					int arrangeVal = fillModeExpression.ValueInt32;

					if (arrangeVal < 1) {
						validFill = false;
						Error(BadAlignSize);
					} else {
						blockSize = Provenance % arrangeVal;

						if (blockSize is 0) {
							validFill = false;
						} else {
							// get inverse for correct number of bytes
							blockSize = arrangeVal - blockSize;
						}
					}
				} else {
					Error_BadResolve(fillModeExpression, BadFillModeExpr);
				}

				break;

			default:
				Error(BadFillCommand, $"\"{fillmode}\" is not a valid fill mode. Legal values are: {SIZE}, {COUNT}, {UNTIL}, {ALIGN}, {ARRANGE}");
				AbortCommand();
				return;
		}

#pragma warning disable CS8600 // this will never end up null, so it's fine
		IExpressionReturn fillvalexpr = null;
#pragma warning restore CS8600

		OperandString fillvalstr = default;

		// if it's not random then "::" is required
		if (!foundAllocator) {
			if (!random) {
				Error(MsgInfo.MissingAllocator);
				AbortCommand();
				return;
			}
		} else {
			if (ReadRestOfCommand(out fillvalstr)) {
				if (!random) {
					fillvalexpr = ParseOperand(fillvalstr);
				}
			} else {
				if (!random) {
					Error("Missing fill value");
					AbortCommand();
					return;
				}
			}
		}

		if (validFill && TryAdvanceProgramCounter(blockSize)) {
			if (random) {
				XoRandom source = fillvalstr.IsNull
					? _random // use current RNG
					: new(CharSpanHelpers.HashString(fillvalstr));

				Span<byte> randfill = new(_pcPtr, blockSize);
				source.NextBytes(randfill);
			} else if (fillvalexpr!.NeedsRequest) {
				_fillRequests.Add(new(fillvalexpr, _offset, blockSize, fillwordsize, _time, CurrentSourceLine));
			} else {
				PerformBlockFill(_pcPtr, blockSize, fillwordsize, fillvalexpr.ValueInt32);
			}

			TrackAndStep(blockSize);
		}
	}

	private (BreakpointTriggers, BreakpointInserts) GetBreakPointParams() {
		// just return the current default if none is specified
		BreakpointInserts inserts;
		BreakpointTriggers triggers;

		char* read = _reading;
		char* theend = _fileEnd;

		var targ = FastRead.ReadAlphaNumericSpan(ref read, theend);

		if (targ.Length is 0) {
			triggers = _defaultBpTriggers;
		} else {
			triggers = BreakpointTriggers.None;

			foreach (char c in targ) {
				switch (c.FastUpper()) {
					case 'R': triggers |= BreakpointTriggers.Read; continue;
					case 'W': triggers |= BreakpointTriggers.Write; continue;
					case 'X': triggers |= BreakpointTriggers.Execute; continue;
				}

				UnexpectedCharacter(c);
				AbortCommand();
				return (_defaultBpTriggers, _defaultBpInsert);
			}
		}

		if (*read is '+') {
			read++;
			switch (read->FastUpper()) {
				case 'N':
					inserts = BreakpointInserts.NOP;
					read++;
					break;

				case 'B':
					inserts = BreakpointInserts.BRK;
					read++;
					break;

				case 'W':
					inserts = BreakpointInserts.WDM;
					read++;
					break;

				case 'C':
					inserts = BreakpointInserts.COP;
					read++;
					break;

				default:
					if (read->IsNiceBoundary) {
						inserts = BreakpointInserts.Nothing;
						break;
					} else {
						UnexpectedCharacter();
						AbortCommand();
						return (triggers, BreakpointInserts.Nothing);
					}
			}
		} else {
			inserts = _defaultBpInsert;
		}

		_reading = read;
		FinishCommand();

		return (triggers, inserts);
	}

	private void Directive_BREAKPOINTS() {
		if (_bpControl is BreakpointsControl.Kill) {
			AbortCommand();
			return;
		}

		if (!DemandSpaceWithError()) return;

		char* read = _reading;
		char* theend = _fileEnd;

		var bpcheck = FastRead.ReadAlphaNumeric(ref read, theend);

		int len = bpcheck.Length;

		if (len is > 0 and < 10) {
			Span<char> bplow = stackalloc char[len];

			bpcheck.FastLower(bplow);

			switch (bpcheck) {
				case "off":
					_bpControl = BreakpointsControl.Off;
					FinishCommand();
					return;

				case "kill":
					_bpControl = BreakpointsControl.Kill;
					FinishCommand();
					return;
			}
		}

		var (triggers, inserts) = GetBreakPointParams();

		_bpControl = BreakpointsControl.On;
		_defaultBpInsert = inserts;
		_defaultBpTriggers = triggers;

	}

	private void GenerateBreakpoint() {
		if (_bpControl <= BreakpointsControl.Off) {
			AbortCommand();
			return;
		}

		_reading++;

		var (triggers, inserts) = GetBreakPointParams();

		Breakpoint addbk = new(Provenance, triggers);

		_breakpoints.Add(addbk);

		switch (inserts, _architecture) {
			case (BreakpointInserts.Nothing, _): break;

			case (BreakpointInserts.NOP, SnesArchitecture.WDC65816): WriteOpImplied(0xEA); break;
			case (BreakpointInserts.NOP, SnesArchitecture.SPC700): WriteOpImplied(0x00); break;
			case (BreakpointInserts.NOP, SnesArchitecture.SuperFX): WriteOpImplied(0x01); break;

			case (BreakpointInserts.BRK, SnesArchitecture.WDC65816): WriteOpImplied(0x00); break;
			case (BreakpointInserts.BRK, SnesArchitecture.SPC700): WriteOpImplied(0x0F); break;

			case (BreakpointInserts.WDM, SnesArchitecture.WDC65816): WriteOpB(0x42, 0xBE); break;
			case (BreakpointInserts.COP, SnesArchitecture.WDC65816): WriteOpB(0x02, 0xBE); break;

			default:
				// avoid Enum.GetName
				string opName = inserts switch {
					BreakpointInserts.NOP /*     */ => "NOP",
					BreakpointInserts.WDM /*     */ => "WDM",
					BreakpointInserts.BRK /*     */ => "BRK",
					BreakpointInserts.COP /*     */ => "COP",
					_ /*                         */ => "instruction"
				};

				string archName = _architecture switch {
					SnesArchitecture.WDC65816 /*   */ => "WDC65816",
					SnesArchitecture.SPC700 /*     */ => "SPC700",
					SnesArchitecture.SuperFX /*    */ => "SuperFX",
					_ /*                           */ => "current"
				};

				Warning($"Cannot insert {opName} for in {archName} archichecture");
				break;
		}
	}

	private bool TrySpecialPrint(OperandString chunk, [NotNullWhen(true)] out string? contents) {
		if (TestIsQuotedStringAndExplode(chunk, out string? contentsStr)) {
			contents = contentsStr;
			return true;
		}

		int len = chunk.Length;

		if (len is > 0 and < 10) {
			Span<char> clow = stackalloc char[len];
			chunk.FastLower(clow);

			switch (clow) {
				case "pc":
					contents = $"{_pc:X6}";
					return true;

				case "bar":
					contents = LongBar;
					return true;

				case "site":
					contents = $"{Provenance:X6}";
					return true;

				case "offset":
					contents = $"{_offset:X6}";
					return true;
			}
		}

		contents = null;
		return false;
	}

	private bool TryGetPrintContents(out string? contents) {
		if (!_reading->IsSkippableSpace) {
			if (_reading->IsLineEnd) {
				contents = null;
				return true;
			} else {
				UnexpectedCharacter();
				AbortCommand();
				contents = null;
				return false;
			}
		}

		if (AdvanceOverWhitespace()) {
			contents = null;
			return true;
		}

		StringBuilder printer = new();

		bool moreStuff;

		do {
			moreStuff = ReadToNextComma(out OperandString printChunk);

			if (TrySpecialPrint(printChunk, out var printChunkStr)) {
				printer.Append(printChunkStr);
			} else {
				IExpressionReturn exprChunk = ParseOperand(printChunk);

				if (exprChunk.Resolved) {
					printer.Append(exprChunk.Value);
				} else {
					printer.Append(0);
				}
			}
		} while (moreStuff);

		contents = printer.ToString();
		return true;
	}

	private void Directive_PRINTF() {
		const string MissingFormat = "Missing format string.";

		if (!_reading->IsSkippableSpace) {
			if (_reading->IsLineEnd) {
				Warning(MissingFormat);
				return;
			} else {
				UnexpectedCharacter();
				AbortCommand();
				return;
			}
		}

		if (AdvanceOverWhitespace()) {
			Warning(MissingFormat);
			return;
		}

		bool moreStuff = ReadToNextComma(out OperandString printChunk);

		if (!TryGetQuotedStringExploded(printChunk, out var formatString)) {
			Warning("Invalid format string.");
			AbortCommand();
			return;
		}

		List<object> formatObjects = [];

		if (moreStuff) {
			do {
				moreStuff = ReadToNextComma(out printChunk);

				if (TrySpecialPrint(printChunk, out var printChunkStr)) {
					formatObjects.Add(printChunkStr);
				} else if (printChunk.TestIfVariable()) {
					if (TryGetVariable(printChunk.AsSpan(1), out var vari)) {
						formatObjects.Add(vari);
					} else {
						Warning(MsgInfo.VariableNotFound(printChunk));
						formatObjects.Add(printChunk);
					}
				} else {
					IExpressionReturn exprChunk = ParseOperand(printChunk);

					if (!exprChunk.Resolved) {
						Warning($"Unresolved expression in {PRINTF} at index {formatObjects.Count}");
					}

					formatObjects.Add(exprChunk);
				}
			} while (moreStuff);
		}

		try {
			MessageOut.WriteLine(string.Format(formatString, CollectionsMarshal.AsSpan(formatObjects)));
		} catch (Exception e) {
			Warning("Format error: ", e.Message);
		}
	}

	private void Directive_ALLOCATE() {
		int blockAddress = -1;
		int blockEnd = -1;
		bool sizedBlock = false;

		// just restart the command so we can reuse all the code
		_reading = _commandStart;

		var parentState = _previousState;

		if (_pooling) {
			Error("Close pool before beginning allocation block.");
			ClearPools();
		}

		do {
			while (_reading < _fileEnd) {
				char c = *_reading;

				if (c.IsSkippableSpace) {
					_reading++;
					continue;
				} else if (c is NewLine) {
					_reading++;
					_sourceLine++;
					continue;
				}

				_commandStart = _reading;

				c = *_reading;

				switch (c) {
					// skip over comments
					case ';':
						FastRead.ReadUntilLineEnd(ref _reading, _fileEnd);
						continue;

					case ':':
						if (FastRead.ColonIsSeparator(_reading)) {
							_reading += 2;
						} else {
							UnexpectedCharacter();
							_reading++;
						}
						continue;

					case '!':
						DeclareVariable();
						continue;

					case '%':
						InvokeMacro();
						continue;

					case '.':
						if (TryReadSublabelName(out string? chunkName, out int depth)) {
							UpdateHierarchy(chunkName, depth);

							bool goodName = TryFullyExpandLabelName(depth, chunkName, out string? fullName);

							if (AdvanceOverWhitespace() || !FastRead.IsAllocator(_reading)) {
								if (goodName) {
									AssignedSymbol addsym = new(fullName!, blockAddress);
									AddOrReplaceSymbol(addsym);
								}
							} else {
								if (goodName) {
									_reading += 2;
									TryToAllocate(fullName!);
								}
							}
						}

						continue;

					// identifiers / mnemonics
					case 'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G' or 'H' or 'I' or 'J' or 'K' or 'L' or 'M' or
							'N' or 'O' or 'P' or 'Q' or 'R' or 'S' or 'T' or 'U' or 'V' or 'W' or 'X' or 'Y' or 'Z' or
							'a' or 'b' or 'c' or 'd' or 'e' or 'f' or 'g' or 'h' or 'i' or 'j' or 'k' or 'l' or 'm' or
							'n' or 'o' or 'p' or 'q' or 'r' or 's' or 't' or 'u' or 'v' or 'w' or 'x' or 'y' or 'z' or '_':
						CharSpan labelName = FastRead.ReadSublabelChunk(ref _reading, _fileEnd);

						char dpeek = *_reading;

						if (dpeek is ':') {
							_reading++;
							if (DemandSpaceAfterColon()) {
								AssignedSymbol addLabel = new(new string(labelName), blockAddress);

								AddOrReplaceSymbol(addLabel);
								UpdateHierarchy(addLabel.Name, 0);
							}

							continue;
						}

						char* peeking = _reading;

						FastRead.SkipInlineWhitespace(ref peeking, _fileEnd);

						if (*peeking is '=') {
							_reading = peeking + 1;
							CreateAssignedSymbol(labelName);

						} else if (FastRead.IsAllocator(peeking)) {
							_reading = peeking + 2;
							TryToAllocate(new string(labelName));

						} else if (labelName.MatchesIgnoreCase(ALLOCATE)) {
							if (!DemandSpaceWithError()) continue;

							sizedBlock = false;

							if (!RestOfCommand(out char* allocOperandStart, out char* allocOperandEnd)) {
								Error("Missing operand to allocate.");
								continue;
							}

							char* addressEnd = allocOperandEnd;
							char* addressRead = allocOperandStart;

							while (addressRead < allocOperandEnd) {
								if (FastRead.IsAllocator(addressRead)) {
									addressEnd = addressRead;
									break;
								}
								addressRead++;
							}

							OperandString addrContents = OperandString.CreateTrimmed(allocOperandStart, addressEnd);
							OperandString sizeContents = OperandString.CreateTrimmed(addressEnd + 2, allocOperandEnd);

							if (addrContents.Length is 0) {
								Error("Missing operand to allocate.");
								continue;
							}

							if (addrContents.OnlyCharIs(SPLAT)) {
								if (_nextAllocBlockStart is < 0) {
									Error("No previous allocation in allocation block.");
									PanicAllocation();
									continue;
								}

								blockAddress = _nextAllocBlockStart;

							} else {
								IExpressionReturn addrLoc = ParseOperand(addrContents);

								if (!addrLoc.Resolved) {
									Error("Block allocation address must be immediately resolvable.");
									PanicAllocation();
									continue;
								}

								blockAddress = addrLoc.ValueInt32;

								if (!SnesHelpers.IsValidBusAddress(blockAddress)) {
									Error("Invalid bus address.");
									PanicAllocation();
									continue;
								} else {
									_nextAllocBlockStart = blockAddress;
								}
							}

							if (sizeContents.IsEmpty) {
								continue;
							}

							IExpressionReturn sizeVal = ParseOperand(sizeContents);

							if (!sizeVal.Resolved) {
								Error("Block allocation size must be immediately resolvable.");
								blockEnd = int.MaxValue;
							} else {
								int blockSize = sizeVal.ValueInt32;
								blockEnd = blockAddress + blockSize;

								if (blockSize < 1) {
									Error("Block allocation size must be greater than 0");
								} else {
									sizedBlock = true;
									_nextAllocBlockStart = blockEnd;
								}
							}
						} else if (labelName.MatchesIgnoreCase(ENDALLOCATE)) {
							FinishCommand();
							return;

						} else if (labelName.MatchesIgnoreCase(SKIP)) {
							if (TryReadRestAsExpression(SKIP, SymbolContext.Provenance, out IExpressionReturn skipSize)) {
								AdvanceAllocation(skipSize.ValueInt32);
							}

						} else if (!HandleMoreDirectives(labelName)) {
							Error("Unexpected syntax in allocation block.");
							AbortCommand();
						}

						continue;

						void AdvanceAllocation(int allocStep) {
							blockAddress += allocStep;

							if (sizedBlock) {
								if (blockAddress > blockEnd) {
									Warning("Allocation chunk has exceeded its allotted size.");
									// unsize to avoid error spam
									blockEnd = int.MaxValue;
								}
							} else {
								_nextAllocBlockStart = blockAddress;
							}
						}

						void TryToAllocate(string allocatedName) {
							if (!TryReadRestAsExpression("allocation assignment", SymbolContext.Default, out var skipval)) {
								return;
							}

							int allocatedSizeOut = skipval.ValueInt32;

							if (allocatedSizeOut < 0) {
								Error("Symbol allocation cannot be a negative size.");
								return;
							}

							AllocatedSymbol alloc = new(allocatedName, blockAddress, allocatedSizeOut);

							AddOrReplaceSymbol(alloc);

							AdvanceAllocation(allocatedSizeOut);
						}

						void PanicAllocation() {
							blockAddress = 0;
							_nextAllocBlockStart = -1;
						}

					default:
						UnexpectedCharacter();
						AbortCommand();
						continue;
				}
			}

			if (_previousState == parentState) {
				Error("Unclosed allocation block.");
				return;
			}
		} while (PopSourceControl());
	}

	private bool TryGetEncoder(CharSpan encoderName, [NotNullWhen(true)] out SnesEncoder? encoder) {
		if (_encoders.TryGetValue(encoderName, out encoder)) {
			return true;

		} else if (encoderName is "ascii") {
			encoder = SnesEncoder.ASCII;
			return true;

		} else if (encoderName is "unicode") {
			encoder = SnesEncoder.Unicode;
			return true;

		} else {
			return false;
		}
	}

	// It'd be nice to try to remember when an encoder is redefined
	// but it would probably require tracking the file it's attached to as well
	// and generating an encoder is nearly instant, unlike reading a file
	// so, for now, whatever...
	private void Directive_ENCODER() {
		if (!DemandSpaceWithError()) return;

		if (!FastRead.TryReadIdentifier(ref _reading, _fileEnd, out CharSpan encoderName)) {
			Error(MsgInfo.BadIdentifier);
			AbortCommand();
			return;
		}

		bool encoderExists = TryGetEncoder(encoderName, out var encoder);

		// we hit the end, so just use the encoder
		if (AdvanceOverWhitespace()) {
			if (encoderExists) {
				_currentEncoder = encoder!;
			} else {
				Error("No such encoder");
			}
			return;
		}

		if (encoderExists) {
			Error("An encoder with this name already exists.");
		}

		// otherwise it needs to be an assignment
		if (*_reading is not '=') {
			Error("Expected an equals sign (=)");
			AbortCommand();
			return;
		}

		_reading++;
		FastRead.SkipInlineWhitespace(ref _reading, _fileEnd);

		if (TryReadAndExplodeStringContents(ref _reading, _fileEnd, out string? encPath)) {
			if (!TryGetSource(encPath, out var encoderFile)) {
				AbortCommand();
				return;
			}

			Dictionary<char, uint> encoding = new(256);

			CharSpan encoderBuffer = encoderFile.AsSpan();

			int line = 0;
			int lineLen;
			char* cptr, cend;
			int lastValue = 0;

			const char BadLastChar = '\n'; // this is never possible because we go line by line

			char lastChar = BadLastChar;

			foreach (var encLine in encoderBuffer.EnumerateLines()) {
				line++;
				lineLen = encLine.Length;

				if (lineLen is 0) {
					continue;
				}

				// would rather use a CharSpan, but all the utility routines use pointers
				fixed (char* encLinePtr = encLine) {
					cptr = encLinePtr;
					cend = cptr + lineLen;

					char defChar = *cptr;

					// look for ellipsis range begin
					if (defChar is '.') {
						if (lineLen >= 3 && AsciiHelpers.FastTest(cptr + 1, '.', '.')) {
							cptr += 3;

							if (FastRead.SkipInlineWhitespaceWithEndCheck(ref cptr, cend)) {
								ErrorInEncoderFile("Unexpected elipsis");
							} else if (lastChar is BadLastChar) {
								ErrorInEncoderFile("No range to continue");
							} else {
								char endRangeChar = *cptr;
								cptr++;

								if (AssertRemainderIsEmpty()) {
									if (endRangeChar <= lastChar) {
										ErrorInEncoderFile("Invalid range");
									} else {
										lastChar++;
										AddRangeOfChars(endRangeChar);
										continue;
									}
								}
							}

							lastChar = BadLastChar;
							continue;
						}
					}

					if (++cptr == cend) {
						TryAddChar(defChar, lastValue++);
						lastChar = defChar;
						continue;
					}

					if (FastRead.SkipInlineWhitespaceWithEndCheck(ref cptr, cend)) {
						ErrorInEncoderFile($"Missing operator");
						continue;
					}

					if (*cptr is '.') {
						if (lineLen >= 4 && AsciiHelpers.FastTest(cptr + 1, '.', '.')) {
							cptr += 3;

							if (FastRead.SkipInlineWhitespaceWithEndCheck(ref cptr, cend)) {
								ErrorInEncoderFile("Unexpected elipsis");
							} else {
								char endRangeChar = *cptr;
								cptr++;

								if (AssertRemainderIsEmpty()) {
									if (endRangeChar <= defChar) {
										ErrorInEncoderFile("Invalid range");
									} else {
										lastChar = defChar;
										AddRangeOfChars(endRangeChar);
										continue;
									}
								}
							}

							lastChar = BadLastChar;
							continue;
						}

						// if it's still wrong, let it fall through to the '=' and fail
					}

					if (*cptr is not '=') {
						ErrorInEncoderFile("Invalid character");
						continue;
					}

					cptr++;

					if (FastRead.SkipInlineWhitespaceWithEndCheck(ref cptr, cend)) {
						ErrorInEncoderFile($"Missing value");
					} else {
						if (FastRead.TryReadHexRaw(ref cptr, cend, out int hexval) && AssertRemainderIsEmpty()) {
							lastChar = defChar;
							lastValue = hexval;
							TryAddChar(defChar, lastValue++);
							continue;
						} else {
							ErrorInEncoderFile("Invalid hex value");
						}
					}

					lastChar = NewLine;

					bool AssertRemainderIsEmpty() {
						if (!FastRead.IsWhiteSpace(cptr, cend)) {
							ErrorInEncoderFile($"Unexpected '{cptr[0]}' character");
							return false;
						}
						return true;
					}

					void AddRangeOfChars(char lastCharInRange) {
						int lv = lastValue;
						char lc = lastChar;

						for (; lc <= lastCharInRange; lc++) {
							TryAddChar(lc, lv++);
						}

						lastChar = lastCharInRange;
						lastValue = lv;
					}

					void TryAddChar(char toAdd, int addValue) {
						if (encoding.ContainsKey(toAdd)) {
							Warning($"Duplicate definition for '{toAdd}' on line {line} of {encPath}");
							encoding[toAdd] = (uint) addValue;
						} else {
							encoding.Add(toAdd, (uint) addValue);
						}

						//Console.WriteLine($"Adding '{toAdd}' = {addValue:X2}");
					}

					void ErrorInEncoderFile(string message) {
						Error($"{message} on line {line} of {encPath}");
					}
				}
			} // end dict loop

			if (!encoderExists) {
				string encNom = new(encoderName);
				SnesEncoder addEnc = new SnesEncoder.UserDefined(encNom, encoding);
				_encoders.Add(encNom, addEnc);
			}

			FinishCommand();

			// end encoder creation
		} else {
			AbortCommand();
		}
	}

	private void Directive_IF() {
		if (!DemandSpaceWithError()) return;

		if (TryReadRestAsExpression("if", SymbolContext.Default, out var ifval)) {
			_ifChain = new(_ifChain);

			if (ifval.Value.IsFalse()) {
				SkipToElseOrEndIf();
			}
		} else {
			SkipToEndIf();
		}
	}

	private void SkipToElseOrEndIf() {
		while (_reading < _fileEnd) {
			if (CaptureLine(out CharSpan blockLine)) {
				if (blockLine.BeginsWithIgnoreCaseAndSpace(IF)) {
					SkipToEndIf(); // this will also skip any ELSE inherently

					// special case for when endif is last thing in file
					if (_reading >= _fileEnd) {
						return;
					}
				} else if (blockLine.MatchesIgnoreCase(ELSE) || blockLine.MatchesIgnoreCase(ENDIF)) {
					return;
				}
			}
		}

		Error(MsgInfo.UnclosedIf);
	}

	private void SkipToEndIf() {
		int blockDepth = 1;

		while (_reading < _fileEnd) {
			if (CaptureLine(out CharSpan blockLine)) {
				if (blockLine.BeginsWithIgnoreCaseAndSpace(IF)) {
					blockDepth++;
				} else if (blockLine.MatchesIgnoreCase(ENDIF)) {
					if (--blockDepth is 0) {
						return;
					}
				}
			}
		}

		Error(MsgInfo.UnclosedIf);
	}

	private void Directive_ELSE() {
		if (_ifChain?.HitElse ?? true) {
			Error($"Unexpected {ELSE}");
			AbortCommand();
		} else {
			_ifChain.HitElse = true;
			FinishCommand();
			SkipToElseOrEndIf();
		}
	}

	private void Directive_ENDIF() {
		if (_ifChain is null) {
			Error($"Unexpected {ENDIF}");
			AbortCommand();
		} else {
			_ifChain = _ifChain.Parent;
			FinishCommand();
		}
	}

	private protected virtual void Directive_MMC() {
		Error("MMC rebanking is not enabled for this mapper mode.");
	}

	private void Directive_BANKGUARD() {
		if (DemandSpaceWithError() && DemandRestOfCommand(out OperandString contents)) {
			int len = contents.Length;

			if (contents.Length < 6) {
				Span<char> test = stackalloc char[contents.Length];
				contents.FastLower(test);

				switch (test) {
					case "off":
						_bankGuardMask = BankGuardMask_Off;
						return;

					case "half":
						_bankGuardMask = BankGuardMask_Half;
						return;

					case "full":
						_bankGuardMask = BankGuardMask_Full;
						return;
				}
			}

			Error($"Invalid bank guard name: {CharSpanHelpers.ClampForMessage(contents)}");
		}
	}

	private void Directive_VECTOR() {
		if (DemandSpaceWithError() && DemandRestOfCommand(out OperandString contents)) {
			if (contents.Length < 10) {
				Span<char> contentsLower = stackalloc char[contents.Length];

				contents.FastLower(contentsLower);

				switch (contentsLower) {
					case "cop":
						TrySetVector(ref _copVectorNative, "native COP");
						return;

					case "brk":
						TrySetVector(ref _brkVector, "native BRK");
						return;

					case "abort":
						TrySetVector(ref _abortVectorNative, "native abort");
						return;

					case "nmi":
						TrySetVector(ref _nmiVectorNative, "native NMI");
						return;

					case "irq":
						TrySetVector(ref _irqVectorNative, "native IRQ");
						return;

					case "reset":
						TrySetVector(ref _resetVector, "reset");
						return;

					case "ecop":
						TrySetVector(ref _copVectorEmulation, "emulation mode COP");
						return;

					case "eabort":
						TrySetVector(ref _abortVectorEmulation, "emulation mode abort");
						return;

					case "enmi":
						TrySetVector(ref _nmiVectorEmulation, "emulation mode NMI");
						return;

					case "eirq":
						TrySetVector(ref _irqVectorEmulation, "emulation mode IRQ/BRK");
						return;
				}
			}

			Error($"Invalid vector name: {CharSpanHelpers.ClampForMessage(contents)}");

			void TrySetVector(ref int? vector, string name) {
				if (Mapper is MapperMode.None or MapperMode.Invalid) {
					Error("This assembly mapper cannot apply interrupt vectors.");
					return;
				}

				if (vector is not null) {
					Error($"The {name} vector has already been defined.");

				} else {
					if (JumpTargetMakesSense(_pc, 0x00_FFE0)) {
						vector = _pc;
					} else {
						Error("Vectors must be in bank 00 or mirror.");
					}
				}
			}
		}
	}
}
