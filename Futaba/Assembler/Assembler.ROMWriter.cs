namespace Futaba;

unsafe partial class Assembler {
	private const int RomBufferPadding = 128;

	// We're always going to allocate a power of 2 plus a little extra.
	// This mainly comes into play when dealing with the Grow option,
	// and we don't want to be reallocating 1 byte at a time.
	private int _allocatedLength = 0;

	/// <summary>
	/// Current program counter.
	/// </summary>
	private protected int _pc = 0;

	/// <summary>
	/// Current provenance.
	/// </summary>
	private int _site = 0;

	/// <summary>
	/// Pointer to address 0 of the ROM buffer.
	/// </summary>
	private byte* _romPtr;

	/// <summary>
	/// Pointer to current working address.
	/// </summary>
	private byte* _pcPtr;

	/// <summary>
	/// Manually handled difference between working pointer and base pointer.
	/// </summary>
	internal int _offset;

	// This represents when a write occurred.
	// As a single pass assembler, we don't have another way to tell the order of code
	// if it's handled in a different order than encountered--namely, when dealing with requests.
	// To compensate for that, we track the strict order of any write or request made
	// and use that to determine if something is allowed to write.
	// This technically has a potential to overflow,
	// but, given the nature of the consumers, this should never happen.
	/// <summary>
	/// Current write ordinal.
	/// </summary>
	private uint _time = 0;
	private const int TimeSize = sizeof(uint);

	/// <summary>
	/// Current ordinal address.
	/// </summary>
	private uint* _timePtr;

	private bool _inProvenance = false;

	internal int Provenance {
		get => _inProvenance ? _site : _pc;
		set {
			_inProvenance = true;
			_site = value;
		}
	}

	// Warning throttles
	private bool _shutUpAboutAddresses = false;
	private bool _shutUpAboutSegments = false;

	private List<Dock> _segments = [];
	private Dock? _currentSegment = null;

	private Stack<int> _pcStack = new(64);
	private Stack<long> _provStack = new(64);

	private void PushPC() {
		_pcStack.Push(_pc);
	}

	private void PullPC() {
		if (_pcStack.TryPop(out int newpc)) {
			_pc = newpc;
			_offset = AddressToOffset(_pc);
			_pcPtr = _romPtr + _offset;
		} else {
			Error("No program counter to pop from stack.");
		}
	}

	private void PushProvenance() {
		if (_inProvenance) {
			_provStack.Push(_site);
		} else {
			_provStack.Push(long.MinValue);
		}
	}

	private void PullProvenance() {
		if (_provStack.TryPop(out long newpv)) {
			if (newpv is long.MinValue) {
				_inProvenance = false;
			} else {
				_site = (int) newpv;
				_inProvenance = true;
			}
		} else {
			Error("No address to pop from stack.");
		}
	}

	private void Skip(int steps) {
		if (TryAdvanceProgramCounter(steps)) {
			AdvanceProgramCounter(steps);
		}
	}

	/// <summary>
	/// Placeholders to help keep things aligned in ROM
	/// </summary>
	private void PlaceholderOp(byte opcode, int operandSize) {
		if (TryAdvanceProgramCounter(operandSize + 1)) {
			_pcPtr[0] = opcode;
			new Span<byte>(_pcPtr + 1, operandSize).Clear();
			AdvanceProgramCounter(operandSize);
		}
	}

	/// <inheritdoc cref="PlaceholderOp(byte, int)"/>
	private void PlaceholderOp(byte opcode) {
		if (TryAdvanceProgramCounter(1)) {
			_pcPtr[0] = opcode;
			TrackAndStep1();
		}
	}

	private void WarningIfWeirdLocalJump(int addr, int dest) {
		if (!JumpTargetMakesSense(addr, dest)) {
			Warning("Possibly erroneous absolute jump; use .w to suppress this message.");
		}
	}

	private void WarnCounter(int guarded, int newLocation) {
		if (newLocation > guarded) {
			Error($"Current location (${newLocation:X6}) has passed guarded location ${guarded:X6}.");
		}
	}

	private static void PerformBlockFill(byte* ptr, int blocksize, int wordsize, int val) {
		if (blocksize < 1) {
			return;
		}

		// timing handled by fill command
		int chunkSize;
		int left;

		switch (wordsize) {
			case 1:
				SnesHelpers.FillBlockB(ptr, (byte) val, blocksize);
				break;

			case 2:
				chunkSize = blocksize >> 1;

				SnesHelpers.FillBlockW(ptr, (ushort) val, chunkSize);

				if ((blocksize & 1) is 1) {
					ptr[blocksize - 1] = (byte) val;
				}

				break;

			case 3:
				(chunkSize, left) = Math.DivRem(blocksize, 3);

				ushort val16 = SnesHelpers.FixBigEndian((ushort) val);

				for (int i = chunkSize; i > 0; i--, ptr += 3) {
					*(ushort*) ptr = val16;
					ptr[2] = (byte) (val >> 16);
				}

				if (left is 1) {
					ptr[0] = (byte) val;

				} else if (left is 2) {
					*(ushort*) ptr = val16;

				}

				break;

			case 4:
				chunkSize = blocksize >> 2;

				SnesHelpers.FillBlockD(ptr, val, chunkSize);

				left = blocksize & 3;

				if (left is 1) {
					ptr[blocksize - 1] = (byte) val;

				} else if (left is 2) {
					SnesHelpers.WriteW(ptr + blocksize - 2, val);

				} else if (left is 3) {
					SnesHelpers.WriteL(ptr + blocksize - 3, val);
				}

				break;

			default:
				for (int start = 0; start < wordsize; start++) {
					byte fillval = (byte) val;
					val >>= 8;

					for (int j = start; j < blocksize; j += wordsize) {
						ptr[j] = fillval;
					}
				}
				break;
		}
	}

	private void FullfillBlockFillRequest(int offset, int blocksize, int wordsize, int val, uint time) {
		if (blocksize < 1) {
			return;
		}

		byte* writePtr = _romPtr + offset; // put these into registers without the additional offset
		uint* timePtr = _timePtr + offset;

		if (wordsize is 1) {
			byte addVal = (byte) val;
			for (int i = 0; i < blocksize; i++, writePtr++, timePtr++) {
				if (*timePtr <= time) {
					*writePtr = addVal;
					*timePtr = time;
				}
			}

			return;
		} else {
			for (int start = 0; start < wordsize; start++) {
				byte fillval = (byte) val;
				val >>= 8;

				for (int j = start; j < blocksize; j += wordsize) {
					var h = timePtr[j];
					if (timePtr[j] <= time) {
						writePtr[j] = fillval;
						timePtr[j] = time;
					}
				}
			}
		}
	}

	private void WriteData(IExpressionReturn val, int size) {
		if (TryAdvanceProgramCounter(size)) {
			if (val.NeedsRequest) {
				CreateSimpleRequest(val, _offset, size);
			} else {
				switch (size) {
					case 1:
						_pcPtr[0] = (byte) val.ValueInt32;
						break;

					case 2:
						SnesHelpers.WriteW(_pcPtr, val.ValueInt32);
						break;

					case 3:
						SnesHelpers.WriteL(_pcPtr, val.ValueInt32);
						break;

					case 4:
						SnesHelpers.WriteD(_pcPtr, val.ValueInt32);
						break;
				}
			}

			TrackAndStep(size);
		}
	}

	private void TrackAndStep(int step) {
		new Span<uint>(_timePtr + _offset, step).Fill(_time++);
		AdvanceProgramCounter(step);
	}

	private void WriteData(int offset, IExpressionReturn val, int size) {
		if (TestOffsetBounds(offset + size)) {
			if (val.NeedsRequest) {
				CreateSimpleRequest(val, offset, size);

			} else {
				byte* wrTemp = _romPtr + offset;

				switch (size) {
					case 1:
						wrTemp[0] = (byte) val.ValueInt32;
						break;

					case 2:
						SnesHelpers.WriteW(wrTemp, val.ValueInt32);
						break;

					case 3:
						SnesHelpers.WriteL(wrTemp, val.ValueInt32);
						break;

					case 4:
						SnesHelpers.WriteD(wrTemp, val.ValueInt32);
						break;
				}
			}

			ReserveRange(offset, size);
		}
	}

	private void WriteData(int offset, int value, int size) {
		if (TestOffsetBounds(offset + size)) {
			byte* wrTemp = _romPtr + offset;

			switch (size) {
				case 1:
					wrTemp[0] = (byte) value;
					break;

				case 2:
					SnesHelpers.WriteW(wrTemp, value);
					break;

				case 3:
					SnesHelpers.WriteL(wrTemp, value);
					break;

				case 4:
					SnesHelpers.WriteD(wrTemp, value);
					break;
			}

			ReserveRange(offset, size);
		}
	}

	// TODO try to use vector128.shuffle?
	private void InsertEncodedString(int offset, int wordsize, string text, SnesEncoder encoder) {
		int length = text.Length;

		byte* bufferB = _romPtr + offset;
		var enc = encoder.EncoderFunction;

		switch (wordsize) {
			case 1:
				foreach (char c in text) {
					*bufferB = (byte) enc(c);
					bufferB++;
				}
				break;

			case 2:
				if (BitConverter.IsLittleEndian && encoder == SnesEncoder.Unicode) {
					text.AsSpan().CopyTo(new Span<char>(bufferB, length));
				} else {
					foreach (char c in text) {
						SnesHelpers.WriteW(bufferB, (int) enc(c));
						bufferB += 2;
					}
				}

				break;

			case 3:
				foreach (char c in text) {
					SnesHelpers.WriteL(bufferB, (int) enc(c));
					bufferB += 3;
				}
				break;

			case 4:
				foreach (char c in text) {
					SnesHelpers.WriteD(bufferB, (int) enc(c));
					bufferB += 4;
				}

				break;
		}

		ReserveRange(offset, length * wordsize);
	}

	private void ReserveRange(int offset, int count) {
		new Span<uint>(_timePtr + offset, count).Fill(_time++);
	}

	// The routines in this region are roughly organized such that
	// the TrackAndStep routines appear after the ones that use them
	// just to maybe influence cleaner compilation wrt cache
	#region writes

	private void WriteOpImplied(IExpressionReturn opcode) {
		if (TryAdvanceProgramCounter(1)) {
			if (opcode.NeedsRequest) {
				CreateSimpleRequest(opcode, _offset, 1);
			} else {
				_pcPtr[0] = (byte) opcode.ValueInt32;
			}

			TrackAndStep1();
		}
	}

	void AssembleImplied(OperandString argstr, SizeToken argsize, byte opcode) {
		if (argsize is not SizeToken.Unspecified) {
			TokensOnImplied();
		}

		if (!argstr.IsEmpty) {
			InvalidAddressingMode();
		}

		WriteOpImplied(opcode);
	}

	private void WriteOpImplied(byte opcode) {
		if (TryAdvanceProgramCounter(1)) {
			_pcPtr[0] = opcode;
			TrackAndStep1();
		}
	}

	private void TrackAndStep1() {
		_timePtr[_offset] = _time++;
		AdvanceProgramCounter(1);
	}

	private void AssembleBranch(OperandString argstr, byte opcode) {
		int len = argstr.Length;

		if (len is 0) {
			MissingOperand();
			PlaceholderOp(opcode, 1);
		} else {
			char c = argstr.GetUnchecked(0);

			// check for special characters
			if (len is 1) {
				if (c is BROP) {
					WriteOpBrop(opcode);
					return;
				} else if (c is JNOP) {
					WriteOpB(opcode, 0x00);
					return;
				} else if (c is BTYS) {
					WriteOpB(opcode, 0xFE);
					return;
				}
			}

			if (c is BranchLiteral) {
				WriteOpB(opcode, ParseOperandSliced(argstr, 1));
			} else {
				WriteOpRel8(opcode, ParseOperand(argstr));
			}
		}
	}

	private void WriteOpRel8(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(2)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				_branchRequests.Add(new BranchRequest(val, _offset + 1, Provenance + 2, _time, CurrentSourceLine));

			} else {
				var (good, distance) = SnesHelpers.TestGetBranchDistance(Provenance + 2, val.ValueInt32);

				if (good) {
					wr[1] = (byte) distance;
				} else {
					wr[1] = 0; // placeholder
					Error(SnesHelpers.BranchDistanceError(distance, 1));
				}
			}

			TrackAndStep2();
		}
	}

	private void WriteOpBrop(byte opcode) {
		if (TryAdvanceProgramCounter(2)) {
			_pcPtr[0] = opcode;
			// Operand write intentionally skipped
			TrackAndStep2();
		}
	}

	private void WriteOpB(byte opcode, byte val) {
		if (TryAdvanceProgramCounter(2)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;
			wr[1] = val;
			TrackAndStep2();
		}
	}

	private void WriteOpB(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(2)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				CreateSimpleRequest(val, _offset + 1, 1);
			} else {
				wr[1] = (byte) val.ValueInt32;
			}

			TrackAndStep2();
		}
	}

	private void TrackAndStep2() {
		uint* mod = _timePtr + _offset;
		uint wt = _time++;
		mod[0] = wt;
		mod[1] = wt;
		AdvanceProgramCounter(2);
	}

	private void WriteOpBB(byte opcode, IExpressionReturn val1, IExpressionReturn val2) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val1.NeedsRequest) {
				CreateSimpleRequest(val1, _offset + 1, 1);
			} else {
				wr[1] = (byte) val1.ValueInt32;
			}

			if (val2.NeedsRequest) {
				CreateSimpleRequest(val2, _offset + 2, 1);
			} else {
				wr[2] = (byte) val2.ValueInt32;
			}

			TrackAndStep3();
		}
	}

	private void WriteOpBB(byte opcode, IExpressionReturn val1, byte val2) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;
			wr[2] = val2;

			if (val1.NeedsRequest) {
				CreateSimpleRequest(val1, _offset + 1, 1);
			} else {
				wr[1] = (byte) val1.ValueInt32;
			}

			TrackAndStep3();
		}
	}

	private void WriteOpBB(byte opcode, byte val1, IExpressionReturn val2) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;
			wr[1] = val1;

			if (val2.NeedsRequest) {
				CreateSimpleRequest(val2, _offset + 2, 1);
			} else {
				wr[2] = (byte) val2.ValueInt32;
			}

			TrackAndStep3();
		}
	}

	private void WriteOpBRel(byte opcode, IExpressionReturn val1, IExpressionReturn val2) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val1.NeedsRequest) {
				CreateSimpleRequest(val1, _offset + 1, 1);
			} else {
				wr[1] = (byte) val1.ValueInt32;
			}

			if (val2.NeedsRequest) {
				_branchRequests.Add(new BranchRequest(val2, _offset + 2, Provenance + 3, _time, CurrentSourceLine));

			} else {
				var (good, distance) = SnesHelpers.TestGetBranchDistance(Provenance + 3, val2.ValueInt32);

				if (good) {
					wr[2] = (byte) distance;
				} else {
					wr[2] = 0; // placeholder
					Error(SnesHelpers.BranchDistanceError(distance, 1));
				}
			}

			TrackAndStep3();
		}
	}

	private void WriteOpBBrop(byte opcode, IExpressionReturn val1) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val1.NeedsRequest) {
				CreateSimpleRequest(val1, _offset + 1, 1);
			} else {
				wr[1] = (byte) val1.ValueInt32;
			}

			// Operand write intentionally skipped

			TrackAndStep3();
		}
	}

	/// <summary>
	/// JMP (abs)   JML [abs]
	/// </summary>
	private void WriteOpWBank00(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				if (MissingTokenSeverity >= MissingTokenSeverity.AbsolutePointers) {
					_jumpRequests.Add(new DirectBankPointerRequest(val, _offset + 1, Provenance, _time, CurrentSourceLine));
				} else {
					CreateSimpleRequest(val, _offset + 1, 2);
				}
			} else {
				int vval = val.ValueInt32 & SnesHelpers.AbsoluteMask;

				if (MissingTokenSeverity >= MissingTokenSeverity.AbsolutePointers) {
					WarningIfWeirdLocalJump(vval, Provenance);
				}

				SnesHelpers.WriteW(wr + 1, val.ValueInt32);
			}

			TrackAndStep3();
		}
	}

	private void WriteOpRel16(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				_branchRequests.Add(new BranchRequestLong(val, _offset + 1, Provenance + 3, _time, CurrentSourceLine));
			} else {
				var (good, distance) = SnesHelpers.TestGetBranchDistanceLong(Provenance + 3, val.ValueInt32);

				if (good) {
					SnesHelpers.WriteW(wr + 1, distance);
				} else {
					SnesHelpers.WriteW(wr + 1, 0); // placeholder
					Error(SnesHelpers.BranchDistanceError(distance, 2));
				}
			}

			TrackAndStep3();
		}
	}

	/// <summary>
	/// For absolute jumps and calls
	/// </summary>
	private void WriteOpJ(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				if (MissingTokenSeverity >= MissingTokenSeverity.AbsolutePointers) {
					_jumpRequests.Add(new AbsolutePointerRequest(val, _offset + 1, Provenance, _time, CurrentSourceLine));
				} else {
					CreateSimpleRequest(val, _offset + 1, 2);
				}
			} else {
				int vval = val.ValueInt32;

				if (MissingTokenSeverity >= MissingTokenSeverity.AbsolutePointers) {
					WarningIfWeirdLocalJump(vval, Provenance);
				}
				SnesHelpers.WriteW(wr + 1, val.ValueInt32);
			}
			TrackAndStep3();
		}
	}

	private void WriteOpW(byte opcode, int val) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;
			SnesHelpers.WriteW(wr + 1, val);

			TrackAndStep3();
		}
	}

	private void WriteOpW(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(3)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				CreateSimpleRequest(val, _offset + 1, 2);
			} else {
				SnesHelpers.WriteW(wr + 1, val.ValueInt32);
			}

			TrackAndStep3();
		}
	}

	private void TrackAndStep3() {
		uint* mod = _timePtr + _offset;
		uint wt = _time++;
		mod[0] = wt;
		mod[1] = wt;
		mod[2] = wt;
		AdvanceProgramCounter(3);
	}

	private void WriteOpL(byte opcode, IExpressionReturn val) {
		if (TryAdvanceProgramCounter(4)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			if (val.NeedsRequest) {
				CreateSimpleRequest(val, _offset + 1, 3);
			} else {
				SnesHelpers.WriteL(wr + 1, val.ValueInt32);
			}

			TrackAndStep4();
		}
	}

	private void WriteOpL(byte opcode, int val) {
		if (TryAdvanceProgramCounter(4)) {
			byte* wr = _pcPtr;
			wr[0] = opcode;

			SnesHelpers.WriteL(wr + 1, val);

			TrackAndStep4();
		}
	}

	private void TrackAndStep4() {
		uint* mod = _timePtr + _offset;
		uint wt = _time++;
		mod[0] = wt;
		mod[1] = wt;
		mod[2] = wt;
		mod[3] = wt;
		AdvanceProgramCounter(4);
	}

	#endregion

	// FF_8* covers anything completely out of bounds
	private const int BankGuardMask_Off = unchecked((int) 0xFF_80_0000);
	private const int BankGuardMask_Half = unchecked((int) 0xFF_FF_8000);
	private const int BankGuardMask_Full = unchecked((int) 0xFF_FF_0000);

	private int _bankGuardMask = BankGuardMask_Half;

	internal bool TryAdvanceProgramCounter(int step) {
		if (!TestOffsetBounds(_offset + step)) {
			if (!_shutUpAboutAddresses) {
				Error("The program counter has escaped the valid ROM region.");
				_shutUpAboutAddresses = true;
			}
			return false;
		}

		int pctest = _pc + step - 1;
		pctest ^= _pc;
		pctest &= _bankGuardMask;

		if (pctest is not 0) {
			if (!_shutUpAboutAddresses) {
				Error("A protected bank boundary was crossed during assembly.");
				_shutUpAboutAddresses = true;
			}

			AdvanceProgramCounter(step); // advance anyways
			return false;
		}

		return true;
	}

	#region Address move tests

	private void TryMovePC(IExpressionReturn oper) {
		int addr;
		int offset;

		if (oper is ExpressionSymbol el && el.Item is Dock seg && el.Context is SymbolContext.RomAddress) {
			addr = seg.Address;
			offset = seg.BinaryOffset;
			_currentSegment = seg;

		} else {
			addr = oper.ValueInt32;
			offset = AddressToOffset(addr);
			_currentSegment = null;
		}

		if (TestOffsetBounds(offset)) {
			if (AddressIsRom(addr)) {
				// TODO make a single MovePc(int) and use that for all explicit moves
				_pc = addr;
				_offset = offset;
				_pcPtr = _romPtr + offset;

				_shutUpAboutAddresses = false;
				_shutUpAboutSegments = false;

				TestProtectedSegments(offset);
			} else {
				Error("Not a ROM address.");
			}
		}
	}

	private void AdvanceProgramCounter(int step) {
		_pcPtr += step;
		_site += step;
		_offset += step;

		StepPC(step);

		if (_segments.Count > 0) {
			StepCurrentSegment(step);
			TestProtectedSegments(_offset - 1);
		}
	}

	private void StepCurrentSegment(int step) {
		Dock? curSeg = _currentSegment;

		if (curSeg is not null) {
			curSeg.Advance(step, OffsetToAddress);

			if (!_shutUpAboutSegments && curSeg.State is DockState.Overflowed) {
				Error($"Assembly of segment \"{curSeg}\" has escaped its protected region.");
				_shutUpAboutSegments = true;
			}
		}
	}

	private void TestProtectedSegments(int offset) {
		if (!_shutUpAboutSegments) {
			Dock? curSeg = _currentSegment;

			foreach (Dock ptest in _segments) {
				if (ptest != curSeg) {
					if (ptest.OffsetIsInRegion(offset)) {
						Error($"Assembly has entered protected region \"{curSeg}\"");
						_shutUpAboutSegments = true;
						break;
					}
				}
			}
		}
	}

	private bool TryToResize(int newSize) {
		switch (OverflowAction) {
			case RomOverflowAction.Error:
				if (!_shutUpAboutAddresses) {
					Error("Cannot assemble outside the ROM's binary range.");
					_shutUpAboutAddresses = true;
				}
				return false;

			case RomOverflowAction.Grow:
			case RomOverflowAction.Expand:
				if (ResizeRom(newSize)) {
					if (OverflowAction is RomOverflowAction.Expand) {
						CurrentRomSize = _allocatedLength;
					}

					return true;
				}

				if (!_shutUpAboutAddresses) {
					Error("The ROM cannot be enlarged any further.");
					_shutUpAboutAddresses = true;
				}

				return false;
		}

		return true;
	}

	private bool ResizeRom(int desiredSize) {
		if (desiredSize < 1 || desiredSize > MaxRomSize) {
			return false;
		}

		int sizeChange = desiredSize.CompareTo(CurrentRomSize);

		if (sizeChange is 0) {
			return true;
		}

		int newAllocSize = SnesHelpers.GetAllocationSize(desiredSize);
		int oldAlloc = _allocatedLength;

		if (newAllocSize != oldAlloc) {
			ReallocRomBuffer(newAllocSize);

			// clear the newly allocated memory
			if (newAllocSize > oldAlloc) {
				new Span<byte>(_romPtr + oldAlloc, newAllocSize - oldAlloc).Clear();
			}
		}

		CurrentRomSize = desiredSize;
		return true;
	}

	private void ReallocRomBuffer(int size) {
		_romPtr = NativeMemory.ReAllocNice(_romPtr, size + RomBufferPadding);
		_timePtr = NativeMemory.ReAllocNice(_timePtr, size + RomBufferPadding);
		_allocatedLength = size;

		_pcPtr = _romPtr + _offset;
	}

	/// <summary>
	/// Clean up the unmanaged buffers.
	/// </summary>
	private void DeallocRomBuffer() {
		if (_romPtr != default) {
			NativeMemory.AlignedFree(_romPtr);
			_romPtr = default;
		}

		if (_timePtr != default) {
			NativeMemory.AlignedFree(_timePtr);
			_timePtr = default;
		}

		_pcPtr = default;
	}

	private void ResetRomBuffer() {
		CurrentRomSize = InitialRomSize;
		ReallocRomBuffer(SnesHelpers.GetAllocationSize(InitialRomSize));
		NativeMemory.Clear(_timePtr, (nuint) (TimeSize * _allocatedLength));

		_time = 1; // the baserom should count as 0

		Span<byte> rom = new(_romPtr, _allocatedLength);

		if (_baseRom is not null) {
			int baseromlen = Math.Min(_baseRom.Length, _allocatedLength);

			_baseRom.AsSpan().CopyTo(rom[..baseromlen]);

			rom = rom[baseromlen..];
		}

		rom.Clear();
	}

	#endregion
}
