namespace Futaba;

internal sealed class FileWatcher : IDisposable {
	private readonly Dictionary<string, IWatchedFile?> _files = [];
	public string DirectoryPath { get; }
	private readonly FileSystemWatcher _watcher;
	public bool IsEmpty => _files.Count is 0;

	public bool Enabled {
		get => _watcher.EnableRaisingEvents;
		set => _watcher.EnableRaisingEvents = value;
	}

	public FileWatcher(string directory) {
		DirectoryPath = directory;

		_watcher = new(directory, "*.*") {
			IncludeSubdirectories = false,
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
		};
		
		_watcher.Changed += OnAnyChange;
		_watcher.Deleted += OnAnyChange;
		_watcher.Renamed += OnAnyChange;
	}

	private FileSystemEventHandler? _filesChanged;

	public event FileSystemEventHandler? SourceChange {
		add => _filesChanged += value;
		remove => _filesChanged -= value;
	}

	private void OnAnyChange(object sender, FileSystemEventArgs e) {
		if (_disposed) {
			return;
		}

		switch (e.ChangeType) {
			case WatcherChangeTypes.Renamed:
				if (e is RenamedEventArgs r) {
					if (_files.TryGetValue(r.OldFullPath, out var rfile)) {
						rfile?.Change = e.ChangeType;
						_filesChanged?.Invoke(sender, e);
					}
				}

				goto case WatcherChangeTypes.Created;

			case WatcherChangeTypes.Created:
			case WatcherChangeTypes.Deleted:
			case WatcherChangeTypes.Changed:
				if (_files.TryGetValue(e.FullPath, out var file)) {
					file?.Change = e.ChangeType;
					_filesChanged?.Invoke(sender, e);
				}

				break;
		}
	}

	private bool _disposed;

	public void WatchFile(IWatchedFile file) {
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

			_files.Clear();
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
