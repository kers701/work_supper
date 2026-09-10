@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo [1/2] 发布 ProcessGuard.exe ...
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true
if errorlevel 1 (
  echo 编译失败，请先安装 .NET 8 SDK
  pause
  exit /b 1
)

set "EXE=bin\Release\net8.0-windows\win-x64\publish\ProcessGuard.exe"
echo.
echo 已生成: %EXE%
echo.
echo 若要对 exe 做代码签名，请运行: sign.bat
echo （需自备 cert.pfx 代码签名证书）
echo.
pause
