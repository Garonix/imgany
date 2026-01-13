# imgany

[English](./README_EN.md)

Windows 系统托盘剪贴板图片处理工具，支持自动保存、图床上传、常用路径管理等功能。

## 功能特性

### 核心功能
- **剪贴板图片保存**: 复制图片后，在资源管理器中按 `Ctrl+V` 直接保存为 PNG 文件
- **图片链接下载**: 复制图片 URL 后粘贴，自动下载并保存到当前文件夹
- **自动模式**: 监听剪贴板变化，自动保存图片到指定目录

### 图床功能
- **Lsky Pro 支持**: 集成兰空图床 API，支持登录认证和游客上传
- **仅上传模式**: 可选择只上传不保存本地
- **链接自动复制**: 上传成功后图片链接自动写入剪贴板

### 便捷功能
- **常用路径管理**: 保存常用目录，通过托盘菜单快速切换保存路径
- **开机自启动**: 通过 Windows 任务计划程序实现
- **便携模式**: 配置文件保存在程序目录，可随 U 盘携带

## 系统要求

- Windows 10/11
- .NET 10.0 Desktop Runtime 或更高版本

> **⚠️ 首次运行前请安装 .NET 运行时**  
> 下载地址：[.NET 10.0 Desktop Runtime (Windows x64)](https://dotnet.microsoft.com/download/dotnet/10.0)

## 安装

1. 下载最新 Release 或自行编译
2. 解压到任意目录
3. 运行 `imgany.exe`

## 使用说明

### 基本操作
1. 程序启动后常驻系统托盘
2. 复制图片或图片链接
3. 在资源管理器中按 `Ctrl+V` 保存

### 自动模式
1. 右键托盘图标 → 设置 → 启用自动模式
2. 配置保存路径
3. 复制图片后自动保存到指定目录

### 图床配置
1. 勾选"启用图床功能"
2. 填写图床域名（如 `https://img.example.com`）
3. 选择登录方式：
   - 游客上传：无需账号（需服务端支持）
   - 账号登录：填写邮箱和密码

## 配置文件

程序配置保存在 `config.json`，与程序同目录。

```json
{
  "FilePrefix": "Img",
  "AutoSave": false,
  "AutoSavePath": "",
  "FavoritePaths": {},
  "UploadToHost": false,
  "UploadHostUrl": "",
  "EnableUploadNotification": true
}
```

## 构建

```bash
dotnet build -c Release
```

## 技术栈

- C# / .NET 10.0
- Windows Forms
- Windows API (Clipboard, Shell)

## 许可证

MIT License
