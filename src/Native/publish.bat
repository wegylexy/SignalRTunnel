setlocal
cd /d %~dp0
dotnet publish --self-contained -r %NuGetRuntimeIdentifier% -c %Configuration%
set ilc=%USERPROFILE%\.nuget\packages\runtime.%NuGetRuntimeIdentifier%.microsoft.dotnet.ilcompiler\6.0.0-rc.1.21420.1
set out=bin\%Configuration%\net6.0\%NuGetRuntimeIdentifier%\publish\
xcopy "%ilc%\sdk\bootstrapperdll.lib" "%out%" /Y
xcopy "%ilc%\sdk\Runtime.lib" "%out%" /Y
xcopy "%ilc%\framework\System.IO.Compression.Native.lib" "%out%" /Y