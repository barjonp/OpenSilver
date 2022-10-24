@ECHO off

SETLOCAL

SET BUILD_DIR=%~dp0..\..
SET SRC_DIR=%~dp0..\..\..\src

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

IF "%~1" == "" (
	SET CFG=Release
) ELSE (
	IF /I "%~1" == "Release" (
		SET CFG=Release
		GOTO :select_version
	)
	IF /I "%~1" == "Debug" (
		SET CFG=Debug
		GOTO :select_version
	)
	GOTO :invalid
)

:select_version
IF "%~2" == "" (
	SET /P PKG_VER="%ESC%[92mOpenSilver.JSInterop version:%ESC%[0m "
) ELSE (
	SET PKG_VER=%2
)

:build
ECHO.
ECHO %ESC%[95mRestoring NuGet packages%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget restore %SRC_DIR%\OpenSilver.sln -v quiet

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0m%CFG% %ESC%[95mconfiguration%ESC%[0m
ECHO. 
msbuild %SRC_DIR%\Runtime\JSInterop\OpenSilver.JSInterop.csproj -p:Configuration=%CFG% -clp:ErrorsOnly -restore

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0mOpenSilver.JSInterop %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget pack %BUILD_DIR%\nuspec\OpenSilver.JSInterop.nuspec -OutputDirectory "%BUILD_DIR%\output\OpenSilver" -Properties "PackageVersion=%PKG_VER%;Configuration=%CFG%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver"

GOTO :end

:invalid
ECHO '%1' is not a valid configuration (SL or UWP)
PAUSE
EXIT

:end
ENDLOCAL