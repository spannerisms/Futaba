using Microsoft.Win32.SafeHandles;

namespace Futaba;

internal unsafe sealed class SourceBinary : IWatchedFile, IWatchedFileFactory<SourceBinary> {
	private byte* _allocBlock;
	public string? Error { get; private set; } = null;
	public string FileName { get; }
	public string? DirectoryName { get; }
	public SafeFileHandle? Handle { get; private set; }
	public DateTime LastLoaded { get; private set; } = default;
	public int Length { get; private set; }
	public WatcherChangeTypes Change { get; set; }
	public bool WasUsed { get; set; } = false;
	private bool _disposed = false;

	private SourceBinary(string path) {
		FileName = Path.GetFullPath(path);
		DirectoryName = Path.GetDirectoryName(FileName);
		Handle = IWatchedFile.OpenHandle(FileName);
		Refresh();
	}

	public static SourceBinary Create(string path) {
		return new SourceBinary(path);
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

					int length = (int) fstream.Length;

					if (_allocBlock == default) {
						_allocBlock = NativeMemory.AllocNice<byte>(length);
						Length = length;

					} else if (Length != length) {
						_allocBlock = NativeMemory.ReAllocNice(_allocBlock, length);
						Length = length;
					}

					fstream.ReadExactly(new Span<byte>(_allocBlock, length));

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

		NativeMemory.AlignedFree(_allocBlock);

		_allocBlock = default;

		Length = 0;
	}

	public bool TryGetSpan(int start, int length, out Span<byte> span) {
		if (_disposed || (uint) length > Length || (uint) start >= Length || (start + length) > Length || _allocBlock == default) {
			span = default;
			return false;
		}

		span = new(_allocBlock + start, length);

		return true;
	}

	~SourceBinary() {
		Dispose(false);
	}

	/// <inheritdoc cref="IDisposable.Dispose()"/>
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc cref="IDisposable.Dispose()"/>
	public void Dispose(bool disposing) {
		if (_disposed) return;

		_disposed = true;

		if (disposing) {
			Handle?.Dispose();
		}

		Handle = null;

		NativeMemory.AlignedFree(_allocBlock);
	}
}
