@ECHO OFF
RD /S /Q ..\Netch\bin > NUL 2>&1
RD /S /Q ..\Netch\obj > NUL 2>&1
MSBuild /t:Rebuild /p:Configuration=Release;Platform=x64 ..\Netch.sln
MSBuild /t:Rebuild /p:Configuration=Release;Platform=x86 ..\Netch.sln
PAUSE