namespace FutabaCLI;

/// <summary>
/// A minimal, dependency-free editor for the small subset of INI-like syntax
/// used by freedesktop.org files such as mimeapps.list: "[Section]" headers
/// and "key=value" lines. Preserves every line it doesn't touch -- comments,
/// blank lines, and sections/keys it knows nothing about -- so it's safe to
/// use on a file that other applications also read and write.
/// </summary>
internal sealed class IniDocument {
	readonly List<string> _lines;

	IniDocument(List<string> lines) => _lines = lines;

	public static IniDocument Load(string path) =>
		new(File.Exists(path) ? [.. File.ReadAllLines(path)] : []);

	/// <summary>
	/// Reads the current value of "key" in "[section]" (null if either is
	/// absent), passes it to <paramref name="transform"/>, and writes the
	/// result back: a non-null result sets the key (creating it and/or the
	/// section as needed), a null result removes the key if present.
	/// </summary>
	public void Update(string section, string key, Func<string?, string?> transform) {
		int headerIndex = _lines.FindIndex(l => l.Trim() == $"[{section}]");
		if (headerIndex >= 0) {
			// process the section, finding the target key and the last content line
			int keyLine = -1;
			int lastContentLine = headerIndex;
			for (int end = headerIndex + 1; end < _lines.Count; ++end) {
				string line = _lines[end].Trim();
				if (line.Length != 0) {
					if (line[0] == '[') {
						break;  // next section; bail
					}
					lastContentLine = end;
					int eq = line.IndexOf('=');
					if (keyLine < 0 && eq >= 0 && line[..eq].Trim() == key) {
						keyLine = end;
					}
				}
			}
			string? newValue = transform(keyLine < 0 ? null : ValueOf(_lines[keyLine]));
			if (keyLine >= 0) {
				if (newValue is null) {
					_lines.RemoveAt(keyLine);
				} else {
					_lines[keyLine] = FormatLine(key, newValue);
				}
			} else if (newValue is not null) {
				_lines.Insert(lastContentLine + 1, FormatLine(key, newValue));
			}
		} else {
			// no such section: transform(null) decides whether to append one
			if (transform(null) is string created) {
				if (_lines.Count > 0 && _lines[^1].Length != 0) {
					_lines.Add("");
				}
				_lines.Add($"[{section}]");
				_lines.Add(FormatLine(key, created));
			}
		}
	}

	public override string ToString() => string.Join('\n', _lines);

	static string FormatLine(string key, string value) => $"{key}={value}";
	static string ValueOf(string line) => line[(line.IndexOf('=') + 1)..];
}
