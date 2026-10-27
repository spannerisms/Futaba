namespace Futaba;

internal class BranchRequest(IExpressionReturn item, int offset, int address, uint time, SourceLine line) : INeedResolution {
	public IExpressionReturn Item => item;

	public int Offset => offset;
	public int Address => address;

	public int Distance => Item.ValueInt32 - Address;

	public virtual int Size => 1;

	public uint Time => time;

	public int Value => Item.ValueInt32;

	public SourceLine SourceLine => line;

	public bool TryToResolve() {
		return Item.TryToResolve();
	}

	public virtual bool DistanceTooLarge => !SnesHelpers.TestGetBranchDistance(Address, Item.ValueInt32).Item1;
}

internal sealed class BranchRequestLong(IExpressionReturn item, int offset, int address, uint time, SourceLine line)
	: BranchRequest(item, offset, address, time, line) {
	public override int Size => 2;
	public override bool DistanceTooLarge => !SnesHelpers.TestGetBranchDistanceLong(Address, Item.ValueInt32).Item1;

}
