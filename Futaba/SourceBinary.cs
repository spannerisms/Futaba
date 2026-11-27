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
	private bool _allocated = false;

	private SourceBinary(string path, SafeFileHandle? handle) {
		Handle = handle;
		FileName = Path.GetFullPath(path);
		DirectoryName = Path.GetDirectoryName(FileName);
	}

	public static SourceBinary TryCreate(string path) {
		SafeFileHandle? handle = IWatchedFile.TryOpenHandle(path, out string? error);

		SourceBinary ret = new(path, handle) {
			Error = error
		};

		if (error is null && handle is not null) {
			ret.Refresh();
		}

		return ret;
	}

	public void Refresh() {
		if (!IWatchedFile.RevalidateHandle(this, out var newHandle, out var error)) {
			Error = error;
			newHandle?.Dispose();
			Handle?.Dispose();
			Handle = null;

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
					var fstream = new FileStream(newHandle, FileAccess.Read);
					int length = (int) fstream.Length;

					fstream.Position = 0;

					if (_allocBlock == default) {
						_allocBlock = NativeMemory.AllocNice<byte>(length);
						Length = length;

					} else if (Length != length) {
						_allocBlock = NativeMemory.ReAllocNice(_allocBlock, length);
						Length = length;
					}

					_allocated = true;

					fstream.ReadExactly(new Span<byte>(_allocBlock, length));

					return;

				} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
					Error = e.Message;
				}
			} else {
				Error = null;
				return;
			}
		}

		NativeMemory.AlignedFree(_allocBlock);

		_allocBlock = default;
		_allocated = false;

		Length = 0;
	}

	public bool TryGetSpan(int start, int length, out Span<byte> span) {
		if (_disposed || !_allocated) {
			span = default;
			return false;
		}

		// edge case fix
		if (start is 0 && length is 0 && Length is 0) {
			span = new(_allocBlock, 0);
			return true;
		}

		if ((uint) length > Length || (uint) start >= Length || ((long) start + length) > Length) {
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
		_allocated = false;

		if (disposing) {
			Handle?.Dispose();
		}

		Handle = null;

		NativeMemory.AlignedFree(_allocBlock);
	}
}
