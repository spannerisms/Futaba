using System.Collections.Frozen;

namespace Futaba;

internal delegate ExpressionItem? FunctionCallGet(Assembler assembler, List<ExpressionItem> args);

internal static class FunctionCall {
	// TODO refactor this to be more robust
	internal static readonly FrozenDictionary<string, FunctionCallGet> BuiltInFunctions = new Dictionary<string, FunctionCallGet>() {
		{ "sin", (ass, args) => TestForBadArity(ass, "sin", 1, args) ? new Sin(args[0]) : null },
		{ "cos", (ass, args) => TestForBadArity(ass, "cos", 1, args) ? new Cos(args[0]) : null },
		{ "tan", (ass, args) => TestForBadArity(ass, "tan", 1, args) ? new Tan(args[0]) : null },

		{ "asin", (ass, args) => TestForBadArity(ass, "asin", 1, args) ? new Asin(args[0]) : null },
		{ "acos", (ass, args) => TestForBadArity(ass, "acos", 1, args) ? new Acos(args[0]) : null },
		{ "atan", (ass, args) => TestForBadArity(ass, "atan", 1, args) ? new Atan(args[0]) : null },

		{ "log", (ass, args) => TestForBadArity(ass, "log", 1, args) ? new Log10(args[0]) : null },
		{ "log2", (ass, args) => TestForBadArity(ass, "log2", 1, args) ? new Log2(args[0]) : null },
		{ "logb", (ass, args) => TestForBadArity(ass, "logb", 2, args) ? new LogB(args[0], args[1]) : null },

		{ "pow", (ass, args) => TestForBadArity(ass, "pow", 2, args) ? new Pow(args[0], args[1]) : null },
		{ "sqrt", (ass, args) => TestForBadArity(ass, "sqrt", 1, args) ? new Root2(args[0]) : null },
		{ "root", (ass, args) => TestForBadArity(ass, "root", 2, args) ? new Root(args[0], args[1]) : null },

		{ "max", (ass, args) => TestForBadArity(ass, "max", 2, args) ? new Max(args[0], args[1]) : null },
		{ "min", (ass, args) => TestForBadArity(ass, "min", 2, args) ? new Min(args[0], args[1]) : null },

		{ "clamp", (ass, args) => TestForBadArity(ass, "clamp", 3, args) ? new Clamp(args[0], args[1], args[2]) : null },
		{ "round", (ass, args) => TestForBadArity(ass, "round", 2, args) ? new Round(args[0], args[1]) : null },
		{ "select", (ass, args) => TestForBadArity(ass, "select", 3, args) ? new Select(args[0], args[1], args[2]) : null },


		{ "vram", (ass, args) => TestForBadArity(ass, "vram", 1, args) ? new VramAddress(args[0]) : null },
		{ "obsel", (ass, args) => TestForBadArity(ass, "obsel", 3, args) ? new ObSel(args[0], args[1], args[3]) : null },
		{ "bgnba", (ass, args) => TestForBadArity(ass, "bgnba", 2, args) ? new BgNba(args[0], args[1]) : null },
		{ "bgsc", (ass, args) => TestForBadArity(ass, "bgsc", 3, args) ? new BgSc(args[0], args[1], args[2]) : null },
		{ "rtn", (ass, args) => TestForBadArity(ass, "rtn", 1, args) ? new Rtn(args[0]) : null },
		{ "argof", (ass, args) => TestForBadArity(ass, "argof", 1, args) ? new ArgOf(args[0]) : null },
		{ "rebank", (ass, args) => TestForBadArity(ass, "rebank", 2, args) ? new Rebank(args[0], args[1]) : null },


		{ "floor", (ass, args) => TestForBadArity(ass, "floor", 1, args) ? new Floor(args[0]) : null },
		{ "ceil", (ass, args) => TestForBadArity(ass, "ceil", 1, args) ? new Ceil(args[0]) : null },
		{ "int", (ass, args) => TestForBadArity(ass, "int", 1, args) ? new IntCast(args[0]) : null },
		{ "abs", (ass, args) => TestForBadArity(ass, "abs", 1, args) ? new Abs(args[0]) : null },

		{ "bcd", (ass, args) => TestForBadArity(ass, "bcd", 1, args) ? new Bcd(args[0]) : null },
		{ "bitn", (ass, args) => TestForBadArity(ass, "bitn", 1, args) ? new BitN(args[0]) : null },

		{ "hash", (ass, args) => TestForBadArity(ass, "hash", 1, args) ? new StringExpression.HashString(args[0]) : null },
		{ "len", (ass, args) => TestForBadArity(ass, "len", 1, args) ? new StringExpression.GetStringLength(args[0]) : null },
		{ "streq", (ass, args) => TestForBadArity(ass, "streq", 2, args) ? new StringExpression.CompareStrings(args[0], args[1]) : null },

		{ "exists", (ass, args) => TestForBadArity(ass, "exists", 1, args) ? new ItemExists(args[0]) : null },

		{ "expr", (ass, args) => TestForBadArity(ass, "expr", 1, args) ? args[0] : null },

		{ "rand", GetRandom },

		{ "col", GetColor },
		{ "color", GetColor },

		{ "read", ReadFile },
	}.ToFrozenDictionary();

	private static bool TestForBadArity(Assembler assembler, string name, int arity, List<ExpressionItem> args) {
		if (args.Count == arity) {
			return true;
		} else {
			assembler.Error($"Wrong number of arguments to function [{name}]: got {args.Count}; expected {arity}.");
			return false;
		}
	}

	private sealed class GetFileFunc(Assembler assembler, ExpressionItem itemName, ExpressionItem? itemOffset,  ExpressionItem? itemSize) : ExpressionItem {
		private const int MaxBytes = 8;

		public override ExpressionItem Clone() => new GetFileFunc(assembler, itemName, itemSize, itemOffset);

		private ExpressionState _state = ExpressionState.Unresolved;
		public override ExpressionState State => _state;

		public override (decimal, ExpressionState) TryEvaluate() {
			int size;

			ExpressionState state;

			if (itemSize is null) {
				size = 1;
				state = ExpressionState.Resolved;
			} else {
				(var dc, state) = itemSize.TryEvaluate();

				if (!state.IsResolved) {
					return (MathHelpers.NaN, _state = state);
				}

				size = dc.AsInt();

				if (size is < 1 or > MaxBytes) {
					return (MathHelpers.NaN, _state = ExpressionState.InvalidArgument);
				}
			}

			int offset;

			if (itemOffset is null) {
				offset = 0;

			} else {
				var (dc, ds) = itemOffset.TryEvaluate();

				if (!ds.IsResolved) {
					return (MathHelpers.NaN, _state = ds);
				}

				offset = dc.AsInt();

				if (offset < 0) {
					return (MathHelpers.NaN, _state = ExpressionState.InvalidArgument);
				}

				// don't overwrite state if itemSize was ExpressionState.ResolvedButForceRequest
				if (state is ExpressionState.Resolved) {
					state = ds;
				}
			}

			if (itemName is not IStringToken st) {
				return (MathHelpers.NaN, _state = ExpressionState.InvalidArgumentToStringFunction);
			}

			Span<byte> rdb = stackalloc byte[MaxBytes];
			rdb.Clear();

			// using try because there are so many potential errors,
			// and this function shouldn't be called that frequently,
			// so the overhead of catch is fine
			if (assembler.TryGetBinary(st.Contents, out var rdFile)) {
				if ((uint) size <= MaxBytes && rdFile.TryGetSpan(offset, size, out var copySpan)) {
					copySpan.CopyTo(rdb);

					ulong ret = BinaryPrimitives.ReadUInt64LittleEndian(rdb);
					return (ret, _state = state);

				}
			}

			return (MathHelpers.NaN, _state = ExpressionState.InvalidArgument);
		}
	}

	private static ExpressionItem? ReadFile(Assembler assembler, List<ExpressionItem> args) {
		switch (args.Count) {
			case 1: return new GetFileFunc(assembler, args[0], null, null);
			case 2: return new GetFileFunc(assembler, args[0], args[1], null);
			case 3: return new GetFileFunc(assembler, args[0], args[1], args[2]);
		}

		assembler.Error($"Wrong number of arguments to function [read]: got {args.Count}, expected 1, 2 or 3.");

		return null;
	}

	private static ExpressionItem? GetRandom(Assembler assembler, List<ExpressionItem> args) {
		if (TestForBadArity(assembler, "random", 2, args)) {
			return new Rand(args[0], args[1], assembler._random);

		} else {
			return null;
		}
	}

	private sealed class Rand(ExpressionItem left, ExpressionItem right, XoRandom random) : BinaryExpression(left, right) {
		private readonly XoRandom _random = random;
		public override ExpressionItem Clone() => new Rand(Left.Clone(), Right.Clone(), _random);

		protected override decimal Equate(decimal a, decimal b) {
			if (a > b) {
				(a, b) = (b, a);
			}

			return _random.NextInt64(a.AsLong(), b.AsLong());
		}
	}

	private static ExpressionItem? GetColor(Assembler assembler, List<ExpressionItem> args) {
		int argCount = args.Count;

		if (argCount is 1) {
			return new UnaryColor(args[0]);

		} else if (argCount is 3) {
			return new TernaryColor(args[0], args[1], args[2]);

		} else {
			assembler.Error($"Wrong number of arguments to function [color]: got {argCount}, expected 1 or 3.");
			return null;
		}
	}

	private sealed class Sin(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Sin(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (decimal) Math.Sin((double) a);
		}
	}

	private sealed class Cos(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Cos(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (decimal) Math.Cos((double) a);
		}
	}
	private sealed class Tan(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Tan(Item.Clone());
		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Tan((double) a));
		}
	}
		private sealed class Asin(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Asin(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (decimal) Math.Asin((double) a);
		}
	}

	private sealed class Acos(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Acos(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (decimal) Math.Asin((double) a);
		}
	}
	private sealed class Atan(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Atan(Item.Clone());
		protected override decimal Equate(decimal a) {
			return (decimal) Math.Atan((double) a);
		}
	}

	private sealed class ItemExists(ExpressionItem item) : ExpressionItem {
		private ExpressionState _state;
		public override ExpressionState State => _state;
		public override ExpressionItem Clone() => new ItemExists(item.Clone());

		public override (decimal, ExpressionState) TryEvaluate() {
			decimal value;

			var (_, state) = item.TryEvaluate();

			switch (state) {
				case ExpressionState.Resolved:
					value = 1;
					break;

				case ExpressionState.ResolvedButForceRequest:
					value = 0;
					break;

				case ExpressionState.MissingSymbol:
					value = 0;
					state = ExpressionState.ResolvedButForceRequest;
					break;

				case ExpressionState.VariableNotFound:
					value = 0;
					state = ExpressionState.Resolved;
					break;

				default:
					value = 0;
					break;
			}

			return (value, _state = state);
		}
	}

	private sealed class Ceil(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Ceil(Item.Clone());
		protected override decimal Equate(decimal a) {
			return decimal.Ceiling(a);
		}
	}

	private sealed class Floor(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Floor(Item.Clone());
		protected override decimal Equate(decimal a) {
			return decimal.Floor(a);
		}
	}

	private sealed class IntCast(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new IntCast(Item.Clone());
		protected override decimal Equate(decimal a) {
			return decimal.Truncate(a);
		}
	}

	private sealed class VramAddress(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new VramAddress(Item.Clone());
		protected override decimal Equate(decimal a) {
			ushort ret = (ushort) a.AsInt();
			ret >>= 1;
			return ret;
		}
	}

	private sealed class UnaryColor(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new UnaryColor(Item.Clone());
		protected override decimal Equate(decimal a) {
			int rgb = a.AsInt();

			int r = rgb >> 19;
			int g = (rgb >> 6) & 0x03E0;
			int b = (rgb << 7) & 0x7C00;

			return r | g | b;
		}
	}

	private sealed class Rtn(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Rtn(Item.Clone());

		protected override decimal Equate(decimal a) {
			return a.AsLong() - 1;
		}
	}

	private sealed class ArgOf(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new ArgOf(Item.Clone());

		protected override decimal Equate(decimal a) {
			return a.AsLong() + 1;
		}
	}

	private sealed class Abs(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Abs(Item.Clone());
		protected override decimal Equate(decimal a) {
			return decimal.Abs(a);
		}
	}

	private sealed class Bcd(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new Bcd(Item.Clone());
		protected override decimal Equate(decimal a) {
			ulong ret = 0;

			ulong quotient = (ulong) a.AsLong();

			int shifting = 0;

			while (quotient > 0) {
				(quotient, ulong remainder) = Math.DivRem(quotient, 10);

				ret += remainder << shifting;

				shifting += 4;
			}

			return ret;
		}
	}

	private sealed class BitN(ExpressionItem item) : UnaryExpression(item) {
		public override ExpressionItem Clone() => new BitN(Item.Clone());
		protected override decimal Equate(decimal a) {
			return 1 << a.AsInt();
		}
	}

	private sealed class Pow(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Pow(Left.Clone(), Right.Clone());

		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Pow((double) a, (double) b));
		}
	}

	private sealed class LogB(ExpressionItem left, ExpressionItem right) : BinaryExpression.SafeBinary(left, right) {
		public override ExpressionItem Clone() => new LogB(Left.Clone(), Right.Clone());
		protected override bool Validate(decimal a, decimal b) {
			if (b > 1M && a > 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		// log_b(a)
		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Log((double) a, (double) b));
		}
	}

	private sealed class Log2(ExpressionItem item) : UnaryExpression.SafeUnary(item) {
		public override ExpressionItem Clone() => new Log2(Item.Clone());
		protected override bool Validate(decimal a) {
			if (a > 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Log2((double) a));
		}
	}

	private sealed class Log10(ExpressionItem item) : UnaryExpression.SafeUnary(item) {
		public override ExpressionItem Clone() => new Log10(Item.Clone());
		protected override bool Validate(decimal a) {
			if (a > 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Log10((double) a));
		}
	}


	private sealed class Root(ExpressionItem left, ExpressionItem right) : BinaryExpression.SafeBinary(left, right) {
		public override ExpressionItem Clone() => new Root(Left.Clone(), Right.Clone());
		protected override bool Validate(decimal a, decimal b) {
			if ((double) b != 0.0D && a >= 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a, decimal b) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Pow((double) a, (double) (1 / b)));
		}
	}

	private sealed class Root2(ExpressionItem item) : UnaryExpression.SafeUnary(item) {
		public override ExpressionItem Clone() => new Root2(Item.Clone());

		protected override bool Validate(decimal a) {
			if (a >= 0M) {
				return true;
			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a) {
			return MathHelpers.ClampCastDoubleAsDecimal(Math.Sqrt((double) a));
		}
	}

	private sealed class Max(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Max(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a >= b ? a : b;
		}
	}

	private sealed class Min(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Min(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return a <= b ? a : b;
		}
	}

	private sealed class Round(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Round(Left.Clone(), Right.Clone());
		protected unsafe override decimal Equate(decimal a, decimal b) {
			int prec = b.AsInt();

			// decimal.Round doesn't accept negative values
			// so we've gotta roll this ourselves
			if (prec < 0) {
				// create a factor of 10 to use for manipulation
				decimal scaling = 1_0000_0000_0000_0000_0000_0000_0000M; // largest power of 10

				// change the scaler's scale factor to the appropriate power of 10
				*(int*) &scaling = (28 + prec) << 16;

				// divide by the scale factor
				a /= scaling;

				// kill the fractional part
				a = decimal.Truncate(a);

				// return to original order of magnitude
				a *= scaling;

				return a;

			} else if (prec > 28) {
				return a;

			} else {
				return decimal.Round(a, prec);
			}
		}
	}

	private sealed class Rebank(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new Rebank(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			return (a.AsLong() & SnesHelpers.AbsoluteMask) | ((b.AsLong() & 0xFFL) << 16);
		}
	}

	private sealed class TernaryColor(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : TernaryExpression(itema, itemb, itemc) {
		public override ExpressionItem Clone() => new TernaryColor(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());

		protected override decimal Equate(decimal a, decimal b, decimal c) {
			int r = a.AsByte() >> 3;
			int g = (b.AsByte() & 0xF8) << 2;
			int bl = c.AsByte() << 7;

			return r | g | bl;
		}
	}

	private sealed class Clamp(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : TernaryExpression.SafeTernary(itema, itemb, itemc) {
		public override ExpressionItem Clone() => new Clamp(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());

		protected override bool Validate(decimal a, decimal b, decimal c) {
			if (b <= c) {
				return true;

			} else {
				_state = ExpressionState.InvalidArgument;
				return false;
			}
		}

		protected override decimal Equate(decimal a, decimal b, decimal c) {
			// not using decimal.Clamp because we've already validated the inputs
			if (a < b) {
				return b;

			} else if (a > c) {
				return c;

			} else {
				return a;
			}
		}
	}

	private sealed class Select(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : TernaryExpression(itema, itemb, itemc) {
		public override ExpressionItem Clone() => new Select(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());
		protected override decimal Equate(decimal a, decimal b, decimal c) {
			return a.IsTrue() ? b : c;
		}
	}

	private sealed class BgSc(ExpressionItem itema, ExpressionItem itemb, ExpressionItem itemc) : TernaryExpression(itema, itemb, itemc) {
		public override ExpressionItem Clone() => new BgSc(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());
		protected override decimal Equate(decimal a, decimal b, decimal c) {
			int ai = (a.AsInt() >> 9) & 0xFC;
			int bi = b.IsTrue() ? 0b10 : 0;
			int ci = c.IsTrue() ? 0b01 : 0;

			return ai | bi | ci;
		}
	}

	private sealed class BgNba(ExpressionItem left, ExpressionItem right) : BinaryExpression(left, right) {
		public override ExpressionItem Clone() => new BgNba(Left.Clone(), Right.Clone());
		protected override decimal Equate(decimal a, decimal b) {
			int ai = (a.AsInt() >> 13) & 0x0F;
			int bi = (b.AsInt() >> 9) & 0xF0;

			return ai | bi;
		}
	}

	private sealed class ObSel(ExpressionItem address, ExpressionItem nsoffset, ExpressionItem sizes) : TernaryExpression(address, nsoffset, sizes) {
		public override ExpressionItem Clone() => new ObSel(ItemA.Clone(), ItemB.Clone(), ItemC.Clone());
		protected override decimal Equate(decimal a, decimal b, decimal c) {
			int ai = (a.AsInt() >> 14) & 0x07;
			int bi = (b.AsInt() << 3) & 0x18;
			int ci = (b.AsInt() << 5) & 0xE0;

			return ai | bi | ci;
		}
	}
}
