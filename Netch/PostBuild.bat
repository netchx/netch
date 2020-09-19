if %Configuration%==Release (
:: Merge dlls
%ILMergeConsolePath% /wildcards /out:%TargetDir%NetchMerged.exe ^
/lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319" ^
/targetplatform:v4,"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8" ^
%TargetDir%Netch.exe ^
%TargetDir%*.dll

DEL /f %TargetDir%*.dll >NUL 2>&1
MOVE /Y %TargetDir%NetchMerged.exe %TargetDir%Netch.exe >NUL
)

RD /S /Q %TargetDir%bin >NUL 2>&1
RD /S /Q %TargetDir%i18n >NUL 2>&1
RD /S /Q %TargetDir%mode >NUL 2>&1

XCOPY /s /Y %SolutionDir%binaries %TargetDir%bin\ >NUL
XCOPY /s /Y %SolutionDir%translations\i18n %TargetDir%i18n\ >NUL
XCOPY /s /Y %SolutionDir%modes\mode %TargetDir%mode\ >NUL

DEL /f %TargetDir%*.config >NUL 2>&1
DEL /f %TargetDir%*.pdb >NUL 2>&1
RD /s /Q %TargetDir%x86 >NUL 2>&1
