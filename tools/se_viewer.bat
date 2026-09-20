@echo off
rem 効果音ビューアを起動してブラウザで開く（「Unity に書き込む」が使える。閉じるときはこの窓を閉じる。窓の中はクリックしない）
cd /d "%~dp0.."
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":8765 " ^| findstr LISTENING') do taskkill /F /PID %%p >nul 2>&1
start "" http://localhost:8765/se_viewer.html
py -3 tools/ui_server.py 8765
pause
