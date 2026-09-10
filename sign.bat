@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "EXE=bin\Release\net8.0-windows\win-x64\publish\ProcessGuard.exe"
if not exist "%EXE%" (
  echo 找不到 %EXE%
  echo 请先运行 build.bat 编译
  pause
  exit /b 1
)

if not exist "cert.pfx" (
  echo.
  echo 【未找到证书】请把代码签名证书放到本目录，命名为: cert.pfx
  echo.
  echo 说明:
  echo   - 需要向 CA 购买的代码签名证书（Code Signing Certificate）
  echo   - 自签证书无法消除 Windows SmartScreen 警告
  echo   - 也可只在 GitHub Actions 里配置 Secrets 自动签名
  echo.
  pause
  exit /b 1
)

set /p CERT_PWD=请输入证书密码: 

where signtool >nul 2>&1
if errorlevel 1 (
  echo 正在查找 Windows SDK 中的 signtool ...
  for /f "delims=" %%i in ('dir /s /b "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" 2^>nul') do set "SIGNTOOL=%%i" & goto :found
  echo 找不到 signtool.exe，请安装 Windows SDK
  pause
  exit /b 1
)
set "SIGNTOOL=signtool"

:found
if defined SIGNTOOL if not "%SIGNTOOL%"=="signtool" goto :dosign
if "%SIGNTOOL%"=="signtool" goto :dosign

:dosign
echo.
echo 使用: %SIGNTOOL%
echo 签名: %EXE%
"%SIGNTOOL%" sign /f cert.pfx /p "%CERT_PWD%" /tr http://timestamp.digicert.com /td sha256 /fd sha256 /v "%EXE%"
if errorlevel 1 (
  echo 签名失败
  pause
  exit /b 1
)
"%SIGNTOOL%" verify /pa /v "%EXE%"
echo.
echo 签名完成。
pause
