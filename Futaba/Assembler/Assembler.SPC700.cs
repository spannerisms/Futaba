namespace Futaba;

unsafe partial class Assembler {
	private void AssembleSPC700(OpSPC instruction) {
		SizeToken size = GetSizeToken();

		if (size is SizeToken.L) {
			InvalidAddressingMode();
			AbortCommand();
			return;
		} else if (size is SizeToken.Invalid) {
			AbortCommand();
			return;
		}

		ReadRestOfCommand(out OperandString opstr);

		int len = opstr.Length;

		IExpressionReturn valD, valS;
		SPC700Target targD, targS = SPC700Target.Invalid;

		OperandString strD, strS;

		switch (instruction) {
			case OpSPC.ADC:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();
				DoStandardAccumulatorModes(0x80, targD, valD, targS, valS);
				break;

			case OpSPC.ADDW: /*                                                                    */ AssembleYAopD(0x7A); break;

			case OpSPC.AND:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();
				DoStandardAccumulatorModes(0x20, targD, valD, targS, valS);
				break;

			case OpSPC.AND0: /*                                                                    */ AssembleOneBitOpsWithNot(0, 0x4A, 0x6A); break;
			case OpSPC.AND1: /*                                                                    */ AssembleOneBitOpsWithNot(1, 0x4A, 0x6A); break;
			case OpSPC.AND2: /*                                                                    */ AssembleOneBitOpsWithNot(2, 0x4A, 0x6A); break;
			case OpSPC.AND3: /*                                                                    */ AssembleOneBitOpsWithNot(3, 0x4A, 0x6A); break;
			case OpSPC.AND4: /*                                                                    */ AssembleOneBitOpsWithNot(4, 0x4A, 0x6A); break;
			case OpSPC.AND5: /*                                                                    */ AssembleOneBitOpsWithNot(5, 0x4A, 0x6A); break;
			case OpSPC.AND6: /*                                                                    */ AssembleOneBitOpsWithNot(6, 0x4A, 0x6A); break;
			case OpSPC.AND7: /*                                                                    */ AssembleOneBitOpsWithNot(7, 0x4A, 0x6A); break;

			case OpSPC.ASL: /*                                                                     */ DoShiftRolls(0x00); break;

			case OpSPC.BBC0: /*                                                                    */ AssembleBranchBitTest(0x13); break;
			case OpSPC.BBC1: /*                                                                    */ AssembleBranchBitTest(0x33); break;
			case OpSPC.BBC2: /*                                                                    */ AssembleBranchBitTest(0x53); break;
			case OpSPC.BBC3: /*                                                                    */ AssembleBranchBitTest(0x73); break;
			case OpSPC.BBC4: /*                                                                    */ AssembleBranchBitTest(0x93); break;
			case OpSPC.BBC5: /*                                                                    */ AssembleBranchBitTest(0xB3); break;
			case OpSPC.BBC6: /*                                                                    */ AssembleBranchBitTest(0xD3); break;
			case OpSPC.BBC7: /*                                                                    */ AssembleBranchBitTest(0xF3); break;
			case OpSPC.BBS0: /*                                                                    */ AssembleBranchBitTest(0x03); break;
			case OpSPC.BBS1: /*                                                                    */ AssembleBranchBitTest(0x23); break;
			case OpSPC.BBS2: /*                                                                    */ AssembleBranchBitTest(0x43); break;
			case OpSPC.BBS3: /*                                                                    */ AssembleBranchBitTest(0x63); break;
			case OpSPC.BBS4: /*                                                                    */ AssembleBranchBitTest(0x83); break;
			case OpSPC.BBS5: /*                                                                    */ AssembleBranchBitTest(0xA3); break;
			case OpSPC.BBS6: /*                                                                    */ AssembleBranchBitTest(0xC3); break;
			case OpSPC.BBS7: /*                                                                    */ AssembleBranchBitTest(0xE3); break;

			case OpSPC.BCC: /*                                                                     */ AssembleBranch(opstr, 0x90); break;
			case OpSPC.BCS: /*                                                                     */ AssembleBranch(opstr, 0xB0); break;
			case OpSPC.BEQ: /*                                                                     */ AssembleBranch(opstr, 0xF0); break;
			case OpSPC.BMI: /*                                                                     */ AssembleBranch(opstr, 0x30); break;
			case OpSPC.BNE: /*                                                                     */ AssembleBranch(opstr, 0xD0); break;
			case OpSPC.BPL: /*                                                                     */ AssembleBranch(opstr, 0x10); break;
			case OpSPC.BRA: /*                                                                     */ AssembleBranch(opstr, 0x2F); break;
			case OpSPC.BVC: /*                                                                     */ AssembleBranch(opstr, 0x50); break;
			case OpSPC.BVS: /*                                                                     */ AssembleBranch(opstr, 0x70); break;

			case OpSPC.BRK: /*                                                                     */ AssembleImplied(opstr, size, 0x0F); break;

			case OpSPC.CALL:
				OnlyDotW(size);

				WriteOpW(0x3F, ParseOperand(opstr));
				break;

			case OpSPC.CBNE:
				if (!opstr.SplitByComma(out strD, out strS)) {
					InvalidAddressingMode();
					PlaceholderOp(0x2E, 2);
					break;
				}

				byte cbneOpcode;
				(targD, valD) = GetOneMode(strD);

				if (targD is SPC700Target.DirectPage) {
					cbneOpcode = 0x2E;
				} else if (targD is SPC700Target.Ambiguous) {
					cbneOpcode = 0x2E;
					AggressiveSizeTokens();
				} else if (targD is SPC700Target.DirectPageIndexedX) {
					cbneOpcode = 0xDE;
				} else if (targD is SPC700Target.AmbiguousX) {
					cbneOpcode = 0xDE;
					AggressiveSizeTokens();
				} else {
					InvalidAddressingMode();
					PlaceholderOp(0x2E, 2);
					break;
				}

				int cbneLen = strS.Length;

				if (cbneLen is 0) {
					MissingOperand();
					PlaceholderOp(cbneOpcode, 2);
					break;
				}

				char cbneFirst = strS.First;

				if (cbneLen is 1) {
					if (cbneFirst is BROP) {
						WriteOpBBrop(cbneOpcode, valD);
						break;
					} else if (cbneFirst is JNOP) {
						WriteOpBB(cbneOpcode, valD, 0x00);
						break;
					} else if (cbneFirst is BTYS) {
						WriteOpBB(cbneOpcode, valD, 0xFD);
						break;
					}
				}

				if (cbneFirst is BranchLiteral) {
					valS = ParseOperandSliced(strS, 1);
					WriteOpBB(cbneOpcode, valD, valS);
				} else {
					valS = ParseOperand(strS);
					WriteOpBRel(cbneOpcode, valD, valS);
				}

				break;

			case OpSPC.CLR0: /*                                                                    */ AssembleDirectPageOnly(0x12); break;
			case OpSPC.CLR1: /*                                                                    */ AssembleDirectPageOnly(0x32); break;
			case OpSPC.CLR2: /*                                                                    */ AssembleDirectPageOnly(0x52); break;
			case OpSPC.CLR3: /*                                                                    */ AssembleDirectPageOnly(0x72); break;
			case OpSPC.CLR4: /*                                                                    */ AssembleDirectPageOnly(0x92); break;
			case OpSPC.CLR5: /*                                                                    */ AssembleDirectPageOnly(0xB2); break;
			case OpSPC.CLR6: /*                                                                    */ AssembleDirectPageOnly(0xD2); break;
			case OpSPC.CLR7: /*                                                                    */ AssembleDirectPageOnly(0xF2); break;

			case OpSPC.CLRC: /*                                                                    */ AssembleImplied(opstr, size, 0x60); break;
			case OpSPC.CLRP: /*                                                                    */ AssembleImplied(opstr, size, 0x20); break;
			case OpSPC.CLRV: /*                                                                    */ AssembleImplied(opstr, size, 0xE0); break;

			case OpSPC.CMP:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();

				switch (targD) {
					case SPC700Target.X:
						switch (ResolveAmbiguities(targS, valS)) {
							case SPC700Target.Immediate: /*                                        */ WriteBAgg(0xC8, valS); return;
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0x3E, valS); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0x1E, valS); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.Y:
						switch (ResolveAmbiguities(targS, valS)) {
							case SPC700Target.Immediate: /*                                        */ WriteBAgg(0xAD, valS); return;
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0x7E, valS); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0x5E, valS); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					default:
						DoStandardAccumulatorModes(0x60, targD, valD, targS, valS);
						return;
				}

			case OpSPC.CMPW: /*                                                                    */ AssembleYAopD(0x5A); break;

			case OpSPC.DAA: /*                                                                     */ AssembleATargeted(0xDF); break;
			case OpSPC.DAS: /*                                                                     */ AssembleATargeted(0xBE); break;

			case OpSPC.DBNZ:
				if (!opstr.SplitByComma(out strD, out strS)) {
					InvalidAddressingMode();
					break;
				}

				(targD, valD) = GetOneMode(strD);

				if (targD is SPC700Target.DirectPage or SPC700Target.Y) {
					// do nothing, it's good
				} else if (targD is SPC700Target.Ambiguous) {
					AggressiveSizeTokens();
				} else {
					InvalidAddressingMode();
					break;
				}

				int dbnzLen = strS.Length;

				if (dbnzLen is 0) {
					MissingOperand();

					goto PlaceHolderDBNZ;
				}

				char dbnzFirst = strS.First;

				if (dbnzLen is 1) {
					if (dbnzFirst is BROP) {
						if (targD is SPC700Target.Y) {
							WriteOpBrop(0xFE);
						} else {
							WriteOpBBrop(0x6E, valD);
						}
						break;

					} else if (dbnzFirst is JNOP) {
						if (targD is SPC700Target.Y) {
							WriteOpB(0xFE, 0x00);
						} else {
							WriteOpBB(0x6E, ParseOperand(strD), 0x00);
						}
						break;

					} else if (dbnzFirst is BTYS) {
						if (targD is SPC700Target.Y) {
							WriteOpB(0xFE, 0xFE);
						} else {
							WriteOpBB(0x6E, valD, 0xFD);
						}
						break;

					}
				}

				if (dbnzFirst is BranchLiteral) {
					valS = ParseOperandSliced(strS, 1);

					if (targD is SPC700Target.Y) {
						WriteOpB(0xFE, valS);
					} else {
						WriteOpBB(0x6E, valD, valS);
					}
				} else {
					valS = ParseOperand(strS);

					if (targD is SPC700Target.Y) {
						WriteOpRel8(0xFE, valS);
					} else {
						WriteOpBRel(0x6E, valD, valS);
					}
				}

				break;

				PlaceHolderDBNZ:
				if (targD is SPC700Target.Y) {
					PlaceholderOp(0xFE, 1);
				} else {
					PlaceholderOp(0x6E, 2);
				}

				break;

			case OpSPC.DEC:
				(targD, valD) = GetOneMode(opstr);

				switch (ResolveAmbiguities(targD, valD)) {
					case SPC700Target.A: /*                                                        */ WriteNoToken(0x9C); return;
					case SPC700Target.X: /*                                                        */ WriteNoToken(0x1D); return;
					case SPC700Target.Y: /*                                                        */ WriteNoToken(0xDC); return;
					case SPC700Target.DirectPage: /*                                               */ WriteDotB(0x8B, valD); return;
					case SPC700Target.DirectPageIndexedX: /*                                       */ WriteBAgg(0x9B, valD); return;
					case SPC700Target.Absolute: /*                                                 */ WriteDotW(0x8C, valD); return;
					default: /*                                                                    */ InvalidAddressingMode(); return;
				}

			case OpSPC.DECW: /*                                                                    */ AssembleDirectPageOnly(0x1A); break;

			case OpSPC.DI: /*                                                                      */ AssembleImplied(opstr, size, 0xC0); break;

			case OpSPC.DIV:
				// assemble it right away then let errors happen
				WriteNoToken(0x9E);

				if (opstr.SplitByComma(out strD, out strS)) {
					// fast testing since they're fixed size operands
					if (IsYA(strD) && strS.OnlyChar is 'X' or 'x') {
						break;
					}
				}

				InvalidAddressingMode();
				break;

			case OpSPC.EI: /*                                                                      */ AssembleImplied(opstr, size, 0xA0); break;

			case OpSPC.EOR:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();
				DoStandardAccumulatorModes(0x40, targD, valD, targS, valS);
				break;

			case OpSPC.EOR0: /*                                                                    */ AssembleOneBitOps(0, 0x8A); break;
			case OpSPC.EOR1: /*                                                                    */ AssembleOneBitOps(1, 0x8A); break;
			case OpSPC.EOR2: /*                                                                    */ AssembleOneBitOps(2, 0x8A); break;
			case OpSPC.EOR3: /*                                                                    */ AssembleOneBitOps(3, 0x8A); break;
			case OpSPC.EOR4: /*                                                                    */ AssembleOneBitOps(4, 0x8A); break;
			case OpSPC.EOR5: /*                                                                    */ AssembleOneBitOps(5, 0x8A); break;
			case OpSPC.EOR6: /*                                                                    */ AssembleOneBitOps(6, 0x8A); break;
			case OpSPC.EOR7: /*                                                                    */ AssembleOneBitOps(7, 0x8A); break;

			case OpSPC.INC:
				(targD, valD) = GetOneMode(opstr);

				switch (ResolveAmbiguities(targD, valD)) {
					case SPC700Target.A: /*                                                        */ WriteNoToken(0xBC); return;
					case SPC700Target.X: /*                                                        */ WriteNoToken(0x3D); return;
					case SPC700Target.Y: /*                                                        */ WriteNoToken(0xFC); return;
					case SPC700Target.DirectPage: /*                                               */ WriteDotB(0xAB, valD); return;
					case SPC700Target.DirectPageIndexedX: /*                                       */ WriteBAgg(0xBB, valD); return;
					case SPC700Target.Absolute: /*                                                 */ WriteDotW(0xAC, valD); return;
					default: /*                                                                    */ InvalidAddressingMode(); return;
				}

			case OpSPC.INCW: /*                                                                    */ AssembleDirectPageOnly(0x3A); break;

			case OpSPC.JMP:
				(targD, valD) = GetOneMode(opstr);

				OnlyDotW(size);

				switch (targD) {
					case SPC700Target.Ambiguous:
					case SPC700Target.Absolute: /*                                                 */ WriteOpW(0x5F, valD); break;
					case SPC700Target.AmbiguousIndirectX:
					case SPC700Target.AbsoluteIndirectIndexedX: /*                                 */ WriteOpW(0x1F, valD); break;
					default: /*                                                                    */ InvalidAddressingMode(); break;
				}

				break;

			case OpSPC.LSR: /*                                                                     */ DoShiftRolls(0x40); break;

			case OpSPC.MOV:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();

				switch (targD) {
					case SPC700Target.A:
						switch (ResolveAmbiguities(targS, valS)) {
							case SPC700Target.X: /*                                                */ WriteNoToken(0x7D); return;
							case SPC700Target.Y: /*                                                */ WriteNoToken(0xDD); return;
							case SPC700Target.Immediate: /*                                        */ WriteBAgg(0xE8, valS); return;
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0xE4, valS); return;
							case SPC700Target.DirectPageIndexedX: /*                               */ WriteDotB(0xF4, valS); return;
							case SPC700Target.DirectPageIndirectY: /*                              */ WriteBAgg(0xF7, valS); return;
							case SPC700Target.DirectPageIndirectX: /*                              */ WriteBAgg(0xE7, valS); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0xE5, valS); return;
							case SPC700Target.AbsoluteIndexedX: /*                                 */ WriteDotW(0xF5, valS); return;
							case SPC700Target.AbsoluteIndexedY: /*                                 */ WriteDotW(0xF6, valS); return;
							case SPC700Target.XIndirect: /*                                        */ WriteNoToken(0xE6); return;
							case SPC700Target.XIndirectIncrement: /*                               */ WriteNoToken(0xBF); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.X:
						switch (ResolveAmbiguities(targS, valS)) {
							case SPC700Target.A: /*                                                */ WriteNoToken(0x5D); return;
							case SPC700Target.SP: /*                                               */ WriteNoToken(0x9D); return;
							case SPC700Target.Immediate: /*                                        */ WriteBAgg(0xCD, valS); return;
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0xF8, valS); return;
							case SPC700Target.DirectPageIndexedY: /*                               */ WriteBAgg(0xF9, valS); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0xE9, valS); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.Y:
						switch (ResolveAmbiguities(targS, valS)) {
							case SPC700Target.A: /*                                                */ WriteNoToken(0xFD); return;
							case SPC700Target.Immediate: /*                                        */ WriteBAgg(0x8D, valS); return;
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0xEB, valS); return;
							case SPC700Target.DirectPageIndexedX: /*                               */ WriteBAgg(0xFB, valS); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0xEC, valS); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.SP:
						if (targS is SPC700Target.X) {
							WriteNoToken(0xBD);
						} else {
							InvalidAddressingMode();
						}
						return;

					case SPC700Target.XIndirect:
						if (targS is SPC700Target.A) {
							WriteNoToken(0xC6);
						} else {
							InvalidAddressingMode();
						}
						return;

					case SPC700Target.XIndirectIncrement:
						if (targS is SPC700Target.A) {
							WriteNoToken(0xAF);
						} else {
							InvalidAddressingMode();
						}
						return;
				}

				// if destination failed, try to resolve base on source
				switch (targS) {
					case SPC700Target.A:
						switch (ResolveAmbiguities(targD, valD)) {
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0xC4, valD); return;
							case SPC700Target.DirectPageIndexedX: /*                               */ WriteDotB(0xD4, valD); return;
							case SPC700Target.DirectPageIndirectX: /*                              */ WriteBAgg(0xC7, valD); return;
							case SPC700Target.DirectPageIndirectY: /*                              */ WriteBAgg(0xD7, valD); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0xC5, valD); return;
							case SPC700Target.AbsoluteIndexedX: /*                                 */ WriteDotW(0xD5, valD); return;
							case SPC700Target.AbsoluteIndexedY: /*                                 */ WriteDotW(0xD6, valD); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.X:
						switch (ResolveAmbiguities(targD, valD)) {
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0xD8, valD); return;
							case SPC700Target.DirectPageIndexedY: /*                               */ WriteBAgg(0xD9, valD); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0xC9, valD); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.Y:
						switch (ResolveAmbiguities(targD, valD)) {
							case SPC700Target.DirectPage: /*                                       */ WriteDotB(0xCB, valD); return;
							case SPC700Target.DirectPageIndexedX: /*                               */ WriteBAgg(0xDB, valD); return;
							case SPC700Target.Absolute: /*                                         */ WriteDotW(0xCC, valD); return;
							default: /*                                                            */ InvalidAddressingMode(); return;
						}

					case SPC700Target.Immediate:
						if (targD is SPC700Target.DirectPage or SPC700Target.Ambiguous) {
							WriteDotBB(0x8F, valD, valS);
							return;
						}
						break;

					case SPC700Target.DirectPage or SPC700Target.Ambiguous:
						if (targD is SPC700Target.DirectPage or SPC700Target.Ambiguous) {
							WriteDotBB(0xFA, valD, valS);
							return;
						}
						break;
				}

				InvalidAddressingMode();
				break;

			case OpSPC.MOV0: /*                                                                    */ AssembleOneBitMov(0); break;
			case OpSPC.MOV1: /*                                                                    */ AssembleOneBitMov(1); break;
			case OpSPC.MOV2: /*                                                                    */ AssembleOneBitMov(2); break;
			case OpSPC.MOV3: /*                                                                    */ AssembleOneBitMov(3); break;
			case OpSPC.MOV4: /*                                                                    */ AssembleOneBitMov(4); break;
			case OpSPC.MOV5: /*                                                                    */ AssembleOneBitMov(5); break;
			case OpSPC.MOV6: /*                                                                    */ AssembleOneBitMov(6); break;
			case OpSPC.MOV7: /*                                                                    */ AssembleOneBitMov(7); break;

			case OpSPC.MOVW:
				if (opstr.SplitByComma(out strD, out strS)) {
					if (IsYA(strD)) {
						var (targYA, exprYA) = GetOneMode(strS);

						if (targYA is SPC700Target.DirectPage or SPC700Target.Ambiguous) {
							AggroDotB(size);
						} else {
							InvalidAddressingMode();
						}

						WriteDotB(0xBA, exprYA);
						return;

					} else if (IsYA(strS)) {
						var (targYA, exprYA) = GetOneMode(strD);

						if (targYA is SPC700Target.DirectPage or SPC700Target.Ambiguous) {
							AggroDotB(size);
						} else {
							InvalidAddressingMode();
						}

						WriteDotB(0xDA, exprYA);
						return;
					}
				}

				InvalidAddressingMode();
				PlaceholderOp(0xBA, 1);

				break;

			case OpSPC.MUL:
				WriteNoToken(0xCF);

				if (!IsYA(opstr)) {
					InvalidAddressingMode();
				}

				break;

			case OpSPC.NOP: /*                                                                     */ AssembleImplied(opstr, size, 0x00); break;

			case OpSPC.NOT0: /*                                                                    */ AssembleOneBitNot(0); break;
			case OpSPC.NOT1: /*                                                                    */ AssembleOneBitNot(1); break;
			case OpSPC.NOT2: /*                                                                    */ AssembleOneBitNot(2); break;
			case OpSPC.NOT3: /*                                                                    */ AssembleOneBitNot(3); break;
			case OpSPC.NOT4: /*                                                                    */ AssembleOneBitNot(4); break;
			case OpSPC.NOT5: /*                                                                    */ AssembleOneBitNot(5); break;
			case OpSPC.NOT6: /*                                                                    */ AssembleOneBitNot(6); break;
			case OpSPC.NOT7: /*                                                                    */ AssembleOneBitNot(7); break;

			case OpSPC.NOTC: /*                                                                    */ AssembleImplied(opstr, size, 0xED); break;

			case OpSPC.OR:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();
				DoStandardAccumulatorModes(0x00, targD, valD, targS, valS);
				break;

			case OpSPC.OR0: /*                                                                     */ AssembleOneBitOpsWithNot(0, 0x0A, 0x2A); break;
			case OpSPC.OR1: /*                                                                     */ AssembleOneBitOpsWithNot(1, 0x0A, 0x2A); break;
			case OpSPC.OR2: /*                                                                     */ AssembleOneBitOpsWithNot(2, 0x0A, 0x2A); break;
			case OpSPC.OR3: /*                                                                     */ AssembleOneBitOpsWithNot(3, 0x0A, 0x2A); break;
			case OpSPC.OR4: /*                                                                     */ AssembleOneBitOpsWithNot(4, 0x0A, 0x2A); break;
			case OpSPC.OR5: /*                                                                     */ AssembleOneBitOpsWithNot(5, 0x0A, 0x2A); break;
			case OpSPC.OR6: /*                                                                     */ AssembleOneBitOpsWithNot(6, 0x0A, 0x2A); break;
			case OpSPC.OR7: /*                                                                     */ AssembleOneBitOpsWithNot(7, 0x0A, 0x2A); break;

			case OpSPC.PCALL:
				NoTokens(size);
				WriteOpB(0x4F, ParseOperand(opstr));
				break;

			case OpSPC.POP:
				if (len is 1) {
					char poptest = opstr.GetUnchecked(0).FastUpper();

					if (poptest is 'A') {
						WriteNoToken(0xAE);
						return;
					} else if (poptest is 'X') {
						WriteNoToken(0xCE);
						return;
					} else if (poptest is 'Y') {
						WriteNoToken(0xEE);
						return;
					}
				} else if (IsPSW(opstr)) {
					WriteNoToken(0x8E);
					return; ;
				}

				InvalidAddressingMode();
				PlaceholderOp(0xAE);
				break;

			case OpSPC.PUSH:
				if (len is 1) {
					char pushtest = opstr.GetUnchecked(0).FastUpper();

					if (pushtest is 'A') {
						WriteNoToken(0x2D);
						return;
					} else if (pushtest is 'X') {
						WriteNoToken(0x4D);
						return;
					} else if (pushtest is 'Y') {
						WriteNoToken(0x6D);
						return;
					}
				} else if (IsPSW(opstr)) {
					WriteNoToken(0x0D);
					return;
				}

				InvalidAddressingMode();
				PlaceholderOp(0x2D);
				break;

			case OpSPC.RET: /*                                                                     */ AssembleImplied(opstr, size, 0x6F); break;
			case OpSPC.RETI: /*                                                                    */ AssembleImplied(opstr, size, 0x7F); break;

			case OpSPC.ROL: /*                                                                     */ DoShiftRolls(0x20); break;
			case OpSPC.ROR: /*                                                                     */ DoShiftRolls(0x60); break;

			case OpSPC.SBC:
				(targD, targS, valD, valS) = GetDoubleAddressingModes();
				DoStandardAccumulatorModes(0xA0, targD, valD, targS, valS);
				break;

			case OpSPC.SET0: /*                                                                    */ AssembleDirectPageOnly(0x02); break;
			case OpSPC.SET1: /*                                                                    */ AssembleDirectPageOnly(0x22); break;
			case OpSPC.SET2: /*                                                                    */ AssembleDirectPageOnly(0x42); break;
			case OpSPC.SET3: /*                                                                    */ AssembleDirectPageOnly(0x62); break;
			case OpSPC.SET4: /*                                                                    */ AssembleDirectPageOnly(0x82); break;
			case OpSPC.SET5: /*                                                                    */ AssembleDirectPageOnly(0xA2); break;
			case OpSPC.SET6: /*                                                                    */ AssembleDirectPageOnly(0xC2); break;
			case OpSPC.SET7: /*                                                                    */ AssembleDirectPageOnly(0xE2); break;

			case OpSPC.SETC: /*                                                                    */ AssembleImplied(opstr, size, 0x80); break;
			case OpSPC.SETP: /*                                                                    */ AssembleImplied(opstr, size, 0x40); break;

			case OpSPC.SLEEP: /*                                                                   */ AssembleImplied(opstr, size, 0xEF); break;
			case OpSPC.STOP: /*                                                                    */ AssembleImplied(opstr, size, 0xFF); break;

			case OpSPC.SUBW: /*                                                                    */ AssembleYAopD(0x9A); break;

			case OpSPC.TCALL:
				NoTokens(size);
				WriteOpImplied(new SpcTcallInstruction(ParseOperand(opstr)));
				break;

			case OpSPC.TCLR: /*                                                                    */ AssembleTmodA(0x4E); break;
			case OpSPC.TSET: /*                                                                    */ AssembleTmodA(0x0E); break;

			case OpSPC.XCN: /*                                                                     */ AssembleATargeted(0x9F); break;
		}

		return;

		void WriteDotB(byte opcode, IExpressionReturn val) {
			PreferDotB(size);
			WriteOpB(opcode, val);
		}

		void WriteDotW(byte opcode, IExpressionReturn val) {
			PreferDotW(size);
			WriteOpW(opcode, val);
		}

		void WriteBAgg(byte opcode, IExpressionReturn val) {
			AggroDotB(size);
			WriteOpB(opcode, val);
		}

		void WriteDotBB(byte opcode, IExpressionReturn exprD, IExpressionReturn exprS) {
			AggroDotB(size);
			WriteOpBB(opcode, exprS, exprD);
		}

		void AssembleYAopD(byte opcode) {
			if (opstr.SplitByComma(out strD, out strS) && IsYA(strD)) {
				var (targYA, exprYA) = GetOneMode(strS);

				if (targYA is SPC700Target.DirectPage or SPC700Target.Ambiguous) {
					AggroDotB(size);
				} else {
					InvalidAddressingMode();
				}

				WriteOpB(opcode, exprYA);
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 1);
			}
		}

		void AssembleOneBitMov(int bitx) {
			if (opstr.SplitByComma(out strD, out strS)) {
				if (strD.OnlyChar.CiIs('C')) {
					AggroDotW(size);
					WriteOpW(0xAA, new SpcBitInstruction(ParseOperand(strS), bitx));
					return;
				} else if (strS.OnlyChar.CiIs('C')) {
					AggroDotW(size);
					WriteOpW(0xCA, new SpcBitInstruction(ParseOperand(strD), bitx));
					return;
				}
			}

			InvalidAddressingMode();
			PlaceholderOp(0xAA, 2);
		}

		void AssembleOneBitNot(int bitx) {
			AggroDotW(size);

			WriteOpW(0xEA, new SpcBitInstruction(ParseOperand(opstr), bitx));
		}

		void AssembleOneBitOps(int bitx, byte opcode) {
			if (opstr.SplitByComma(out var obopD, out var obopS) && obopD.OnlyChar.CiIs('C')) {
				AggroDotW(size);
				WriteOpW(opcode, new SpcBitInstruction(ParseOperand(obopS), bitx));
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 2);
			}
		}

		void AssembleOneBitOpsWithNot(int bitx, byte opcode, byte opcodeNot) {
			if (opstr.SplitByComma(out var obopD, out var obopS) && obopD.OnlyChar.CiIs('C')) {

				if (obopS.First is '/') {
					obopS = obopS.Slice(1);
					opcode = opcodeNot;
				}

				AggroDotW(size);
				WriteOpW(opcode, new SpcBitInstruction(ParseOperand(obopS), bitx));
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 2);
			}
		}

		void AssembleDirectPageOnly(byte opcode) {
			var (targ, strOp) = GetOneMode(opstr);

			if (targ is SPC700Target.DirectPage) {
				WriteOpB(opcode, strOp);
			} else if (targ is SPC700Target.Ambiguous) {
				AggressiveSizeTokens();
				WriteOpB(opcode, strOp);
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 1);
			}
		}

		void AssembleBranchBitTest(byte opcode) {
			if (opstr.SplitByComma(out var strTest, out var strBranch)) {
				var (targ, val) = GetOneMode(strTest);

				if (targ is not (SPC700Target.DirectPage or SPC700Target.Ambiguous)) {
					InvalidAddressingMode();
					PlaceholderOp(opcode, 2);
					return;
				}

				AggroDotB(size);

				int branchLen = strBranch.Length;

				if (branchLen is 0) {
					MissingOperand();
					PlaceholderOp(opcode, 2);
				} else {
					char c = strBranch.GetUnchecked(0);
					val = ParseOperand(strTest);

					if (branchLen is 1) {
						if (c is BROP) {
							WriteOpBBrop(opcode, val);
							return;
						} else if (c is JNOP) {
							WriteOpBB(opcode, val, 0x00);
							return;
						} else if (c is BTYS) {
							WriteOpBB(opcode, val, 0xFE);
							return;
						}
					}

					if (c is BranchLiteral) {
						WriteOpBB(opcode, val, ParseOperandSliced(strBranch, 1));
					} else {
						WriteOpBRel(opcode, val, ParseOperand(strBranch));
					}
				}
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 2);
			}
		}

		void AssembleTmodA(byte opcode) {
			var (tmopD, tmopS, tvalD, tvalS) = GetDoubleAddressingModes();

			if (tmopS is SPC700Target.A) {
				AggroDotW(size);
				WriteOpW(opcode, tvalD);
			} else {
				InvalidAddressingMode();
				PlaceholderOp(opcode, 2);
			}
		}

		void DoShiftRolls(byte opbase) {
			var (srTarg, srVal) = GetOneMode(opstr);

			switch (ResolveAmbiguities(srTarg, srVal)) {
				case SPC700Target.A: /*                                                            */ WriteNoToken((byte) (opbase | 0x1C)); return;
				case SPC700Target.DirectPage: /*                                                   */ WriteDotB((byte) (opbase | 0x0B), srVal); return;
				case SPC700Target.Absolute: /*                                                     */ WriteDotW((byte) (opbase | 0x0C), srVal); return;
				case SPC700Target.DirectPageIndexedX: /*                                           */ WriteBAgg((byte) (opbase | 0x1B), srVal); return;
				default: /*                                                                        */ InvalidAddressingMode(); return;
			}
		}

		void DoStandardAccumulatorModes(byte opbase, SPC700Target destination, IExpressionReturn valD, SPC700Target source, IExpressionReturn valS) {
			switch (destination) {
				case SPC700Target.A:
					switch (ResolveAmbiguities(source, valS)) {
						case SPC700Target.Immediate: /*                                            */ WriteBAgg((byte) (opbase | 0x08), valS); return;
						case SPC700Target.XIndirect: /*                                            */ WriteNoToken((byte) (opbase | 0x06)); return;
						case SPC700Target.DirectPage: /*                                           */ WriteDotB((byte) (opbase | 0x04), valS); return;
						case SPC700Target.DirectPageIndexedX: /*                                   */ WriteDotB((byte) (opbase | 0x14), valS); return;
						case SPC700Target.DirectPageIndirectY: /*                                  */ WriteBAgg((byte) (opbase | 0x17), valS); return;
						case SPC700Target.DirectPageIndirectX: /*                                  */ WriteBAgg((byte) (opbase | 0x07), valS); return;
						case SPC700Target.Absolute: /*                                             */ WriteDotW((byte) (opbase | 0x05), valS); return;
						case SPC700Target.AbsoluteIndexedX: /*                                     */ WriteDotW((byte) (opbase | 0x15), valS); return;
						case SPC700Target.AbsoluteIndexedY: /*                                     */ WriteDotW((byte) (opbase | 0x16), valS); return;
					}

					break;

				case SPC700Target.XIndirect:
					if (destination is SPC700Target.YIndirect) {
						WriteNoToken((byte) (opbase | 0x19));
						return;
					}
					break;

				case SPC700Target.DirectPage or SPC700Target.Ambiguous:
					source = ResolveAmbiguities(source, valS);

					if (source is SPC700Target.Immediate) {
						WriteDotBB((byte) (opbase | 0x18), valD, valS);
						return;
					} else if (source is SPC700Target.DirectPage) {
						WriteDotBB((byte) (opbase | 0x09), valD, valS);
						return;
					}

					break;
			}

			InvalidAddressingMode();
		}

		static SPC700Target ResolveAmbiguities(SPC700Target target, IExpressionReturn operand) {
			return target switch {
				SPC700Target.Ambiguous => PickTargetSizeBasedOnOperand(target, operand, SPC700Target.DirectPage, SPC700Target.Absolute),
				SPC700Target.AmbiguousX => PickTargetSizeBasedOnOperand(target, operand, SPC700Target.DirectPageIndexedX, SPC700Target.AbsoluteIndexedX),
				SPC700Target.AmbiguousY => SPC700Target.AbsoluteIndexedY,
				_ => target
			};
		}

		static SPC700Target PickTargetSizeBasedOnOperand(SPC700Target targ, IExpressionReturn valtest, SPC700Target for8, SPC700Target for16) {
			if (valtest.NeedsRequest || !SnesHelpers.FitsIn8(valtest.ValueInt32)) {
				return for16;
			} else {
				return for8;
			}
		}

		(SPC700Target targetD, SPC700Target targetS, IExpressionReturn exprD, IExpressionReturn exprS) GetDoubleAddressingModes() {
			if (opstr.SplitByComma(out var opStrD, out var opStrS)) {
				var (retTargD, retValD) = GetOneMode(opStrD);
				var (retTargS, retValS) = GetOneMode(opStrS);

				return (retTargD, retTargS, retValD, retValS);
			} else {
				return (SPC700Target.Invalid, SPC700Target.Invalid, MathHelpers.InvalidExpression, MathHelpers.InvalidExpression);
			}

		}

		(SPC700Target target, IExpressionReturn expression) GetOneMode(OperandString mstr) {
			int mlen = mstr.Length;

			if (mlen is 0) {
				return (SPC700Target.Empty, MathHelpers.InvalidExpression);
			}

			char l1 = mstr.First;

			if (mlen is 1) {
				char f1 = l1.FastUpper();
				if (f1 is 'A') return (SPC700Target.A, MathHelpers.InvalidExpression);
				if (f1 is 'X') return (SPC700Target.X, MathHelpers.InvalidExpression);
				if (f1 is 'Y') return (SPC700Target.Y, MathHelpers.InvalidExpression);
				if (f1 is 'C') return (SPC700Target.C, MathHelpers.InvalidExpression);
				if (l1 is SPLAT) return (SPC700Target.SPLAT, MathHelpers.InvalidExpression);
				if (l1 is BTYS) return (SPC700Target.BTYS, MathHelpers.InvalidExpression);
				if (l1 is JNOP) return (SPC700Target.JNOP, MathHelpers.InvalidExpression);
			} else if (mlen is 2) {
				char f1 = l1.FastUpper();
				if (f1 is 'S') {
					if (mstr.GetUnchecked(1).CiIs('P')) return (SPC700Target.SP, MathHelpers.InvalidExpression);
				} else if (f1 is 'Y') {
					if (mstr.GetUnchecked(1).CiIs('A')) return (SPC700Target.YA, MathHelpers.InvalidExpression);
				}
			}

			if (l1 is '#') {
				return (SPC700Target.Immediate, ParseOperandSliced(mstr, 1));
			}

			if (l1 is '(') {
				// it has to be one of these special ones or it's invalid at this length
				if (mlen < 5) {
					if (mlen is 3) {
						if (mstr.GetUnchecked(2) is ')') {
							char fmid = mstr.GetUnchecked(1).FastUpper();
							if (fmid is 'X') return (SPC700Target.XIndirect, MathHelpers.InvalidExpression);
							if (fmid is 'Y') return (SPC700Target.YIndirect, MathHelpers.InvalidExpression);
						}
					} else if (mlen is 4) {
						if (mstr.GetUnchecked(1).CiIs('X')) {
							char* xitest = mstr.Start + 2;
							char xi1 = *xitest;

							if (xi1 is '+') {
								if (xitest[1] is ')') return (SPC700Target.XIndirectIncrement, MathHelpers.InvalidExpression);
							} else if (xi1 is ')') {
								if (xitest[1] is '+') return (SPC700Target.XIndirectIncrement, MathHelpers.InvalidExpression);
							}
						}
					}

					return (SPC700Target.Invalid, MathHelpers.InvalidExpression);
				}

				char mlast = mstr.Last;

				if (mlast.CiIs('Y')) {
					if (mstr[^2] is '+' && mstr[^3] is ')') {
						mstr = mstr.SliceEndsAndTrim(1, 3);

						return size switch {
							SizeToken.B => (SPC700Target.DirectPageIndirectY, ParseOperand(mstr)),
							SizeToken.W => (SPC700Target.Invalid, ParseOperand(mstr)),
							_ => (SPC700Target.AmbiguousIndirectY, ParseOperand(mstr))
						};
					}
				} else if (mlast is ')') {
					if (mstr[^3] is '+' && mstr[^2].CiIs('X')) {
						mstr = mstr.SliceEndsAndTrim(1, 3);

						return size switch {
							SizeToken.B => (SPC700Target.DirectPageIndirectX, ParseOperand(mstr)),
							SizeToken.W => (SPC700Target.AbsoluteIndirectIndexedX, ParseOperand(mstr)),
							_ => (SPC700Target.AmbiguousIndirectX, ParseOperand(mstr))
						};
					}
				}

				return (SPC700Target.Invalid, MathHelpers.InvalidExpression);
			}

			if (mstr[^2] is '+') {
				char mla = mstr.Last.FastUpper();

				if (mla is 'X') {
					mstr = mstr.SliceEndsAndTrim(0, 2);
					return size switch {
						SizeToken.B => (SPC700Target.DirectPageIndexedX, ParseOperand(mstr)),
						SizeToken.W => (SPC700Target.AbsoluteIndexedX, ParseOperand(mstr)),
						_ => (SPC700Target.AmbiguousX, ParseOperand(mstr))
					};
				}

				if (mla is 'Y') {
					mstr = mstr.SliceEndsAndTrim(0, 2);
					return size switch {
						SizeToken.B => (SPC700Target.DirectPageIndexedY, ParseOperand(mstr)),
						SizeToken.W => (SPC700Target.AbsoluteIndexedY, ParseOperand(mstr)),
						_ => (SPC700Target.AmbiguousY, ParseOperand(mstr))
					};
				}
			}

			return size switch {
				SizeToken.B => (SPC700Target.DirectPage, ParseOperand(mstr)),
				SizeToken.W => (SPC700Target.Absolute, ParseOperand(mstr)),
				_ => (SPC700Target.Ambiguous, ParseOperand(mstr))
			};
		}

		void WriteNoToken(byte opcode) {
			NoTokens(size);
			WriteOpImplied(opcode);
		}

		void AssembleATargeted(byte opcode) {
			NoTokens(size);
			WriteOpImplied(opcode);

			if (len is 0 || opstr.OnlyChar.CiIs('A')) {
				// do nothing
			} else {
				InvalidAddressingMode();
			}
		}

		static bool IsYA(OperandString test) {
			if (test.Length is 2) {
				char* yatest = test.Start;

				//if (BitConverter.IsLittleEndian) return AsciiHelpers.TestLowercase(yatest, 'Y', 'A'); 

				if (*yatest is 'Y' or 'y') {
					return yatest[1] is 'A' or 'a';
				}
			}

			return false;
		}

		static bool IsPSW(OperandString test) {
			if (test.Length is 3) {
				char* yatest = test.Start;

				if (*yatest is 'P' or 'p') {
					if (yatest[1] is 'S' or 's') {
						return yatest[2] is 'W' or 'w';
					}
				}
			}

			return false;
		}
	}

	private static OpSPC TestForSPC700(OperandString chars) {
		int len = chars.Length; // copy so we don't need to check chars.Length every time

		// if it's not even 3-5 characters, give up now
		if (len is < 3) {
			// and actually take care of 2 letter mnemonics here, since there are only three of those
			if (len is 2) {
				char l2a = chars.GetUnchecked(0).FastLower();
				char l2b = chars.GetUnchecked(1).FastLower();

				if (l2a is 'o') { // only one that sees regular use, so it's first
					if (l2b is 'r') return OpSPC.OR;
				} else if (l2b is 'i') {
					if (l2a is 'd') return OpSPC.DI;
					if (l2a is 'e') return OpSPC.EI;
				}
			}

			return OpSPC.NotGood;
		} else if (len > 4) {
			// ditto 5 as we did 2

			if (len is 5) {
				char* read5 = chars.Start;

				char l5a = read5->FastLower();

				ulong l5w;

				if (l5a is 'p') {
					if (AsciiHelpers.TestLowercase(read5 + 1, 'c', 'a', 'l', 'l')) return OpSPC.PCALL;

				} else if (l5a is 't') {
					l5w = AsciiHelpers.GetCharAs64Lower(read5 + 1);

					if (AsciiHelpers.FastTest(l5w, 'c', 'l', 'r', '1')) return OpSPC.TCLR;
					if (AsciiHelpers.FastTest(l5w, 's', 'e', 't', '1')) return OpSPC.TSET;
					if (AsciiHelpers.FastTest(l5w, 'c', 'a', 'l', 'l')) return OpSPC.TCALL;

				} else if (l5a is 's') {
					if (AsciiHelpers.TestLowercase(read5 + 1, 'l', 'e', 'e', 'p')) return OpSPC.SLEEP;
				}
			}

			return OpSPC.NotGood;
		}

		char* reading = chars.Start;

		char a = reading[0].FastLower();
		char b = reading[1].FastLower();
		char c = reading[2].FastLower();

		char d;

		// adding every letter to encourage a jump table

		switch (a) {
			case 'a':
				if (b is 'd') {
					if (len is 3) return SelectC('c', OpSPC.ADC);
					return SelectCD('d', 'w', OpSPC.ADDW);
				}

				if (b is 'n') {
					if (c is 'd') {
						if (len is 3) return OpSPC.AND;

						int movd = reading[3] - '0';

						if ((uint) movd < 8) {
							return OpSPC.AND0 + movd;
						}
					}
				} else if (b is 's' && len is 3) {
					return SelectC('l', OpSPC.ASL);
				}

				return OpSPC.NotGood;

			case 'b':
				if (len is 3) {
					// try to encourage another jump table here
					switch (b) {
						case 'c': return SelectCorC('c', OpSPC.BCC, 's', OpSPC.BCS);

						case 'e': return SelectC('q', OpSPC.BEQ);
						case 'm': return SelectC('i', OpSPC.BMI);
						case 'n': return SelectC('e', OpSPC.BNE);
						case 'p': return SelectC('l', OpSPC.BPL);

						case 'r': return SelectCorC('a', OpSPC.BRA, 'k', OpSPC.BRK);

						case 'v': return SelectCorC('c', OpSPC.BVC, 's', OpSPC.BVS);

						// filler to encourage jump table
						case 'f': return OpSPC.NotGood;
						case 'g': return OpSPC.NotGood;
						case 'h': return OpSPC.NotGood;
						case 't': return OpSPC.NotGood;
						case 'd': return OpSPC.NotGood;

						default: break; // just to shut up the switch expression suggestion
					}
				} else {
					// 4 letter mnemonics
					if (b is 'b') {
						int bbcd = reading[3] - '0';

						if ((uint) bbcd < 8) {
							if (c is 'c') return OpSPC.BBC0 + bbcd;
							if (c is 's') return OpSPC.BBS0 + bbcd;
						}
					}
				}

				return OpSPC.NotGood;

			case 'c':
				if (b is 'm') return SelectCorCD('p', 'w', OpSPC.CMP, OpSPC.CMPW);

				if (len is 4) {
					if (b is 'a') return SelectCD('l', 'l', OpSPC.CALL);
					if (b is 'b') return SelectCD('n', 'e', OpSPC.CBNE);

					if ((b, c) is ('l', 'r')) {
						d = reading[3].FastLower();

						if (d is 'c') return OpSPC.CLRC; // first because it's the most common

						int clrd = d - '0';

						if ((uint) clrd < 8) return OpSPC.CLR0 + clrd;

						if (d is 'v') return OpSPC.CLRV;
						if (d is 'p') return OpSPC.CLRP;
					}
				}

				return OpSPC.NotGood;

			case 'd':
				if (b is 'e') return SelectCorCD('c', 'w', OpSPC.DEC, OpSPC.DECW);

				if (len is 4) {
					return SelectBCD('b', 'n', 'z', OpSPC.DBNZ);
				} else {
					if (b is 'i') return SelectC('v', OpSPC.DIV);
					if (b is 'a') return SelectCorC('a', OpSPC.DAA, 's', OpSPC.DAS);
				}

				return OpSPC.NotGood;

			case 'e':
				if ((b, c) is ('o', 'r')) {
					if (len is 3) return OpSPC.EOR;

					int movd = reading[3] - '0';

					if ((uint) movd < 8) {
						return OpSPC.EOR0 + movd;
					}
				}

				return OpSPC.NotGood;

			case 'f': return OpSPC.NotGood;
			case 'g': return OpSPC.NotGood;
			case 'h': return OpSPC.NotGood;

			case 'i':
				if (b is 'n') return SelectCorCD('c', 'w', OpSPC.INC, OpSPC.INCW);

				return OpSPC.NotGood;

			case 'j':
				if (len is 3) return SelectBC('m', 'p', OpSPC.JMP);

				return OpSPC.NotGood;

			case 'k': return OpSPC.NotGood;
			case 'l':
				if (len is 3) return SelectBC('s', 'r', OpSPC.LSR);

				return OpSPC.NotGood;

			case 'm':
				if (len is 3) {
					if (b is 'o') return SelectC('v', OpSPC.MOV);
					if (b is 'u') return SelectC('l', OpSPC.MUL);
				} else {
					if ((b, c) is ('o', 'v')) {
						d = reading[3];
						if (d is 'w' or 'W') return OpSPC.MOVW;

						int movd = d - '0';
						if ((uint) movd < 8) {
							return OpSPC.MOV0 + movd;
						}
					}
				}

				return OpSPC.NotGood;

			case 'n':
				if (b is 'o') {
					if (len is 4) {
						if (c is 't') {
							d = reading[3].FastLower();

							if (d is 'c') {
								return OpSPC.NOTC;
							}

							int notd = d - '0';

							if ((uint) notd < 8) {
								return OpSPC.NOT0 + notd;
							}
						}
					} else {
						if (c is 'p') {
							return OpSPC.NOP;
						}
					}
				}

				return OpSPC.NotGood;

			case 'o':
				if (len is 3 && b is 'r') {
					int ord = c - '0'; // since we fast lowercase, numbers are unaffected
					if ((uint) ord < 8) {
						return OpSPC.OR0 + ord;
					}
				}

				return OpSPC.NotGood;

			case 'p':
				if (len is 3) return SelectBC('o', 'p', OpSPC.POP);

				return SelectBCD('u', 's', 'h', OpSPC.PUSH);

			case 'q': return OpSPC.NotGood;

			case 'r':
				if (b is 'e') return SelectCorCD('t', 'i', OpSPC.RET, OpSPC.RETI);

				if (len is 3) {
					if (b is 'o') return SelectCorC('l', OpSPC.ROL, 'r', OpSPC.ROR);
				}

				return OpSPC.NotGood;

			case 's':
				if (len is 3) {
					return SelectBC('b', 'c', OpSPC.SBC);
				} else {
					if (b is 'e') {
						if (c is 't') {
							d = reading[3].FastLower();

							if (d is 'c') { // first because it's the most common
								return OpSPC.SETC;
							}

							int setd = d - '0';

							if ((uint) setd < 8) {
								return OpSPC.SET0 + setd;
							} else if (d is 'p') {
								return OpSPC.SETP;
							}
						}
					} else if (b is 'u') {
						return SelectCD('b', 'w', OpSPC.SUBW);
					} else if (b is 't') {
						return SelectCD('o', 'p', OpSPC.STOP);
					}
				}

				return OpSPC.NotGood;

			case 't':
				if (len is 4) {
					if (b is 's') return SelectCD('e', 't', OpSPC.TSET);
					if (b is 'c') return SelectCD('l', 'r', OpSPC.TCLR);
				}

				return OpSPC.NotGood;

			case 'u': return OpSPC.NotGood;
			case 'v': return OpSPC.NotGood;
			case 'w': return OpSPC.NotGood;

			case 'x':
				if (len is 3) return SelectBC('c', 'n', OpSPC.XCN);

				return OpSPC.NotGood;
		}

		return OpSPC.NotGood;

		OpSPC SelectBCD(char bc, char cc, char cd, OpSPC op) {
			if (b == bc) {
				if (c == cc) {
					if (reading[3].FastLower() == cd) {
						return op;
					}
				}
			}

			return OpSPC.NotGood;
		}

		OpSPC SelectBC(char bc, char cc, OpSPC op) {
			if (b == bc) {
				if (c == cc) {
					return op;
				}
			}

			return OpSPC.NotGood;
		}

		OpSPC SelectC(char cc, OpSPC op) {
			return c == cc ? op : OpSPC.NotGood;
		}

		OpSPC SelectCD(char cc, char cd, OpSPC op) {
			if (c == cc) {
				if (reading[3].FastLower() == cd) {
					return op;
				}
			}

			return OpSPC.NotGood;
		}

		OpSPC SelectCorC(char cc1, OpSPC op1, char cc2, OpSPC op2) {
			if (c == cc1) return op1;
			if (c == cc2) return op2;

			return OpSPC.NotGood;
		}

		OpSPC SelectCorCD(char cc, char cd, OpSPC op, OpSPC alt) {
			if (c == cc) {
				if (len == 3) return op;
				if (reading[3].FastLower() == cd) return alt;
			}

			return OpSPC.NotGood;
		}
	}
}

// Special classes to handle these special cases
file sealed class SpcBitInstruction(IExpressionReturn item, int bitx) : IExpressionReturn {
	private readonly int _bitMask = (bitx << 13) & 0xE000;

	public ExpressionState ReturnState => item.ReturnState;

	public bool Resolved => item.Resolved;
	public bool NeedsRequest => item.NeedsRequest;

	public decimal Value {
		get {
			if (!item.Resolved) {
				return MathHelpers.NaN;
			}

			return (item.ValueInt32 & 0x1FFF) | _bitMask;
		}
	}

	public bool TryToResolve() {
		return item.TryToResolve();
	}
}

file sealed class SpcTcallInstruction(IExpressionReturn item) : IExpressionReturn {
	public ExpressionState ReturnState => item.ReturnState;

	public bool Resolved => item.Resolved;
	public bool NeedsRequest => item.NeedsRequest;

	public decimal Value {
		get {
			if (!item.Resolved) {
				return MathHelpers.NaN;
			}

			return (byte) ((item.ValueInt32 << 4) | 1);
		}
	}

	public bool TryToResolve() {
		return item.TryToResolve();
	}
}

file enum SPC700Target : int {
	NotGiven = 0,
	Invalid,

	Empty,

	Ambiguous,
	AmbiguousX,
	AmbiguousY,
	AmbiguousIndirectX,
	AmbiguousIndirectY,

	A,
	X,
	Y,
	YA,
	SP,
	C,

	Immediate,
	Relative,

	XIndirect,
	XIndirectIncrement,
	YIndirect,

	DirectPage,
	DirectPageIndexedX,
	DirectPageIndexedY,
	DirectPageIndirectX,
	DirectPageIndirectY,

	Absolute,
	AbsoluteIndexedX,
	AbsoluteIndirectIndexedX,
	AbsoluteIndexedY,

	// special cases
	SPLAT,
	BTYS,
	JNOP,
}
