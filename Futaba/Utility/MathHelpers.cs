namespace Futaba;

internal static class MathHelpers {
	/// <summary>
	/// Placeholder that returns 0, but isn't resolved and doesn't ask for a request.
	/// </summary>
	internal static readonly IExpressionReturn InvalidExpression = new InvalidExpression();

	internal static readonly ExpressionItem BadExpressionNode = new BadExpressionNode();

	internal static decimal ClampCastDoubleAsDecimal(double d) => d switch {
		double.PositiveInfinity => decimal.MaxValue,
		double.NegativeInfinity => decimal.MinValue,
		double.NaN => 0,
		_ => (decimal) d,
	};

	extension (ExpressionState expst) {
		internal bool IsResolved {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (uint) expst <= (uint) ExpressionState.ResolvedButForceRequest;
		}

		internal bool WarrantsRequest {
			[DebuggerStepThrough]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => expst >= ExpressionState.ResolvedButForceRequest;
		}
	}

	internal const decimal DecimalTrue = 1M;
	internal const decimal DecimalFalse = 0M;
	internal const decimal NaN = -0M;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static decimal GetTruth(bool pred) {
		return pred ? DecimalTrue : DecimalFalse;
	}

	extension (decimal d) {
		// we need this to allow overflow to pass silently when converting to integral values
		[DebuggerStepThrough]
		internal unsafe long AsLong() {
			d = decimal.Truncate(d);

			long* dbytes = (long*) &d;

			long ret = dbytes[1];

			if (*(int*) dbytes < 0) {
				return -ret;
			}

			return ret;
		}

		[DebuggerStepThrough]
		internal unsafe int AsInt() {
			d = decimal.Truncate(d);

			long* dbytes = (long*) &d;

			int ret = (int) dbytes[1];

			if (*(int*) dbytes < 0) {
				return -ret;
			}

			return ret;
		}

		[DebuggerStepThrough]
		internal unsafe byte AsByte() {
			d = decimal.Truncate(d);

			byte* dbytes = (byte*) &d;

			byte ret;

			if (BitConverter.IsLittleEndian) {
				ret = dbytes[8];

			} else {
				ret = dbytes[15];
			}

			if (*(int*) dbytes < 0) {
				return (byte) -ret;
			}

			return ret;
		}

		[DebuggerStepThrough]
		internal unsafe bool IsFalse() {
			uint* ptr = (uint*) &d;
			ulong test = *(ulong*) (ptr + 1);
			test |= *(ulong*) (ptr + 2);

			return test == 0;
		}

		[DebuggerStepThrough]
		internal unsafe bool IsTrue() {
			uint* ptr = (uint*) &d;
			ulong test = *(ulong*) (ptr + 1);
			test |= *(ulong*) (ptr + 2);

			return test != 0;
		}
	}
}

file sealed class InvalidExpression : IExpressionReturn {
	public decimal Value => MathHelpers.NaN;

	public bool Resolved => false;
	public bool NeedsRequest => false;
	public ExpressionState ReturnState => ExpressionState.Unresolved;

	public bool TryToResolve() {
		return Resolved;
	}
}

file sealed class BadExpressionNode : ExpressionItem {
	public override ExpressionState State => ExpressionState.Unresolved;

	public override ExpressionItem Clone() {
		return this;
	}

	public override (decimal, ExpressionState) TryEvaluate() {
		return (MathHelpers.NaN, ExpressionState.Unresolved);
	}
}
