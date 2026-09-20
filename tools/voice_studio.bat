@echo off
rem ボイススタジオを起動してブラウザで開く（閉じるときはこの窓を閉じる）
cd /d "%~dp0.."
start "" http://localhost:8768/
py -3 toolsoice_studio.py
pause
