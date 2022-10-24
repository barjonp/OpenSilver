@ECHO off

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

:prompt_cfg
SET /P c="Select a configuration (%ESC%[92m[Y]%ESC%[0m for SL | %ESC%[92mN%ESC%[0m for UWP)?"
IF /I "%c%" == "Y" (
	SET CFG=SL
	GOTO :end
)
IF /I "%c%" == "" (
	SET CFG=SL
	GOTO :end
)
IF /I "%c%" == "N" (
	SET CFG=UWP
	GOTO :end
)
GOTO :prompt_cfg

:end