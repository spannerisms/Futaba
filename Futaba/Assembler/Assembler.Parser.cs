namespace Futaba;

unsafe partial class Assembler {
	private const int MaxChain = 512;

	/// <summary>
	/// Returns whether or not this instance is busy with assembly.
	/// </summary>
	public bool Busy { get; private set; } = false;

	private SourcePointers? _previousState = null;

	private SourceObject _currentSourceObject = SourceFile.Empty;
	private SourceLine CurrentSourceLine => new(_currentSourceObject, _sourceLine, _lastCommandStart);

	private char* _reading = default;
	private char* _lastCommandStart = default;
	private char* _fileEnd = default;
	private IfLink? _ifChain = null;
	private int _sourceLine = 0;

	/// <summary>
	/// Things that should be done both before and after assembly
	/// both to reduce allocations and have a consistent outside state
	/// </summary>
	private protected virtual void RequiredInitAndCleanUp() {
		_currentEncoder = SnesEncoder.ASCII;

		_previousState = null;
		_ifChain = null;
		_currentSegment = null;

		Array.Fill(_labelNameHierarchy, string.Empty);
		Array.Clear(_pcHierarchy);

		ClearPools();
	}

	private void TryAssembly() {
		try {
			Busy = true;

			// clear file watchers
			_processedEvent = true;

			foreach (FileWatcher w in _watchedFiles.Values) {
				if (w.IsEmpty) {
					w.Enabled = false;
				} else {
					w.Clear();
				}
			}

			_processedEvent = false;

			MessageOut.Reset();
			ErrorOut.Reset();

			// reset the assembler's state
			ErrorCount = 0;
			WarningCount = 0;
			ErrorLine = null;

			if (!TryGetSource(EntryPoint.FullName, out var entryObj)) {
				return;
			}

			_shutUpAboutAddresses = false;
			_shutUpAboutSegments = false;

			_random.Reseed(_randomSeed);

			ResetRomBuffer();

			// Initial pass of header sets properties that the user can overwrite
			if (AutoPopulateHeader) {
				InitializeHeader();
			}

			_architecture = SnesArchitecture.WDC65816;
			_bankGuardMask = BankGuardMask_Half;

			_bpControl = BreakpointsControl.Off;
			_inProvenance = false;
			_nextAllocBlockStart = -1;

			// reset symbols
			_symbols.Clear();
			_symbols.CopyEntries(_initialSymbols);

			Array.Clear(_pluslabels);
			Array.Clear(_minuslabels);

			_variables.Clear();

			foreach ((string k, Variable v) in _initialVariables) {
				_variables.Add(k, v.Clone());
			}

			RequiredInitAndCleanUp();

			// reset source state
			_previousState = null;
			WatchFile(entryObj);
			TransferSourceControl(entryObj);

			StartAssembly();
			ResolveAllRequests();

			if (AutoPopulateHeader) {
				FinalizeHeader();
			} else if (CalculateChecksum) {
				RecalculateChecksum();
			}

		} catch {
			if (_currentSourceObject == SourceFile.Empty) {
				ErrorLine = "Unable to determine line.";
			} else {
				var srcLine = CurrentSourceLine;

				if (!srcLine.IsNull) {
					ErrorLine = $"{srcLine}";
				}
			}

			throw;

		} finally {
			RequiredInitAndCleanUp();

			_currentSourceObject = SourceFile.Empty;

			foreach (var (fileName, fileObj) in _loadedFiles) {
				if (fileObj?.WasUsed ?? false) {
					fileObj.WasUsed = false;
				} else {
					fileObj?.Dispose();
					_loadedFiles.Remove(fileName);
				}
			}

			foreach (var (fileName, fileObj) in _loadedBinaries) {
				if (fileObj.WasUsed) {
					fileObj.WasUsed = false;
				} else {
					fileObj.Dispose();
					_loadedBinaries.Remove(fileName);
				}
			}

			foreach (var srcObj in _loadedSourceObjects) {
				srcObj?.Dispose();
			}

			_loadedSourceObjects.Clear();
			_macros.Clear();
			_functions.Clear();
			_encoders.Clear();

			_simpleRequests.Clear();
			_fillRequests.Clear();
			_jumpRequests.Clear();
			_branchRequests.Clear();

			_segments.Clear();

			_pcStack.Clear();
			_provStack.Clear();

			Busy = false;
		}
	}

	private void StartAssembly() {
		do {
			while (_reading < _fileEnd) {
				// find the next command
				char c = *_reading;

				// these are so common that we'll handle them outside of the switch for efficiency
				// TODO benchmark if this is still a good idea (probably irrelevant)
				// skip over spaces
				if (c.IsSkippableSpace) {
					_reading++;
					// very fast whitespace skipping
					//char* rd = reading; // put in register
					//while ((++rd)->IsSkippableSpace) ;
					//
					//reading = rd;
					continue;

				} else if (c is NewLine) {
					_reading++;
					_sourceLine++;
					continue;
				}

				_lastCommandStart = _reading;

				// everything else is some sort of interesting character!
				switch (c) {
					// skip over comments
					case CommentChar:
						FastRead.ReadUntilLineEnd(ref _reading, _fileEnd);
						continue;

					case ':':
						if (FastRead.ColonIsSeparator(_reading)) {
							_reading += 2;
						} else {
							UnexpectedCharacter();
							AbortCommand();
						}
						continue;

					case '|':
						GenerateAddressMarker();
						continue;

					case '!':
						DeclareVariable();
						continue;

					case '#':
						GenerateExtrinsicLabel();
						continue;

					case '%':
						InvokeMacro();
						continue;

					case '+':
					case '-':
						GenerateRelativeLabel(c);
						continue;

					case '^':
						GenerateBuoy();
						continue;

					case '.':
						GenerateSublabel();
						continue;

					case '`':
						GenerateBreakpoint();
						continue;

					case '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9':
					case '/':
					case '"':
					case '$':
					case '&':
					case '\'':
					case '(':
					case ')':
					case '*':
					case ',':
					case '<':
					case '=':
					case '>':
					case '\\':
					case '[':
					case ']':
					case '~':
					case '@':
						UnexpectedCharacter();
						AbortCommand();
						continue;

					// undocumented because I honestly hate this
					case '{':
					case '}':
						_reading++;
						continue;

					// symbols / labels / mnemonics / directives
					case	'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G' or 'H' or 'I' or 'J' or 'K' or 'L' or 'M' or
							'N' or 'O' or 'P' or 'Q' or 'R' or 'S' or 'T' or 'U' or 'V' or 'W' or 'X' or 'Y' or 'Z' or
							'a' or 'b' or 'c' or 'd' or 'e' or 'f' or 'g' or 'h' or 'i' or 'j' or 'k' or 'l' or 'm' or
							'n' or 'o' or 'p' or 'q' or 'r' or 's' or 't' or 'u' or 'v' or 'w' or 'x' or 'y' or 'z' or '_':
						// no need to do an identifier check, since the first character is absolutely valid
						OperandString symOpDir = FastRead.ReadAlphaNumeric(ref _reading, _fileEnd);

						// check for a colon
						if (*_reading is ':') {
							_reading++;

							if (DemandSpaceAfterColon()) {
								Label addLabel = new(symOpDir, Provenance, _pc, _offset);

								AddOrReplaceSymbol(addLabel);
								UpdateHierarchy(addLabel.Name, 0);

								if (_pooling) {
									Error($"Use '{ENDPOOL}' before declaring a new hierarchy.");
									ClearPools();
								}
							}

							continue;
						}

						char* peeking = _reading;

						FastRead.SkipInlineWhitespace(ref peeking, _fileEnd);

						if (*peeking is '=') {
							_reading = peeking + 1;
							CreateAssignedSymbol(symOpDir);
							continue;
						}

						// try treating it as a data statement
						if (symOpDir.Length is 2 && c is 'd' or 'D') {
							// we could technically do a special check for the SPC instruction di here
							// but no one should ever use that, so who cares if it's a little slower?
							switch (symOpDir[1]) {
								case 'b' or 'B': DoDataItems(1); continue;
								case 'w' or 'W': DoDataItems(2); continue;
								case 'l' or 'L': DoDataItems(3); continue;
								case 'd' or 'D': DoDataItems(4); continue;
							}
						}

						// test for it being an instruction
						// if it is, assemble it
						if (_architecture is SnesArchitecture.WDC65816) {
							OpWdc opco = TestFor65816(symOpDir);

							if (opco is not OpWdc.NotGood) {
								Assemble65816(opco);
								continue;
							}
						} else if (_architecture is SnesArchitecture.SPC700) {
							OpSPC opco = TestForSPC700(symOpDir);

							if (opco is not OpSPC.NotGood) {
								AssembleSPC700(opco);
								continue;
							}
						} else if (_architecture is SnesArchitecture.SuperFX) {
							OpSFX opco = TestForSuperFX(symOpDir);

							if (opco is not OpSFX.NotGood) {
								AssembleSuperFX(opco);
								continue;
							}
						}

						HandleDirectives(symOpDir);
						continue;

					default:
						UnexpectedCharacter();
						AbortCommand();
						continue;

				} // end switch
			} // end source while
		} while (PopSourceControl()); // end stack while
	}

	private void HandleDirectives(OperandString directive) {
		// Assume everything else is a directive
		int dlen = directive.Length;

		if (dlen <= LongestDirectiveLength) {
			scoped Span<char> dirword = stackalloc char[dlen];
			directive.FastLower(dirword);

			switch (dirword) {
				case PILE:
					Directive_PILE();
					return;

				case PLOP:
					Directive_PLOP();
					return;

				case PILING:
					Directive_PILING();
					return;

				case DOCK:
					Directive_DOCK();
					return;

				case UNDOCK:
					_currentSegment = null;
					return;

				case POOL:
					Directive_POOL();
					return;

				case ENDPOOL:
					if (!_pooling) {
						Warning("Not currently pooling.");
					}

					ClearPools();
					FinishCommand();
					return;

				case INCSRC:
					Directive_INCSRC();
					return;

				case INCBIN:
					Directive_INCBIN();
					return;

				case ORG:
					if (DemandSpaceWithError() && TryReadRestAsExpression(ORG, SymbolContext.RomAddress, out IExpressionReturn orgpc)) {
						TryMovePC(orgpc);
					}

					return;

				case SITE:
					if (DemandSpaceWithError() && DemandRestOfCommand(out OperandString site)) {
						if (site.OnlyCharIs(SPLAT)) {
							_inProvenance = false;
						} else {
							IExpressionReturn siteval = ParseOperand(site, SymbolContext.Provenance);

							if (siteval.Resolved) {
								Provenance = siteval.ValueInt32;
							} else {
								Error_BadResolve(siteval, MsgInfo.MustBeResolvedNow(SITE));
							}
						}
					}

					return;

				case SAFEORG:
					if (DemandSpaceWithError() && DemandRestOfCommand(out OperandString safeOrgArg)) {
						IExpressionReturn safewarn = ParseOperand(safeOrgArg, SymbolContext.Provenance);
						IExpressionReturn safeorg = ParseOperand(safeOrgArg, SymbolContext.RomAddress);

						if (safeorg.Resolved && safewarn.Resolved) {
							WarnCounter(safewarn.ValueInt32, _pc);
							TryMovePC(safeorg);
						} else {
							Error_BadResolve(safeorg, $"argument to {SAFEORG} must be immediately resolvable");
						}
					}

					return;

				case WARNPC:
					if (DemandSpaceWithError() && TryReadRestAsExpression(WARNPC, SymbolContext.Provenance, out IExpressionReturn warnpc)) {
						WarnCounter(warnpc.ValueInt32, _pc);
					}

					return;

				case WARNSITE:
					if (DemandSpaceWithError() && TryReadRestAsExpression(WARNPC, SymbolContext.Provenance, out IExpressionReturn warnsite)) {
						WarnCounter(warnsite.ValueInt32, Provenance);
					}

					return;

				case SKIP:
					if (DemandSpaceWithError() && TryReadRestAsExpression(SKIP, SymbolContext.Provenance, out IExpressionReturn skipSize)) {
						Skip(skipSize.ValueInt32);
					}

					return;

				case SKIPTO:
					if (DemandSpaceWithError() && TryReadRestAsExpression(SKIPTO, SymbolContext.RomAddress, out IExpressionReturn skiptoExpr)) {
						int skiptoValue = skiptoExpr.ValueInt32;

						if (skiptoValue > 0) {
							int skipAmount = skiptoValue - Provenance;

							if (skipAmount >= 0) {
								Skip(skipAmount);
							} else {
								Error($"Target of {SKIPTO} is behind current provenance.");
							}
						} else {
							Error(MsgInfo.PositiveArgument);
						}
					}
					return;

				case ALIGN:
					if (DemandSpaceWithError() && TryReadRestAsExpression(ALIGN, SymbolContext.RomAddress, out IExpressionReturn alignSize)) {
						int alignValue = alignSize.ValueInt32;

						if (alignValue > 0) {
							int skipAmount = _pc % alignValue;

							if (skipAmount is not 0) {
								Skip(alignValue - skipAmount);
							}
						} else {
							Error(MsgInfo.PositiveArgument);
						}
					}
					return;

				case ARRANGE:
					if (DemandSpaceWithError() && TryReadRestAsExpression(ARRANGE, SymbolContext.Provenance, out IExpressionReturn arrangeSize)) {
						int arrangeValue = arrangeSize.ValueInt32;

						if (arrangeValue > 0) {
							int skipAmount = Provenance % arrangeValue;

							if (skipAmount is not 0) {
								Skip(arrangeValue - skipAmount);
							}
						} else {
							Error(MsgInfo.PositiveArgument);
						}
					}
					return;

				case PUSHPC:
					PushPC();
					FinishCommand();
					return;

				case PULLPC:
					PullPC();
					FinishCommand();
					return;

				case PUSHSITE:
					PushProvenance();
					FinishCommand();
					return;

				case PULLSITE:
					PullProvenance();
					FinishCommand();
					return;

				case BANKGUARD:
					Directive_BANKGUARD();
					return;

				case REBANK:
					Directive_REBANK();
					return;

				case ALLOCATE:
					Directive_ALLOCATE();
					return;

				case FILL:
					Directive_FILL();
					return;

				case BREAKPOINTS:
					Directive_BREAKPOINTS();
					return;

				case LANG:
				case ARCH:
					Directive_LANG();
					return;

				case MMC:
					Directive_MMC();
					return;

				case RAW:
					Directive_RAW();
					return;

				case BYTE:
				case WORD:
				case LONG:
				case DOUBLE:
				case SIZE:
				case COUNT:
				case RANDOM:
				case ENDALLOCATE:
					Error(MsgInfo.UnexKeyword, directive.ToString());
					AbortCommand();
					return;

				default:
					if (HandleMoreDirectives(dirword)) {
						return; // exit if directive was handled
					}

					break; // otherwise fall through to the error
			}
		}

		Error("Unrecognized text", $"\"{directive}\"");
		AbortCommand();
	}

	// These directives can be used in multiple block types,
	// so they're split out for shared access.
	private bool HandleMoreDirectives(CharSpan directive) {
		switch (directive) {
			case PRINT:
				if (TryGetPrintContents(out var contents)) {
					if (contents is not null) {
						MessageOut.WriteLine(contents);
					} else {
						MessageOut.WriteLine();
					}
				}
				return true;

			case PRINTF:
				Directive_PRINTF();
				return true;

			case WARN:
				if (TryGetPrintContents(out contents)) {
					Console.ForegroundColor = ConsoleColor.DarkYellow;

					ErrorOut.Write(CurrentSourceLine);
					ErrorOut.Write(WarningText);
					ErrorOut.Write(contents ?? "Unspecified warning");

					Console.ResetColor();
					ErrorOut.WriteLine();
					IncrementWarnings();
				}
				return true;

			case ERROR:
				if (TryGetPrintContents(out contents)) {
					Console.ForegroundColor = ConsoleColor.Red;

					ErrorOut.Write(CurrentSourceLine);
					ErrorOut.Write(ErrorText);
					ErrorOut.Write(contents ?? "Unspecified error");

					Console.ResetColor();
					ErrorOut.WriteLine();

					IncrementErrors();
				}
				return true;

			case MACRO:
				Directive_MACRO();
				return true;

			case FUNCTION:
				Directive_FUNCTION();
				return true;

			case IF:
				Directive_IF();
				return true;

			case ELSE:
				Directive_ELSE();
				return true;

			case ENDIF:
				Directive_ENDIF();
				return true;

			case ENCODER:
				Directive_ENCODER();
				return true;
		}

		return false;
	}

	private void TransferSourceControl<T>(T source) where T : SourceObject {
		if ((_previousState?.Depth ?? 0) >= MaxChain) {
			string MaxObj = $"The maximum number ({MaxChain}) of nested or chained assembly objects has been hit.";
			Error(MaxObj);
			throw new InvalidOperationException(MaxObj);
		}

		if (typeof(T) != typeof(SourceFile)) {
			_loadedSourceObjects.Add(source);
		}

		if (_currentSourceObject != SourceFile.Empty) {
			_previousState = new(_currentSourceObject, _reading, _sourceLine, _ifChain, _previousState);
		}

		_currentSourceObject = source;
		_fileEnd = source.SourceEnd;
		_reading = source.SourceStart;
		_lastCommandStart = _reading;
		_sourceLine = 1;
		_ifChain = null;
	}

	private bool PopSourceControl() {
		// check the parent for where to resume or if we should end
		if (_previousState is SourcePointers sp) {

			// TODO maybe have this for a verbose mode
#if DEBUG
			//if (CurrentSourceObject is SourceFile) {
			//	Console.WriteLine($"Finished {CurrentSourceObject}");
			//}
#endif

			_previousState = sp.Parent;
			(_currentSourceObject, _reading, _sourceLine, _ifChain) = sp;
			_fileEnd = _currentSourceObject.SourceEnd;

			return true;
		} else {
			return false;
		}
	}

	private class IfLink(IfLink? parent) {
		internal IfLink? Parent { get; } = parent;
		internal int Depth { get; } = parent?.Depth ?? 1;
		internal bool HitElse { get; set; } = false;
	}

	private sealed class SourcePointers(SourceObject source, char* current, int line, IfLink? ifstack, SourcePointers? parent) {
		internal SourcePointers? Parent { get; } = parent;
		internal SourceObject Source { get; } = source;
		internal char* Current { get; } = current;
		internal int Line { get; } = line;
		internal IfLink? IfStack { get; } = ifstack;
		internal int Depth { get; } = (parent?.Depth + 1) ?? 0;

		public void Deconstruct(out SourceObject _source, out char* _current, out int _line, out IfLink? _ifstack) {
			_current = Current;
			_line = Line;
			_source = Source;
			_ifstack = IfStack;
		}
	}
}
