using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace FutabaCLI;

internal static partial class Igarashi {
	[SupportedOSPlatform("linux")]
	sealed class LinuxAssociationRegistrar : IAssociationRegistrar {
		static readonly string DataHome = ResolveXdgDir("XDG_DATA_HOME", ".local", "share");
		static readonly string ConfigHome = ResolveXdgDir("XDG_CONFIG_HOME", ".config");
		static readonly string LocalBinDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");

		readonly string _mimePackagePath = Path.Combine(DataHome, "mime", "packages", "futaba.xml");
		readonly string _desktopFilePath = Path.Combine(DataHome, "applications", DesktopFileId);
		readonly string _appIconPath = Path.Combine(DataHome, "icons", "hicolor", "512x512", "apps", $"{IconName}.png");
		readonly string _mimeIconPath = Path.Combine(DataHome, "icons", "hicolor", "512x512", "mimetypes", $"{MimeIconName}.png");
		readonly string _mimeAppsListPath = Path.Combine(ConfigHome, "mimeapps.list");
		readonly string _binaryLinkPath = Path.Combine(LocalBinDir, "futaba");

		// per the XDG Base Directory spec, a relative (or empty) value is treated as unset
		static string ResolveXdgDir(string envVar, params string[] fallbackParts) {
			if (Environment.GetEnvironmentVariable(envVar) is string p && Path.IsPathRooted(p)) {
				return p;
			}

			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine([home, .. fallbackParts]);
		}

		public int Register() {
			if (Environment.ProcessPath is not string processPath) {
				Error("Unable to determine the path of the running executable.");
				return Exit_Error;
			}

			WriteMimePackage();
			WriteDesktopEntry(processPath);
			UpdateMimeApps(add: true);
			InstallIcons();
			LinkBinary(processPath);

			RefreshDatabases();

			Console.WriteLine("Successfully registered .futaba file association and app shortcut.");

			WarnIfLocalBinMissingFromPath();

			return Exit_Good;
		}

		public int Unregister() {
			RemoveIfExists(_mimePackagePath);
			RemoveIfExists(_desktopFilePath);
			RemoveIfExists(_appIconPath);
			RemoveIfExists(_mimeIconPath);

			UpdateMimeApps(add: false);

			UnlinkBinary();

			RefreshDatabases();

			Console.WriteLine("Successfully unregistered .futaba file association and app shortcut.");

			return Exit_Good;
		}

		static void CreateParentDirectory(string path) {
			// should never get a parentless path in our usage
			string dir = Path.GetDirectoryName(path)
				?? throw new UnreachableException($"'{path}' has no parent directory.");
			Directory.CreateDirectory(dir);
		}

		// ---- mime package (shared-mime-info) ----
		void WriteMimePackage() {
			CreateParentDirectory(_mimePackagePath);
			WriteUtf8NoBom(_mimePackagePath, $"""
				<?xml version="1.0" encoding="UTF-8"?>
				<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
					<mime-type type="{MimeType}">
						<comment>Futaba assembly manifest</comment>
						<sub-class-of type="text/plain"/>
						<glob pattern="*{ManifestExtension}"/>
						<icon name="{MimeIconName}"/>
						<generic-icon name="text-x-generic"/>
					</mime-type>
				</mime-info>
				""");
		}

		// ---- desktop entry ----
		void WriteDesktopEntry(string processPath) {
			CreateParentDirectory(_desktopFilePath);
			WriteUtf8NoBom(_desktopFilePath, $"""
				[Desktop Entry]
				Type=Application
				Name=Futaba
				GenericName=SNES Assembler
				Comment=Assemble Super Famicom / SNES ROMs
				Exec={EscapeExecArgument(processPath)} build %f
				Icon={IconName}
				Terminal=true
				MimeType={MimeType};
				Categories=Development;Building;
				""");
		}

		// Desktop Entry Exec values require two layers of escaping, applied in this
		// order when writing: first the Exec argument quoting (wrap in double quotes;
		// inside them, backslash-escape \ " ` $, and double any literal %), then the
		// general Desktop Entry "string" escaping, under which the whole value is
		// unescaped before the Exec grammar ever sees it -- so every backslash just
		// written above must itself be doubled to survive that pass. (There's no
		// built-in .NET helper for this -- it's a narrow, spec-specific grammar,
		// distinct from both POSIX shell quoting and Windows argv quoting.)
		static string EscapeExecArgument(string arg) {
			string quoted = '"' + arg
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("`", "\\`")
				.Replace("$", "\\$")
				.Replace("%", "%%")
				+ '"';

			return quoted.Replace("\\", "\\\\");
		}

		// ---- mimeapps.list ----

		void UpdateMimeApps(bool add) {
			if (!add && !File.Exists(_mimeAppsListPath)) {
				return;
			}

			IniDocument doc = IniDocument.Load(_mimeAppsListPath);

			UpdateAssociation(doc, "Default Applications", isSet: false, add);
			UpdateAssociation(doc, "Added Associations", isSet: true, add);

			Directory.CreateDirectory(ConfigHome);
			WriteUtf8NoBom(_mimeAppsListPath, doc.ToString());
		}

		// "Default Applications" holds a single preferred app per MIME type;
		// "Added Associations" is an unordered set of supplementary apps.
		// IniDocument preserves every other line in the file -- mimeapps.list
		// routinely already holds unrelated associations (e.g. a mailto:
		// handler) set by other applications.
		static void UpdateAssociation(IniDocument doc, string section, bool isSet, bool add) {
			doc.Update(section, MimeType, currentRaw => {
				string[] current = Helpers.CleanSplit(currentRaw ?? "", ';');

				string[] updated = add
					? isSet
						? current.Contains(DesktopFileId) ? current : [.. current, DesktopFileId]
						: [DesktopFileId]
					: [.. current.Where(a => a != DesktopFileId)];

				return updated.Length is 0 ? null : string.Join(';', updated) + ';';
			});
		}

		// ---- icons ----

		void InstallIcons() {
			using Stream? resource = typeof(Igarashi).Assembly.GetManifestResourceStream("futaba.png");

			if (resource is null) {
				Warning("Embedded icon resource not found; the file association was registered without an icon.");
				return;
			}

			using MemoryStream ms = new();
			resource.CopyTo(ms);
			byte[] png = ms.ToArray();

			WriteIcon(_appIconPath, png);
			WriteIcon(_mimeIconPath, png);

			static void WriteIcon(string path, byte[] data) {
				CreateParentDirectory(path);
				File.WriteAllBytes(path, data);
			}
		}

		// ---- PATH symlink ----

		void LinkBinary(string processPath) {
			Directory.CreateDirectory(LocalBinDir);
			// File.Delete is a no-op if absent and removes a dangling symlink
			File.Delete(_binaryLinkPath);
			new FileInfo(_binaryLinkPath).CreateAsSymbolicLink(processPath);
		}

		void UnlinkBinary() {
			FileInfo link = new(_binaryLinkPath);
			// only remove it if it's a symlink to a "futaba" binary, so a user's own
			// unrelated real binary at this path is never touched
			if (link.LinkTarget is string target && Path.GetFileName(target).EqualsI("futaba")) {
				link.Delete();
			}
		}

		void WarnIfLocalBinMissingFromPath() {
			string? pathVar = Environment.GetEnvironmentVariable("PATH");

			bool onPath = pathVar is not null && pathVar
				.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
				.Any(d => Path.TrimEndingDirectorySeparator(d) == Path.TrimEndingDirectorySeparator(LocalBinDir));

			if (!onPath) {
				Warning($"'{LocalBinDir}' is not on your PATH. Add it to your shell profile to run 'futaba' by name.");
			}
		}

		// ---- database refresh ----

		void RefreshDatabases() {
			string mimeDir = Path.Combine(DataHome, "mime");
			string appsDir = Path.Combine(DataHome, "applications");

			if (!TryRunTool("update-mime-database", [mimeDir], out int? mimeExitCode)) {
				Warning("'update-mime-database' was not found on PATH.");
				Console.WriteLine($"  Run this manually to finish registering the file association: update-mime-database \"{mimeDir}\"");
			} else if (mimeExitCode is not 0) {
				// null here means it launched but we don't have its exit code --
				// most likely a timeout, but don't assert that as the specific cause
				string detail = mimeExitCode is int code ? $" (exit code {code})" : "";
				Warning($"'update-mime-database' did not complete successfully{detail}; the file association may need a manual refresh.");
			}

			TryRunTool("update-desktop-database", [appsDir], out _);

			// deliberately not running gtk-update-icon-cache: it needs an index.theme
			// in the target directory to build a cache, which a per-user hicolor
			// override directory like ours doesn't have and doesn't need -- GTK's
			// icon theme lookup falls back to scanning the directory directly when
			// no cache is present
		}

		// Attempts to launch a best-effort refresh tool by its bare command name.
		// Like other TryXxx methods, false is the expected, non-exceptional outcome
		// for the one case most callers act on: the command isn't installed at all
		// (ENOENT) -- it is NOT a success/failure result on its own. exitCode
		// carries the actual outcome once the command did launch: null if it could
		// not be launched at all, or if it had to be killed after running past the
		// timeout; otherwise its real exit code (0 for success).
		static bool TryRunTool(string command, string[] args, out int? exitCode) {
			exitCode = null;

			try {
				ProcessStartInfo psi = new(command) {
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				};

				foreach (string a in args) {
					psi.ArgumentList.Add(a);
				}
				// Process.Start with a bare command name searches PATH.
				using Process proc = Process.Start(psi)!;

				if (proc.WaitForExit(TimeSpan.FromSeconds(30))) {
					exitCode = proc.ExitCode;
				} else {
					proc.Kill(entireProcessTree: true);
				}

				return true;
			} catch (Win32Exception e) when (e.NativeErrorCode is 2) {
				return false; // ENOENT: the command isn't on PATH
			} catch {
				return true; // started but failed some other way; still "found"
			}
		}

		// ---- shared file helpers ----

		static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

		// the desktop specs mandate plain UTF-8, no BOM, LF-only line endings, and
		// exactly one trailing newline; normalizing here means callers can just
		// write natural-looking (CRLF-source, no-trailing-newline) literals
		static void WriteUtf8NoBom(string path, string content) {
			string normalized = content.ReplaceLineEndings("\n").TrimEnd('\n') + '\n';
			File.WriteAllText(path, normalized, Utf8NoBom);
		}

		static void RemoveIfExists(string path) {
			if (File.Exists(path)) {
				File.Delete(path);
			}
		}
	}
}
