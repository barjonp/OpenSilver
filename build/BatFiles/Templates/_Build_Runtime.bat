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
	SET PKG_ID=OpenSilver.Runtime
	SET PKG_CORE_ID=OpenSilver.Runtime.Core
) ELSE (
	SET PKG_ID=OpenSilver.Runtime.UwpCompatible
	SET PKG_CORE_ID=OpenSilver.Runtime.Core.UwpCompatible
)

IF "%~2" == "" (
	SET /P PKG_VER="%ESC%[92mOpenSilver.Runtime version:%ESC%[0m "
	SET /P PKG_CORE_VER="%ESC%[92mOpenSilver.Runtime.Core version:%ESC%[0m "
) ELSE (
	SET PKG_VER=%2
	IF "%~3" == "" (
		SET PKG_CORE_VER=%2
	) ELSE (
		SET PKG_CORE_VER=%3
	)
)

:build
ECHO.
ECHO %ESC%[95mRestoring NuGet packages%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget restore %SRC_DIR%\OpenSilver.sln -v quiet

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0m%CFG% %ESC%[95mconfiguration%ESC%[0m
ECHO. 
msbuild %BUILD_DIR%\slnf\OpenSilver.Runtime.slnf -p:Configuration=%CFG% -clp:ErrorsOnly -restore

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0m%PKG_ID% %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget pack %BUILD_DIR%\nuspec\OpenSilver.Runtime.nuspec -OutputDirectory "%BUILD_DIR%\output\OpenSilver" -Properties "PackageId=%PKG_ID%;PackageVersion=%PKG_VER%;OpenSilverRuntimeCoreId=%PKG_CORE_ID%;OpenSilverRuntimeCoreVersion=%PKG_CORE_VER%;Configuration=%CFG%;RepositoryUrl=https://github.com/OpenSilver/OpenSilver"

GOTO :end

:invalid
ECHO '%1' is not a valid configuration (SL or UWP)
PAUSE
EXIT

:end
ENDLOCAL