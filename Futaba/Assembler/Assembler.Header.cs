namespace Futaba;

unsafe partial class Assembler {
	private int? _copVectorNative = null;
	private int? _brkVector = null;
	private int? _abortVectorNative = null;
	private int? _nmiVectorNative = null;
	private int? _irqVectorNative = null;

	private int? _copVectorEmulation = null;
	private int? _abortVectorEmulation = null;
	private int? _nmiVectorEmulation = null;
	private int? _resetVector = null;
	private int? _irqVectorEmulation = null;

	private protected virtual void AdjustInterruptVectors() {
		byte* vectorTable = _romPtr + AddressToOffset(0x00FFE0);

		if (_copVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x04, _copVectorEmulation.Value);
		}

		if (_brkVector is not null) {
			SnesHelpers.WriteW(vectorTable + 0x06, _brkVector.Value);
		}

		if (_abortVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x08, _abortVectorEmulation.Value);
		}

		if (_nmiVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x0A, _nmiVectorEmulation.Value);
		}

		if (_irqVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x0E, _irqVectorEmulation.Value);
		}

		if (_copVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x14, _copVectorEmulation.Value);
		}

		if (_abortVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x18, _abortVectorEmulation.Value);
		}

		if (_nmiVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x1A, _nmiVectorEmulation.Value);
		}

		if (_resetVector is not null) {
			SnesHelpers.WriteW(vectorTable + 0x1C, _resetVector.Value);
		}

		if (_irqVectorEmulation is not null) {
			SnesHelpers.WriteW(vectorTable + 0x1E, _irqVectorEmulation.Value);
		}
	}

	private protected virtual void InitializeHeader() {
		byte* header = _romPtr + AddressToOffset(RomHeader.ExtendedHeaderLocation);

		if (ExtendedHeader) {
			header[0x2A] = 0x33;

			SnesHelpers.InsertHeaderString(MakerCode, header + 0x00, SnesHelpers.MakerCodeLength);
			SnesHelpers.InsertHeaderString(GameCode, header + 0x02, SnesHelpers.GameCodeLength);

			new Span<byte>(header + 6, 8).Clear();

			header[0x0E] = SpecialVersion;
			header[0x0F] = CartridgeSubtype;

		} else {
			header[0x2A] = 0x01;
		}

		SnesHelpers.InsertHeaderString(Title, header + 0x10, RomHeader.TitleLength);

		// ram size
		header[0x28] = SnesHelpers.GetHeaderSizeByte(RamSize);

		// get rom type
		if (HasCoprocessor) {
			header[0x26] = (byte) ((int) Coprocessor >> 16);

			header[0x26] |= (HasRam, HasBattery) switch {
				(false, false) => 0x03,
				(false, true) => 0x06,
				(true, false) => 0x04,
				(true, true) => 0x05,
			};

			if (ExtendedHeader) {
				header[0x0D] = header[0x28]; // duplicate ram size
			}

		} else {
			if (HasRam) {
				header[0x26] = (byte) (HasBattery ? 0x02 : 0x01);
			} else {
				header[0x26] = 0x00;
			}
		}

		header[0x29] = Destination.Value;

		header[0x2B] = RomVersion;
	}

	private protected virtual void FinalizeHeader() {
		byte* header = _romPtr + AddressToOffset(RomHeader.ExtendedHeaderLocation);

		byte mapcode = MapperCode;

		if (FastRom) {
			mapcode |= 0x10;
		}

		header[0x25] = mapcode;

		header[0x27] = SnesHelpers.GetHeaderSizeByte(CurrentRomSize);

		RecalculateChecksum();
	}

	/// <summary>
	/// Gets or sets the software title, automatically sanitized to match the Nintendo specifications
	/// as outlined in BOOK I, 1-2-12 (page 32):
	/// <list type="bullet">
	/// <item>21 characters in length</item>
	/// <item>Only printable ASCII (code points 0x20-0x7E)</item>
	/// <item>Padded with spaces</item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// The value of this property has no effect on the software's ability to function,
	/// but it may be checked by other programs (such as emulators) to facilitate
	/// identification or otherwise determine how to handle a given program.
	/// <para>
	/// <inheritdoc cref="OverflowAction" path="/remarks"/>
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="OverflowAction" path="/exception"/>
	public string Title {
		get;
		set {
			ThrowIfAssembling();
			field = RomHeader.SanitizeTitle(value);
		}
	} = "";

	/// <summary>
	/// Gets or sets whether the internal header is populated by the assembler.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public bool AutoPopulateHeader {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = false;

	/// <summary>
	/// Gets or sets whether the internal checksum is calculated and inserted into the header after assembly.
	/// </summary>
	/// <remarks>
	/// This property is ignored when <see cref="AutoPopulateHeader"/> is <see langword="true"/>.
	/// <para>
	/// <inheritdoc cref="OverflowAction" path="/remarks"/>
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="ThrowIfAssembling" path="/exception"/>
	public bool CalculateChecksum {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = false;

	/// <summary>
	/// Recalculates the checksum and complement for this program
	/// and inserts those values into the header at the appropriate location
	/// as outlined on page 1-2-21 of BOOK I.
	/// </summary>
	private protected virtual void RecalculateChecksum() {
		// placeholder checksum for calculation
		ushort* complementPointer = (ushort*) (_romPtr + AddressToOffset(RomHeader.ExtendedHeaderLocation + 0x2C));

		// yes, the complement appears before the checksum in the header
		complementPointer[1] = 0xBEBE;
		complementPointer[0] = 0x4141;

		ushort sum = RomHeader.CalculateChecksum(_romPtr, CurrentRomSize);

		complementPointer[1] = sum;
		complementPointer[0] = (ushort) ~sum;
	}

	/// <summary>
	/// Gets or sets whether this software utilizes FastROM.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public bool FastRom {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = false;

	/// <summary>
	/// Gets or sets the initial size of the ROM binary.
	/// </summary>
	/// <remarks>
	/// The helper function <see cref="RomHeader.TryGetRomSize(string, out int)"/> can be used to test string tokens for valid sizes.<br/>
	/// The instance function <see cref="IsValidRomSize(int)"/> can be used to test if a given value is legal for this mapper mode.
	/// <para>
	/// <inheritdoc cref="OverflowAction" path="//remarks"/>
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <inheritdoc cref="OverflowAction" path="/exception"/>
	public int InitialRomSize {
		get;
		set {
			ThrowIfAssembling();

			if (!IsValidRomSize(value)) {
				Throw(MinRomSize, MaxRomSize);
				[StackTraceHidden]
				static void Throw(int min, int max)
					=> throw new ArgumentOutOfRangeException(nameof(value), $"ROM size must be between {min} and {max}");
			}

			field = value;
			CurrentRomSize = value;
		}
	}

	/// <summary>
	/// Gets the current size of the ROM.
	/// </summary>
	/// <remarks>
	/// This may differ from <see cref="InitialRomSize"/> if the ROM grew during assembly.
	/// </remarks>
	public int CurrentRomSize { get; private set; }

	/// <summary>
	/// Gets or sets the size of the on-board RAM in bytes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <inheritdoc cref="RomHeader.IsValidRamSize(int)" path="//remarks"/>
	/// If not a power of two, the value of this property will be rounded up to the next power of two.
	/// </para>
	/// <para>
	/// <inheritdoc cref="OverflowAction" path="/remarks"/>
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="InitialRomSize" path="/exception"/>
	public int RamSize {
		get;
		set {
			ThrowIfAssembling();

			field = value switch {
				< 0 or > RomHeader.MaxRamSize => throw new ArgumentOutOfRangeException($"RAM size must be in the inclusive range [0, {RomHeader.MaxRamSize}]."),
				0 => 0,
				<= 2048 => 2048,
				<= 4096 => 4096,
				<= 8192 => 8192,
				<= 16384 => 16384,
				<= 32768 => 32768,
				<= 65536 => 65536,
				<= 131072 => 131072,
				<= 262144 => 262144,
				<= RomHeader.MaxRamSize => RomHeader.MaxRamSize,
			};
		}
	}

	/// <summary>
	/// Gets or sets whether this software includes an on-board battery to protect its RAM.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public bool HasBattery {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = true;

	/// <summary>
	/// Returns whether or not this software includes on-board RAM.
	/// </summary>
	public bool HasRam => RamSize > 0;

	/// <summary>
	/// Gets or sets the co-processor used by this software.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public virtual Coprocessor Coprocessor {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = Coprocessor.None;

	/// <summary>
	/// Returns whether or not this software includes a coprocessor.
	/// </summary>
	public bool HasCoprocessor => false;

	/// <summary>
	/// Gets or sets the target language/region of the software.
	/// </summary>
	/// <inheritdoc cref="Title" path="//remarks|//exception"/>
	public Region Destination {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = Region.NorthAmerica;

	/// <summary>
	/// Gets or sets the software version of the build.
	/// </summary>
	/// <inheritdoc cref="Title" path="//remarks|//exception"/>
	public byte RomVersion {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = 0x00;

	/// <summary>
	/// Gets or sets whether this software uses the extended internal ROM header.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public bool ExtendedHeader {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = true;

	/// <summary>
	/// Gets or sets the 2 character maker code.
	/// </summary>
	/// <inheritdoc cref="GameCode" path="//remarks|//exception"/>
	public string MakerCode {
		get;
		set {
			ThrowIfAssembling();
			field = SnesHelpers.SanitizeString(value, SnesHelpers.MakerCodeLength);
		}
	} = "  ";

	/// <summary>
	/// Gets or sets the 4 character game code.
	/// </summary>
	/// <remarks>
	/// <para id="exheader">
	/// This property is only applicable when <see cref="ExtendedHeader"/> is <see langword="true"/>.
	/// </para>
	/// Input will be sanitized to the printable ASCII characters.
	/// Longer and shorter strings will be adjusted to the correct length.
	/// <para>
	/// <para>
	/// Official guidelines dictate this field should only include uppercase letters,
	/// but this restriction is not imposed.
	/// </para>
	/// <inheritdoc cref="Title" path="/remarks"/>
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="OverflowAction" path="/exception"/>
	public string GameCode {
		get;
		set {
			ThrowIfAssembling();
			field = SnesHelpers.SanitizeString(value, SnesHelpers.GameCodeLength);
		}
	} = "    ";

	/// <summary>
	/// Gets or sets the special version.
	/// This was a rarely used value primarily intended for promotion events, etc.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="GameCode" path="/remarks/para[@id='exheader']"/>
	/// <inheritdoc cref="Title" path="/remarks"/>
	/// </remarks>
	/// <inheritdoc cref="Title" path="//exception"/>
	public byte SpecialVersion {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = 0x00;

	/// <summary>
	/// Gets or sets the cartidge type subversion.
	/// This was a rarely used value intended to distinguish software with the same cartridge type.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="GameCode" path="/remarks/para[@id='exheader']"/>
	/// <inheritdoc cref="Title" path="/remarks"/>
	/// </remarks>
	/// <inheritdoc cref="Title" path="//exception"/>
	public byte CartridgeSubtype {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = 0x00;
}
