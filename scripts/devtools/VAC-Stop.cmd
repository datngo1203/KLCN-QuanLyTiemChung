@echo off
chcp 65001 >nul
set "VAC_POWERSHELL=%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
if not exist "%VAC_POWERSHELL%" set "VAC_POWERSHELL=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
"%VAC_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0VAC.ps1" -Mode stop
set "VAC_RESULT=%ERRORLEVEL%"
pause
exit /b %VAC_RESULT%
