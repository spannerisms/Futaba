namespace Futaba;

/// <summary>
/// Base class that mapped assemblers inherit from.
/// </summary>
public unsafe abstract partial class Assembler : IDisposable {

	/// <inheritdoc cref="FutabaApp.Version"/>
	public static Version Version => FutabaApp.Version;

	#region Private fields
	private SnesArchitecture _architecture = SnesArchitecture.WDC65816;

	private SnesEncoder _currentEncoder = SnesEncoder.ASCII;
	private SpannedLookup<SnesEncoder> _encoders = new(32);

	private Dictionary<string, SourceFile> _loadedFiles = new(32);
	private Dictionary<string, SourceBinary> _loadedBinaries = new(64);
	private List<SourceObject> _loadedSourceObjects = new(128);

	private int _nextAllocBlockStart = -1;

	private BreakpointsControl _bpControl = BreakpointsControl.Off;
	private BreakpointInserts _defaultBpInsert = BreakpointInserts.Nothing;
	private BreakpointTriggers _defaultBpTriggers = BreakpointTriggers.RWX;

	private FileSystemEventHandler? _filesChanged;
	private bool _processedEvent = true; // Prevents more than one event from firing.
	private bool _disposed = false;
	private Dictionary<string, FileWatcher> _watchedFiles = [];

	private byte[]? _baseRom = null;

	private UInt128 _randomSeed = new((ulong) DateTime.Now.Ticks, (ulong) DateTime.UtcNow.Ticks);
	internal readonly XoRandom _random = new(0);

	// need to use private fields for these
	// so they can be removed during disposal without throwing an exception
	private TextWriter _messageOut = Console.Out;
	private TextWriter _errorOut = Console.Error;

	#endregion

	/// <summary>
	/// Gets the file that acts as the entry point of assembly.
	/// </summary>
	public FileInfo EntryPoint { get; }

	/// <summary>
	/// Gets or sets the base binary data used to populate the initial buffer.
	/// If <see langword="null"/>, assembly begins with a zero-filled buffer.
	/// <para>
	/// If the size of this array differs from <see cref="InitialRomSize"/>
	/// then either excess data will be ignored, or the unused space will be
	/// initialized to zero.
	/// </para>
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public byte[]? BaseRom {
		get => _baseRom;
		set {
			ThrowIfAssembling();

			if (value is null) {
				_baseRom = null;

			} else {
				// avoid reallocation when the sizes are identical
				if (_baseRom is not null && _baseRom.Length == value.Length) {
					value.CopyTo(_baseRom, 0);

				} else {
					_baseRom = [.. value];
				}
			}
		}
	}

	/// <summary>
	/// Seeds the internal RNG to allow deterministic output.
	/// </summary>
	/// <inheritdoc cref="ThrowIfAssembling" path="//remarks|//exception"/>
	public void SeedRNG(long seed1, long seed2) {
		ThrowIfAssembling();
		_randomSeed = new((ulong) seed1, (ulong) seed2);
	}

	/// <summary>
	/// Gets or sets what action to take when assembly occurs beyond the ROM's current size.
	/// </summary>
	/// <remarks>
	/// This property cannot be modified while assembly is occurring.
	/// Attempting to do so will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <inheritdoc cref="ThrowIfAssembling" path="/exception"/>
	public RomOverflowAction OverflowAction {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = RomOverflowAction.Error;

	/// <summary>
	/// Get or set the warning level for missing tokens.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	public MissingTokenSeverity MissingTokenSeverity {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = MissingTokenSeverity.Ambiguous;

	/// <summary>
	/// Returns whether or not any breaking errors occured during assembly.
	/// </summary>
	/// <remarks>
	/// If this returns <see langword="true"/>,
	/// then assembly is considered to have failed,
	/// and whatever output was produced is not reliable.
	/// </remarks>
	public bool HasErrors => ErrorCount > 0;

	/// <summary>
	/// Returns the number of errors encountered during assembly.
	/// </summary>
	public int ErrorCount { get; private set; } = 0;

	/// <summary>
	/// Returns the number of warnings encountered during assembly.
	/// </summary>
	public int WarningCount { get; private set; } = 0;

	/// <summary>
	/// Gets or sets whether to treat warnings as errors.
	/// </summary>
	public bool WarningsAreErrors {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	} = false;

	/// <summary>
	/// Gets or sets the maximum number of errors that can occur during assembly.
	/// If the number of errors exceeds this value, an <see cref="InvalidOperationException"/> will be thrown.
	/// </summary>
	/// <inheritdoc cref="OverflowAction" path="//remarks|//exception"/>
	/// <exception cref="ArgumentOutOfRangeException">If the passed value is less than 0.</exception>
	public int MaximumErrors {
		get;
		set {
			ThrowIfAssembling();
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
			field = value;
		}
	} = int.MaxValue;

	/// <summary>
	/// <para>
	/// If an exception is thrown during assembly,
	/// this will return information about the source line
	/// that was being handled at the time it occurred.
	/// </para>
	/// <para>
	/// If no exception has occurred, this will return <see langword="null"/>.
	/// </para>
	/// </summary>
	/// <remarks>
	/// Note that the source line returned is not necessarily the cause of the exception.
	/// </remarks>
	public string? ErrorLine { get; private set; } = null;

	/// <summary>
	/// Gets or sets where print messages are printed.
	/// Defaults to <see cref="Console.Out"/>.
	/// </summary>
	/// <remarks>
	/// The assembler instance does not handle the disposal of this field.
	/// <para>
	/// <inheritdoc cref="OverflowAction" path="//remarks"/>
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="OverflowAction" path="//exception"/>
	public TextWriter MessageOut {
		get => _messageOut;
		set {
			ThrowIfAssembling();
			_messageOut = value;
		}
	}

	/// <summary>
	/// Gets or sets where error and warning messages are printed.
	/// Defaults to <see cref="Console.Error"/>.
	/// </summary>
	/// <inheritdoc cref="MessageOut" path="//remarks|//exception"/>
	public TextWriter ErrorOut {
		get => _errorOut;
		set {
			ThrowIfAssembling();
			_errorOut = value;
		}
	}

	/// <summary>
	/// Gets or sets whether the assembler operates in incremental mode,
	/// allowing it to detect and raise events about file changes.
	/// </summary>
	public bool Incremental {
		get;
		set {
			ThrowIfAssembling();
			field = value;
		}
	}

	/// <summary>
	/// Creates a new assembler object using the given file as its entry point.
	/// </summary>
	private protected Assembler(FileInfo entry) {
		EntryPoint = entry;

		_romPtr = default;
		_timePtr = default;
		_pcPtr = default;

		_offset = 0;

		InitialRomSize = MinRomSize;
	}

	/// <summary>
	/// Starts assembly beginning at this instance's designated entry point.
	/// </summary>
	/// <remarks>
	/// Assembly errors do not throw an exception or halt assembly.
	/// Verify assembly succeeded with <see cref="HasErrors"/>,
	/// <para>
	/// <inheritdoc cref="ThrowIfAssembling()" path="/remarks"/>
	/// </para>
	/// </remarks>
	/// <returns>A new <see langword="byte"/> array containing the assembled machine code.</returns>
	/// <inheritdoc cref="ThrowIfAssembling()" path="/exception"/>
	public byte[] Assemble() {
		ThrowIfAssembling();

		try {
			TryAssembly();

			return [.. new ReadOnlySpan<byte>(_romPtr, CurrentRomSize)];

		} catch {
			throw;

		} finally {
			DeallocRomBuffer();
		}
	}

	/// <summary>
	/// Starts assembly beginning at the designated entry point.
	/// Data is written directly to the file specified.
	/// </summary>
	/// <inheritdoc cref="Assemble()" path="//remarks|//exception"/>
	public void AssembleFile(string outputFilePath) {
		ThrowIfAssembling();

		try {
			TryAssembly();

			using var stream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

			stream.Position = 0;
			stream.SetLength(0);

			stream.Write(new ReadOnlySpan<byte>(_romPtr, CurrentRomSize));
			stream.Flush(true);

		} catch {
			throw;

		} finally {
			DeallocRomBuffer();
		}
	}

	/// <inheritdoc cref="AssembleFile(string)"/>
	public void AssembleFile(FileInfo outputFile) {
		AssembleFile(outputFile.FullName);
	}

	/// <remarks>
	/// This action cannot be performed while assembly is occurring.
	/// Attempting to do so will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <exception cref="InvalidOperationException">If the assembler is busy with assembly.</exception>
	private void ThrowIfAssembling() {
		if (AmBusy()) {
			ThrowBusy();

			[DoesNotReturn]
			static void ThrowBusy() {
				throw new InvalidOperationException("This action cannot be performed while assembly is occurring.");
			}
		}
	}

	/// <summary>
	/// Just a wrapper so that it can handle disposal exceptions too.
	/// </summary>
	private bool AmBusy() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return Busy;
	}

	/// <summary>
	/// Finalizer.
	/// </summary>
	~Assembler() {
		Dispose(false);
	}

	/// <inheritdoc/>
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	protected virtual void Dispose(bool disposing) {
		Busy = true;

		if (_disposed) {
			return;
		}

		_disposed = true;

		_filesChanged = null;

		if (disposing) {
			// managed resources
			if (_watchedFiles is not null) {
				foreach (var f in _watchedFiles.Values) {
					f?.Dispose();
				}
			}

			if (_loadedFiles is not null) {
				foreach (var f in _loadedFiles.Values) {
					f?.Dispose();
				}
			}

			if (_loadedBinaries is not null) {
				foreach (var f in _loadedBinaries.Values) {
					f?.Dispose();
				}
			}
		}

		// unmanaged resources
		DeallocRomBuffer();

		_currentSourceObject = default!;

		_loadedFiles?.Clear();
		_loadedFiles = null!;

		_loadedBinaries?.Clear();
		_loadedBinaries = null!;

		_watchedFiles?.Clear();
		_watchedFiles = null!;

		_symbols.Clear();
		_symbols = default;

		_initialVariables.Clear();
		_initialVariables = default!;

		_variables.Clear();
		_variables = default;

		_loadedSourceObjects.Clear();
		_loadedSourceObjects = default!;

		_macros.Clear();
		_macros = default!;

		_functions.Clear();
		_functions = default;

		_encoders.Clear();
		_encoders = default;
		_currentEncoder = default!;

		_previousState = default;
		_ifChain = default;

		_simpleRequests.Clear();
		_simpleRequests = default!;

		_fillRequests.Clear();
		_fillRequests = default!;

		_jumpRequests.Clear();
		_jumpRequests = default!;

		_branchRequests.Clear();
		_branchRequests = default!;

		_segments.Clear();
		_segments = default!;

		_messageOut = default!;
		_errorOut = default!;
		_baseRom = null;
	}
}
