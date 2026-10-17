using Microsoft.Win32.SafeHandles;

namespace Futaba;

/// <summary>
/// Encapsulates an assembly source file.
/// </summary>
internal unsafe sealed class SourceFile : SourceObject, IWatchedFile, IWatchedFileFactory<SourceFile> {
	public SafeFileHandle? Handle { get; private set; }
	public override FileInfo FileInfo { get; }

	public string FileName { get; }
	internal string ShortName { get; init; }
	public override string ObjectName => ShortName;
	public string? DirectoryName { get; }

	public DateTime LastLoaded { get; private set; } = default;

	public string? Error { get; private set; } = null;

	public int Length { get; private set; } = 0;
	public WatcherChangeTypes Change { get; set; }

	public bool WasUsed { get; set; } = false;
	internal override bool IsMacro => false;

	/// <summary>
	/// Creates a new source file container.
	/// </summary>
	private SourceFile(string path) {
		Handle = IWatchedFile.OpenHandle(path);

		FileInfo = new(path);
		ShortName = FileInfo.Name;
		FileName = FileInfo.FullName;
		DirectoryName = FileInfo.DirectoryName;

		Refresh();
	}

	public CharSpan AsSpan() {
		return new CharSpan(SourceStart, Length);
	}

	public static SourceFile Create(string path) {
		return new SourceFile(path);
	}

	public void Refresh() {
		if (!IWatchedFile.RevalidateHandle(this, out var newHandle, out var error)) {
			Error = error;

		} else {
			bool reload;

			if (!ReferenceEquals(newHandle, Handle)) {
				reload = true;
				Handle?.Dispose();
				Handle = newHandle;

			} else {
				reload = LastLoaded != File.GetLastWriteTimeUtc(newHandle);
			}

			Change = default;

			if (reload) {
				LastLoaded = File.GetLastWriteTimeUtc(newHandle);

				try {
					//Console.WriteLine($"Loading {FullName}");

					var fstream = new FileStream(newHandle, FileAccess.Read);
					var ftext = new StreamReader(fstream);

					int length = (int) FileInfo.Length;

					int bufferLen = length + (BufferPadding * 2);

					if (AllocBlock == default) {
						AllocBlock = NativeMemory.AllocNice<char>(bufferLen);

					} else {
						AllocBlock = NativeMemory.ReAllocNice(AllocBlock, bufferLen);
					}

					SourceStart = AllocBlock + BufferPadding;

					SourceEnd = SourceStart + ftext.Read(new Span<char>(SourceStart, length));

					ApplyPadding();
					Length = (int) (SourceEnd - SourceStart);
					Error = null;

					return;

				} catch (Exception e) {
					Error = e.Message;
				}
			} else {
				Error = null;
				//Console.WriteLine($"{FullName} is unchanged.");
				return;
			}
		}

		NativeMemory.AlignedFree(AllocBlock);

		AllocBlock = default;
		SourceStart = default;
		SourceEnd = default;

		Length = 0;
	}

	public override string ToString() {
		return $"File: {ShortName}";
	}

	protected override void DisposeResources() {
		Handle?.Dispose();
	}

	internal static readonly SourceObject Empty = new EmptySource();
}

file sealed class EmptySource : SourceObject {
	public override FileInfo? FileInfo => null;
	internal override bool IsMacro => false;
	public override string ObjectName => string.Empty;

	internal EmptySource() {
		_disposed = true;
	}

	public override string ToString() {
		return "NULL";
	}

	public override void Dispose() {
		// do nothing, since this has nothing allocated to it
	}

	protected override void DisposeResources() {
		// do nothing
	}
}
