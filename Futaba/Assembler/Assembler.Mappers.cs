namespace Futaba;

partial class Assembler {
	/// <inheritdoc cref="CreateAssemblerWithMapper(MapperMode,FileInfo)" path="//summary|//returns"/>
	/// <param name="mode"><inheritdoc cref="CreateAssemblerWithMapper(MapperMode,FileInfo)" path="/param[@name='mode']"/></param>
	/// <param name="path">The path of the file to be used as an entry point</param>
	public static Assembler CreateAssemblerWithMapper(MapperMode mode, string path) {
		return mode switch {
			MapperMode.None /*               */ => new UnmappedAssembler(path),
			MapperMode.Lorom /*              */ => new LoromAssembler(path),
			MapperMode.ExLorom /*            */ => new ExLoromAssembler(path),
			MapperMode.Hirom /*              */ => new HiromAssembler(path),
			MapperMode.ExHirom /*            */ => new ExHiromAssembler(path),
			MapperMode.Sa1 /*                */ => new Sa1Assembler(path),
			_ => /*                          */ throw new ArgumentException("Invalid mapper mode.", nameof(mode)),
		};
	}

	/// <summary>
	/// Creates an assembler for the given mode and using the specified entry point.
	/// </summary>
	/// <returns>A new <see cref="Assembler"/> object for the given mode and entry point.</returns>
	/// <param name="mode">The enum for the desired mapper.</param>
	/// <param name="entry">The <see cref="FileInfo"/> object to be used as the entry point of assembly.</param>
	public static Assembler CreateAssemblerWithMapper(MapperMode mode, FileInfo entry) {
		return CreateAssemblerWithMapper(mode, entry.FullName);
	}

	/// <summary>
	/// Gets the mapper mode used by this instance.
	/// </summary>
	public abstract MapperMode Mapper { get; }

	private protected abstract byte MapperCode { get; }

	/// <summary>
	/// Gets the maximum allowed ROM size for this mapper mode.
	/// </summary>
	public abstract int MaxRomSize { get; }

	/// <summary>
	/// Gets the minimum allowed ROM size for this mapper mode.
	/// </summary>
	public abstract int MinRomSize { get; }

	/// <summary>
	/// Tests an integer for being within the valid range of ROM sizes for this instance's mapper mode.
	/// </summary>
	/// <param name="size">The size to test.</param>
	/// <returns><see langword="true"/> if the size can be considered valid; otherwise <see langword="false"/>.</returns>
	public bool IsValidRomSize(int size) {
		return size >= MinRomSize && size <= MaxRomSize;
	}

	private bool TestOffsetBounds(int offset) {
		if ((uint) offset > MaxRomSize) {
			Error("This is very out of bounds.");
			return false;
		}

		if (offset > CurrentRomSize) {
			return ResizeRom(offset);
		}

		return true;
	}

	private protected abstract bool JumpTargetMakesSense(int address, int destination);

	/// <summary>
	/// Converts a given address to a binary offset.
	/// </summary>
	/// <param name="address">The address to convert.</param>
	public abstract int AddressToOffset(int address);

	/// <summary>
	/// Converts a given binary offset to a SNES address.
	/// </summary>
	/// <param name="offset">The binary offset to convert.</param>
	public abstract int OffsetToAddress(int offset);

	private protected abstract int GetOffsetFromAddress(int address);
	private protected abstract int GetAddressFromOffset(int offset);

	private protected abstract void StepPC(int step);

	private protected void StepPCLorom(int step) {
		int pc = _pc; // put into a register

		int smallStep, bankStep;

		if (step > 0) {
			smallStep = step & 0x7FFF;

			pc += smallStep;

			pc |= 0x8000;

			bankStep = step & 0xFF_8000;

			if (bankStep is not 0) {
				bankStep <<= 1;
				pc += bankStep;
			}
		} else {
			step = -step;

			smallStep = step & 0x7FFF;

			pc -= smallStep;

			if ((pc & 0x8000) is 0) {
				pc -= 0x01_8000;
			}

			bankStep = step & 0xFF_8000;

			if (bankStep is not 0) {
				bankStep <<= 1;
				pc -= bankStep;
			}
		}

		_pc = pc;
	}

	private protected void StepPCHirom(int step) {
		_pc += step;
	}

	/// <summary>
	/// Tests whether a given address points to a memory location corresponding to program ROM.
	/// </summary>
	/// <param name="address">The address to test</param>
	/// <returns><see langword="true"/> if the address points to program ROM; otherwise <see langword="false"/>.</returns>
	// TODO probably needs tinkering (eg DSP is mapped to bank 6x)
	public virtual bool AddressIsRom(int address) {
		// completely out of bounds
		if ((uint) address > 0xFF_FFFF) {
			return false;
		}

		// remove WRAM
		if ((address & 0xFE_0000) is 0x7E_0000) {
			return false;
		}

		// whether or not this area of the address space
		// is RAM or ROM depends on the PCB configuration
		// but we'll just assume the upper half is ROM
		// irrespective of mapper, it cannot be unique RAM
		// when this block is RAM; it's always a mirror of the lower half
		if ((address & 0x70_8000) is 0x70_0000) {
			return false;
		}

		// test for bank 40+ or upper halves to identify ROM
		return (address & 0x40_8000) is not 0;
	}
}

/// <summary>
/// Represents an assembler with no mapping mode.
/// </summary>
public sealed class UnmappedAssembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with no mapper map using the given file as its entry point.
	/// </summary>
	public UnmappedAssembler(FileInfo entry) : base(entry.FullName) { }

	/// <inheritdoc cref="UnmappedAssembler(FileInfo)"/>
	public UnmappedAssembler(string entry) : base(entry) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.None;
	private protected override byte MapperCode => 0x00;

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) => offset;
	private protected override int GetAddressFromOffset(int offset) => offset;

	/// <inheritdoc/>
	public override int AddressToOffset(int address) => address;
	private protected override int GetOffsetFromAddress(int address) => address;

	/// <inheritdoc/>
	public override bool AddressIsRom(int address) {
		return true;
	}

	private protected override void StepPC(int step) {
		_pc += step;
	}

	/// <inheritdoc/>
	public override int MinRomSize => 0;

	/// <inheritdoc/>
	public override int MaxRomSize => 0x80_0000;

	// doesn't make sense to perform these on an unmapped rom.
	private protected override void InitializeHeader() { }
	private protected override void AdjustInterruptVectors() { }
	private protected override void FinalizeHeader() { }
	private protected override void RecalculateChecksum() { }

	private protected override bool JumpTargetMakesSense(int address, int destination) {
		return ((address ^ destination) & SnesHelpers.BankMask) is 0;
	}
}

/// <summary>
/// Represents an assembler that uses the lorom mapping mode.
/// </summary>
public sealed class LoromAssembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the lorom mapping mode using the given file as its entry point.
	/// </summary>
	public LoromAssembler(FileInfo entry) : base(entry.FullName) { }

	/// <inheritdoc cref="LoromAssembler(FileInfo)"/>
	public LoromAssembler(string entry) : base(entry) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.Lorom;
	private protected override byte MapperCode => 0x20;

	/// <inheritdoc/>
	public override int MinRomSize => SnesHelpers.BankSize;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxRegularSize;

	private protected override void StepPC(int step) {
		StepPCLorom(step);
	}

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) => SnesHelpers.OffsetToLorom(offset);
	private protected override int GetAddressFromOffset(int offset) {
		return SnesHelpers.OffsetToLorom(offset);
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) => SnesHelpers.LoromToOffset(address);
	private protected override int GetOffsetFromAddress(int address) {
		return SnesHelpers.LoromToOffset(address);
	}

	private protected override bool JumpTargetMakesSense(int address, int destination) {
		return SnesHelpers.JumpTargetMakesSenseSmallRom(address, destination);
	}
}

/// <summary>
/// Represents an assembler that uses the hirom mapping mode.
/// </summary>
public sealed class HiromAssembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the hirom mapping mode using the given file as its entry point.
	/// </summary>
	public HiromAssembler(FileInfo entry) : base(entry.FullName) { }

	/// <inheritdoc cref="HiromAssembler(FileInfo)"/>
	public HiromAssembler(string entry) : base(entry) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.Hirom;
	private protected override byte MapperCode => 0x21;

	/// <inheritdoc/>
	public override int MinRomSize => SnesHelpers.BankSize * 2;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxRegularSize;

	private protected override void StepPC(int step) {
		StepPCHirom(step);
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) => SnesHelpers.HiromToOffset(address);
	private protected override int GetOffsetFromAddress(int address) {
		return SnesHelpers.HiromToOffset(address);
	}

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) => SnesHelpers.OffsetToHirom(offset);
	private protected override int GetAddressFromOffset(int offset) {
		return SnesHelpers.OffsetToHirom(offset);
	}

	private protected override bool JumpTargetMakesSense(int address, int destination) {
		return SnesHelpers.JumpTargetMakesSenseSmallRom(address, destination);
	}
}

/// <summary>
/// Represents an assembler that uses the exlorom mapping mode.
/// </summary>
public sealed class ExLoromAssembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the exlorom mapping mode using the given file as its entry point.
	/// </summary>
	public ExLoromAssembler(FileInfo entry) : base(entry.FullName) { }

	/// <inheritdoc cref="ExLoromAssembler(FileInfo)"/>
	public ExLoromAssembler(string entry) : base(entry) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.ExLorom;
	private protected override byte MapperCode => 0x25;

	/// <inheritdoc/>
	public override int MinRomSize => 0x40_0000;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxExSize;

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) => GetAddressFromOffset(offset);
	private protected override int GetAddressFromOffset(int offset) {
		// TODO - this is probably wrong
		return SnesHelpers.OffsetToLorom(offset) ^ 0x80_0000;
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) => GetOffsetFromAddress(address);
	private protected override int GetOffsetFromAddress(int address) {
		return SnesHelpers.LoromToOffset(address) ^ 0x40_0000;
	}

	private protected override void StepPC(int step) {
		_pc = AddressToOffset(_offset);
	}

	private protected override bool JumpTargetMakesSense(int address, int destination) {
		return SnesHelpers.JumpTargetMakesSenseExRom(address, destination);
	}

}

/// <summary>
/// Represents an assembler that uses the exhirom mapping mode.
/// </summary>
public sealed class ExHiromAssembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the ExHirom mapping mode using the given file as its entry point.
	/// </summary>
	public ExHiromAssembler(FileInfo entry) : base(entry.FullName) { }

	/// <inheritdoc cref="ExHiromAssembler(FileInfo)"/>
	public ExHiromAssembler(string entry) : base(entry) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.ExHirom;
	private protected override byte MapperCode => 0x25;

	/// <inheritdoc/>
	public override int MinRomSize => 0x40_0000;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxExSize;

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) => GetAddressFromOffset(offset);
	private protected override int GetAddressFromOffset(int offset) {
		// TODO verify
		return offset ^ 0x80_0000;
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) => GetOffsetFromAddress(address);
	private protected override int GetOffsetFromAddress(int address) {
		return (address & 0x7F_FFFF) ^ 0x40_0000;
	}

	private protected override void StepPC(int step) {
		StepPCHirom(step);
	}

	private protected override bool JumpTargetMakesSense(int address, int destination) {
		return SnesHelpers.JumpTargetMakesSenseExRom(address, destination);
	}
}

/// <summary>
/// Represents an assembler that uses the SA-1 (MMC) mapping mode.
/// </summary>
public sealed class Sa1Assembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the SA-1 using the given file as its entry point.
	/// </summary>
	public Sa1Assembler(FileInfo entry) : base(entry.FullName) { }

	/// <inheritdoc cref="Sa1Assembler(FileInfo)"/>
	public Sa1Assembler(string entry) : base(entry) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.Sa1;
	private protected override byte MapperCode => 0x23;

	private byte _CxBank;
	private byte _DxBank;
	private byte _ExBank;
	private byte _FxBank;

	private bool _CxLorom;
	private bool _DxLorom;
	private bool _ExLorom;
	private bool _FxLorom;

	/// <summary>
	/// Gets the processor used by this assembler.
	/// </summary>
	/// <remarks>
	/// This property cannot be changed for this mapper mode. Attempting to do so will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <inheritdoc path="//exception"/>
	public override Coprocessor Coprocessor {
		get => Coprocessor.SA1;
		set {
			if (value != Coprocessor.SA1) throw new InvalidOperationException(MsgInfo.NoChipChange);
		}
	}

	/// <inheritdoc path="//remarks"/>
	public override int MinRomSize => SnesHelpers.BankSize;

	/// <inheritdoc path="//remarks"/>
	public override int MaxRomSize => SnesHelpers.MaxMmcSize;

	private protected override void RequiredInitAndCleanUp() {
		_CxBank = 0x00;
		_DxBank = 0x01;
		_ExBank = 0x02;
		_FxBank = 0x03;

		_CxLorom = false;
		_DxLorom = false;
		_ExLorom = false;
		_FxLorom = false;

		base.RequiredInitAndCleanUp();
	}

	private protected override void Directive_MMC() {
		if (!DemandSpaceWithError()) return;

		if (!ReadRestOfCommand(out OperandString operand)) {
			return;
		}

		StringSplitter mmcops = new(operand);

		int bankSet = -1;

		StringSplitResult more;

		do {
			if (++bankSet > 3) {
				Error("Too many items in mmc directive.");
				AbortCommand();
				return;
			}

			more = mmcops.GetNextComma(out var next);

			if (next.OnlyChar is '*') continue;

			// get first and last char, because only those matter
			if (next is [char lorom, .. var middle, char region]) {
				if (middle.IsEmptyOrWhiteSpace()) {
					if (lorom.CiIs('L') || lorom.CiIs('H')) {
						region -= '0';

						if (region < 8) {
							byte bankRegion = (byte) region;
							bool isLorom = lorom.CiIs('L');

							switch (bankSet) {
								case 0:
									_CxBank = bankRegion;
									_CxLorom = isLorom;
									continue;

								case 1:
									_DxBank = bankRegion;
									_DxLorom = isLorom;
									continue;

								case 2:
									_ExBank = bankRegion;
									_ExLorom = isLorom;
									continue;

								case 3:
									_FxBank = bankRegion;
									_FxLorom = isLorom;
									continue;
							}
						}
					}
				}
			}

			Error("Invalid mmc banking instruction", (string) next);

		} while (more is StringSplitResult.MoreContent);
	}

	private int GetEffectiveBank(int address) {
		if ((uint) address > 0xFF_FFFF) {
			return -1;
		}

		return address switch {
			< 0x20_0000 => GetEffectiveLorom(address, _CxLorom ? _CxBank : 0),
			< 0x40_0000 => GetEffectiveLorom(address, _DxLorom ? _DxBank : 1),

			< 0x80_0000 => -1,

			< 0xA0_0000 => GetEffectiveLorom(address, _ExLorom ? _ExBank : 2),
			< 0xC0_0000 => GetEffectiveLorom(address, _FxLorom ? _FxBank : 3),

			< 0xD0_0000 => GetEffectiveHirom(address, _CxBank),
			< 0xE0_0000 => GetEffectiveHirom(address, _DxBank),
			< 0xF0_0000 => GetEffectiveHirom(address, _ExBank),
			_ /*     */ => GetEffectiveHirom(address, _FxBank),
		};

		static int GetEffectiveLorom(int addr, int bank) {
			addr &= 0x1F_0000;
			addr >>= 1;
			bank <<= SnesHelpers.MmcBankShift;
			return addr | bank;
		}

		static int GetEffectiveHirom(int addr, int bank) {
			addr &= 0xF_0000;
			bank <<= SnesHelpers.MmcBankShift;
			return addr | bank;
		}
	}

	// TODO needs stress testing
	private protected override bool JumpTargetMakesSense(int address, int destination) {
		int bankChange = (address ^ destination) & SnesHelpers.BankMask;

		if (bankChange is 0) {
			return true;
		}

		// catches negative numbers and out of bounds for both simultaneously
		if ((uint) (destination | address) > 0xFF_FFFF) {
			return false;
		}

		// see if we're in compatible, mirrored regions currently

		// TODO this probably needs tinkering e.g. when assembly time has a different banking than runtime
		// or just say "supress it with .w if you do that"

		//
		// This covers everything hirom
		//

		if (address >= 0xC0_0000) {
			int addressOffset = GetEffectiveBank(address);
			int destOffset = GetEffectiveBank(destination);

			if (destination >= 0xC0_0000) {
				return destOffset == addressOffset;

			} else if (SnesHelpers.IsInUpperBankHalf(address)) {
				return destOffset == addressOffset;

			} else if (SnesHelpers.IsInUpperBankHalf(destination)) {
				return destOffset == addressOffset;
			}

			return false;

		} else if (destination >= 0xC0_0000) {
			int addressOffset = GetEffectiveBank(address);
			int destOffset = GetEffectiveBank(destination);

			if (SnesHelpers.IsInUpperBankHalf(address)) {
				return destOffset == addressOffset;

			} else if (SnesHelpers.IsInUpperBankHalf(destination)) {
				return destOffset == addressOffset;
			}

			return false;
		}

		//
		// Check lorom area
		//

		// handles mirrored memory
		if (SnesHelpers.IsInMirroredBank(address)) {
			if (SnesHelpers.IsWram(destination)) {
				return SnesHelpers.IsMirroredWram(destination);  // mirrored WRAM is allowed

			} else if (SnesHelpers.IsCommonMirror(destination)) {
				return true;
			}
		}

		// handles lorom banks that match;
		if (SnesHelpers.IsInUpperBankHalf(destination) && SnesHelpers.IsInUpperBankHalf(address)) {
			return GetEffectiveBank(address) == GetEffectiveBank(destination);
		}

		// handles the rest
		return SnesHelpers.JumpTargetMakesSenseEdgeCases(address, destination);
	}

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) {
		return SnesHelpers.OffsetToSa1(offset);
	}

	private protected override int GetAddressFromOffset(int offset) {
		int bank = offset >>> SnesHelpers.MmcBankShift;
		int bankOffset = offset & SnesHelpers.MmcBankingMask;

		if (_CxBank == bank) {
			if (_CxLorom) {
				return SnesHelpers.OffsetToLorom(bankOffset);
			} else {
				return SnesHelpers.OffsetToHirom(bankOffset);
			}
		}

		if (_DxBank == bank) {
			if (_DxLorom) {
				return SnesHelpers.OffsetToLorom(bankOffset) | 0x20_0000;
			} else {
				return SnesHelpers.OffsetToHirom(bankOffset) | 0x10_0000;
			}
		}

		if (_ExBank == bank) {
			if (_ExLorom) {
				return SnesHelpers.OffsetToLorom(bankOffset) | 0x80_0000;
			} else {
				return SnesHelpers.OffsetToHirom(bankOffset) | 0x20_0000;
			}
		}

		if (_FxBank == bank) {
			if (_FxLorom) {
				return SnesHelpers.OffsetToLorom(bankOffset) | 0xA0_0000;
			} else {
				return SnesHelpers.OffsetToHirom(bankOffset) | 0x30_0000;
			}
		}

		return OffsetToAddress(offset);
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
		return SnesHelpers.Sa1ToOffset(address);
	}

	private protected override int GetOffsetFromAddress(int address) {
		if (address < 0) {
			return -1;
		}

		//
		// Lorom area
		//

		if (address < 0x20_0000) {
			if (_CxLorom) {
				return AddressToLoromBankedOffset(address, _CxBank);

			} else {
				return SnesHelpers.Sa1ToOffset(address);
			}
		}

		if (address < 0x40_0000) {
			if (_DxLorom) {
				return AddressToLoromBankedOffset(address, _DxBank);

			} else {
				return SnesHelpers.Sa1ToOffset(address);
			}
		}

		if (address < 0xA0_0000) {
			if (_ExLorom) {
				return AddressToLoromBankedOffset(address, _ExBank);

			} else {
				return SnesHelpers.Sa1ToOffset(address);
			}
		}

		if (address < 0xC0_0000) {
			if (_FxLorom) {
				return AddressToLoromBankedOffset(address, _FxBank);

			} else {
				return SnesHelpers.Sa1ToOffset(address);
			}
		}

		//
		// Hirom area
		//

		if (address is >= 0xC0_0000) {
			int subBank = address & SnesHelpers.MmcBankingMask;

			if (address < 0xD0_0000) {
				return subBank | (_CxBank << SnesHelpers.MmcBankShift);

			} else if (address < 0xE0_0000) {
				return subBank | (_DxBank << SnesHelpers.MmcBankShift);

			} else if (address < 0xF0_0000) {
				return subBank | (_ExBank << SnesHelpers.MmcBankShift);

			} else {
				return subBank | (_FxBank << SnesHelpers.MmcBankShift);
			}
		}

		//
		// everything else
		//

		return -1;
	}

	private static int AddressToLoromBankedOffset(int address, byte bank) {
		address = ((address & 0x1F_0000) >> 1) | (address & 0x7FFF);

		return address | (bank << SnesHelpers.MmcBankShift);
	}

	private protected override void StepPC(int step) {
		if (step is 0) {
			return;
		}

		int offset = _offset;

		// if we don't end up in a new bank region, then we're good
		// we want this fast path that covers 99% of cases
		// using subtraction because _offset has already been stepped by AdvanceProgramCounter
		if (!SnesHelpers.OffsetChangedMmcBank(offset, offset - step)) {
			if (_pc < 0xC0_0000) {
				// this can't cross into C0
				// because that would have been flagged as an MMC bank change
				StepPCLorom(step);

			} else {
				// likewise, this can't cross into BF
				StepPCHirom(step);
			}

			return;
		}

		//
		// otherwise, we need to carefully step through each increment
		// separate method so we can get some inlining
		//

		DoBigPCStep(step);
	}

	// no inlining because this is an edge case handler
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void DoBigPCStep(int step) {
		Warning("An MMC banking region was crossed. Data contiguity may be compromised.");

		int pc = _pc;

		// we can try to treat hirom as just a big step
		if (pc >= 0xC0_0000) {
			int newpc = pc + step;

			// as long as we didn't step too far backwards when it's negative, we're fine
			// BF to C0 is contiguous, so this is a tiny shortcut.
			if (newpc >= 0xBF_8000) {
				_pc = newpc;
				RelocateOffset(GetOffsetFromAddress(newpc));
				return;
			}
		}

		// step 32KB at a time

		if (step > 0) {

			// positive steps

			while (step > 0) {
				int deduct = int.Min(0x8000, step);

				step -= deduct;
				pc += deduct;

				// test lorom areas
				if (pc < 0xC0_0000) {
					// test for entering BWRAM
					if (pc is < 0x80_0000 and >= 0x40_0000) {
						// jump to region E
						pc += 0x40_0000;
					}

					// just force move to upper half of bank
					// using OR because we might be doing an incomplete step
					// adding would send us to the next bank when that happens
					pc |= 0x8000;

				} else {
					// explicit do nothing
					// hirom can keep its PC
				}
			}

		} else {

			// negative steps

			step = -step; // negate to make the loop easier to understand

			while (step > 0) {
				int deduct = int.Min(0x8000, step);

				step -= deduct;
				pc -= deduct;

				// test lorom areas
				if (pc < 0xC0_0000) {
					// test for entering BWRAM
					if (pc is < 0x80_0000 and >= 0x40_0000) {
						// jump to region D
						pc -= 0x40_0000;
					}

					// adjust for lorom
					if ((pc & 0x8000) is 0) {
						pc -= 0x00_8000;
					}

				} else {
					// explicit do nothing
					// hirom can keep its PC
					// hirom may enter lorom here
					// but because we're doing 32KB at a time,
					// it is guaranteed to land in a valid ROM area
				}
			}
		}

		// once we're done, just force recalc the offset
		RelocateOffset(GetOffsetFromAddress(pc));
	}

	/// <inheritdoc/>
	public override bool AddressIsRom(int address) {
		// FF_.._.... removes numbers that are negative or too large
		// .._40_.... removes BWRAM and WRAM
		// .._.._8000 demands high bank half
		if (((uint) address & 0xFF_40_8000u) is 0x8000) {
			return true;
		}

		// final check to cover banks C0-FF
		return address is >= 0xC0_0000 and <= 0xFF_FFFF;
	}
}
