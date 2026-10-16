namespace Futaba;

partial class Assembler {

	/// <summary>
	/// Occurs when a change is discovered in any source file.
	/// </summary>
	public event FileSystemEventHandler? SourceChange {
		add => _filesChanged += value;
		remove => _filesChanged -= value;
	}

	[return: NotNullIfNotNull(nameof(directoryName))]
	private FileWatcher? GetWatcher(string? directoryName) {
		if (directoryName is null) {
			return null;
		}

		ref var watcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_watchedFiles, directoryName, out _);

		if (watcher is null) {
			watcher = new(directoryName);
			watcher.SourceChange += RaiseSourceChangeEvent;
		} else {
			watcher.Enabled = true;
		}

		return watcher;
	}

	private void WatchFile(IWatchedFile file) {
		if (!Incremental) {
			return;
		}

		GetWatcher(file.DirectoryName)?.WatchFile(file);
	}

	private void WatchFile(string path) {
		if (!Incremental) {
			return;
		}

		GetWatcher(Path.GetDirectoryName(path))?.WatchFile(path);
	}

	private void RaiseSourceChangeEvent(object sender, FileSystemEventArgs e) {
		if (_disposed || _processedEvent) {
			return;
		}

		_processedEvent = true;
		_filesChanged?.Invoke(this, e);
	}

	internal bool TryGetSource(string path, [NotNullWhen(true)] out SourceFile? file) {
		if (TryGetFileName(path, out var fullPath)) {
			ref var fileRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_loadedFiles, fullPath, out _);

			if (fileRef is not null) {
				fileRef.Refresh();

			} else {
				fileRef = new(fullPath);
			}

			if (fileRef.Error is not null) {
				SourceLine eline = _currentSourceObject == SourceFile.Empty
					? default
					: CurrentSourceLine;

				Error("Problem loading file", fileRef.Error, eline);
				WatchFile(fullPath);
				file = null;
				return false;
			}

			fileRef.WasUsed = true;

			file = fileRef;
			return true;
		} else {
			file = null;
			return false;
		}
	}

	internal bool TryGetBinary(string path, [NotNullWhen(true)] out SourceBinary? file) {
		if (TryGetFileName(path, out var fullPath)) {
			ref var fileRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_loadedBinaries, fullPath, out _);

			if (fileRef is not null) {
				fileRef.Refresh();
			} else {
				fileRef = new(fullPath);
			}

			if (fileRef.Error is not null) {
				SourceLine eline = _currentSourceObject == SourceFile.Empty
					? default
					: CurrentSourceLine;

				Error("Problem loading file", fileRef.Error, eline);
				WatchFile(fullPath);
				file = null;
				return false;
			}

			fileRef.WasUsed = true;

			file = fileRef;
			return true;
		} else {
			file = null;
			return false;
		}
	}

	internal bool TryGetFileName(string path, [NotNullWhen(true)] out string? filePath) {
		if (Path.IsPathFullyQualified(path)) {
			filePath = path;
			return true;

		} else if (_currentSourceObject.FileInfo?.Directory is DirectoryInfo dir) {
			// fully qualify it
			filePath = Path.Join(dir.FullName, path);
			return true;

		} else {
			Error("Unable to find current directory.");
			filePath = null;
			return false;
		}
	}

}
