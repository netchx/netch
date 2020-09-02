if %Configuration%==Release (
:: Merge dlls
%ILMergeConsolePath% %TargetDir%Netch.exe ^
/out:%TargetDir%NetchMerged.exe ^
%TargetDir%Dia2Lib.dll ^
%TargetDir%Interop.NetFwTypeLib.dll ^
%TargetDir%Interop.TaskScheduler.dll ^
%TargetDir%MaxMind.Db.dll ^
%TargetDir%MaxMind.GeoIP2.dll ^
%TargetDir%Microsoft.Diagnostics.FastSerialization.dll ^
%TargetDir%Microsoft.Diagnostics.Tracing.TraceEvent.dll ^
%TargetDir%Microsoft.WindowsAPICodePack.dll ^
%TargetDir%Microsoft.WindowsAPICodePack.Shell.dll ^
%TargetDir%NetchLib.dll ^
%TargetDir%Newtonsoft.Json.dll ^
%TargetDir%OSExtensions.dll ^
%TargetDir%System.Buffers.dll ^
%TargetDir%System.Collections.Immutable.dll ^
%TargetDir%System.Memory.dll ^
%TargetDir%System.Net.IPNetwork.dll ^
%TargetDir%System.Numerics.Vectors.dll ^
%TargetDir%System.Reflection.Metadata.dll ^
%TargetDir%System.Runtime.CompilerServices.Unsafe.dll ^
%TargetDir%TraceReloggerLib.dll

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
