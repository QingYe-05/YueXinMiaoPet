@echo off
setlocal
set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%codex-status.ps1" -Status coding -Title "Codex coding" -Message "Updating YueXinMiaoPet project" -Task "example" -Progress 45 -Source "bat"
endlocal
