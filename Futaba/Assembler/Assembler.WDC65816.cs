namespace Futaba;

unsafe partial class Assembler {
	private void Assemble65816(OpWdc instruction) {
		SizeToken size = GetSizeToken();

		if (size is SizeToken.Invalid) {
			AbortCommand();
			return;
		}

		ReadRestOfCommand(out OperandString opstr);

		IExpressionReturn opval;
		AddressingMode mode;

		switch (instruction) {
			case OpWdc.ADC:
				// standard accumulator modes could be condensed into a single routine with base opcodes (and a special case for STA #i)
				// but for now... ehhhhhhhhhhhhhhh
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0x61, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0x63, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x65, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0x67, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0x69, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0x69, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x6D, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0x6F, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0x71, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0x72, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0x73, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x75, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0x77, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0x79, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x7D, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0x7F, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.AND:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0x21, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0x23, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x25, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0x27, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0x29, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0x29, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x2D, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0x2F, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0x31, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0x32, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0x33, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x35, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0x37, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0x39, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x3D, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0x3F, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.ASL:
				if (size is SizeToken.Unspecified && RepeatIfRepeatable(opstr, 0x0A)) break;

				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x06, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x0E, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x16, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x1E, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.BCC: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0x90); break;
			case OpWdc.BCS: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0xB0); break;
			case OpWdc.BEQ: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0xF0); break;

			case OpWdc.BIT:
				if (opstr.First is ImmediateMode) {
					(mode, opval) = HandleImmediate(opstr, size);
					switch (mode) {
						case AddressingMode.Immediate8: /*                                         */ WriteOpB(0x89, opval); break;
						case AddressingMode.Immediate16: /*                                        */ WriteOpW(0x89, opval); break;
						default: /*                                                                */ InvalidAddressingMode(); break;
					}
				} else {
					(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
					switch (mode) {
						case AddressingMode.DirectPage: /*                                         */ WriteOpB(0x24, opval); break;
						case AddressingMode.Absolute: /*                                           */ WriteOpW(0x2C, opval); break;
						case AddressingMode.DirectPageIndexedX: /*                                 */ WriteOpB(0x34, opval); break;
						case AddressingMode.AbsoluteIndexedX: /*                                   */ WriteOpW(0x3C, opval); break;
						default: /*                                                                */ InvalidAddressingMode(); break;
					}
				}

				break;

			case OpWdc.BMI: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0x30); break;
			case OpWdc.BNE: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0xD0); break;
			case OpWdc.BPL: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0x10); break;
			case OpWdc.BRA: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0x80); break;

			case OpWdc.BRK: /*                                                                        */ AssembleSignature(opstr, size, 0x00); break;

			case OpWdc.BRL: /*                                                                        */ NoTokens(size); AssembleBranchLong(opstr, 0x82); break;
			case OpWdc.BVC: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0x50); break;
			case OpWdc.BVS: /*                                                                        */ NoTokens(size); AssembleBranch(opstr, 0x70); break;

			case OpWdc.CLC: /*                                                                        */ AssembleImplied(opstr, size, 0x18); break;
			case OpWdc.CLD: /*                                                                        */ AssembleImplied(opstr, size, 0xD8); break;
			case OpWdc.CLI: /*                                                                        */ AssembleImplied(opstr, size, 0x58); break;
			case OpWdc.CLV: /*                                                                        */ AssembleImplied(opstr, size, 0xB8); break;

			case OpWdc.CMP:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0xC1, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0xC3, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xC5, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0xC7, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xC9, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xC9, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xCD, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0xCF, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0xD1, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0xD2, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0xD3, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0xD5, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0xD7, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0xD9, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0xDD, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0xDF, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.COP: /*                                                                        */ AssembleSignature(opstr, size, 0x02); break;

			case OpWdc.CPX:
				(mode, opval) = CheckLowerMemoryModesAndImmediate(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xE4, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xEC, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xE0, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xE0, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.CPY:
				(mode, opval) = CheckLowerMemoryModesAndImmediate(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xC4, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xCC, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xC0, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xC0, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.DEC:
				if (size is SizeToken.Unspecified && RepeatIfRepeatable(opstr, 0x3A)) break;

				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xC6, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xCE, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0xD6, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0xDE, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.DEX: /*                                                                        */ AssembleRepeatable(opstr, size, 0xCA); break;
			case OpWdc.DEY: /*                                                                        */ AssembleRepeatable(opstr, size, 0x88); break;

			case OpWdc.EOR:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0x41, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0x43, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x45, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0x47, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0x49, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0x49, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x4D, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0x4F, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0x51, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0x52, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0x53, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x55, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0x57, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0x59, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x5D, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0x5F, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.INC:
				if (size is SizeToken.Unspecified && RepeatIfRepeatable(opstr, 0x1A)) break;

				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xE6, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xEE, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0xF6, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0xFE, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.INX: /*                                                                        */ AssembleRepeatable(opstr, size, 0xE8); break;
			case OpWdc.INY: /*                                                                        */ AssembleRepeatable(opstr, size, 0xC8); break;

			// these don't need their own functions, since they're all unique
			case OpWdc.JML:

				if (opstr.IsEmpty) {
					MissingOperand();
				} else if (opstr.OnlyCharIs(JNOP)) {
					WriteOpL(0x5C, Provenance + 4);
				} else {
					if (opstr.First is IndirectLongOpen) {
						if (opstr.Last is IndirectLongClose) {
							opval = ParseOperandSliced(opstr, 1, 1);

							if (size is SizeToken.W) {
								WriteOpW(0xDC, opval);
								break;
							} else if (size is SizeToken.Unspecified) {
								WriteOpWBank00(0xDC, opval);
								break;
							}
						}
					} else {
						if (size is SizeToken.Unspecified or SizeToken.L) {
							opval = ParseOperand(opstr);
							WriteOpL(0x5C, opval);
							break;
						}
					}
					InvalidAddressingMode();
				}

				break;

			case OpWdc.JMP:
				if (opstr.IsEmpty) {
					MissingOperand();
					PlaceholderOp(0x4C, 2);
				} else if (opstr.OnlyCharIs(JNOP)) {
					WriteOpW(0x4C, Provenance + 3);
				} else {
					if (opstr.First is IndirectOpen) {
						if (opstr.Last is IndirectClose) {
							var jmid = opstr.SliceBothEnds(1, 1);

							if (jmid.HasIndexer(IndexToken65816, out char jind, out OperandString jindstr)) {
								if (jind.CiIs(XRegister)) {
									opval = ParseOperand(jindstr);

									if (size is SizeToken.W) {
										WriteOpW(0x7C, opval);
										break;
									} else if (size is SizeToken.Unspecified) {
										WriteOpJ(0x7C, opval);
										break;
									}
								}
							} else {
								opval = ParseOperand(jmid);

								if (size is SizeToken.W) {
									WriteOpW(0x6C, opval);
									break;
								} else if (size is SizeToken.Unspecified) {
									WriteOpWBank00(0x6C, opval);
									break;
								}
							}
						}
					} else {
						opval = ParseOperand(opstr);

						if (size is SizeToken.W or SizeToken.Unspecified) {
							WriteOpW(0x4C, opval);
							break;
						}
					}

					InvalidAddressingMode();
					PlaceholderOp(0x4C, 2);
				}

				break;

			case OpWdc.JSL:
				if (opstr.IsEmpty) {
					MissingOperand();
					PlaceholderOp(0x22, 3);
				} else if (size is SizeToken.Unspecified or SizeToken.L) {
					WriteOpL(0x22, ParseOperand(opstr));
				} else {
					InvalidAddressingMode();
					PlaceholderOp(0x22, 3);
				}

				break;

			case OpWdc.JSR:
				if (opstr.IsEmpty) {
					MissingOperand();
					PlaceholderOp(0x20, 2);
					break;
				} else {
					if (opstr.First is IndirectOpen) {
						if (opstr.Last is IndirectClose) {
							var jmid = opstr.SliceBothEnds(1, 1);

							if (jmid.HasIndexer(IndexToken65816, out char jind, out OperandString jindstr)) {
								if (jind.CiIs(XRegister)) {
									opval = ParseOperand(jindstr);

									if (size is SizeToken.W) {
										WriteOpW(0xFC, opval);
										break;
									} else if (size is SizeToken.Unspecified) {
										WriteOpJ(0xFC, opval);
										break;
									}
								}
							}
						}
					} else {
						opval = ParseOperand(opstr);

						if (size is SizeToken.W) {
							WriteOpW(0x20, opval);
							break;
						} else if (size is SizeToken.Unspecified) {
							WriteOpJ(0x20, opval);
							break;
						}
					}
				}

				InvalidAddressingMode();
				PlaceholderOp(0x20, 2);
				break;

			case OpWdc.LDA:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0xA1, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0xA3, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xA5, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0xA7, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xA9, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xA9, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xAD, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0xAF, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0xB1, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0xB2, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0xB3, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0xB5, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0xB7, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0xB9, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0xBD, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0xBF, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.LDX:
				(mode, opval) = CheckIndexerLoadModes(opstr, size, YRegister);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xA6, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xA2, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xA2, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xAE, opval); break;
					case AddressingMode.DirectPageIndexedY: /*                                     */ WriteOpB(0xB6, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0xBE, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.LDY:
				(mode, opval) = CheckIndexerLoadModes(opstr, size, XRegister);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xA4, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xA0, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xA0, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xAC, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0xB4, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0xBC, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.LSR:
				if (size is SizeToken.Unspecified && RepeatIfRepeatable(opstr, 0x4A)) break;

				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x46, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x4E, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x56, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x5E, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.MVN: /*                                                                        */ NoTokens(size); AssembleBlockMove(opstr, 0x54); break;
			case OpWdc.MVP: /*                                                                        */ NoTokens(size); AssembleBlockMove(opstr, 0x44); break;

			case OpWdc.NOP: /*                                                                        */ AssembleRepeatable(opstr, size, 0xEA); break;

			case OpWdc.ORA:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0x01, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0x03, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x05, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0x07, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0x09, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0x09, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x0D, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0x0F, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0x11, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0x12, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0x13, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x15, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0x17, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0x19, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x1D, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0x1F, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.PEA:
				AggroDotW(size);

				if (opstr.IsEmpty) {
					MissingOperand();
					PlaceholderOp(0xF4, 2);
				} else {
					WriteOpW(0xF4, ParseOperand(opstr));
				}

				break;

			case OpWdc.PEI:
				AggroDotB(size);

				if (opstr.IsEmpty) {
					MissingOperand();
					PlaceholderOp(0xD4, 1);
				} else if (opstr.First is IndirectOpen && opstr.Last is IndirectClose) {
					WriteOpB(0xD4, ParseOperandSliced(opstr, 1, 1));
				} else {
					InvalidAddressingMode();
					PlaceholderOp(0xD4, 1);
				}

				break;

			case OpWdc.PER: /*                                                                        */ NoTokens(size); AssembleBranchLong(opstr, 0x62); break;

			case OpWdc.PHA: /*                                                                        */ AssembleImplied(opstr, size, 0x48); break;
			case OpWdc.PHB: /*                                                                        */ AssembleImplied(opstr, size, 0x8B); break;
			case OpWdc.PHD: /*                                                                        */ AssembleImplied(opstr, size, 0x0B); break;
			case OpWdc.PHK: /*                                                                        */ AssembleImplied(opstr, size, 0x4B); break;
			case OpWdc.PHP: /*                                                                        */ AssembleImplied(opstr, size, 0x08); break;
			case OpWdc.PHX: /*                                                                        */ AssembleImplied(opstr, size, 0xDA); break;
			case OpWdc.PHY: /*                                                                        */ AssembleImplied(opstr, size, 0x5A); break;
			case OpWdc.PLA: /*                                                                        */ AssembleImplied(opstr, size, 0x68); break;
			case OpWdc.PLB: /*                                                                        */ AssembleImplied(opstr, size, 0xAB); break;
			case OpWdc.PLD: /*                                                                        */ AssembleImplied(opstr, size, 0x2B); break;
			case OpWdc.PLP: /*                                                                        */ AssembleImplied(opstr, size, 0x28); break;
			case OpWdc.PLX: /*                                                                        */ AssembleImplied(opstr, size, 0xFA); break;
			case OpWdc.PLY: /*                                                                        */ AssembleImplied(opstr, size, 0x7A); break;

			case OpWdc.REP: /*                                                                        */ AssemblePFlagChange(opstr, size, 0xC2); break;

			case OpWdc.ROL:
				if (size is SizeToken.Unspecified && RepeatIfRepeatable(opstr, 0x2A)) break;

				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x26, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x2E, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x36, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x3E, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.ROR:
				if (size is SizeToken.Unspecified && RepeatIfRepeatable(opstr, 0x6A)) break;

				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x66, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x6E, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x76, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x7E, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.RTI: /*                                                                        */ AssembleImplied(opstr, size, 0x40); break;
			case OpWdc.RTL: /*                                                                        */ AssembleImplied(opstr, size, 0x6B); break;
			case OpWdc.RTS: /*                                                                        */ AssembleImplied(opstr, size, 0x60); break;

			case OpWdc.SBC:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0xE1, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0xE3, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0xE5, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0xE7, opval); break;
					case AddressingMode.Immediate8: /*                                             */ WriteOpB(0xE9, opval); break;
					case AddressingMode.Immediate16: /*                                            */ WriteOpW(0xE9, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0xED, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0xEF, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0xF1, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0xF2, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0xF3, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0xF5, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0xF7, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0xF9, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0xFD, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0xFF, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.SEC: /*                                                                        */ AssembleImplied(opstr, size, 0x38); break;
			case OpWdc.SED: /*                                                                        */ AssembleImplied(opstr, size, 0xF8); break;
			case OpWdc.SEI: /*                                                                        */ AssembleImplied(opstr, size, 0x78); break;

			case OpWdc.SEP: /*                                                                        */ AssemblePFlagChange(opstr, size, 0xE2); break;

			case OpWdc.STA:
				(mode, opval) = CheckStandardAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPageIndirectIndexedX: /*                             */ WriteOpB(0x81, opval); break;
					case AddressingMode.StackRelative: /*                                          */ WriteOpB(0x83, opval); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x85, opval); break;
					case AddressingMode.DirectPageIndirectLong: /*                                 */ WriteOpB(0x87, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x8D, opval); break;
					case AddressingMode.AbsoluteLong: /*                                           */ WriteOpL(0x8F, opval); break;
					case AddressingMode.DirectPageIndirectIndexedY: /*                             */ WriteOpB(0x91, opval); break;
					case AddressingMode.DirectPageIndirect: /*                                     */ WriteOpB(0x92, opval); break;
					case AddressingMode.StackRelativeIndirectIndexedY: /*                          */ WriteOpB(0x93, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x95, opval); break;
					case AddressingMode.DirectPageIndirectLongIndexedY: /*                         */ WriteOpB(0x97, opval); break;
					case AddressingMode.AbsoluteIndexedY: /*                                       */ WriteOpW(0x99, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x9D, opval); break;
					case AddressingMode.AbsoluteLongIndexedX: /*                                   */ WriteOpL(0x9F, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}
				break;

			case OpWdc.STP: /*                                                                        */ AssembleImplied(opstr, size, 0xDB); break;

			case OpWdc.STX:
				(mode, opval) = CheckIndexerStoreModes(opstr, size, YRegister);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x86, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x8E, opval); break;
					case AddressingMode.DirectPageIndexedY: /*                                     */ WriteOpB(0x96, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.STY:
				(mode, opval) = CheckIndexerStoreModes(opstr, size, XRegister);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x84, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x8C, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x94, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpWdc.STZ:
				(mode, opval) = CheckStricterAccumulatorModes(opstr, size);
				switch (mode) {
					case AddressingMode.Empty: /*                                                  */ MissingOperand(); break;
					case AddressingMode.DirectPage: /*                                             */ WriteOpB(0x64, opval); break;
					case AddressingMode.Absolute: /*                                               */ WriteOpW(0x9C, opval); break;
					case AddressingMode.DirectPageIndexedX: /*                                     */ WriteOpB(0x74, opval); break;
					case AddressingMode.AbsoluteIndexedX: /*                                       */ WriteOpW(0x9E, opval); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}
				break;

			case OpWdc.TAX: /*                                                                        */ AssembleImplied(opstr, size, 0xAA); break;
			case OpWdc.TAY: /*                                                                        */ AssembleImplied(opstr, size, 0xA8); break;
			case OpWdc.TCD: /*                                                                        */ AssembleImplied(opstr, size, 0x5B); break;
			case OpWdc.TCS: /*                                                                        */ AssembleImplied(opstr, size, 0x1B); break;
			case OpWdc.TDC: /*                                                                        */ AssembleImplied(opstr, size, 0x7B); break;

			case OpWdc.TRB: /*                                                                        */ AssembleBitTests(opstr, size, 0x10); break;
			case OpWdc.TSB: /*                                                                        */ AssembleBitTests(opstr, size, 0x00); break;

			case OpWdc.TSC: /*                                                                        */ AssembleImplied(opstr, size, 0x3B); break;
			case OpWdc.TSX: /*                                                                        */ AssembleImplied(opstr, size, 0xBA); break;
			case OpWdc.TXA: /*                                                                        */ AssembleImplied(opstr, size, 0x8A); break;
			case OpWdc.TXS: /*                                                                        */ AssembleImplied(opstr, size, 0x9A); break;
			case OpWdc.TXY: /*                                                                        */ AssembleImplied(opstr, size, 0x9B); break;
			case OpWdc.TYA: /*                                                                        */ AssembleImplied(opstr, size, 0x98); break;
			case OpWdc.TYX: /*                                                                        */ AssembleImplied(opstr, size, 0xBB); break;

			case OpWdc.WAI: /*                                                                        */ AssembleImplied(opstr, size, 0xCB); break;

			case OpWdc.WDM: /*                                                                        */ AssembleSignature(opstr, size, 0x42); break;

			case OpWdc.XBA: /*                                                                        */ AssembleRepeatable(opstr, size, 0xEB); break;
			case OpWdc.XCE: /*                                                                        */ AssembleImplied(opstr, size, 0xFB); break;
		}

		return;

		(AddressingMode, IExpressionReturn) HandleImmediate(OperandString argstr, SizeToken argsize) {
			return PickBW(argstr.Slice(1), argsize, AddressingMode.Immediate8, AddressingMode.Immediate16);
		}

		(AddressingMode, IExpressionReturn) PickBWL(OperandString argstr, SizeToken argsize, AddressingMode if8, AddressingMode if16, AddressingMode if24) {
			if (argsize is SizeToken.Unspecified) {
				AmbiguousSizeTokens();

				IExpressionReturn argval = ParseOperand(argstr);

				if (argval.Resolved) {
					int ov = argval.ValueInt32;

					// TODO add resolution for data/program bank and mirrors (program bank probably needs separate routine from data bank)

					if (SnesHelpers.FitsIn8(ov)) {
						return (if8, argval);

					} else if (SnesHelpers.FitsIn16(ov)) {
						return (if16, argval);

					} else {
						return (if24, argval);

					}
				} else {
					return (if24, argval);

				}
			} else if (argsize is SizeToken.B) {
				return (if8, ParseOperand(argstr));

			} else if (argsize is SizeToken.W) {
				return (if16, ParseOperand(argstr));

			} else if (argsize is SizeToken.L) {
				return (if24, ParseOperand(argstr));

			} else {
				return (AddressingMode.InvalidBecauseToken, MathHelpers.InvalidExpression);

			}
		}

		(AddressingMode, IExpressionReturn) PickBW(OperandString argstr, SizeToken argsize, AddressingMode if8, AddressingMode if16) {
			if (argsize is SizeToken.Unspecified) {

				IExpressionReturn argval = ParseOperand(argstr);

				AmbiguousSizeTokens();

				if (argval.Resolved && SnesHelpers.FitsIn8(argval.ValueInt32)) {
					return (if8, argval);
				} else {
					return (if16, argval);
				}

			} else if (argsize is SizeToken.B) {
				return (if8, ParseOperand(argstr));
			} else if (argsize is SizeToken.W) {
				return (if16, ParseOperand(argstr));
			} else {
				return (AddressingMode.InvalidBecauseToken, MathHelpers.InvalidExpression);
			}
		}

		/***********************************************************************\
		 * ADC, AND, CMP, EOR, ORA, LDA, SBC - checks immediate first
		 * STA
		 ***********************************************************************
		 *               i,S      (i,S),Y
		 *       dp       dp,x     abs      abs,X    abs,Y    long     long,X
		 *       (dp)     (dp,X)   (dp),Y   [dp]     [dp],Y
		\***********************************************************************/
		(AddressingMode, IExpressionReturn) CheckStandardAccumulatorModes(OperandString argstr, SizeToken argsize) {
			if (argstr.IsEmpty) {
				return (AddressingMode.Empty, MathHelpers.InvalidExpression);
			}

			char firstChar = argstr.GetUnchecked(0);

			if (firstChar is '#') {
				return HandleImmediate(argstr, argsize);
			}

			// check for indexers
			if (argstr.HasIndexer(IndexToken65816, out char ind, out OperandString indstr)) {
				ind = ind.FastUpper();

				if (ind is XRegister) {
					// it must be dp, abs, or long
					return PickBWL(indstr, argsize, AddressingMode.DirectPageIndexedX, AddressingMode.AbsoluteIndexedX, AddressingMode.AbsoluteLongIndexedX);
				} else if (ind is YRegister) {
					char paren = indstr.First;

					// check for indirect
					if (paren is IndirectOpen) {
						if (indstr.Last is IndirectClose) {
							var opsmid = indstr.SliceBothEnds(1, 1);

							// (i,S),Y
							if (opsmid.HasSpecificIndexer(IndexToken65816, StackRelative, out OperandString indsy)) {
								NoTokens(argsize);
								return (AddressingMode.StackRelativeIndirectIndexedY, ParseOperand(indsy));
							} else {
								// (addr)
								AggroDotB(argsize);
								return (AddressingMode.DirectPageIndirectIndexedY, ParseOperand(opsmid));
							}
						}
					} else if (paren is IndirectLongOpen) {
						if (indstr.Last is IndirectLongClose) {
							AggroDotB(argsize);
							return (AddressingMode.DirectPageIndirectLongIndexedY, ParseOperandSliced(indstr, 1, 1));
						}
					}

					AggroDotW(argsize);
					return (AddressingMode.AbsoluteIndexedY, ParseOperand(indstr));

				} else if (ind is StackRelative) {
					NoTokens(argsize);
					return (AddressingMode.StackRelative, ParseOperand(indstr));
				}
			} else {
				// if it has no indexers, then it's either indirect or "normal"
				if (firstChar is IndirectOpen) {
					if (argstr.Last is IndirectClose) {
						var indr = argstr.SliceBothEnds(1, 1);

						AggroDotB(argsize);

						if (indr.HasSpecificIndexer(IndexToken65816, XRegister, out OperandString indxind)) {
							return (AddressingMode.DirectPageIndirectIndexedX, ParseOperand(indxind));
						} else {
							return (AddressingMode.DirectPageIndirect, ParseOperand(indr));
						}
					}
				} else if (firstChar is IndirectLongOpen) {
					if (argstr.Last is IndirectLongClose) {
						AggroDotB(argsize);
						return (AddressingMode.DirectPageIndirectLong, ParseOperandSliced(argstr, 1, 1));
					}
				}

				return PickBWL(argstr, argsize, AddressingMode.DirectPage, AddressingMode.Absolute, AddressingMode.AbsoluteLong);
			}

			return (AddressingMode.Invalid, MathHelpers.InvalidExpression);
		}

		/***********************************************************************\
		 * ASL, LSR, ROL, ROR, DEC, INC - checks implicit/repeating first
		 * BIT - checks immediate first
		 * STZ
		 ***********************************************************************
		 *       dp       abs      dp,X     abs,X
		\***********************************************************************/
		(AddressingMode, IExpressionReturn) CheckStricterAccumulatorModes(OperandString argstr, SizeToken argsize) {
			if (argstr.IsEmpty) {
				return (AddressingMode.Empty, MathHelpers.InvalidExpression);
			}

			// dp,X or abs,X
			if (argstr.HasSpecificIndexer(IndexToken65816, XRegister, out OperandString indx)) {
				return PickBW(indx, argsize, AddressingMode.DirectPageIndexedX, AddressingMode.AbsoluteIndexedX);
			} else {
				// otherwise it's dp or abs
				return PickBW(argstr, argsize, AddressingMode.DirectPage, AddressingMode.Absolute);
			}
		}

		/***********************************************************************\
		 * TRB, TSB
		 ***********************************************************************
		 *       dp       abs
		\***********************************************************************/
		void AssembleBitTests(OperandString argstr, SizeToken argsize, byte opcode) {
			if (argstr.IsEmpty) {
				MissingOperand();
			} else {
				var (argmode, argval) = PickBW(argstr, argsize, AddressingMode.DirectPage, AddressingMode.Absolute);

				if (argmode is AddressingMode.DirectPage) {
					WriteOpB((byte) (opcode | 0x04), argval);
				} else if (argmode is AddressingMode.Absolute) {
					WriteOpW((byte) (opcode | 0x0C), argval);
				} else {
					InvalidAddressingMode();
				}
			}
		}

		// #i       dp       abs
		(AddressingMode, IExpressionReturn) CheckLowerMemoryModesAndImmediate(OperandString argstr, SizeToken argsize) {
			if (argstr.IsEmpty) {
				return (AddressingMode.Empty, MathHelpers.InvalidExpression);
			}

			if (argstr.GetUnchecked(0) is ImmediateMode) {
				return HandleImmediate(argstr, argsize);
			} else {
				return PickBW(argstr, argsize, AddressingMode.DirectPage, AddressingMode.Absolute);
			}
		}

		// #i       dp       abs      dp,Y     abs,Y
		(AddressingMode, IExpressionReturn) CheckIndexerLoadModes(OperandString argstr, SizeToken argsize, char allowedIndexer) {
			if (argstr.IsEmpty) {
				return (AddressingMode.Empty, MathHelpers.InvalidExpression);
			}

			if (argstr.GetUnchecked(0) is ImmediateMode) {
				return HandleImmediate(argstr, argsize);
			}

			if (argstr.HasIndexer(IndexToken65816, out char reg, out OperandString indstr)) {
				if (allowedIndexer is XRegister) {
					if (reg.CiIs(XRegister)) {
						return PickBW(indstr, argsize, AddressingMode.DirectPageIndexedX, AddressingMode.AbsoluteIndexedX);
					}
				} else /* if (allowedIndexer is YRegister) */ {
					if (reg.CiIs(YRegister)) {
						return PickBW(indstr, argsize, AddressingMode.DirectPageIndexedY, AddressingMode.AbsoluteIndexedY);
					}
				}
			} else {
				return PickBW(argstr, argsize, AddressingMode.DirectPage, AddressingMode.Absolute);
			}

			return (AddressingMode.Invalid, MathHelpers.InvalidExpression);
		}

		// dp       abs      dp,Y
		(AddressingMode, IExpressionReturn) CheckIndexerStoreModes(OperandString argstr, SizeToken argsize, char allowedIndexer) {
			if (argstr.IsEmpty) {
				return (AddressingMode.Empty, MathHelpers.InvalidExpression);
			}

			if (argstr.HasIndexer(IndexToken65816, out char reg, out OperandString indstr)) {
				if (reg.CiIs(allowedIndexer)) {
					AggroDotB(argsize);

					if (allowedIndexer is XRegister) {
						return (AddressingMode.DirectPageIndexedX, ParseOperand(indstr));
					} else {
						return (AddressingMode.DirectPageIndexedY, ParseOperand(indstr));
					}
				}
			} else {
				return PickBW(argstr, argsize, AddressingMode.DirectPage, AddressingMode.Absolute);
			}

			return (AddressingMode.Invalid, MathHelpers.InvalidExpression);
		}

		void AssembleRepeatable(OperandString argstr, SizeToken argsize, byte opcode) {
			if (argsize is not SizeToken.Unspecified) {
				TokensOnImplied();
				PlaceholderOp(opcode);
			}

			if (argstr.IsEmpty) {
				WriteOpImplied(opcode);
			} else if (argstr.GetUnchecked(0) is ImmediateMode) {
				DoRepeating(opcode, argstr);
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode);
			}
		}

		bool RepeatIfRepeatable(OperandString argstr, byte opcode) {
			// if no operand, then we're good to just write
			int len = argstr.Length;

			if (len is 0) {
				WriteOpImplied(opcode);
				return true;
			}

			char c = argstr.GetUnchecked(0);

			// special case for explicitly addressing the accumulator
			if (c.CiIs(ARegister) && len is 1) {
				WriteOpImplied(opcode);
				return true;
			} else if (c is RepeatOp) {
				DoRepeating(opcode, argstr);
				return true;
			}

			return false;
		}

		void DoRepeating(byte opcode, OperandString argstr) {
			IExpressionReturn rest = ParseOperandSliced(argstr, 1);

			if (rest.Resolved) {
				int count = rest.ValueInt32;

				if (count < 1) {
					Error("Repeatable instruction operand must be >= 1");
				} else {
					if (TryAdvanceProgramCounter(count)) {
						SnesHelpers.FillBlockB(_pcPtr, opcode, count);
						TrackAndStep(count);
					}
				}
			} else {
				Error_BadResolve(rest, "Repeatable instruction operand must be immediately resolvable.");
			}
		}

		// BRL, PER
		void AssembleBranchLong(OperandString argstr, byte opcode) {
			int len = argstr.Length;

			if (len is 0) {
				MissingOperand();
				PlaceholderOp(opcode, 1);
			} else {
				char c = argstr.GetUnchecked(0);

				// check for special characters
				if (len is 1) {
					// no reason to implement any of these on PER
					// and because these instructions are kinda bad
					// we'll let the performance take the picosecond hit
					// for the extra comparison here
					if (opcode is 0x82) {
						if (c is BROP) {
							if (TryAdvanceProgramCounter(3)) {
								*_pcPtr = opcode;
								// No operand write
								TrackAndStep3();
							}
							return;
						} else if (c is JNOP) {
							WriteOpW(opcode, 0x0000);
							return;
						} else if (c is BTYS) {
							WriteOpW(opcode, 0xFFFD);
							return;
						}
					}
				}

				if (c is BranchLiteral) {
					WriteOpW(opcode, ParseOperandSliced(argstr, 1));
				} else {
					WriteOpRel16(opcode, ParseOperand(argstr));
				}
			}
		}

		/***********************************************************************\
		 * BRK, COP, WDM
		 ***********************************************************************
		 *       #i - optional
		\***********************************************************************/
		void AssembleSignature(OperandString argstr, SizeToken argsize, byte opcode) {
			if (argstr.IsEmpty) {
				WriteOpB(opcode, 0);
			} else if (argstr.GetUnchecked(0) is not ImmediateMode) {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 1);
			} else {
				OnlyDotB(argsize);
				WriteOpB(opcode, ParseOperandSliced(argstr, 1));
			}
		}

		void AssemblePFlagChange(OperandString argstr, SizeToken argsize, byte opcode) {
			NoTokens(argsize);

			int len = argstr.Length;

			if (len is 0) {
				MissingOperand();
				PlaceholderOp(opcode, 1);
			} else if (argstr.GetUnchecked(0) is ImmediateMode) {
				if (argstr[1] is '[' && argstr.Last is ']') {
					byte flags = 0;
					var flg = argstr.SliceBothEnds(2, 1).AsSpan();

					foreach (var f in flg) {
						switch (f) {
							case 'N' or 'n': flags |= 0x80; break;
							case 'V' or 'v': flags |= 0x40; break;
							case 'M' or 'm': flags |= 0x20; break;
							case 'X' or 'x': flags |= 0x10; break;
							case 'D' or 'd': flags |= 0x08; break;
							case 'I' or 'i': flags |= 0x04; break;
							case 'Z' or 'z': flags |= 0x02; break;
							case 'C' or 'c': flags |= 0x01; break;
							default:
								Error($"Invalid flag character: {f}");
								flags = 0;
								goto StopFlags;
						}
					}

					StopFlags:
					WriteOpB(opcode, flags);
				} else {
					WriteOpB(opcode, ParseOperandSliced(argstr, 1));
				}
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 1);
			}
		}

		void AssembleBlockMove(OperandString argstr, byte opcode) {
			if (argstr.SplitByComma(out var sourceBank, out var destBank)) {
				WriteOpBB(opcode, ParseOperand(destBank), ParseOperand(sourceBank));
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 2);
			}
		}
	}
	private static OpWdc TestFor65816(OperandString chars) {
		// if it's not 3 letters, it's not a 65xx instruction
		if (chars.Length is not 3) {
			// unless...
			if (chars.Length is 4) {
				return *(ulong*) chars.Start == 0x0052_0053_004A_0042 ? OpWdc.JSL : OpWdc.NotGood;
			}

			return OpWdc.NotGood;
		}

		char* reading = chars.Start;

		// these provide very fast case-insensitivity
		// it ruins non-letters, but none of these mnemonics have anything except letters
		char a = reading[0].FastUpper();
		char b = reading[1].FastUpper();
		char c = reading[2].FastUpper();

		// we'll be adding every letter, just to encourage the compiler to make a jump table
		switch (a) {
			case 'A':
				if (b is 'N') return SelectC1('D', OpWdc.AND);
				if (b is 'D') return SelectC1('C', OpWdc.ADC);
				if (b is 'S') return SelectC1('L', OpWdc.ASL);

				break;

			case 'B':
				switch (b) {
					case 'C': return SelectC2('C', OpWdc.BCC, 'S', OpWdc.BCS);
					case 'E': return SelectC1('Q', OpWdc.BEQ);
					case 'N': return SelectC1('E', OpWdc.BNE);
					case 'P': return SelectC1('L', OpWdc.BPL);
					case 'M': return SelectC1('I', OpWdc.BMI);
					case 'I': return SelectC1('T', OpWdc.BIT);
					case 'V': return SelectC2('C', OpWdc.BVC, 'S', OpWdc.BVS);
					case 'R': return SelectC3('A', OpWdc.BRA, 'L', OpWdc.BRL, 'K', OpWdc.BRK);

					// encourage compiler to make a jump table
					case 'Z': return OpWdc.NotGood;
					case 'S': return OpWdc.NotGood;
					case 'L': return OpWdc.NotGood;
					case 'D': break; // shuts up the switch expression suggestion
				}

				break;

			case 'C':
				if (b is 'M') return SelectC1('P', OpWdc.CMP);

				if (b is 'L') {
					if (c is 'C') return OpWdc.CLC;
					if (c is 'V') return OpWdc.CLV;
					if (c is 'D') return OpWdc.CLD;
					if (c is 'I') return OpWdc.CLI;

				} else if (b is 'P') {
					return SelectC2('X', OpWdc.CPX, 'Y', OpWdc.CPY);

				} else if (b is 'O') {
					return SelectC1('P', OpWdc.COP);
				}

				break;

			case 'D':
				if (b is 'E') return SelectC3('C', OpWdc.DEC, 'X', OpWdc.DEX, 'Y', OpWdc.DEY);

				break;

			case 'E':
				if ((b, c) is ('O', 'R')) return OpWdc.EOR;

				break;

			case 'F': break;
			case 'G': break;
			case 'H': break;

			case 'I':
				if (b is 'N') return SelectC3('C', OpWdc.INC, 'X', OpWdc.INX, 'Y', OpWdc.INY);

				break;

			case 'J':
				if (b is 'S') return SelectC2('R', OpWdc.JSR, 'L', OpWdc.JSL);
				if (b is 'M') return SelectC2('P', OpWdc.JMP, 'L', OpWdc.JML);

				break;

			case 'K': break;

			case 'L':
				if (b is 'D') return SelectC3('A', OpWdc.LDA, 'X', OpWdc.LDX, 'Y', OpWdc.LDY);
				if (b is 'S') return SelectC1('R', OpWdc.LSR);

				break;

			case 'M':
				if (b is 'V') return SelectC2('N', OpWdc.MVN, 'P', OpWdc.MVP);

				break;

			case 'N':
				if ((b, c) is ('O', 'P')) return OpWdc.NOP;

				break;

			case 'O':
				if ((b, c) is ('R', 'A')) return OpWdc.ORA;

				break;

			case 'P':
				// TODO profile this versus jump table (get instruction counts by adding a dictionary to the handler for everything to know how to profile)
				if (b is 'H') {
					if (c is 'P') return OpWdc.PHP;
					if (c is 'A') return OpWdc.PHA;
					if (c is 'X') return OpWdc.PHX;
					if (c is 'Y') return OpWdc.PHY;
					if (c is 'B') return OpWdc.PHB;
					if (c is 'K') return OpWdc.PHK;
					if (c is 'D') return OpWdc.PHD;

				} else if (b is 'L') {
					if (c is 'P') return OpWdc.PLP;
					if (c is 'A') return OpWdc.PLA;
					if (c is 'X') return OpWdc.PLX;
					if (c is 'Y') return OpWdc.PLY;
					if (c is 'B') return OpWdc.PLB;
					if (c is 'D') return OpWdc.PLD;

				} else if (b is 'E') {
					if (c is 'I') return OpWdc.PEI;
					if (c is 'A') return OpWdc.PEA;
					if (c is 'R') return OpWdc.PER;
				}

				break;

			case 'Q': break;

			case 'R':
				if (b is 'T') return SelectC3('S', OpWdc.RTS, 'L', OpWdc.RTL, 'I', OpWdc.RTI);
				if (b is 'E') return SelectC1('P', OpWdc.REP);
				if (b is 'O') return SelectC2('L', OpWdc.ROL, 'R', OpWdc.ROR);

				break;

			case 'S':
				if (b is 'T') {
					if (c is 'A') return OpWdc.STA;
					if (c is 'X') return OpWdc.STX;
					if (c is 'Y') return OpWdc.STY;
					if (c is 'Z') return OpWdc.STZ;
					if (c is 'P') return OpWdc.STP;

				} else if (b is 'E') {
					if (c is 'C') return OpWdc.SEC;
					if (c is 'P') return OpWdc.SEP;
					if (c is 'D') return OpWdc.SED;
					if (c is 'I') return OpWdc.SEI;

				} else if (b is 'B') { // b=B, c=C - funny
					if (c is 'C') return OpWdc.SBC;
				}

				break;

			case 'T':
				if (b is 'A') return SelectC2('X', OpWdc.TAX, 'Y', OpWdc.TAY);
				if (b is 'X') return SelectC3('A', OpWdc.TXA, 'Y', OpWdc.TXY, 'S', OpWdc.TXS);
				if (b is 'Y') return SelectC2('A', OpWdc.TYA, 'X', OpWdc.TYX);
				if (b is 'S') return SelectC3('B', OpWdc.TSB, 'C', OpWdc.TSC, 'X', OpWdc.TSX);
				if (b is 'R') return SelectC1('B', OpWdc.TRB);
				if (b is 'C') return SelectC2('D', OpWdc.TCD, 'S', OpWdc.TCS);
				if (b is 'D') return SelectC1('C', OpWdc.TDC);

				break;

			case 'U': break;
			case 'V': break;

			case 'W':
				if (b is 'D') return SelectC1('M', OpWdc.WDM);
				if (b is 'A') return SelectC1('I', OpWdc.WAI);

				break;

			case 'X':
				if (b is 'B') return SelectC1('A', OpWdc.XBA);
				if (b is 'C') return SelectC1('E', OpWdc.XCE);

				break;
		}

		return OpWdc.NotGood;

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		OpWdc SelectC1(char c1, OpWdc op1) {
			return c == c1 ? op1 : OpWdc.NotGood;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		OpWdc SelectC2(char c1, OpWdc op1, char c2, OpWdc op2) {
			if (c == c1) return op1;
			if (c == c2) return op2;

			return OpWdc.NotGood;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		OpWdc SelectC3(char c1, OpWdc op1, char c2, OpWdc op2, char c3, OpWdc op3) {
			if (c == c1) return op1;
			if (c == c2) return op2;
			if (c == c3) return op3;

			return OpWdc.NotGood;
		}
	}
}

file enum AddressingMode : int {
	NotGiven = 0,
	Empty,
	Invalid,
	InvalidBecauseToken,

	Implied,
	ExplicitlyAccumulator,

	ImmediateUnknown,
	AddressUnknown,
	IndexedXUnknown,
	IndexedYUnknown,

	Immediate8,
	Immediate16,

	Immediate8Assumed,
	Immediate16Assumed,

	DirectPage,
	DirectPageIndexedX,
	DirectPageIndexedY,
	DirectPageIndirectLong,
	DirectPageIndirectLongIndexedY,

	DirectPageIndirect,
	DirectPageIndirectIndexedY,
	DirectPageIndirectIndexedX,

	Absolute,
	AbsoluteIndexedX,
	AbsoluteIndexedY,
	AbsoluteIndirect,
	AbsoluteIndirectIndexedX,
	AbsoluteIndirectLong,

	AbsoluteLong,
	AbsoluteLongIndexedX,

	StackRelative,
	StackRelativeIndirectIndexedY,

	Relative,
	RelativeLong,

	MoveBlockBanks,
}
