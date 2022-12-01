@ECHO off

SETLOCAL

SET BUILD_DIR=%~dp0..\..
SET SRC_DIR=%~dp0..\..\..\src

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

IF "%~1" == "" (
	CALL %~dp0_Prompt_cfg.bat
) ELSE (
	IF /I "%~1" == "SL" (
		SET CFG=SL
		GOTO :select_version
	)
	IF /I "%~1" == "UWP" (
		SET CFG=UWP
		GOTO :select_version
	)
	GOTO :invalid
)

:select_version
IF "%~2" == "" (
	SET /P PKG_VER="%ESC%[92mOpenSilver.Simulator version:%ESC%[0m "
	SET /P JSINTEROP_VER="%ESC%[92mOpenSilver.JSInterop version:%ESC%[0m "
) ELSE (
	SET PKG_VER=%2
	IF "%~3" == "" (
		SET /P JSINTEROP_VER="%ESC%[92mOpenSilver.JSInterop version:%ESC%[0m "
	) ELSE (
		SET JSINTEROP_VER=%3
	)
)

ECHO. 
ECHO %ESC%[95mRestoring NuGet packages%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget restore %SRC_DIR%\OpenSilver.sln -v quiet

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0mOpenSilver.Simulator %ESC%[0m
ECHO. 
msbuild %BUILD_DIR%\slnf\OpenSilver.Simulator.slnf -p:Configuration=%CFG% -clp:ErrorsOnly

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0mOpenSilver.Simulator %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget pack %BUILD_DIR%\nuspec\OpenSilver.Simulator.nuspec -OutputDirectory "%BUILD_DIR%\output\OpenSilver" -Properties "PackageVersion=%PKG_VER%;OpenSilverJSInteropVersion=%JSINTEROP_VER%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver"

GOTO :end

:invalid
ECHO '%1' is not a valid configuration (SL or UWP)
PAUSE
EXIT

:end
ENDLOCAL