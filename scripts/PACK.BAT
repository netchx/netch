@ECHO OFF
RD /S /Q release > NUL 2>&1
RD /S /Q x64 > NUL 2>&1
RD /S /Q x86 > NUL 2>&1

MKDIR release > NUL 2>&1
XCOPY /E /Y ..\Netch\bin\x64\Release x64
XCOPY /E /Y ..\Netch\bin\x86\Release x86

PAUSE

CD x64
DEL /F /S /Q *.config
DEL /F /S /Q *.pdb
DEL /F /S /Q *.xml
bc c -l:9 -r Netch.x64.7z *
MOVE Netch.x64.7z ..\release

CD ..\x86
DEL /F /S /Q *.config
DEL /F /S /Q *.pdb
DEL /F /S /Q *.xml
bc c -l:9 -r Netch.x86.7z *
MOVE Netch.x86.7z ..\release

CD ..
RD /S /Q x64
RD /S /Q x86

PAUSE
