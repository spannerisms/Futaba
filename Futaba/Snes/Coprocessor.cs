namespace Futaba.Snes;

/// <summary>
/// 
/// </summary>
// Format we're going with is 0x10_CC_DD_DD
//   CC - Byte that goes in header
//   DD - distinguisher
public enum Coprocessor {
	/// <summary>
	/// No coprocessor
	/// </summary>
	None = 0,

	/// <summary>
	/// DSP1 or variant
	/// </summary>
	DSP = 0x10_00__00_01,

	/// <summary>
	/// GSU, such as Super-FX
	/// </summary>
	SuperFX = 0x10_10__00_00,

	/// <inheritdoc cref="SA1"/>
	GSU = SuperFX,

	/// <summary>
	/// OBC1
	/// </summary>
	OBC1 = 0x10_20__00_00,

	/// <summary>
	/// SA-1
	/// </summary>
	SA1 = 0x10_30__00_00,

	/// <summary>
	/// S-RTC
	/// </summary>
	SDD1 = 0x10_40__00_00,

	/// <summary>
	/// Real-Time Clock
	/// </summary>
	SRTC = 0x10_50__00_00,

	/// <summary>
	/// Other chips
	/// </summary>
	Other = 0x10_E0__00_00,

	/// <summary>
	/// Custom chips
	/// </summary>
	Custom = 0x10_F0__00_00,

	// 0X // DSP
	// 1X // Super FX
	// 2X // OBC1
	// 3X // SA-1
	// EX // other
	// FX // custom
}
