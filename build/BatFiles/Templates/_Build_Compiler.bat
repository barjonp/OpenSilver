@ECHO off

SET BUILD_DIR=%~dp0..\..
SET SRC_DIR=%~dp0..\..\..\src

REM Define the escape character for colored text
for /F %%a in ('"prompt $E$S & echo on & for %%b in (1) do rem"') do set "ESC=%%a"

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
	SET /P PKG_VER="%ESC%[92mOpenSilver.Compiler version:%ESC%[0m "
) ELSE (
	SET PKG_VER=%2
)

ECHO.
ECHO %ESC%[95mRestoring NuGet packages%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget restore %SRC_DIR%\OpenSilver.sln -v quiet

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0m%CFG% %ESC%[95mconfiguration%ESC%[0m
ECHO. 
msbuild %BUILD_DIR%\slnf\OpenSilver.Compiler.slnf -p:Configuration=%CFG% -clp:ErrorsOnly -restore

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0mOpenSilver.Compiler %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget pack %BUILD_DIR%\nuspec\OpenSilver.Compiler.nuspec -OutputDirectory "%BUILD_DIR%\output\OpenSilver" -Properties "PackageVersion=%PKG_VER%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver"

GOTO :end

:invalid
ECHO '%1' is not a valid configuration (SL or UWP)
PAUSE
EXIT

:end