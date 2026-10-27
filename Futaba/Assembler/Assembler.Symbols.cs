namespace Futaba;

// TODO include lang with symbol or something to differentiate?
unsafe partial class Assembler {
	/// <summary>
	/// Returns a new copy of this instance's symbols dictionary.
	/// </summary>
	/// <inheritdoc cref="ThrowIfAssembling()" path="//remarks|//exception"/>
	public Dictionary<string, Symbol> GetSymbols() {
		ThrowIfAssembling();
		return _symbols.GetCopy();
	}

	/// <summary>
	/// Returns a new copy of this instance's variables dictionary.
	/// </summary>
	/// <inheritdoc cref="ThrowIfAssembling()" path="//remarks|//exception"/>
	public Dictionary<string, Variable> GetVariables() {
		ThrowIfAssembling();
		return _variables.GetCopy();
	}

	/// <summary>
	/// Returns a new copy of this instance's breakpoint list.
	/// </summary>
	/// <inheritdoc cref="ThrowIfAssembling()" path="//remarks|//exception"/>
	public Breakpoint[] GetBreakpoints() {
		ThrowIfAssembling();
		return [.. _breakpoints];
	}

	#region Private fields
	private List<Breakpoint> _breakpoints = [];

	private SpannedLookup<MacroCall> _macros = new(128);
	private SpannedLookup<UserFunction> _functions = new(128);

	private Dictionary<string, Symbol> _initialSymbols = [];

	private Dictionary<string, Variable> _initialVariables = [];

	// coax all of these collections onto the large object heap to help reduce GC pressure
	// TODO more case testing to find the best values here to prevent reallocations when extra large
	private SpannedLookup<Symbol> _symbols = new(50000);
	private List<SimpleRequest> _simpleRequests = new(50000);
	private List<BranchRequest> _branchRequests =  new(50000);

	// don't expect these to be as common, so they can go on the normal heap
	private List<FillRequest> _fillRequests = [];
	private List<AbsolutePointerRequest> _jumpRequests = [];
	private SpannedLookup<Variable> _variables = new(50);

	private readonly string[] _nameHierarchy = new string[HierarchySize];
	private readonly int[] _pcHierarchy = new int[HierarchySize];
	private int _hierarchyDepth = 0;

	private readonly int[] _pluslabels = new int[MaxRelativeCount];
	private readonly int[] _minuslabels = new int[MaxRelativeCount];

	private bool _pooling = false;
	private readonly List<string> _poolHeaders = new(8);

	#endregion Private fields

	private void CreateSimpleRequest(IExpressionReturn item, int offset, int size) {
		_simpleRequests.Add(new SimpleRequest(item, offset, size, _time, CurrentSourceLine));
	}

	// TODO need real world performance tests to micro benchmark vectorization of these writes using a mask (and possibly a size mask in an indexed array)
	private void ResolveAllRequests() {
		const string CouldntDoIt = "Unable to resolve request";

		foreach (SimpleRequest sreq in _simpleRequests) {
			if (sreq.TryToResolve()) {
				byte* writing = _romPtr + sreq.Offset;
				uint* timing = _timePtr + sreq.Offset;

				uint resotime = sreq.Time;
				int rvalue = sreq.Value;

				switch (sreq.Size) {
					case 4:
						if (timing[3] <= resotime) {
							writing[3] = (byte) (rvalue >> 24);
							timing[3] = resotime;
						}
						goto case 3;

					case 3:
						if (timing[2] <= resotime) {
							writing[2] = (byte) (rvalue >> 16);
							timing[2] = resotime;
						}
						goto case 2;

					case 2:
						if (timing[1] <= resotime) {
							writing[1] = (byte) (rvalue >> 8);
							timing[1] = resotime;
						}

						goto case 1;

					case 1:
						if (timing[0] <= resotime) {
							writing[0] = (byte) rvalue;
							timing[0] = resotime;
						}
						break;
				}
			} else {
				Error_BadResolve(sreq.Item, CouldntDoIt, sreq.SourceLine);
			}
		}

		foreach (BranchRequest breq in _branchRequests) {
			if (breq.TryToResolve()) {
				int distance = breq.Distance;

				if (breq.DistanceTooLarge) {
					Error(SnesHelpers.BranchDistanceError(breq.Distance, breq.Size), breq.SourceLine);
					continue;
				}

				byte* writing = _romPtr + breq.Offset;
				uint* timing = _timePtr + breq.Offset;
				uint resotime = breq.Time;

				if (breq.Size is 2) {
					if (timing[1] <= resotime) {
						writing[1] = (byte) (distance >> 8);
						timing[1] = resotime;
					}
				}

				if (timing[0] <= resotime) {
					writing[0] = (byte) distance;
					timing[0] = resotime;
				}

			} else {
				Error_BadResolve(breq.Item, CouldntDoIt, breq.SourceLine);
			}
		}

		foreach (AbsolutePointerRequest aprq in _jumpRequests) {
			if (aprq.TryToResolve()) {
				byte* writing = _romPtr + aprq.Offset;
				uint* timing = _timePtr + aprq.Offset;
				uint resotime = aprq.Time;
				int rvalue = aprq.Value;

				if (timing[1] <= resotime) {
					writing[1] = (byte) (rvalue >> 8);
					timing[1] = resotime;
				}

				if (timing[0] <= resotime) {
					writing[0] = (byte) rvalue;
					timing[0] = resotime;
				}

				WarningIfWeirdLocalJump(rvalue, aprq.Provenance);
			} else {
				Error_BadResolve(aprq.Item, CouldntDoIt, aprq.SourceLine);
			}
		}

		foreach (FillRequest freq in _fillRequests) {
			if (freq.TryToResolve()) {
				FullfillBlockFillRequest(freq.Offset, freq.Size, freq.WordSize, freq.Value, freq.Time);
			} else {
				Error_BadResolve(freq.Item, CouldntDoIt, freq.SourceLine);
			}
		}
	}

	private const int MaxLabelNameLength = 100;
	private const int MaxSublabelDepth = 20;
	private const int HierarchySize = MaxSublabelDepth + 4;

	private void UpdateHierarchy(string name, int depth) {
		if (depth > (_hierarchyDepth + 1)) {
			Error("Label has no parent");
		} else if (depth > MaxSublabelDepth) {
			Error($"Exceeded max label depth: {depth}/{MaxSublabelDepth}");
		} else {
			_hierarchyDepth = depth;
			_nameHierarchy[depth] = name;
			_pcHierarchy[depth] = _offset;
		}
	}

	private void AddOrReplaceSymbol(Symbol item) {
		string symbolName = item.Name;

		if (symbolName.Length is < 4) {
			if (Directives.ReservedKeywords.Contains(symbolName)) {
				Error($"{symbolName} is a reserved word and cannot be used as a symbol name.");
				return;
			}
		}

		ref Symbol? getSym = ref _symbols.GetRefOrDefault(symbolName);

		if (getSym is null) {
			getSym = item;
		} else if (getSym is SymbolPlaceholder fl) {
			fl.Desiree = item;
			getSym = item;
		} else {
			Error("A symbol with the given name already exists.");
		}
	}

	private bool DemandColonOnLabel() {
		if (*_reading is not ':') {
			Error("Missing colon on label");
			AbortCommand();
			return false;
		} else {
			_reading++;
			return DemandSpaceAfterColon();
		}
	}

	private bool DemandSpaceAfterColon() {
		if (_reading->IsNiceBoundary) {
			_reading++;
			return true;
		} else {
			Error("Unexpected character after label declaration.");
			AbortCommand();
			return false;
		}
	}

	private Symbol RequestSymbol(CharSpan name) {
		ref var getsym = ref _symbols.GetRefOrDefault(name);

		getsym ??= new SymbolPlaceholder(new string(name));

		return getsym;
	}

	private Symbol RequestSymbol(string name) {
		ref var getsym = ref _symbols.GetRefOrDefault(name);

		getsym ??= new SymbolPlaceholder(name);

		return getsym;
	}

	private bool TryGetSymbol(string name, [NotNullWhen(true)] out Symbol? item) {
		if (_symbols.TryGetValue(name, out item) && item.Resolved) {
			return true;
		} else {
			item = null;
			return false;
		}
	}

	/// <summary>
	/// Adds a given symbol to the assembler's lookup table.
	/// </summary>
	/// <param name="toAdd">The symbol to add.</param>
	/// <inheritdoc cref="TryAddSymbol(string, int)" path="//returns|//remarks|//exception"/>
	public bool TryAddSymbol(Symbol toAdd) {
		ThrowIfAssembling();

		return AddSymbolUnchecked(toAdd);
	}

	/// <summary>
	/// Tries to add a symbol with a given name and value to the assembler's lookup table.
	/// </summary>
	/// <param name="name">The symbol's case-sensitive name.</param>
	/// <param name="value">The symbol's value.</param>
	/// <returns><see langword="true"/> if the symbol was successfully added.</returns>
	/// <inheritdoc cref="ThrowIfAssembling" path="//remarks|//exception"/>
	public bool TryAddSymbol(string name, int value) {
		ThrowIfAssembling();

		if (AssignedSymbol.TryCreate(name, value, out var addSym, out var _)) {
			return AddSymbolUnchecked(addSym);
		}

		return false;
	}

	private bool AddSymbolUnchecked(Symbol toAdd) {
		string name = toAdd.Name;

		if (Directives.ReservedKeywords.Contains(name)) {
			return false;
		}

		ref var dictSym = ref CollectionsMarshal.GetValueRefOrAddDefault(_initialSymbols, name, out bool _);

		if (dictSym is null) {
			dictSym = toAdd;
			return true;
		} else {
			return false;
		}
	}

	/// <summary>
	/// Adds a collection of symbols to the assembler's lookup table.
	/// </summary>
	/// <remarks>
	/// Any symbols that could not be added during the pass will be ignored.
	/// <para><inheritdoc cref="ThrowIfAssembling" path="//remarks"/></para>
	/// </remarks>
	/// <param name="symbols">The collection of symbols to add.</param>
	/// <inheritdoc cref="ThrowIfAssembling" path="//exception"/>
	public void AddSymbols(Symbol[] symbols) {
		ThrowIfAssembling();

		foreach (Symbol toAdd in symbols) {
			_ = AddSymbolUnchecked(toAdd);
		}
	}

	/// <inheritdoc cref="AddSymbols(Symbol[])"/>
	public void AddSymbols(IEnumerable<Symbol> symbols) {
		ThrowIfAssembling();

		foreach (Symbol toAdd in symbols) {
			_ = AddSymbolUnchecked(toAdd);
		}
	}

	/// <summary>
	/// Clears the initial symbols dictionary.
	/// </summary>
	/// <inheritdoc cref="ThrowIfAssembling" path="//remarks|//exception"/>
	public void ClearInitialSymbols() {
		ThrowIfAssembling();
		_initialSymbols.Clear();
	}

	private void CreateAssignedSymbol(CharSpan symbolName) {
		if (!ReadRestOfCommand(out OperandString assignmentOperand)) {
			Error("Missing assignment for symbol");
		} else {
			IExpressionReturn expr = ParseOperand(assignmentOperand);

			if (expr.Resolved) {
				AddOrReplaceSymbol(new AssignedSymbol(new string(symbolName), expr.ValueInt32));
			} else {
				Error_BadResolve(expr, "Symbol assignment must be immediately resolvable.");
			}
		}
	}

	private void Directive_POOL() {
		if (_pooling) {
			Error("Close previous pool before creating a new one.");
		}

		if (DemandSpaceThenTopLevelName(out string? poolName)) {
			_poolHeaders.Clear();
			_hierarchyDepth = 0;

			_pcHierarchy[0] = _offset;

			_pooling = true;
			_poolHeaders.Add(poolName);

			while (!AdvanceOverWhitespace()) {
				if (TryReadTopLevelName(out poolName)) {
					if (_poolHeaders.Contains(poolName)) {
						Warning("Duplicate pool name", poolName);
					} else {
						_poolHeaders.Add(poolName);
					}
				} else {
					Error("Invalid pool name.");
				}

			}
		} else {
			Error(MsgInfo.BadLabelName);
		}
	}

	private void ClearPools() {
		_pooling = false;
		_poolHeaders.Clear();
	}

	private void Directive_PILING() {
		if (!DemandSpaceWithError()) return;

		if (TryReadAnyLabelName(out var fullName, out var chunkName, out int depth)) {
			int capacity;

			if (ReadForAllocator(out var alloc)) {
				IExpressionReturn allocVal = ParseOperand(alloc);

				if (allocVal.Resolved) {
					capacity = allocVal.ValueInt32;

					if (capacity < 1) {
						Error("Piling capacity must be > 0");
						return;
					}
				} else {
					Error(MsgInfo.MustBeResolvedNow(PILING));
					return;
				}
			} else {
				if (depth == 0) {
					if (!DemandColonOnLabel()) {
						return;
					}
				}

				capacity = int.MaxValue;
			}

			Piling addPiling = new(fullName, Provenance, _pc, _offset, capacity);

			AddOrReplaceSymbol(addPiling);
			UpdateHierarchy(chunkName, depth);
		} else {
			AbortCommand();
		}
	}

	private void Directive_DOCK() {
		if (!DemandSpaceWithError()) return;

		if (!TryReadTopLevelName(out string? segmentName)) {
			AbortCommand();
			return;
		}

		int length;

		if (ReadForAllocator(out OperandString alloc)) {
			IExpressionReturn allocVal = ParseOperand(alloc);
			length = allocVal.Resolved ? allocVal.ValueInt32 : -1;

			if (length < 1) {
				Error("Missing or invalid allocation.");
				return;
			}
		} else {
			length = -1;
		}

		Dock addSegment = new(segmentName, Provenance, _pc, _offset, length);

		AddOrReplaceSymbol(addSegment);
		UpdateHierarchy(segmentName, 0);

		if (length > 0) {
			int endOffset = _offset + length;
			Label endSeg = new($"{segmentName}.end", Provenance + length, OffsetToAddress(endOffset), endOffset);
			AddOrReplaceSymbol(endSeg);
			_currentSegment = addSegment;
			_segments.Add(addSegment);
		}
	}

	private void GenerateExtrinsicLabel() {
		_reading++; // skip # token

		if (TryReadTopLevelName(out string? extname)) {
			if (DemandColonOnLabel()) {
				// no hierarchy update (that's the point of this type)
				AddOrReplaceSymbol(new Label(extname, Provenance, _pc, _offset));
			}
		} else {
			Error(MsgInfo.BadLabelName);
		}
	}

	private bool TryExpandSublabelName(ref char* slstart, char* slend, [NotNullWhen(true)] out string? fullName) {
		if (!FastRead.TryReadSublabel(ref slstart, slend, out CharSpan sublabelname, out int dotCount)) {
			Error(MsgInfo.MissingSublabel);
			fullName = null;
			return false;
		} else {
			return TryFullyExpandLabelName(dotCount, sublabelname, out fullName);
		}
	}

	private bool TryReadAnyLabelName([NotNullWhen(true)] out string? fullName, [NotNullWhen(true)] out string? chunkName, out int depth) {
		if (*_reading is SublabelDelimiter) {
			if (TryReadSublabelName(out chunkName, out depth)) {
				return TryFullyExpandLabelName(depth, chunkName, out fullName);
			}
		} else {
			depth = 0;

			if (TryReadTopLevelName(out fullName)) {
				chunkName = fullName;
				return true;
			}
		}

		fullName = null;
		chunkName = null;
		return false;
	}

	private bool TryReadInlineSymbolReference([NotNullWhen(true)] out string? fullName) {
		if (*_reading is SublabelDelimiter) {
			if (TryReadSublabelReference(out CharSpan chunkName, out int depth)) {
				return TryFullyExpandLabelName(depth, chunkName, out fullName);
			} else {
				fullName = null;
				return false;
			}
		} else {
			return TryReadTopLevelName(out fullName);
		}
	}

	private bool DemandSpaceThenTopLevelName([NotNullWhen(true)] out string? fullName) {
		if (AdvanceOverWhitespace()) {
			Error("Unexpected end of command");
			fullName = null;
			return false;
		} else {
			return TryReadTopLevelName(out fullName);
		}
	}

	private bool TryReadTopLevelName([NotNullWhen(true)] out string? fullName) {
		if (FastRead.TryReadIdentifier(ref _reading, _fileEnd, out OperandString symbolName)) {
			fullName = symbolName;
			return true;
		} else {
			fullName = null;
			return false;
		}
	}

	private bool TryReadSublabelName([NotNullWhen(true)] out string? chunkName, out int depth) {
		depth = FastRead.CountCharacter(ref _reading, _fileEnd, SublabelDelimiter);

		CharSpan sublabelname = FastRead.ReadAlphaNumeric(ref _reading, _fileEnd);

		if (sublabelname.IsEmpty) {
			Error(MsgInfo.MissingSublabel);
		} else if (!_reading->IsNiceBoundary) {
			UnexpectedCharacter();
		} else {
			chunkName = new(sublabelname);
			return true;
		}

		chunkName = null;
		return false;
	}

	private bool TryReadSublabelReference(out CharSpan chunkName, out int depth) {
		if (FastRead.TryReadSublabel(ref _reading, _fileEnd, out chunkName, out depth)) {
			if (_reading->IsNiceBoundary) {
				return true;
			}

			UnexpectedCharacter();

		} else {
			Error(MsgInfo.MissingSublabel);
		}

		return false;
	}

	private void GenerateSublabel() {
		if (TryReadSublabelName(out string? chunkName, out int depth)) {
			UpdateHierarchy(chunkName, depth);

			if (!_pooling) {
				if (TryFullyExpandLabelName(depth, chunkName, out string? fullName)) {
					AddOrReplaceSymbol(new Label(fullName, Provenance, _pc, _offset));
				}
			} else {
				foreach (string poolName in _poolHeaders) {
					_nameHierarchy[0] = poolName;

					if (TryFullyExpandLabelName(depth, chunkName, out string? fullName)) {
						AddOrReplaceSymbol(new Label(fullName, Provenance, _pc, _offset));
					}
				}
			}
		}
	}

	private void GenerateBuoy() {
		int backDepth = FastRead.CountCharacter(ref _reading, _fileEnd, '^');

		if (*_reading is not SublabelDelimiter) {
			UnexpectedCharacter();
			AbortCommand();
			return;
		}

		if (!TryReadSublabelName(out string? chunkName, out int depth)) {
			AbortCommand();
			return;
		}

		backDepth = depth - backDepth;

		if (backDepth is < 0 or >= MaxSublabelDepth) {
			Error("Cannot buoy backwards further than declared sublabel's depth.");
			AbortCommand();
			return;
		}

		int quayOffset = _offset - _pcHierarchy[backDepth];

		if (!_pooling) {
			if (TryFullyExpandLabelName(depth, chunkName, out string? fullName)) {
				Buoy addsym = new(fullName, Provenance, _pc, _offset, quayOffset);
				AddOrReplaceSymbol(addsym);
				UpdateHierarchy(chunkName, depth);
			}
		} else {
			foreach (string poolName in _poolHeaders) {
				_nameHierarchy[0] = poolName;

				UpdateHierarchy(chunkName, depth);

				if (TryFullyExpandLabelName(depth, chunkName, out string? fullName)) {
					Buoy addsym = new(fullName, Provenance, _pc, _offset, quayOffset);
					AddOrReplaceSymbol(addsym);
				}
			}
		}
	}

	internal bool TryFullyExpandLabelName(int depth, CharSpan name, [NotNullWhen(true)] out string? fullName) {
		if (name.Length is 0) {
			Error(MsgInfo.MissingLabelName);
		} else if (depth is 0) {
			fullName = new(name);
			return true;
		} else if (depth <= (_hierarchyDepth + 1)) {
			// make depth of 1 faster
			if (depth == 1) {
				fullName = $"{_nameHierarchy[0]}.{name}";
			} else {
				var temp = _nameHierarchy.AsSpan(0, depth);

				fullName = $"{string.Join('.', temp)}.{name}";
			}

			//ValidateLabelNameLength(name);
			return true;
		} else {
			Error("This sublabel has no valid parent.");
		}

		fullName = null;
		return false;
	}

	private const int MaxRelativeCount = 100;
	private static string GetRelativeLabelName(char token, int count, int index) => $"{token}{count}.{index}";

	internal bool TryGetPreviusMinusLabel(int minusCount, [NotNullWhen(true)] out Symbol? label) {
		if (ValidateRelativeCount('-', minusCount)) {
			string name = GetRelativeLabelName('-', minusCount, _minuslabels[minusCount]);
			label = RequestSymbol(name);
			return true;
		} else {
			label = null;
			return false;
		}
	}

	internal bool TryGetNextPlusLabel(int plusCount, [NotNullWhen(true)] out Symbol? label) {
		if (ValidateRelativeCount('+', plusCount)) {
			string name = GetRelativeLabelName('+', plusCount, _pluslabels[plusCount] + 1);
			label = RequestSymbol(name);
			return true;
		} else {
			label = null;
			return false;
		}
	}

	private bool ValidateRelativeCount(char token, int count) {
		if ((uint) count <= MaxRelativeCount) {
			return true;
		} else {
			Error($"Too many {token} tokens: {count} / {MaxRelativeCount}");
			return false;
		}
	}

	private void GenerateRelativeLabel(char token) {
		int relcount = FastRead.CountCharacter(ref _reading, _fileEnd, token);

		if (_reading->IsNiceBoundary) {
			if (ValidateRelativeCount(token, relcount)) {
				int[] table = token is '+' ? _pluslabels : _minuslabels;

				ref int relindex = ref table[relcount];

				relindex++;

				// using the token in the name will guarantee a unique label, since users can't put + or - in label names
				string relname = GetRelativeLabelName(token, relcount, relindex);

				AddOrReplaceSymbol(new Label(relname, Provenance, _pc, _offset));
			}
		} else {
			UnexpectedCharacter();
			AbortCommand();
		}
	}
}
