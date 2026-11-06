org $008000

|008000| BRK #$20                   : print "BRK #$20         " ; 00
|008002| ORA.b ($BE,X)              : print "ORA.b ($BE,X)    " ; 01
|008004| COP #$20                   : print "COP #$20         " ; 02
|008006| ORA 1,S                    : print "ORA 1,S          " ; 03
|008008| TSB.b $BE                  : print "TSB.b $BE        " ; 04
|00800A| ORA.b $BE                  : print "ORA.b $BE        " ; 05
|00800C| ASL.b $BE                  : print "ASL.b $BE        " ; 06
|00800E| ORA.b [$BE]                : print "ORA.b [$BE]      " ; 07
|008010| PHP                        : print "PHP              " ; 08
|008011| ORA.b #$20                 : print "ORA.b #$20       " ; 09
|008013| ASL A                      : print "ASL A            " ; 0A
|008014| PHD                        : print "PHD              " ; 0B
|008015| TSB.w $1234                : print "TSB.w $1234      " ; 0C
|008018| ORA.w $1234                : print "ORA.w $1234      " ; 0D
|00801B| ASL.w $1234                : print "ASL.w $1234      " ; 0E
|00801E| ORA.l $123456              : print "ORA.l $123456    " ; 0F
|008022| ORA.b ($BE),Y              : print "ORA.b ($BE),Y    " ; 11
|008024| ORA.b ($BE)                : print "ORA.b ($BE)      " ; 12
|008026| ORA (1,S),Y                : print "ORA (1,S),Y      " ; 13
|008028| TRB.b $BE                  : print "TRB.b $BE        " ; 14
|00802A| ORA.b $BE,X                : print "ORA.b $BE,X      " ; 15
|00802C| ASL.b $BE,X                : print "ASL.b $BE,X      " ; 16
|00802E| ORA.b [$BE],Y              : print "ORA.b [$BE],Y    " ; 17
|008030| CLC                        : print "CLC              " ; 18
|008031| ORA.w $1234,Y              : print "ORA.w $1234,Y    " ; 19
|008034| INC A                      : print "INC A            " ; 1A
|008035| TCS                        : print "TCS              " ; 1B
|008036| TRB.w $1234                : print "TRB.w $1234      " ; 1C
|008039| ORA.w $1234,X              : print "ORA.w $1234,X    " ; 1D
|00803C| ASL.w $1234,X              : print "ASL.w $1234,X    " ; 1E
|00803F| ORA.l $123456,X            : print "ORA.l $123456,X  " ; 1F
|008043| JSR.w $1234                : print "JSR.w $1234      " ; 20
|008046| AND.b ($BE,X)              : print "AND.b ($BE,X)    " ; 21
|008048| JSL.l $123456              : print "JSL.l $123456    " ; 22
|00804C| AND 1,S                    : print "AND 1,S          " ; 23
|00804E| BIT.b $BE                  : print "BIT.b $BE        " ; 24
|008050| AND.b $BE                  : print "AND.b $BE        " ; 25
|008052| ROL.b $BE                  : print "ROL.b $BE        " ; 26
|008054| AND.b [$BE]                : print "AND.b [$BE]      " ; 27
|008056| PLP                        : print "PLP              " ; 28
|008057| AND.b #$20                 : print "AND.b #$20       " ; 29
|008059| ROL A                      : print "ROL A            " ; 2A
|00805A| PLD                        : print "PLD              " ; 2B
|00805B| BIT.w $1234                : print "BIT.w $1234      " ; 2C
|00805E| AND.w $1234                : print "AND.w $1234      " ; 2D
|008061| ROL.w $1234                : print "ROL.w $1234      " ; 2E
|008064| AND.l $123456              : print "AND.l $123456    " ; 2F
|008068| AND.b ($BE),Y              : print "AND.b ($BE),Y    " ; 31
|00806A| AND.b ($BE)                : print "AND.b ($BE)      " ; 32
|00806C| AND (1,S),Y                : print "AND (1,S),Y      " ; 33
|00806E| BIT.b $BE,X                : print "BIT.b $BE,X      " ; 34
|008070| AND.b $BE,X                : print "AND.b $BE,X      " ; 35
|008072| ROL.b $BE,X                : print "ROL.b $BE,X      " ; 36
|008074| AND.b [$BE],Y              : print "AND.b [$BE],Y    " ; 37
|008076| SEC                        : print "SEC              " ; 38
|008077| AND.w $1234,Y              : print "AND.w $1234,Y    " ; 39
|00807A| DEC A                      : print "DEC A            " ; 3A
|00807B| TSC                        : print "TSC              " ; 3B
|00807C| BIT.w $1234,X              : print "BIT.w $1234,X    " ; 3C
|00807F| AND.w $1234,X              : print "AND.w $1234,X    " ; 3D
|008082| ROL.w $1234,X              : print "ROL.w $1234,X    " ; 3E
|008085| AND.l $123456,X            : print "AND.l $123456,X  " ; 3F
|008089| RTI                        : print "RTI              " ; 40
|00808A| EOR.b ($BE,X)              : print "EOR.b ($BE,X)    " ; 41
|00808C| WDM #$20                   : print "WDM #$20         " ; 42
|00808E| EOR 1,S                    : print "EOR 1,S          " ; 43
|008090| MVP $12,$34                : print "MVP $12,$34      " ; 44
|008093| EOR.b $BE                  : print "EOR.b $BE        " ; 45
|008095| LSR.b $BE                  : print "LSR.b $BE        " ; 46
|008097| EOR.b [$BE]                : print "EOR.b [$BE]      " ; 47
|008099| PHA                        : print "PHA              " ; 48
|00809A| EOR.b #$20                 : print "EOR.b #$20       " ; 49
|00809C| LSR A                      : print "LSR A            " ; 4A
|00809D| PHK                        : print "PHK              " ; 4B
|00809E| JMP.w $1234                : print "JMP.w $1234      " ; 4C
|0080A1| EOR.w $1234                : print "EOR.w $1234      " ; 4D
|0080A4| LSR.w $1234                : print "LSR.w $1234      " ; 4E
|0080A7| EOR.l $123456              : print "EOR.l $123456    " ; 4F
|0080AB| EOR.b ($BE),Y              : print "EOR.b ($BE),Y    " ; 51
|0080AD| EOR.b ($BE)                : print "EOR.b ($BE)      " ; 52
|0080AF| EOR (1,S),Y                : print "EOR (1,S),Y      " ; 53
|0080B1| MVN $12,$34                : print "MVN $12,$34      " ; 54
|0080B4| EOR.b $BE,X                : print "EOR.b $BE,X      " ; 55
|0080B6| LSR.b $BE,X                : print "LSR.b $BE,X      " ; 56
|0080B8| EOR.b [$BE],Y              : print "EOR.b [$BE],Y    " ; 57
|0080BA| CLI                        : print "CLI              " ; 58
|0080BB| EOR.w $1234,Y              : print "EOR.w $1234,Y    " ; 59
|0080BE| PHY                        : print "PHY              " ; 5A
|0080BF| TCD                        : print "TCD              " ; 5B
|0080C0| JML.l $123456              : print "JML.l $123456    " ; 5C
|0080C4| EOR.w $1234,X              : print "EOR.w $1234,X    " ; 5D
|0080C7| LSR.w $1234,X              : print "LSR.w $1234,X    " ; 5E
|0080CA| EOR.l $123456,X            : print "EOR.l $123456,X  " ; 5F
|0080CE| RTS                        : print "RTS              " ; 60
|0080CF| ADC.b ($BE,X)              : print "ADC.b ($BE,X)    " ; 61
|0080D1| ADC 1,S                    : print "ADC 1,S          " ; 63
|0080D3| STZ.b $BE                  : print "STZ.b $BE        " ; 64
|0080D5| ADC.b $BE                  : print "ADC.b $BE        " ; 65
|0080D7| ROR.b $BE                  : print "ROR.b $BE        " ; 66
|0080D9| ADC.b [$BE]                : print "ADC.b [$BE]      " ; 67
|0080DB| PLA                        : print "PLA              " ; 68
|0080DC| ADC.b #$20                 : print "ADC.b #$20       " ; 69
|0080DE| ROR A                      : print "ROR A            " ; 6A
|0080DF| RTL                        : print "RTL              " ; 6B
|0080E0| JMP ($1234)                : print "JMP ($1234)      " ; 6C
|0080E3| ADC.w $1234                : print "ADC.w $1234      " ; 6D
|0080E6| ROR.w $1234                : print "ROR.w $1234      " ; 6E
|0080E9| ADC.l $123456              : print "ADC.l $123456    " ; 6F
|0080ED| ADC.b ($BE),Y              : print "ADC.b ($BE),Y    " ; 71
|0080EF| ADC.b ($BE)                : print "ADC.b ($BE)      " ; 72
|0080F1| ADC (1,S),Y                : print "ADC (1,S),Y      " ; 73
|0080F3| STZ.b $BE,X                : print "STZ.b $BE,X      " ; 74
|0080F5| ADC.b $BE,X                : print "ADC.b $BE,X      " ; 75
|0080F7| ROR.b $BE,X                : print "ROR.b $BE,X      " ; 76
|0080F9| ADC.b [$BE],Y              : print "ADC.b [$BE],Y    " ; 77
|0080FB| SEI                        : print "SEI              " ; 78
|0080FC| ADC.w $1234,Y              : print "ADC.w $1234,Y    " ; 79
|0080FF| PLY                        : print "PLY              " ; 7A
|008100| TDC                        : print "TDC              " ; 7B
|008101| JMP ($1234,X)              : print "JMP ($1234,X)    " ; 7C
|008104| ADC.w $1234,X              : print "ADC.w $1234,X    " ; 7D
|008107| ROR.w $1234,X              : print "ROR.w $1234,X    " ; 7E
|00810A| ADC.l $123456,X            : print "ADC.l $123456,X  " ; 7F
|00810E| STA.b ($BE,X)              : print "STA.b ($BE,X)    " ; 81
|008110| STA 1,S                    : print "STA 1,S          " ; 83
|008112| STY.b $BE                  : print "STY.b $BE        " ; 84
|008114| STA.b $BE                  : print "STA.b $BE        " ; 85
|008116| STX.b $BE                  : print "STX.b $BE        " ; 86
|008118| STA.b [$BE]                : print "STA.b [$BE]      " ; 87
|00811A| DEY                        : print "DEY              " ; 88
|00811B| BIT.b #$20                 : print "BIT.b #$20       " ; 89
|00811D| TXA                        : print "TXA              " ; 8A
|00811E| PHB                        : print "PHB              " ; 8B
|00811F| STY.w $1234                : print "STY.w $1234      " ; 8C
|008122| STA.w $1234                : print "STA.w $1234      " ; 8D
|008125| STX.w $1234                : print "STX.w $1234      " ; 8E
|008128| STA.l $123456              : print "STA.l $123456    " ; 8F
|00812C| STA.b ($BE),Y              : print "STA.b ($BE),Y    " ; 91
|00812E| STA.b ($BE)                : print "STA.b ($BE)      " ; 92
|008130| STA (1,S),Y                : print "STA (1,S),Y      " ; 93
|008132| STY.b $BE,X                : print "STY.b $BE,X      " ; 94
|008134| STA.b $BE,X                : print "STA.b $BE,X      " ; 95
|008136| STX.b $BE,Y                : print "STX.b $BE,Y      " ; 96
|008138| STA.b [$BE],Y              : print "STA.b [$BE],Y    " ; 97
|00813A| TYA                        : print "TYA              " ; 98
|00813B| STA.w $1234,Y              : print "STA.w $1234,Y    " ; 99
|00813E| TXS                        : print "TXS              " ; 9A
|00813F| TXY                        : print "TXY              " ; 9B
|008140| STZ.w $1234                : print "STZ.w $1234      " ; 9C
|008143| STA.w $1234,X              : print "STA.w $1234,X    " ; 9D
|008146| STZ.w $1234,X              : print "STZ.w $1234,X    " ; 9E
|008149| STA.l $123456,X            : print "STA.l $123456,X  " ; 9F
|00814D| LDY.b #$20                 : print "LDY.b #$20       " ; A0
|00814F| LDA.b ($BE,X)              : print "LDA.b ($BE,X)    " ; A1
|008151| LDX.b #$20                 : print "LDX.b #$20       " ; A2
|008153| LDA 1,S                    : print "LDA 1,S          " ; A3
|008155| LDY.b $BE                  : print "LDY.b $BE        " ; A4
|008157| LDA.b $BE                  : print "LDA.b $BE        " ; A5
|008159| LDX.b $BE                  : print "LDX.b $BE        " ; A6
|00815B| LDA.b [$BE]                : print "LDA.b [$BE]      " ; A7
|00815D| TAY                        : print "TAY              " ; A8
|00815E| LDA.b #$20                 : print "LDA.b #$20       " ; A9
|008160| TAX                        : print "TAX              " ; AA
|008161| PLB                        : print "PLB              " ; AB
|008162| LDY.w $1234                : print "LDY.w $1234      " ; AC
|008165| LDA.w $1234                : print "LDA.w $1234      " ; AD
|008168| LDX.w $1234                : print "LDX.w $1234      " ; AE
|00816B| LDA.l $123456              : print "LDA.l $123456    " ; AF
|00816F| LDA.b ($BE),Y              : print "LDA.b ($BE),Y    " ; B1
|008171| LDA.b ($BE)                : print "LDA.b ($BE)      " ; B2
|008173| LDA (1,S),Y                : print "LDA (1,S),Y      " ; B3
|008175| LDY.b $BE,X                : print "LDY.b $BE,X      " ; B4
|008177| LDA.b $BE,X                : print "LDA.b $BE,X      " ; B5
|008179| LDX.b $BE,Y                : print "LDX.b $BE,Y      " ; B6
|00817B| LDA.b [$BE],Y              : print "LDA.b [$BE],Y    " ; B7
|00817D| CLV                        : print "CLV              " ; B8
|00817E| LDA.w $1234,Y              : print "LDA.w $1234,Y    " ; B9
|008181| TSX                        : print "TSX              " ; BA
|008182| TYX                        : print "TYX              " ; BB
|008183| LDY.w $1234,X              : print "LDY.w $1234,X    " ; BC
|008186| LDA.w $1234,X              : print "LDA.w $1234,X    " ; BD
|008189| LDX.w $1234,Y              : print "LDX.w $1234,Y    " ; BE
|00818C| LDA.l $123456,X            : print "LDA.l $123456,X  " ; BF
|008190| CPY.b #$20                 : print "CPY.b #$20       " ; C0
|008192| CMP.b ($BE,X)              : print "CMP.b ($BE,X)    " ; C1
|008194| REP #$20                   : print "REP #$20         " ; C2
|008196| CMP 1,S                    : print "CMP 1,S          " ; C3
|008198| CPY.b $BE                  : print "CPY.b $BE        " ; C4
|00819A| CMP.b $BE                  : print "CMP.b $BE        " ; C5
|00819C| DEC.b $BE                  : print "DEC.b $BE        " ; C6
|00819E| CMP.b [$BE]                : print "CMP.b [$BE]      " ; C7
|0081A0| INY                        : print "INY              " ; C8
|0081A1| CMP.b #$20                 : print "CMP.b #$20       " ; C9
|0081A3| DEX                        : print "DEX              " ; CA
|0081A4| WAI                        : print "WAI              " ; CB
|0081A5| CPY.w $1234                : print "CPY.w $1234      " ; CC
|0081A8| CMP.w $1234                : print "CMP.w $1234      " ; CD
|0081AB| DEC.w $1234                : print "DEC.w $1234      " ; CE
|0081AE| CMP.l $123456              : print "CMP.l $123456    " ; CF
|0081B2| CMP.b ($BE),Y              : print "CMP.b ($BE),Y    " ; D1
|0081B4| CMP.b ($BE)                : print "CMP.b ($BE)      " ; D2
|0081B6| CMP (1,S),Y                : print "CMP (1,S),Y      " ; D3
|0081B8| PEI.b ($BE)                : print "PEI.b ($BE)      " ; D4
|0081BA| CMP.b $BE,X                : print "CMP.b $BE,X      " ; D5
|0081BC| DEC.b $BE,X                : print "DEC.b $BE,X      " ; D6
|0081BE| CMP.b [$BE],Y              : print "CMP.b [$BE],Y    " ; D7
|0081C0| CLD                        : print "CLD              " ; D8
|0081C1| CMP.w $1234,Y              : print "CMP.w $1234,Y    " ; D9
|0081C4| PHX                        : print "PHX              " ; DA
|0081C5| STP                        : print "STP              " ; DB
|0081C6| JML [$1234]                : print "JML [$1234]      " ; DC
|0081C9| CMP.w $1234,X              : print "CMP.w $1234,X    " ; DD
|0081CC| DEC.w $1234,X              : print "DEC.w $1234,X    " ; DE
|0081CF| CMP.l $123456,X            : print "CMP.l $123456,X  " ; DF
|0081D3| CPX.b #$20                 : print "CPX.b #$20       " ; E0
|0081D5| SBC.b ($BE,X)              : print "SBC.b ($BE,X)    " ; E1
|0081D7| SEP #$20                   : print "SEP #$20         " ; E2
|0081D9| SBC 1,S                    : print "SBC 1,S          " ; E3
|0081DB| CPX.b $BE                  : print "CPX.b $BE        " ; E4
|0081DD| SBC.b $BE                  : print "SBC.b $BE        " ; E5
|0081DF| INC.b $BE                  : print "INC.b $BE        " ; E6
|0081E1| SBC.b [$BE]                : print "SBC.b [$BE]      " ; E7
|0081E3| INX                        : print "INX              " ; E8
|0081E4| SBC.b #$20                 : print "SBC.b #$20       " ; E9
|0081E6| NOP                        : print "NOP              " ; EA
|0081E7| XBA                        : print "XBA              " ; EB
|0081E8| CPX.w $1234                : print "CPX.w $1234      " ; EC
|0081EB| SBC.w $1234                : print "SBC.w $1234      " ; ED
|0081EE| INC.w $1234                : print "INC.w $1234      " ; EE
|0081F1| SBC.l $123456              : print "SBC.l $123456    " ; EF
|0081F5| SBC.b ($BE),Y              : print "SBC.b ($BE),Y    " ; F1
|0081F7| SBC.b ($BE)                : print "SBC.b ($BE)      " ; F2
|0081F9| SBC (1,S),Y                : print "SBC (1,S),Y      " ; F3
|0081FB| PEA.w $1234                : print "PEA.w $1234      " ; F4
|0081FE| SBC.b $BE,X                : print "SBC.b $BE,X      " ; F5
|008200| INC.b $BE,X                : print "INC.b $BE,X      " ; F6
|008202| SBC.b [$BE],Y              : print "SBC.b [$BE],Y    " ; F7
|008204| SED                        : print "SED              " ; F8
|008205| SBC.w $1234,Y              : print "SBC.w $1234,Y    " ; F9
|008208| PLX                        : print "PLX              " ; FA
|008209| XCE                        : print "XCE              " ; FB
|00820A| JSR ($1234,X)              : print "JSR ($1234,X)    " ; FC
|00820D| SBC.w $1234,X              : print "SBC.w $1234,X    " ; FD
|008210| INC.w $1234,X              : print "INC.w $1234,X    " ; FE
|008213| SBC.l $123456,X            : print "SBC.l $123456,X  " ; FF


rel:

BPL rel       ; 10
BMI rel       ; 30
BVC rel       ; 50
PER rel       ; 62
BVS rel       ; 70
BRA rel       ; 80
BRL rel       ; 82
BCC rel       ; 90
BCS rel       ; B0
BNE rel       ; D0
BEQ rel       ; F0

