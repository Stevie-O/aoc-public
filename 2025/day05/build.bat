@REM build.bat

perl ..\..\intcode\compile.pl -mapfile=a.map %1 > a.intcode

@copy /y a.intcode %~n1.intcode >nul
