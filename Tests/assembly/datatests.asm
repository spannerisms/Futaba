org $00_8000
db 0, 1, 2, 3, 4, 5, 6, 7
dw 0, 1, 2, 3, 4, 5, 6, 7
dl 0, 1, 2, 3, 4, 5, 6, 7
dd 0, 1, 2, 3, 4, 5, 6, 7

fill byte size 8 :: $BE
fill byte size 8 :: $4321
fill byte count 8 :: $12

fill word size 8 :: $1234
fill word size 7 :: $5678
fill word count 7 :: $BE51

fill long count 7 :: $123456
fill long size 7 :: $ABCDEF
fill long size 8 :: $328197

fill double count 7 :: $BE401299
fill double size 17 :: $F456CD78
fill double size 18 :: $43902155
fill double size 19 :: $13243546

raw 918B4FF23BE6FF9CB2A34B2EF37BD2BCEAE30DAB2047B34A133B4ECE532A9C2F74A695894675DFA7B2130C2DBFDECCF7B4386776912196D20A72B4C7A80F64A3

org $00_C000
fill long until $C020 :: $328197