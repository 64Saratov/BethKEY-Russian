@echo off
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

set RES=
if exist "src_ru\Skyrim_AE_NewFormat.txt" set RES=%RES% /res:"src_ru\Skyrim_AE_NewFormat.txt"
if exist "src_ru\Skyrim_SE_OldFormat.txt" set RES=%RES% /res:"src_ru\Skyrim_SE_OldFormat.txt"
if exist "src_ru\BethKEY.ico" set RES=%RES% /res:"src_ru\BethKEY.ico" /win32icon:"src_ru\BethKEY.ico"

"%CSC%" /nologo /target:winexe /platform:anycpu /out:BethKEY_ru.exe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Data.dll %RES% src_ru\*.cs

if errorlevel 1 (
    echo.
    echo [FAIL] Compilation failed - see errors above.
) else (
    echo.
    echo [OK] BethKEY_ru.exe built. Run it and compare with the original.
)
pause
