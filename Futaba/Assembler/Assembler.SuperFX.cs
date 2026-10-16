namespace Futaba;

unsafe partial class Assembler {
	private void AssembleSuperFX(OpSFX instruction) {
		SizeToken size = GetSizeToken();

		if (size is SizeToken.Invalid) {
			AbortCommand();
			return;
		}

		ReadRestOfCommand(out OperandString opstr);

		int len = opstr.Length;

		// BOOK 2 PAGE 160
		// normally i'd prefer to keep these in alphabetical order
		// but it just feels more readable this way
		switch (instruction) {
			case OpSFX.FROM: /*                                                                    */ AssembleGetRegister(opstr, 0xB0); break;
			case OpSFX.TO: /*                                                                      */ AssembleGetRegister(opstr, 0x10); break;
			case OpSFX.WITH: /*                                                                    */ AssembleGetRegister(opstr, 0x20); break;

			case OpSFX.ALT1: /*                                                                    */ AssembleImplied(opstr, size, 0x3D); break;
			case OpSFX.ALT2: /*                                                                    */ AssembleImplied(opstr, size, 0x3E); break;
			case OpSFX.ALT3: /*                                                                    */ AssembleImplied(opstr, size, 0x3F); break;

			case OpSFX.NOP: /*                                                                     */ AssembleImplied(opstr, size, 0x01); break;

			case OpSFX.ADC: /*                                                                     */ AssembleIorR(opstr, 0x50, shortRn: false, allowZero: true); break;
			case OpSFX.ADD: /*                                                                     */ AssembleIorR(opstr, 0x50, shortRn: true, allowZero: true); break;
			case OpSFX.SUB: /*                                                                     */ AssembleIorR(opstr, 0x60, shortRn: true, allowZero: true); break;
			case OpSFX.AND: /*                                                                     */ AssembleIorR(opstr, 0x70, shortRn: true, allowZero: false); break;
			case OpSFX.BIC: /*                                                                     */ AssembleIorR(opstr, 0x70, shortRn: false, allowZero: false); break;
			case OpSFX.OR: /*                                                                      */ AssembleIorR(opstr, 0xC0, shortRn: true, allowZero: false); break;
			case OpSFX.XOR: /*                                                                     */ AssembleIorR(opstr, 0xC0, shortRn: false, allowZero: false); break;

			case OpSFX.NOT: /*                                                                     */ AssembleImplied(opstr, size, 0x4F); break;
			case OpSFX.ASR: /*                                                                     */ AssembleImplied(opstr, size, 0x96); break;
			case OpSFX.LSR: /*                                                                     */ AssembleImplied(opstr, size, 0x03); break;
			case OpSFX.ROL: /*                                                                     */ AssembleImplied(opstr, size, 0x04); break;
			case OpSFX.ROR: /*                                                                     */ AssembleImplied(opstr, size, 0x97); break;

			case OpSFX.DIV2: /*                                                                    */ AssembleLongOp(0x3D, 0x96); break;
			case OpSFX.FMULT: /*                                                                   */ AssembleImplied(opstr, size, 0x9F); break;
			case OpSFX.LMULT: /*                                                                   */ AssembleLongOp(0x3D, 0x9F); break;
			case OpSFX.UMULT: /*                                                                   */ AssembleIorR(opstr, 0x80, shortRn: false, allowZero: true); break;
			case OpSFX.MULT: /*                                                                    */ AssembleIorR(opstr, 0x80, shortRn: true, allowZero: true); break;

			case OpSFX.HIB: /*                                                                     */ AssembleImplied(opstr, size, 0xC0); break;
			case OpSFX.LOB: /*                                                                     */ AssembleImplied(opstr, size, 0x9E); break;
			case OpSFX.MERGE: /*                                                                   */ AssembleImplied(opstr, size, 0x70); break;
			case OpSFX.SBK: /*                                                                     */ AssembleImplied(opstr, size, 0x90); break;
			case OpSFX.SEX: /*                                                                     */ AssembleImplied(opstr, size, 0x95); break;
			case OpSFX.SWAP: /*                                                                    */ AssembleImplied(opstr, size, 0x4D); break;

			case OpSFX.BCC: /*                                                                     */ AssembleBranch(opstr, 0x0C); break;
			case OpSFX.BCS: /*                                                                     */ AssembleBranch(opstr, 0x0D); break;
			case OpSFX.BEQ: /*                                                                     */ AssembleBranch(opstr, 0x09); break;
			case OpSFX.BGE: /*                                                                     */ AssembleBranch(opstr, 0x06); break;
			case OpSFX.BLT: /*                                                                     */ AssembleBranch(opstr, 0x07); break;
			case OpSFX.BMI: /*                                                                     */ AssembleBranch(opstr, 0x0B); break;
			case OpSFX.BNE: /*                                                                     */ AssembleBranch(opstr, 0x08); break;
			case OpSFX.BPL: /*                                                                     */ AssembleBranch(opstr, 0x0A); break;
			case OpSFX.BRA: /*                                                                     */ AssembleBranch(opstr, 0x05); break;
			case OpSFX.BVC: /*                                                                     */ AssembleBranch(opstr, 0x0E); break;
			case OpSFX.BVS: /*                                                                     */ AssembleBranch(opstr, 0x0F); break;

			case OpSFX.CACHE: /*                                                                   */ AssembleImplied(opstr, size, 0x02); break;
			case OpSFX.CMODE: /*                                                                   */ AssembleLongOp(0x3D, 0x4E); break;
			case OpSFX.COLOR: /*                                                                   */ AssembleImplied(opstr, size, 0x4E); break;
			case OpSFX.PLOT: /*                                                                    */ AssembleImplied(opstr, size, 0x4C); break;
			case OpSFX.RPIX: /*                                                                    */ AssembleLongOp(0x3D, 0x4C); break;

			case OpSFX.GETB: /*                                                                    */ AssembleImplied(opstr, size, 0xEF); break;
			case OpSFX.GETBH: /*                                                                   */ AssembleLongOp(0x3D, 0xEF); break;
			case OpSFX.GETBL: /*                                                                   */ AssembleLongOp(0x3E, 0xEF); break;
			case OpSFX.GETBS: /*                                                                   */ AssembleLongOp(0x3F, 0xEF); break;
			case OpSFX.GETC: /*                                                                    */ AssembleImplied(opstr, size, 0xDF); break;



			case OpSFX.LOOP: /*                                                                    */ AssembleImplied(opstr, size, 0x3C); break;
			case OpSFX.STOP: /*                                                                    */ AssembleImplied(opstr, size, 0x00); break;

			case OpSFX.MOVEB: /*                                                                   */ AssembleMoveStLd(opstr, true); break;
			case OpSFX.MOVEW: /*                                                                   */ AssembleMoveStLd(opstr, false); break;

			case OpSFX.RAMB: /*                                                                    */ AssembleLongOp(0x3E, 0xDF); break;
			case OpSFX.ROMB: /*                                                                    */ AssembleLongOp(0x3F, 0xDF); break;


			// More complex, instruction-specific handlers
			case OpSFX.CMP:
				if (!TryGetRegister(opstr, out byte cmpreg)) {
					RegistersOnly(0, 15);
				}

				cmpreg |= 0x60;
				WriteOpB(0x3F, cmpreg);
				break;


			case OpSFX.DEC:
			case OpSFX.INC:
				byte indecop = instruction is OpSFX.INC ? (byte) 0xD0 : (byte) 0xE0;

				if (!TryGetRegister(opstr, out byte indecreg) || indecreg >= 15) {
					RegistersOnly(0, 14);
					indecreg = 0;
				}

				indecop |= indecreg;
				WriteOpImplied(indecop);

				break;

			case OpSFX.IBT:
				byte ibtOpcode = 0xA0;

				if (opstr.SplitByComma(out var ibtRn, out var ibtVal)) {
					if (TryGetRegister(ibtRn, out var ibtReg)) {
						ibtOpcode |= ibtReg;

						if (IsImmediate(ibtVal, out var ibtExpr)) {
							WriteOpB(ibtOpcode, ibtExpr);
							break;
						} else {
							InvalidAddressingMode();
						}
					}
				} else {
					InvalidAddressingMode();
				}

				PlaceholderOp(ibtOpcode, 1);
				break;

			case OpSFX.IWT:
				byte iwtOpcode = 0xF0;

				if (opstr.SplitByComma(out var iwtRn, out var iwtVal)) {
					if (TryGetRegister(iwtRn, out var iwtReg)) {
						iwtOpcode |= iwtReg;

						if (IsImmediate(iwtVal, out var iwtExpr)) {
							WriteOpW(iwtOpcode, iwtExpr);
							break;
						} else {
							InvalidAddressingMode();
						}
					}
				} else {
					InvalidAddressingMode();
				}

				PlaceholderOp(iwtOpcode, 2);
				break;

			case OpSFX.JMP:
			case OpSFX.LJMP:
				if (!TryGetRegister(opstr, out byte jmpReg) || (jmpReg is < 8 or > 13)) {
					RegistersOnly(8, 13);
					jmpReg = 8; // force register 8 as fallback; 0 is the failure mode, but that's invalid
				}

				jmpReg |= 0x90;

				if (instruction is OpSFX.LJMP) {
					WriteOpB(0x3D, jmpReg);
				} else {
					WriteOpImplied(jmpReg);
				}

				break;

			case OpSFX.LDB:
				if (!TryGetIndirectRegister(opstr, out byte ldbReg) || ldbReg > 11) {
					StLmWarning();
					ldbReg = 0;
				}

				ldbReg |= 0x40;
				WriteOpB(0x3D, ldbReg);

				break;

			case OpSFX.LDW:
				if (!TryGetIndirectRegister(opstr, out byte ldwReg) || ldwReg > 11) {
					StLmWarning();
					ldwReg = 0;
				}

				ldwReg |= 0x40;
				WriteOpImplied(ldwReg);
				break;

			case OpSFX.LEA:
				byte leaOpcode = 0xF0;

				if (opstr.SplitByComma(out var leaRn, out var leaVal)) {
					if (TryGetRegister(leaRn, out var leaReg)) {
						leaOpcode |= leaReg;

						WriteOpW(leaOpcode, ParseOperand(opstr));
						break;
					}
				} else {
					InvalidAddressingMode();
				}

				PlaceholderOp(leaOpcode, 2);
				break;

			case OpSFX.LINK:
				if (IsImmediate(opstr, out var linkval)) {
					if (linkval.Resolved) {
						byte linkop = 0x90;
						byte linkv = (byte) linkval.ValueInt32;

						if (LinkTester(linkv)) {
							linkop |= linkv;
						} else {
							linkop |= 1;
						}

						WriteOpImplied(linkop);
					} else {
						WriteOpImplied(new Sfx4Bit(linkval, 0x90, LinkTester));
					}
				}

				break;

			case OpSFX.LM:
				byte lmReg;
				if (opstr.SplitByComma(out var lmRn, out var lmOp)) {
					if (TryGetRegister(lmRn, out lmReg)) {
						if (IsIndirect(lmOp, out var lmSrcExpr)) {
							if (lmSrcExpr.NeedsRequest) {
								WriteLmSm_unresolved(0x3D, lmReg, lmSrcExpr);
							} else {
								WriteLmSm_resolved(0x3D, lmReg, lmSrcExpr.ValueInt32);
							}
							return;
						}
					}
				} else {
					lmReg = 0;
				}

				InvalidAddressingMode();
				WriteLmSm_resolved(0x3D, lmReg, 0);
				break;

			case OpSFX.LMS:
				byte lmsReg;

				if (opstr.SplitByComma(out var lmsRn, out var lmsOp)) {
					if (TryGetRegister(lmsRn, out lmsReg)) {
						if (IsIndirect(lmsOp, out var lmsSrcExpr)) {
							if (lmsSrcExpr.NeedsRequest) {
								WriteLmSmS_unresolved(0x3D, lmsReg, lmsSrcExpr);
							} else {
								WriteLmSmS_resolved(0x3D, lmsReg, lmsSrcExpr.ValueInt32);
							}
							return;
						}
					}
				} else {
					lmsReg = 0;
				}

				InvalidAddressingMode();
				WriteLmSmS_resolved(0x3D, lmsReg, 0);
				break;

			case OpSFX.MOVE:
				if (opstr.SplitByComma(out var moveDest, out var moveSrc)) {
					bool srcIsRegister = TryGetRegister(moveSrc, out byte moveSrcReg);

					// register destinations
					if (TryGetRegister(moveDest, out byte moveDestReg)) {

						// MOVE Rd, Rs
						if (srcIsRegister) {
							moveDestReg |= 0x10;
							moveSrcReg |= 0x20;
							WriteOpB(moveSrcReg, moveDestReg);

							break;

						// MOVE Rd, #i
						} else if (IsImmediate(moveSrc, out var moveRIexpr)) {
							if (moveRIexpr.NeedsRequest) {
								moveDestReg |= 0xF0;
								WriteOpW(moveDestReg, moveRIexpr);
							} else {
								int tst = moveRIexpr.ValueInt32;

								if (SnesHelpers.FitsIn8(tst & SnesHelpers.AbsoluteMask)) {
									moveDestReg |= 0xA0;
									WriteOpB(moveDestReg, (byte) tst);
								} else {
									moveDestReg |= 0xF0;
									WriteOpW(moveDestReg, tst);
								}
							}

							break;

						// MOVE Rd, (a)
						} else if (IsIndirect(moveSrc, out moveRIexpr)) {
							if (moveRIexpr.NeedsRequest) {
								WriteLmSm_unresolved(0x3D, moveDestReg, moveRIexpr);
							} else {
								int tst = moveRIexpr.ValueInt32;

								if (SnesHelpers.FitsIn8(tst & SnesHelpers.AbsoluteMask)) {
									WriteLmSmS_resolved(0x3D, moveDestReg, tst);
								} else {
									WriteLmSm_resolved(0x3D, moveDestReg, tst);
								}
							}

							break;
						}

					// MOVE (a), Rs
					} else if (srcIsRegister && IsIndirect(moveDest, out var moveSmExpr)) {
						if (moveSmExpr.NeedsRequest) {
							WriteLmSm_unresolved(0x3E, moveSrcReg, moveSmExpr);
						} else {
							int tst = moveSmExpr.ValueInt32;

							if (SnesHelpers.FitsIn8(tst & SnesHelpers.AbsoluteMask)) {
								WriteLmSmS_resolved(0x3E, moveSrcReg, tst);
							} else {
								WriteLmSm_resolved(0x3E, moveSrcReg, tst);
							}
						}

						break;
					}
				}

				InvalidAddressingMode();
				break;


			case OpSFX.MOVES:
				byte movesOpD = 0x20;
				byte movesOpS = 0xB0;

				if (opstr.SplitByComma(out var movesDest, out var movesSrc)) {
					// IMPORTANT this OR should not short-circuit - both operands need to be parsed
					if (!TryGetRegister(movesDest, out byte movesDestReg) | !TryGetRegister(movesSrc, out byte moveSrcReg)) {
						RegistersOnlyAll();
					}

					movesOpD |= movesDestReg;
					movesOpS |= moveSrcReg;
				} else {
					RegistersOnlyAll();
				}

				WriteOpB(movesOpD, movesOpS);
				break;

			case OpSFX.SBC:
				if (!TryGetRegister(opstr, out byte sbcreg)) {
					InvalidAddressingMode();
				}

				sbcreg |= 0x60;
				WriteOpB(0x3D, sbcreg);
				break;

			case OpSFX.SM:
				byte smReg;
				if (opstr.SplitByComma(out var smOp, out var smRn)) {
					if (TryGetRegister(smRn, out smReg)) {
						if (IsIndirect(smOp, out var smSrcExpr)) {
							if (smSrcExpr.NeedsRequest) {
								WriteLmSm_unresolved(0x3E, smReg, smSrcExpr);
							} else {
								WriteLmSm_resolved(0x3E, smReg, smSrcExpr.ValueInt32);
							}
							return;
						}
					}
				} else {
					smReg = 0;
				}

				InvalidAddressingMode();
				WriteLmSm_resolved(0x3E, smReg, 0);
				break;

			case OpSFX.SMS:
				byte smsReg;
				if (opstr.SplitByComma(out var smsOp, out var smsRn)) {
					if (TryGetRegister(smsRn, out smsReg)) {
						if (IsIndirect(smsOp, out var smsSrcExpr)) {
							if (smsSrcExpr.NeedsRequest) {
								WriteLmSmS_unresolved(0x3E, smsReg, smsSrcExpr);
							} else {
								WriteLmSmS_resolved(0x3E, smsReg, smsSrcExpr.ValueInt32);
							}
							return;
						}
					}
				} else {
					smsReg = 0;
				}

				InvalidAddressingMode();
				WriteLmSmS_resolved(0x3E, smsReg, 0);
				break;

			case OpSFX.STB:
				if (!TryGetIndirectRegister(opstr, out byte stbReg) || stbReg > 11) {
					StLmWarning();
					stbReg = 0;
				}

				stbReg |= 0x30;
				WriteOpB(0x3D, stbReg);
				break;

			case OpSFX.STW:
				if (!TryGetIndirectRegister(opstr, out byte stwReg) || stwReg > 11) {
					StLmWarning();
					stbReg = 0;
				}

				stwReg |= 0x30;
				WriteOpImplied(stwReg);
				break;
		}


		bool IsImmediate(OperandString argstr, [NotNullWhen(true)] out IExpressionReturn? expr) {
			if (argstr.First is '#') {
				expr = ParseOperandSliced(argstr, 1);
				return true;
			} else {
				expr = null;
				return false;
			}
		}

		bool IsIndirect(OperandString argstr, [NotNullWhen(true)] out IExpressionReturn? expr) {
			if (argstr.First is '(' && argstr.Last is ')') {
				expr = ParseOperandSliced(opstr, 1, 1);
				return true;
			} else {
				expr = null;
				return false;
			}
		}


		void AssembleMoveStLd(OperandString argstr, bool isBnotW) {
#pragma warning disable IDE0018 // Inline variable declaration - it's neater like this
			byte movewRegD, movewRegS;
#pragma warning restore IDE0018

			if (argstr.SplitByComma(out var movewDest, out var movewSrc)) {
				if (TryGetIndirectRegister(movewDest, out movewRegD)) {
					if (TryGetRegister(movewSrc, out movewRegS)) {
						if (movewRegD > 11) {
							Error("Destination register must be R0-R11.");
							movewRegD = 11;
						}

						movewRegS |= 0x30;

						if (movewRegD is 0) { // No FROM
							if (isBnotW) {
								WriteOpB(0x3D, movewRegS);
							} else {
								WriteOpImplied(movewRegS);
							}
						} else {
							movewRegD |= 0xB0; // FROM
							if (isBnotW) {
								WriteOpSfx3(movewRegD, 0x3D, movewRegS);
							} else {
								WriteOpB(movewRegD, movewRegS);
							}
						}
					} else if (TryGetRegister(movewDest, out movewRegD)) {
						if (TryGetIndirectRegister(movewSrc, out movewRegS)) {
							if (movewRegS > 11) {
								Error("Source register must be R0-R11.");
								movewRegS = 11;
							}

							movewRegD |= 0x40;

							if (movewRegS is 0) { // No TO
								if (isBnotW) {
									WriteOpB(0x3D, movewRegD);
								} else {
									WriteOpImplied(movewRegD);
								}
							} else {
								movewRegS |= 0x10; // TO
								if (isBnotW) {
									WriteOpSfx3(movewRegS, 0x3D, movewRegD);
								} else {
									WriteOpB(movewRegS, movewRegD);
								}
							}
						}
					}
				} else {
					InvalidAddressingMode();
				}
			}
		}


		void WriteOpSfx3(byte b1, byte b2, byte b3) {
			if (TryAdvanceProgramCounter(3)) {
				_pcPtr[0] = b1;
				_pcPtr[1] = b2;
				_pcPtr[2] = b3;

				AdvanceProgramCounter(3);
			}
		}



		void AssembleLongOp(byte opA, byte opB) {
			if (size is not SizeToken.Unspecified) {
				TokensOnImplied();
			} else if (len is 0) {
				WriteOpB(opA, opB);
			} else {
				InvalidAddressingMode();
			}
		}


		void WriteLmSm_resolved(byte opcode, byte reg, int lmoperand) {
			if (TryAdvanceProgramCounter(4)) {
				byte* wr = _pcPtr;
				wr[0] = opcode;
				wr[1] = (byte) (reg | 0xF0);
				*(ushort*) (wr + 2) = (ushort) lmoperand;
				TrackAndStep4();
			}
		}

		void WriteLmSm_unresolved(byte opcode, byte reg, IExpressionReturn lmoperand) {
			if (TryAdvanceProgramCounter(4)) {
				byte* wr = _pcPtr;
				wr[0] = opcode;
				wr[1] = (byte) (reg | 0xF0);

				CreateSimpleRequest(lmoperand, _offset + 2, 2);

				TrackAndStep4();
			}
		}

		void WriteLmSmS_resolved(byte opcode, byte reg, int lmoperand) {
			if (TryAdvanceProgramCounter(3)) {
				byte* wr = _pcPtr;
				wr[0] = opcode;
				wr[1] = (byte) (reg | 0xA0);
				wr[2] = (byte) (lmoperand / 2);
				TrackAndStep3();
			}
		}

		void WriteLmSmS_unresolved(byte opcode, byte reg, IExpressionReturn lmoperand) {
			if (TryAdvanceProgramCounter(3)) {
				byte* wr = _pcPtr;
				wr[0] = opcode;
				wr[1] = (byte) (reg | 0xA0);

				CreateSimpleRequest(new SfxHalf(lmoperand), _offset + 2, 1);
				TrackAndStep3();
			}
		}

		void RegistersOnlyAll() {
			RegistersOnly(0, 15);
		}

		void RegistersOnly(int rmin, int rmax) {
			Error(MsgInfo.InvalidAddressingMode, $"This instruction only accepts registers R{rmin}-R{rmax}.");
		}

		void StLmWarning() {
			Error(MsgInfo.InvalidAddressingMode, $"This instruction only accepts paren-wrapped registers R0-R11.");
		}

		void AssembleIorR(OperandString argstr, byte opcode, bool shortRn, bool allowZero) {
			if (IsImmediate(argstr, out var val)) {
				byte preop = (byte) (shortRn ? 0x3E : 0x3F);

				if (val.Resolved) {
					byte pval = (byte) (val.ValueInt32 & 0xF);

					if (allowZero || TestFor0(pval)) {
						opcode |= pval;
					} else {
						opcode |= 1;
					}
					WriteOpB(preop, opcode);
				} else {
					WriteOpB(preop, new Sfx4Bit(val, opcode, TestFor0));
				}
			} else if (TryGetRegister(argstr, out byte reg)) {
				if (allowZero || reg is not 0) {
					opcode |= reg;
				} else {
					Error(MsgInfo.InvalidAddressingMode, "This instruction does not accept R0.");
					opcode |= 1;
				}

				if (shortRn) {
					WriteOpImplied(opcode);
				} else {
					WriteOpB(0x3D, opcode);
				}

			} else {
				char minop = allowZero ? '0' : '1';
				Error(MsgInfo.InvalidAddressingMode, $"This instruction only accepts immediates values (#{minop}-#15) and registers (R{minop}-R15).");
			}
		}

		bool TryGetIndirectRegister(OperandString test, out byte reg) {
			if (test.First is '(' && test.Last is ')') {
				return TryGetRegister(test.SliceEndsAndTrim(1, 1), out reg);
			}

			reg = 0;

			return false;
		}


		bool TryGetRegister(OperandString test, out byte reg) {
			if (test.First.CiIs('R')) {
				int len = test.Length;

				if (len is 3) {
					if (test.GetUnchecked(1) is '1') {
						int writeR = test.GetUnchecked(2) - '0';

						if ((uint) writeR <= 5) {
							reg = (byte) (writeR + 10);
							return true;
						}
					}
				} else if (len is 2) {
					int writeR = test.GetUnchecked(1) - '0';

					if ((uint) writeR <= 9) {
						reg = (byte) writeR;
						return true;
					}
				}
			}

			reg = 0;
			return false;
		}



		void AssembleGetRegister(OperandString argstr, byte opcode) {
			if (!TryGetRegister(argstr, out byte reg)) {
				// no short circuit
				// always write so things stay neat
				InvalidAddressingMode();
			}

			opcode |= reg;
			WriteOpImplied(opcode);
		}
	}

	private static OpSFX TestForSuperFX(OperandString chars) {
		// copy so we don't need to check chars.Length every time
		int len = chars.Length;

		// if it's not even 2-5 characters, give up now
		if (len is < 2 or > 5) {
			return OpSFX.NotGood;
		}

		char* reading = chars.Start;

		// this TECHNICALLY can cause problems with control characters being confused for numbers...
		// but we're assuming only alphanumeric characters even get this far
		char a = reading[0].FastLower();
		char b = reading[1].FastLower();

		char d, e;

		switch (a) {
			case 'a':
				if (len is 3) {
					if (b is 'd') return SelectCorC('c', OpSFX.ADC, 'd', OpSFX.ADD);
					if (b is 'n') return SelectC('d', OpSFX.AND);
					if (b is 's') return SelectC('r', OpSFX.ASR);
				} else if (len is 4) {
					if (TestBC('l', 't')) {
						int altN = reading[3] - '1';

						if ((uint) altN < 3) {
							return OpSFX.ALT1 + altN;
						}
					}
				}

				return OpSFX.NotGood;

			case 'b':
				if (len is 3) {
					switch (b) {
						case 'c': return SelectCorC('c', OpSFX.BCC, 's', OpSFX.BCS);
						case 'e': return SelectC('q', OpSFX.BEQ);
						case 'g': return SelectC('e', OpSFX.BGE);
						case 'i': return SelectC('c', OpSFX.BIC);
						case 'l': return SelectC('t', OpSFX.BLT);
						case 'm': return SelectC('i', OpSFX.BMI);
						case 'n': return SelectC('e', OpSFX.BNE);
						case 'p': return SelectC('l', OpSFX.BPL);
						case 'r': return SelectC('a', OpSFX.BRA);
						case 'v': return SelectCorC('c', OpSFX.BVC, 's', OpSFX.BVS);
					}
				}
				return OpSFX.NotGood;

			case 'c':
				if (len is 3) return SelectBC('m', 'p', OpSFX.CMP);

				if (len is 5) {
					if (b is 'o') return SelectCDE('l', 'o', 'r', OpSFX.COLOR);
					if (b is 'a') return SelectCDE('c', 'h', 'e', OpSFX.CACHE);
					if (b is 'm') return SelectCDE('o', 'd', 'e', OpSFX.CMODE);
				}

				return OpSFX.NotGood;

			case 'd':
				if (len is 3) return SelectBC('e', 'c', OpSFX.DEC);

				if (len is 4) {
					if (TestBC('i', 'v') && reading[3] is '2') {
						return OpSFX.DIV2;
					}
				}

				return OpSFX.NotGood;

			case 'e': break;

			case 'f':
				if (len is 4) return SelectBCD('r', 'o', 'm', OpSFX.FROM);

				return IsMult(OpSFX.FMULT);

			case 'g':
				if (b is 'e') {
					if (len is 4) {

						if (reading[2].FastLower() is 't') {
							d = reading[3].FastLower();

							if (d is 'b') return OpSFX.GETB;
							if (d is 'c') return OpSFX.GETC;
						}
					} else if (len is 5) {
						if (TestCD('t', 'b')) {
							e = reading[4].FastLower();

							if (e is 's') return OpSFX.GETBS;
							if (e is 'l') return OpSFX.GETBL;
							if (e is 'h') return OpSFX.GETBH;
						}
					}
				}

				return OpSFX.NotGood;

			case 'h':
				if (len is 3) return SelectBC('i', 'b', OpSFX.HIB);

				return OpSFX.NotGood;

			case 'i':
				if (len is 3) {
					if (b is 'n') return SelectC('c', OpSFX.INC);
					if (b is 'b') return SelectC('t', OpSFX.IBT);
					if (b is 'w') return SelectC('t', OpSFX.IWT);
				}

				return OpSFX.NotGood;

			case 'j':
				if (len is 3) return SelectBC('m', 'p', OpSFX.JMP);

				return OpSFX.NotGood;

			case 'k': break;

			case 'l':
				if (len is 4) {
					if (b is 'i') return SelectCD('n', 'k', OpSFX.LINK);
					if (b is 'j') return SelectCD('m', 'p', OpSFX.LJMP);
					if (b is 'o') return SelectCD('o', 'p', OpSFX.LOOP);

					return OpSFX.NotGood;
				}

				if (b is 'm') {
					if (len is 2) return OpSFX.LM;
					if (len is 3) return SelectC('s', OpSFX.LMS);

					return IsMult(OpSFX.LMULT); // checks b again, but whatever
				}

				if (len is 3) {
					if (b is 'd') return SelectCorC('b', OpSFX.LDB, 'w', OpSFX.LDW);
					if (b is 's') return SelectC('r', OpSFX.LSR);
					if (b is 'e') return SelectC('a', OpSFX.LEA);
					if (b is 'o') return SelectC('b', OpSFX.LOB);
				}

				return OpSFX.NotGood;

			case 'm':
				if (b is 'o') {
					if (TestCD('v', 'e')) {
						if (len is 4) return OpSFX.MOVE;

						if (len is 5) {
							e = reading[4].FastLower();

							if (e is 'b') return OpSFX.MOVEB;
							if (e is 's') return OpSFX.MOVES;
							if (e is 'w') return OpSFX.MOVEW;
						}
					}
				} else if (b is 'u') {
					if (len is 4) {
						return SelectCD('l', 't', OpSFX.MULT);
					}
				} else if (b is 'e') {
					if (len is 5) {
						return SelectCDE('r', 'g', 'e', OpSFX.MERGE);
					}
				}

				return OpSFX.NotGood;

			case 'n':
				if (len is 3 && b is 'o') {
					// NOP is probably more common, due to pipelining
					return SelectCorC('p', OpSFX.NOP, 't', OpSFX.NOT);
				}

				return OpSFX.NotGood;

			case 'o':
				if (len is 2 && b is 'r') return OpSFX.OR;

				return OpSFX.NotGood;

			case 'p':
				if (len is 4) return SelectBCD('l', 'o', 't', OpSFX.PLOT);

				return OpSFX.NotGood;

			case 'q': break;

			case 'r':
				if (len is 4) {
					if (b is 'o') return SelectCD('m', 'b', OpSFX.ROMB);
					if (b is 'a') return SelectCD('m', 'b', OpSFX.RAMB);
					if (b is 'p') return SelectCD('i', 'x', OpSFX.RPIX);
				} else if (len is 3) {
					if (b is 'o') return SelectCorC('l', OpSFX.ROL, 'r', OpSFX.ROR);
				}

				return OpSFX.NotGood;

			case 's':
				if (len is 2) {
					if (b is 'm') return OpSFX.SM;
				} else if (len is 3) {
					if (b is 'b') return SelectCorC('c', OpSFX.SBC, 'k', OpSFX.SBK);
					if (b is 'u') return SelectC('b', OpSFX.SUB);
					if (b is 'e') return SelectC('x', OpSFX.SEX);
					if (b is 'm') return SelectC('s', OpSFX.SMS);
					if (b is 't') return SelectCorC('b', OpSFX.STB, 'w', OpSFX.STW);
				} else if (len is 4) {
					if (b is 'w') return SelectCD('a', 'p', OpSFX.SWAP);
					if (b is 't') return SelectCD('o', 'p', OpSFX.STOP);
				}

				return OpSFX.NotGood;

			case 't':
				if (len is 2 && b is 'o') return OpSFX.TO;

				return OpSFX.NotGood;

			case 'u':
				return IsMult(OpSFX.UMULT);

			case 'v': break;

			case 'w':
				if (len is 4) return SelectBCD('i', 't', 'h', OpSFX.WITH);

				return OpSFX.NotGood;

			case 'x':
				if (len is 3) return SelectBC('o', 'r', OpSFX.XOR);

				return OpSFX.NotGood;
		}

		return OpSFX.NotGood;

		OpSFX SelectC(char cc, OpSFX op) {
			return reading[2].FastLower() == cc ? op : OpSFX.NotGood;
		}

		OpSFX SelectCorC(char cc1, OpSFX op1, char cc2, OpSFX op2) {
			char cc0 = reading[2].FastLower();

			if (cc0 == cc1) return op1;
			if (cc0 == cc2) return op2;

			return OpSFX.NotGood;
		}

		OpSFX SelectBC(char bc, char cc, OpSFX op) {
			if (b == bc) {
				if (reading[2].FastLower() == cc) {
					return op;
				}
			}

			return OpSFX.NotGood;
		}

		OpSFX SelectBCD(char bc, char cc, char cd, OpSFX op) {
			if (b == bc) {
				if (reading[2].FastLower() == cc) {
					if (reading[3].FastLower() == cd) {
						return op;
					}
				}
			}

			return OpSFX.NotGood;
		}

		bool TestBC(char bc, char cc) {
			return b == bc && reading[2].FastLower() == cc;
		}

		bool TestCD(char cc, char cd) {
			return (reading[2].FastLower() == cc) && (reading[3].FastLower() == cd);
		}

		OpSFX SelectCDE(char cc, char cd, char ce, OpSFX op) {
			if (reading[2].FastLower() == cc) {
				if (reading[3].FastLower() == cd) {
					if (reading[4].FastLower() == ce) {
						return op;
					}
				}
			}

			return OpSFX.NotGood;
		}

		OpSFX SelectCD(char cc, char cd, OpSFX op) {
			if (reading[2].FastLower() == cc) {
				if (reading[3].FastLower() == cd) {
					return op;
				}
			}

			return OpSFX.NotGood;
		}

		OpSFX IsMult(OpSFX op) {
			if (len is 5) {
				if (b is 'm' && (reading[2] is 'u' or 'U') &&
					(reading[3] is 'l' or 'L') && (reading[4] is 't' or 'T')) {
					return op;
				}
			}

			return OpSFX.NotGood;
		}
	}


	// methods that let us call Error to simplify the resolution logic
	private bool TestFor0(int val) {
		if (val is > 0 and < 16) {
			return true;
		} else {
			Error(MsgInfo.InvalidAddressingMode, "0 is not a valid operand for this instruction.");
			return false;
		}
	}

	private bool LinkTester(int linktest) {
		if (linktest is 0 or > 4) {
			Error(MsgInfo.InvalidAddressingMode, "This instruction only accepts values of 1-4.");
			return false;
		} else {
			return true;
		}
	}
}

file class Sfx4Bit(IExpressionReturn item, byte opcode, Func<int, bool> tester) : IExpressionReturn {
	public ExpressionState ReturnState => item.ReturnState;

	public bool Resolved { get; private set; } = false;

	private bool _gaveUp = false;

	public bool NeedsRequest => item.NeedsRequest;

	public decimal Value {
		get {
			if (!item.Resolved) {
				return MathHelpers.NaN;
			}

			return (item.ValueInt32 & 0xF) | opcode;
		}
	}

	public bool TryToResolve() {
		if (!_gaveUp) {
			if (item.TryToResolve()) {
				_gaveUp = true;
				Resolved = tester(item.ValueInt32);
			}
		}

		return Resolved;
	}
}

file class SfxHalf(IExpressionReturn item) : IExpressionReturn {
	public ExpressionState ReturnState => item.ReturnState;

	public bool Resolved { get; private set; } = false;

	public bool NeedsRequest => item.NeedsRequest;

	public decimal Value => (item.ValueInt32 & 0xFF) / 2;

	public bool TryToResolve() {
		return item.TryToResolve();
	}
}
