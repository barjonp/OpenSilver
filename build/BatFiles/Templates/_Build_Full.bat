@ECHO off

SETLOCAL

SET BUILD_DIR=%~dp0..\..

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

IF "%~1" == "" (
	CALL %~dp0_Prompt_cfg.bat
) ELSE (
	IF /I "%~1" == "SL" (
		SET CFG=SL
		GOTO :select_package_id
	)
	IF /I "%~1" == "UWP" (
		SET CFG=UWP
		GOTO :select_package_id
	)
	GOTO :invalid
)

:select_package_id
IF "%CFG%"=="SL" (
	SET PKG_ID=OpenSilver
	SET PKG_RUNTIME_ID=OpenSilver.Runtime
) ELSE (
	SET PKG_ID=OpenSilver.UwpCompatible
	SET PKG_RUNTIME_ID=OpenSilver.Runtime.UwpCompatible
)

IF "%~2" == "" (
	SET /P PKG_VER="%ESC%[92mOpenSilver version:%ESC%[0m "
	SET /P RUNTIME_VER="%ESC%[92mOpenSilver.Runtime version:%ESC%[0m "
	SET /P COMPILER_VER="%ESC%[92mOpenSilver.Compiler version:%ESC%[0m "
) ELSE (
	SET PKG_VER=%2
	IF "%~3" == "" (
		SET RUNTIME_VER=%2
		SET COMPILER_VER=%2
	) ELSE (
		SET RUNTIME_VER=%3
		IF "%~4" == "" (
			SET /P COMPILER_VER="%ESC%[92mOpenSilver.CompilerLUL version:%ESC%[0m "
		) ELSE (
			SET COMPILER_VER=%4
		)
	)
)

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0mOpenSilver %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget pack %BUILD_DIR%\nuspec\OpenSilver.nuspec -OutputDirectory "%BUILD_DIR%\output\OpenSilver" -Properties "PackageId=%PKG_ID%;PackageVersion=%PKG_VER%;OpenSilverRuntimeId=%PKG_RUNTIME_ID%;OpenSilverRuntimeVersion=%RUNTIME_VER%;OpenSilverCompilerVersion=%COMPILER_VER%;Configuration=%CFG%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver"

GOTO :end

:invalid
ECHO '%1' is not a valid configuration (SL or UWP)
PAUSE
EXIT

:end
ENDLOCAL