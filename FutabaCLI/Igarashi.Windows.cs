using System.Runtime.Versioning;

namespace FutabaCLI;

internal static partial class Igarashi {
	[SupportedOSPlatform("windows")]
	sealed class WindowsAssociationRegistrar : IAssociationRegistrar {
		readonly string _binaryLinkPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "futaba.exe");

		public int Register() {
			if (Environment.ProcessPath is not string propath) {
				Error("Unable to determine the path of the running executable.");
				return Exit_Error;
			}

			var rootKey = Microsoft.Win32.Registry.CurrentUser;

			using var appKey = rootKey.CreateSubKey(@$"SOFTWARE\Classes\{AppHandle}");

			using var shellKey = appKey.CreateSubKey("shell");
			shellKey.SetValue("", DefaultOpen);

			using var shell1 = shellKey.CreateSubKey(@$"{DefaultOpen}\command");
			shell1.SetValue("", @$"""{propath}"" build ""%1""");

			using var extKey = rootKey.CreateSubKey(@$"SOFTWARE\Classes\{ManifestExtension}");
			extKey.SetValue("", AppHandle);

			// File.Delete is a no-op if absent and, unlike FileInfo.Exists, still
			// removes a dangling symlink
			File.Delete(_binaryLinkPath);

			new FileInfo(_binaryLinkPath).CreateAsSymbolicLink(propath);

			Console.WriteLine("Successfully registered .futaba file association and app shortcut.");

			return Exit_Good;
		}

		public int Unregister() {
			var rootKey = Microsoft.Win32.Registry.CurrentUser;

			rootKey.DeleteSubKeyTree(@$"SOFTWARE\Classes\{AppHandle}", throwOnMissingSubKey: false);
			rootKey.DeleteSubKeyTree(@$"SOFTWARE\Classes\{ManifestExtension}", throwOnMissingSubKey: false);

			FileInfo exeSymLink = new(_binaryLinkPath);

			// only remove it if it's a symlink we created, never an unrelated real exe
			if (exeSymLink.LinkTarget is not null) {
				exeSymLink.Delete();
			}

			Console.WriteLine("Successfully unregistered .futaba file association and app shortcut.");

			return Exit_Good;
		}
	}
}
