using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FutabaCLI;

internal sealed class BinaryWatcher : IDisposable {
	private readonly Dictionary<string, WatchedBinary?> _files = [];
	public string DirectoryPath { get; }
	private readonly FileSystemWatcher _watcher;
	private bool _disposed;
	public bool IsEmpty => _files.Count is 0;

	public bool Enabled {
		get => _watcher.EnableRaisingEvents;
		set => _watcher.EnableRaisingEvents = value;
	}

	public BinaryWatcher(string directory) {
		DirectoryPath = directory;

		_watcher = new(directory, "*.*") {
			IncludeSubdirectories = false,
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
		};

		_watcher.Changed += OnAnyChange;
		_watcher.Deleted += OnAnyChange;
		_watcher.Created += OnAnyChange;
		_watcher.Renamed += OnRename;
	}

	private FileSystemEventHandler? _filesChanged;

	public event FileSystemEventHandler? SourceChange {
		add => _filesChanged += value;
		remove => _filesChanged -= value;
	}

	private void Strobe(string path, FileSystemEventArgs e) {
		if (_files.TryGetValue(path, out var file)) {
			file?.Change = e.ChangeType;
			_filesChanged?.Invoke(this, e);
		}
	}

	private void OnRename(object sender, RenamedEventArgs e) {
		if (_disposed) {
			return;
		}

		Strobe(e.OldFullPath, e);
	}

	private void OnAnyChange(object sender, FileSystemEventArgs e) {
		if (_disposed) {
			return;
		}

		Strobe(e.FullPath, e);
	}

	public void WatchFile(WatchedBinary file) {
		if (_disposed) {
			return;
		}

		ref var add = ref CollectionsMarshal.GetValueRefOrAddDefault(_files, file.FileName, out _);
		add ??= file;
	}

	public void WatchFile(string name) {
		if (_disposed) {
			return;
		}

		ref var add = ref CollectionsMarshal.GetValueRefOrAddDefault(_files, name, out _);
	}

	private void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing) {
				_watcher.EnableRaisingEvents = false;
				_watcher.Dispose();
			}

			_files?.Clear();
			_filesChanged = null;

			_disposed = true;
		}
	}

	public void Clear() {
		_files.Clear();
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}

internal sealed class WatchedBinary : IDisposable {
	public byte[]? Data { get; set; }
	public string? Error { get; private set; } = null;
	public string FileName { get; }
	public string? DirectoryName { get; }
	public SafeFileHandle? Handle { get; private set; }
	public DateTime LastLoaded { get; private set; } = default;
	public WatcherChangeTypes Change { get; set; }
	private bool _disposed = false;

	private WatchedBinary(string path, SafeFileHandle? handle) {
		Handle = handle;
		FileName = Path.GetFullPath(path);
		DirectoryName = Path.GetDirectoryName(FileName);
	}

	private static SafeFileHandle? TryOpenHandle(string path, out string? error) {
		try {
			SafeFileHandle ret = File.OpenHandle(path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable,
				FileOptions.SequentialScan | FileOptions.Asynchronous
			);
			error = null;
			return ret;

		} catch (Exception e) {
			error = e.Message;
			return null;
		}
	}

	private enum HandleRevalidation {
		Unchanged,
		Good,
		Bad,
	}

	private HandleRevalidation RevalidateHandle() {
		var change = Change;

		if (change.HasFlag(WatcherChangeTypes.Renamed) ||
			change.HasFlag(WatcherChangeTypes.Deleted) ||
			change.HasFlag(WatcherChangeTypes.Created)) {
			string path = FileName;

			Handle?.Dispose();

			if (File.Exists(path)) {
				Handle = TryOpenHandle(path, out string? error);
				Error = error;
				return Handle is not null ? HandleRevalidation.Good : HandleRevalidation.Bad;

			} else {
				Error = $"File {Path.GetFileName(path)} does not exist.";
				Handle = null;
				return HandleRevalidation.Bad;
			}

		} else if (Handle is not null) {
			Error = null;
			return HandleRevalidation.Unchanged;
		}

		Handle?.Dispose();
		Handle = null;
		Error = "Invalid file handle.";
		return HandleRevalidation.Bad;
	}

	public static WatchedBinary TryCreate(string path) {
		SafeFileHandle? handle = TryOpenHandle(path, out string? error);

		WatchedBinary ret = new(path, handle) {
			Error = error
		};

		if (error is null && handle is not null) {
			ret.Refresh();
		}

		return ret;
	}

	public bool Refresh() {
		HandleRevalidation result = RevalidateHandle();
		if (result is HandleRevalidation.Bad) {
			Data = null;
			return false;

		} else {
			if (Handle is null) {
				return false;
			}

			bool reload = LastLoaded != File.GetLastWriteTimeUtc(Handle);
			Change = default;

			if (reload) {
				LastLoaded = File.GetLastWriteTimeUtc(Handle);

				try {
					var fstream = new FileStream(Handle, FileAccess.Read);

					int length = (int) fstream.Length;

					if (Data is null || Data.Length != length) {
						Data = GC.AllocateUninitializedArray<byte>(length);
					}

					fstream.ReadExactly(Data);
					Error = null;
					return true;

				} catch (Exception e) {
					Error = e.Message;
				}
			} else {
				Error = null;
				return false;
			}
		}

		Data = null;
		return false;
	}

	~WatchedBinary() {
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
		Data = null;
	}
}
