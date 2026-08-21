# MPV AI Portable

Windows 便携式 MPV 整合包，提供图形界面，并整合以下播放模式：

- 普通硬件解码播放
- RIFE 2× AI 插帧
- RIFE 2× + Real-ESRGAN 轻量超分
- 低负载 RIFE 2×
- GLSL 轻量增强（不调用 AI）

## 下载

请到 GitHub 的 **Releases** 下载 `MPV-AI-Portable` 发布资源，不要从仓库的 `Code → Download ZIP` 获取运行包。

完整便携包约数 GB，GitHub 版本会提供 `.7z.001`、`.7z.002` 等分卷。下载全部分卷后，用 7-Zip 打开 `.7z.001` 解压即可。

## 使用

1. 解压完整发布包，保留全部目录结构。
2. 双击 `MPV-GUI.exe`。
3. 选择视频，或把视频拖到窗口中。
4. 选择播放模式并点击“开始播放”。

普通播放和 GLSL 模式不要求 NVIDIA GPU；RIFE/Real-ESRGAN 模式需要正常安装 NVIDIA 驱动。第一次使用 AI 模式或新的视频分辨率时，TensorRT 会为当前显卡编译本机缓存，首次启动可能需要等待几分钟。

## 建议

- 4K：普通播放或 GLSL 轻量增强
- 1080p：RIFE 2× 插帧
- 720p/480p：RIFE + Real-ESRGAN

## 项目目录

- `MPV-GUI.cs`：图形界面源码
- `构建-发布包.ps1`：生成便携发布目录
- `docs/`：发布和目录说明
- `发布包/`：本机生成的运行副本，不应提交到 Git 仓库

## 校验

Windows PowerShell：

```powershell
Get-FileHash .\MPV-AI-Portable-v1.0.8.7z -Algorithm SHA256
```

第三方组件的原始许可证随发布包保留。项目发布结构和使用说明见 `docs/`。
