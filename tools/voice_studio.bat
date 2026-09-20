@echo off
rem ボイススタジオを起動してブラウザで開く（閉じるときはこの窓を閉じる。窓の中はクリックしない）
cd /d "%~dp0.."
rem 前の窓が残っていたら止める（このポートはこのツール専用）
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":8768 " ^| findstr LISTENING') do taskkill /F /PID %%p >nul 2>&1
start "" http://localhost:8768/
py -3 tools/voice_studio.py
pause
