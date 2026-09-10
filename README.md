=# 现场守护 ProcessGuard

Windows 桌面程序。工位软件闪退或端口掉线后，按配置自动重新打开。

双击 `ProcessGuard.exe` 即可使用，工位电脑不用装 Python / Java。

## 功能

- 多套配置，各自开关 + 应用总开关
- 条件：进程消失 / 进程存在 / 端口掉线 / 端口可连通
- 条件支持 AND / OR
- 动作：打开指定软件
- 打开前可先结束残留进程
- 触发后冷却，避免连开多个窗口
- 关闭窗口进入托盘，后台继续检测
- 显示已触发次数

配置文件：`%USERPROFILE%\.process_guard\configs.json`

## 本地编译

需要安装 [.NET 8 SDK](https://dotnet.microsoft.com/download)。

```bat
dotnet publish -c Release -r win-x64 --self-contained true
```

产物：

`bin\Release\net8.0-windows\win-x64\publish\ProcessGuard.exe`

## GitHub 自动出 exe

推送到 GitHub 后，在 Actions 里会自动构建 exe。  
打 tag（例如 `v1.0.0`）时会挂到 Release 上。

## 初始化仓库

```bat
git init
git add .
git commit -m "feat: 现场守护初版"
git branch -M main
git remote add origin 你的仓库地址
git push -u origin main
```
