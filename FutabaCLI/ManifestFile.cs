using Microsoft.Win32.SafeHandles;

namespace FutabaCLI;

internal enum HandleRevalidation {
	Unchanged,
	Good,
	Bad,
}

internal abstract class ManifestFile : IDisposable {
	public string? Error { get; protected set; } = null;
	public string FileName { get; }
	public string? DirectoryName { get; }
	public SafeFileHandle? Handle { get; protected set; }
	public DateTime LastLoaded { get; protected set; } = default;
	public WatcherChangeTypes Change { get; set; }
	protected bool _disposed = false;

	protected ManifestFile(string path) {
		Handle = TryOpenHandle(path, out _);
		FileName = Path.GetFullPath(path);
		DirectoryName = Path.GetDirectoryName(FileName);
	}

	protected ManifestFile(string path, SafeFileHandle? handle) {
		Handle = handle;
		FileName = Path.GetFullPath(path);
		DirectoryName = Path.GetDirectoryName(FileName);
	}

	protected static SafeFileHandle? TryOpenHandle(string path, out string? error) {
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

	protected bool RevalidateHandle([NotNullWhen(true)] out SafeFileHandle? handle) {
		var change = Change;

		if (change.HasFlag(WatcherChangeTypes.Renamed) ||
			change.HasFlag(WatcherChangeTypes.Deleted) ||
			change.HasFlag(WatcherChangeTypes.Created)) {

			handle = TryOpenHandle(FileName, out var error);
			Error = error;
			return handle is not null;

		} else if (Handle is not null) {
			handle = Handle;
			Error = null;
			return true;
		}

		handle = null;
		Error = "Invalid file handle.";
		return false;
	}

	public abstract HandleRevalidation Refresh();

	~ManifestFile() {
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

		Subdispose(disposing);
		Handle = null;
	}

	protected abstract void Subdispose(bool dispoing);
}
