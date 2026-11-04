!EXPECTED_ERRORS = 0

macro SE()
	print
	warn "Should error"
	!EXPECTED_ERRORS += 1
endmacro



; invalid label tests

%SE()
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

%SE()
---------------------------------------------------------------------------------------------------------------------------------------------------------

%SE()
#EXTRINSIC_ASSIGNMENT = 0


Label:
^.buoy1
%SE()
^^.buoy2






%SE()
org $80_8000
skip 3
warnpc $80_8001

%SE()
site $0000
skip 2
warnsite $0001

site *

%SE()
skip $8000
skip 1

%SE()
NOP #-1
