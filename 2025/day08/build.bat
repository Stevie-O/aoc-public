@echo off

if "%~1"=="-t" (
    set TRACE=--trace
    shift
) else if "%~1"=="--trace" (
    set TRACE=--trace
    shift
)

if "%1" == "" goto no_input

echo on
perl ..\..\intcode\compile.pl %TRACE% -mapfile=a.map %1 > a.intcode
@echo off

copy /y a.intcode %~n1.intcode >nul
goto end

:no_input

echo.
echo Usage: build [-t ^| --trace] file.intcodeS
echo.

:end
