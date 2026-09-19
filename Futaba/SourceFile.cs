namespace Futaba;

/// <summary>
/// Encapsulates an assembly source file.
/// </summary>
internal unsafe sealed class SourceFile : SourceObject, IWatchedFile {
	public string FileName { get; }

	public bool WasUsed { get; set; } = false;

	public DateTime LastLoaded { get; private set; } = default;
	public string? Error { get; private set; } = null;

	internal string ShortName { get; init; }
	public override FileInfo FileInfo { get; }

	public override string ObjectName => ShortName;

	internal override bool IsMacro => false;

	public int Length { get; private set; } = 0;

	/// <summary>
	/// Creates a new source file container.
	/// </summary>
	internal SourceFile(string path) {
		FileInfo = new(path);
		ShortName = FileInfo.Name;
		FileName = FileInfo.FullName;

		Refresh();
	}


	public CharSpan AsSpan() {
		return new CharSpan(SourceStart, Length);
	}

	public void Refresh() {
		FileInfo check = new(FileName);

		if (check.Exists) {
			if (LastLoaded != check.LastWriteTimeUtc) {
				LastLoaded = check.LastWriteTimeUtc;

				try {
					//Console.WriteLine($"Loading {FullName}");

					using var ftext = FileInfo.OpenText();

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
		} else {
			Error = $"File {FileName} does not exist.";
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

	internal static readonly SourceObject Empty = new EmptySource();
}


file sealed class EmptySource : SourceObject {
	public override FileInfo? FileInfo => null;
	internal override bool IsMacro => false;
	public override string ObjectName => string.Empty;


	internal EmptySource() {
		disposed = true;
	}

	public override string ToString() {
		return "NULL";
	}

	public override void Dispose() {
		// do nothing, since this has nothing allocated to it
	}

}
