using Microsoft.Win32.SafeHandles;

namespace Futaba;

internal interface IWatchedFile : IDisposable {
	/// <summary>
	/// The full path of this file.
	/// </summary>
	public string FileName { get; }

	public string? DirectoryName { get; }

	public bool WasUsed { get; set; }

	public SafeFileHandle? Handle { get; }

	public DateTime LastLoaded { get; }

	public string? Error { get; }

	public WatcherChangeTypes Change { get; set; }

	public abstract void Refresh();

	internal static SafeFileHandle OpenHandle(string path) {
		return File.OpenHandle(path,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable,
			FileOptions.SequentialScan | FileOptions.Asynchronous
		);
	}

	internal static bool RevalidateHandle(IWatchedFile file, [NotNullWhen(true)] out SafeFileHandle? handle, [NotNullWhen(false)] out string? error) {
		var change = file.Change;

		if (change.HasFlag(WatcherChangeTypes.Renamed) ||
			change.HasFlag(WatcherChangeTypes.Deleted) ||
			change.HasFlag(WatcherChangeTypes.Created)) {
			string path = file.FileName;

			if (File.Exists(path)) {
				handle = OpenHandle(path);
				error = null;
				return true;

			} else {
				error = $"File {Path.GetFileName(path)} does not exist.";
				handle = null;
				return false;
			}

		} else if (file.Handle is not null) {
			handle = file.Handle;
			error = null;
			return true;
		}

		handle = null;
		error = "Invalid file handle.";
		return false;
	}

}
