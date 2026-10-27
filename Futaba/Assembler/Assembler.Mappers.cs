namespace Futaba;

partial class Assembler {
	/// <inheritdoc cref="CreateAssemblerWithMapper(MapperMode,FileInfo)" path="//summary|//returns"/>
	/// <param name="mode"><inheritdoc cref="CreateAssemblerWithMapper(MapperMode,FileInfo)" path="/param[@name='mode']"/></param>
	/// <param name="path">The path of the file to be used as an entry point</param>
	public static Assembler CreateAssemblerWithMapper(MapperMode mode, string path) {
		return CreateAssemblerWithMapper(mode, new FileInfo(path));
	}

	/// <summary>
	/// Creates an assembler for the given mode and using the specified entry point.
	/// </summary>
	/// <returns>A new <see cref="Assembler"/> object for the given mode and entry point.</returns>
	/// <param name="mode">The enum for the desired mapper.</param>
	/// <param name="entry">The <see cref="FileInfo"/> object to be used as the entry point of assembly.</param>
	public static Assembler CreateAssemblerWithMapper(MapperMode mode, FileInfo entry) {
		return mode switch {
			MapperMode.None /*               */ => new UnmappedAssembler(entry),
			MapperMode.Lorom /*              */ => new LoromAssembler(entry),
			MapperMode.ExLorom /*            */ => new ExLoromAssembler(entry),
			MapperMode.Hirom /*              */ => new HiromAssembler(entry),
			MapperMode.ExHirom /*            */ => new ExHiromAssembler(entry),
			MapperMode.Sa1 /*                */ => new Sa1Assembler(entry),
			_ => /*                          */ throw new ArgumentException("Invalid mapper mode.", nameof(mode)),
		};
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

		_pc = pc & SnesHelpers.AddressMask;
	}

	private protected void StepPCHirom(int step) {
		int pc = _pc; // force into register
		pc += step;
		_pc = pc & SnesHelpers.AddressMask;
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
	public UnmappedAssembler(FileInfo entry) : base(entry) { }

	/// <inheritdoc cref="UnmappedAssembler(FileInfo)"/>
	public UnmappedAssembler(string entry) : this(new FileInfo(entry)) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.None;
	private protected override byte MapperCode => 0x00;

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) {
		return offset;
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
		return address;
	}

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
		return ((address ^ destination) & SnesHelpers.BankMask) is not 0;
	}
}

/// <summary>
/// Represents an assembler that uses the lorom mapping mode.
/// </summary>
public sealed class LoromAssembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the lorom mapping mode using the given file as its entry point.
	/// </summary>
	public LoromAssembler(FileInfo entry) : base(entry) { }

	/// <inheritdoc cref="LoromAssembler(FileInfo)"/>
	public LoromAssembler(string entry) : this(new FileInfo(entry)) { }

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
	public override int OffsetToAddress(int offset) {
		return SnesHelpers.OffsetToLorom(offset);
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
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
	public HiromAssembler(FileInfo entry) : base(entry) { }

	/// <inheritdoc cref="HiromAssembler(FileInfo)"/>
	public HiromAssembler(string entry) : this(new FileInfo(entry)) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.Hirom;
	private protected override byte MapperCode => 0x21;

	/// <inheritdoc/>
	public override int MinRomSize => SnesHelpers.BankSize;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxRegularSize;

	private protected override void StepPC(int step) {
		StepPCHirom(step);
	}

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) {
		return offset | 0xC0_0000;
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
		return address & 0x3F_FFFF;
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
	public ExLoromAssembler(FileInfo entry) : base(entry) { }

	/// <inheritdoc cref="ExLoromAssembler(FileInfo)"/>
	public ExLoromAssembler(string entry) : this(new FileInfo(entry)) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.ExLorom;
	private protected override byte MapperCode => 0x25;

	/// <inheritdoc/>
	public override int MinRomSize => 0x40_0000;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxExSize;

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) {
		// TODO - this is probably wrong
		return SnesHelpers.OffsetToLorom(offset) ^ 0x80_0000;
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
		return SnesHelpers.LoromToOffset(address) ^ 0x40_0000;
	}

	private protected override void StepPC(int step) {
		StepPCLorom(step);
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
	public ExHiromAssembler(FileInfo entry) : base(entry) { }

	/// <inheritdoc cref="ExHiromAssembler(FileInfo)"/>
	public ExHiromAssembler(string entry) : this(new FileInfo(entry)) { }

	/// <inheritdoc/>
	public override MapperMode Mapper => MapperMode.ExHirom;
	private protected override byte MapperCode => 0x25;

	/// <inheritdoc/>
	public override int MinRomSize => 0x40_0000;

	/// <inheritdoc/>
	public override int MaxRomSize => SnesHelpers.MaxExSize;

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) {
		// TODO
		return offset ^ 0x80_0000;
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
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
// TODO fix this up
public sealed class Sa1Assembler : Assembler {
	/// <summary>
	/// Creates a new assembler instance with the SA-1 using the given file as its entry point.
	/// </summary>
	public Sa1Assembler(FileInfo entry) : base(entry) { }

	/// <inheritdoc cref="Sa1Assembler(FileInfo)"/>
	public Sa1Assembler(string entry) : this(new FileInfo(entry)) { }

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
		throw new NotImplementedException();
	}

	private protected override bool JumpTargetMakesSense(int address, int destination) {
		return SnesHelpers.JumpTargetMakesSenseMmc(address, destination);
	}

	/// <inheritdoc/>
	public override int AddressToOffset(int address) {
		return SnesHelpers.LoromToOffset(address);
	}

	/// <inheritdoc/>
	public override int OffsetToAddress(int offset) {
		return SnesHelpers.OffsetToLorom(offset);
	}

	private protected override void StepPC(int step) {
		StepPCLorom(step);
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
