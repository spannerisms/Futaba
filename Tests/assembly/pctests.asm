macro AssertPC(addr)
	if {addr} != !:pc
		error "Assertion failed: {addr} != ", pc
	endif
endmacro

macro AssertSite(addr)
	if {addr} != !:site
		error "Assertion failed: {addr} != ", site
	endif
endmacro

org $80_8000
%AssertPC($80_8000)
%AssertSite($80_8000)
site $2000
%AssertPC($80_8000)
%AssertSite($00_2000)

skip 20
%AssertPC($80_8000+20)
%AssertSite($00_2000+20)

site *
%AssertPC($80_8000+20)
%AssertSite(!:pc)

align $40
%AssertPC($80_8040)
%AssertSite($80_8040)

align $40
%AssertPC($80_8040)
%AssertSite($80_8040)

align $20
%AssertPC($80_8040)
%AssertSite($80_8040)

site $0050
%AssertPC($80_8040)
%AssertSite($00_0050)

align $80
%AssertPC($80_8080)
%AssertSite($00_0090)

arrange $80
%AssertPC($80_80F0)
%AssertSite($00_0100)


pushpc

%AssertPC($80_80F0)
%AssertSite($00_0100)

org $80_9000

%AssertPC($80_9000)
%AssertSite($00_0100)

skip $20

%AssertPC($80_9020)
%AssertSite($00_0120)

pullpc

%AssertPC($80_80F0)
%AssertSite($00_0120)


pushsite

%AssertPC($80_80F0)
%AssertSite($00_0120)

site $00_3000

%AssertPC($80_80F0)
%AssertSite($00_3000)

skip $20

%AssertPC($80_8110)
%AssertSite($00_3020)

pullsite

%AssertPC($80_8110)
%AssertSite($00_0120)


skipto $0200

%AssertPC($80_81F0)
%AssertSite($00_0200)


