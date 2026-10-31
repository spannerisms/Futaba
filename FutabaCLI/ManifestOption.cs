using System.Runtime.InteropServices;

namespace FutabaCLI;

internal class ManifestOption {
	internal static readonly Dictionary<string, Dictionary<string, ManifestOption>> ManifestKeywords;
	public string Name { get; }

	public string TemplateDefault { get; init; } = string.Empty;

	public bool IncludeInTemplate { get; init; } = true;

	private ManifestOption(string name) {
		Name = name;
	}

	internal static readonly ManifestOption ProjectType = new("type") { TemplateDefault = "<homebrew/disassembly>" };

	static ManifestOption() {
		ManifestKeywords = [];
		const string boolargs = "<true/false>";

		const string c_ASSEMBLY = "assembly";
		AddToCategory(c_ASSEMBLY, new("entry") { TemplateDefault = "<entry>.asm" });
		AddToCategory(c_ASSEMBLY, ProjectType);
		AddToCategory(c_ASSEMBLY, new("output") { TemplateDefault = "<target>.sfc" });
		AddToCategory(c_ASSEMBLY, new("addsymbols") { TemplateDefault = "standard" });
		AddToCategory(c_ASSEMBLY, new("nullfill") { TemplateDefault = "<zero, byte, string, random>, <args>" });
		AddToCategory(c_ASSEMBLY, new("randomseed") { TemplateDefault = "<string to hash>" });
		AddToCategory(c_ASSEMBLY, new("incremental") { TemplateDefault = boolargs });

		const string c_HEADER = "header";
		AddToCategory(c_HEADER, new("autofill") { TemplateDefault = boolargs });
		AddToCategory(c_HEADER, new("checksum") { TemplateDefault = boolargs });
		AddToCategory(c_HEADER, new("mapmode") { TemplateDefault = "<lorom/hirom/exlorom/exhirom/sa1/none>" });
		AddToCategory(c_HEADER, new("fastrom") { TemplateDefault = boolargs });
		AddToCategory(c_HEADER, new("title") { TemplateDefault = "<21 character ASCII name>" });
		AddToCategory(c_HEADER, new("romsize") { TemplateDefault = "<64KB/128KB/256KB/512KB/1M/2M/3M/4M/6M/8M>" });
		AddToCategory(c_HEADER, new("ramsize") { TemplateDefault = "<none/2KB/4KB/8KB/16KB/32KB/64KB/128KB/256KB>" });
		AddToCategory(c_HEADER, new("version") { TemplateDefault = "00" });
		AddToCategory(c_HEADER, new("region") { TemplateDefault = string.Empty });
		AddToCategory(c_HEADER, new("coprocessor") { TemplateDefault = string.Empty, IncludeInTemplate = false });
		AddToCategory(c_HEADER, new("extended") { TemplateDefault = boolargs });
		AddToCategory(c_HEADER, new("gamecode") { TemplateDefault = "XXXX" });
		AddToCategory(c_HEADER, new("makercode") { TemplateDefault = "XX" });
		AddToCategory(c_HEADER, new("specialversion") { TemplateDefault = string.Empty, IncludeInTemplate = false });
		AddToCategory(c_HEADER, new("cartridgetype") { TemplateDefault = string.Empty, IncludeInTemplate = false });

		const string c_DEBUG = "debug";
		AddToCategory(c_DEBUG, new("stdout") { TemplateDefault = string.Empty, IncludeInTemplate = false });
		AddToCategory(c_DEBUG, new("errorout") { TemplateDefault = string.Empty, IncludeInTemplate = false });
		AddToCategory(c_DEBUG, new("overflow") { TemplateDefault = "<error/grow/expand>" });
		AddToCategory(c_DEBUG, new("tokens") { TemplateDefault = "<off/ignore/ambiguous/aggressive/absolute/all>" });
		AddToCategory(c_DEBUG, new("symbols") { TemplateDefault = "[mlb/wla...]" });
		AddToCategory(c_DEBUG, new("warnaserror") { TemplateDefault = boolargs });
		AddToCategory(c_DEBUG, new("maxerrors") { TemplateDefault = "25" });

		const string c_DISASSEMBLY = "disassembly";
		AddToCategory(c_DISASSEMBLY, new("crc") { TemplateDefault = string.Empty });
		AddToCategory(c_DISASSEMBLY, new("md5") { TemplateDefault = string.Empty });
		AddToCategory(c_DISASSEMBLY, new("sha1") { TemplateDefault = string.Empty });
		AddToCategory(c_DISASSEMBLY, new("diff") { TemplateDefault = "<nominal>.sfc" });

		static void AddToCategory(string category, ManifestOption item) {
			ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(ManifestKeywords, category, out _);
			list ??= [];
			list.Add(item.Name, item);
		}
	}
}
