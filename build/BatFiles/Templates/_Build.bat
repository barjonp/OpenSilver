@ECHO off

SETLOCAL

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

IF "%~1" == "" (
	CALL %~dp0_Prompt_cfg.bat
) ELSE (
	IF /I "%~1" == "SL" (
		SET CFG=SL
		GOTO :select_versions
	)
	IF /I "%~1" == "UWP" (
		SET CFG=UWP
		GOTO :select_versions
	)
	GOTO :invalid_cfg
)

:select_versions
IF "%~2" == "" (
	SET /P JSINTEROP_VER="%ESC%[92mOpenSilver.JSInterop version:%ESC%[0m "
	SET /P RUNTIME_CORE_VER="%ESC%[92mOpenSilver.Runtime.Core version:%ESC%[0m "
	SET /P RUNTIME_VER="%ESC%[92mOpenSilver.Runtime version:%ESC%[0m "
	SET /P COMPILER_VER="%ESC%[92mOpenSilver.Compiler version:%ESC%[0m "
	SET /P WASM_VER="%ESC%[92mOpenSilver.WebAssembly version:%ESC%[0m "
	SET /P SIMULATOR_VER="%ESC%[92mOpenSilver.Simulator version:%ESC%[0m "
	SET /P FULL_VER="%ESC%[92mOpenSilver version:%ESC%[0m "
	GOTO :build
) ELSE (	
	IF "%~3" == "" (
		SET JSINTEROP_VER=%2
		SET RUNTIME_CORE_VER=%2
		SET RUNTIME_VER=%2
		SET COMPILER_VER=%2
		SET WASM_VER=%2
		SET SIMULATOR_VER=%2
		SET FULL_VER=%2
		GOTO :build
	) ELSE (		
		IF "%~4" NEQ "" (
			IF "%~5" NEQ "" (
				IF "%~6" NEQ "" (
					IF "%~7" NEQ "" (
						IF "%~8" NEQ "" (
						    SET JSINTEROP_VER=%2
						    SET RUNTIME_CORE_VER=%3
						    SET RUNTIME_VER=%4
						    SET COMPILER_VER=%5
						    SET WASM_VER=%6
						    SET SIMULATOR_VER=%7
						    SET FULL_VER=%8
						    GOTO :build
						)
					)
				)
			)
		)
		GOTO :invalid_versions
	)
)

:invalid_versions
ECHO %ESC%[95mInvalid arguments. 0, 1, 2 or 8 arguments are expected.%ESC%[0m
ECHO  %ESC%[95m0 argument%ESC%[0m - You will be prompted for the configuration (SL or UWP) and each version
ECHO  %ESC%[95m1 argument%ESC%[0m - You will be prompted for each version
ECHO  %ESC%[95m2 arguments%ESC%[0m - Same version number for all packages
ECHO  %ESC%[95m8 arguments%ESC%[0m - The build configuration and each version number is provided in the arguments
PAUSE
EXIT

:invalid_cfg
ECHO '%1' is not a valid configuration (SL or UWP)
PAUSE
EXIT

:build
CALL %~dp0_Build_JSInterop.bat Release %JSINTEROP_VER%
CALL %~dp0_Build_Runtime_Core.bat %CFG% %RUNTIME_CORE_VER% %JSINTEROP_VER%
CALL %~dp0_Build_Runtime.bat %CFG% %RUNTIME_VER% %RUNTIME_CORE_VER%
CALL %~dp0_Build_Compiler.bat %CFG% %COMPILER_VER%
CALL %~dp0_Build_Wasm.bat %CFG% %WASM_VER% %JSINTEROP_VER%
CALL %~dp0_Build_Simulator.bat %CFG% %SIMULATOR_VER% %JSINTEROP_VER%
CALL %~dp0_Build_Full.bat %CFG% %FULL_VER% %RUNTIME_VER% %COMPILER_VER%

ENDLOCAL