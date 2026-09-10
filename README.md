# 现场守护 ProcessGuard

Windows 桌面程序。工位软件闪退、网络端口掉线或 **串口掉线** 后，按配置自动重新打开软件。

双击 `ProcessGuard.exe` 即可，工位电脑不用装 Python / Java。

## 签名构建在哪里？

签名步骤已写在仓库里，**不是单独一个“签名版工程”**：

| 位置 | 说明 |
|------|------|
| `.github/workflows/build-windows.yml` | 第 28 行起 `Code sign (optional)` |
| `build.bat` / `sign.bat` | 本机编译 + 本机签名 |
| `签名构建说明.txt` | 详细步骤 |

**未配置证书时会自动跳过签名**，Actions 日志会出现「未配置 WINDOWS_CERT_PFX，跳过签名」。  
要真正签上名，需在 GitHub Secrets 配置 `WINDOWS_CERT_PFX`（pfx 的 Base64）和 `WINDOWS_CERT_PASSWORD`。

详见下文「GitHub 自动构建 + 签名」与 `签名构建说明.txt`。



## 功能

- 多套配置，各自开关 + 应用总开关
- **检测间隔、触发后冷却均可自定义，单位：分钟**
- 条件：
  - 进程消失 / 进程存在
  - 网络端口掉线 / 可连通
  - **串口消失（掉线）/ 串口存在**（如 COM3）
- 条件支持 AND / OR
- 动作：打开指定软件；可先结束残留进程
- 关闭窗口进入托盘，后台继续检测
- 显示已触发次数

配置文件：`%USERPROFILE%\.process_guard\configs.json`

## 串口条件示例

扫码枪 / 工控设备 USB 转串口常会变成 `COM3`：

| 类型 | 含义 |
|------|------|
| `serial_missing` | 串口不在系统中（拔掉/掉线）时成立 |
| `serial_present` | 串口存在时成立 |
| `serial_idle` | **串口空闲**：存在但无人占用（可独占打开）时成立 |
| `serial_busy` | **串口非空闲**：存在且被程序占用时成立 |

软件还在但已丢掉串口连接时，串口会变成空闲，用 `serial_idle` 触发重启更合适。  
添加条件时会列出当前电脑已有串口，也可手动填 `COM3`。

> 判定方式：尝试短暂独占打开 COM 口。能打开=空闲；`UnauthorizedAccess` 等=被占用。检测瞬间可能与业务软件抢端口，请把检测间隔设合理（如 1～2 分钟）。

## 本地编译

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download)。

```bat
dotnet publish -c Release -r win-x64 --self-contained true
```

产物：`bin\Release\net8.0-windows\win-x64\publish\ProcessGuard.exe`

### 本地代码签名（可选）

有代码签名证书 `cert.pfx` 时：

```bat
signtool sign /f cert.pfx /p 证书密码 /tr http://timestamp.digicert.com /td sha256 /fd sha256 ProcessGuard.exe
signtool verify /pa ProcessGuard.exe
```

`signtool` 一般在 Windows SDK 的 `bin\x64` 目录。

## GitHub 自动构建 + 签名

推送后 Actions 自动出 exe。打 tag（如 `v1.1.0`）会挂到 Release。

### 配置签名 Secrets（可选）

仓库 **Settings → Secrets and variables → Actions** 添加：

| Secret | 说明 |
|--------|------|
| `WINDOWS_CERT_PFX` | 证书 `.pfx` 文件的 **Base64** 全文 |
| `WINDOWS_CERT_PASSWORD` | 证书密码 |

生成 Base64（本机 PowerShell）：

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("cert.pfx")) | Set-Clipboard
```

未配置 Secrets 时仍会构建未签名的 exe，不影响使用；签名后可减少 SmartScreen 拦截。

## 初始化仓库

```bat
git init
git add .
git commit -m "feat: 现场守护 v1.1 串口检测与签名构建"
git branch -M main
git remote add origin 你的仓库地址.git
git push -u origin main
```
