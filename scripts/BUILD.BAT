@ECHO OFF
RD /S /Q ..\Netch\bin > NUL 2>&1
RD /S /Q ..\Netch\obj > NUL 2>&1
MSBuild -m -r /t:Rebuild /p:Configuration=Release;Platform=x64 ..\Netch.sln
MSBuild -m -r /t:Rebuild /p:Configuration=Release;Platform=x86 ..\Netch.sln
PAUSE