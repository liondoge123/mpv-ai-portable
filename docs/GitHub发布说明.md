# GitHub 发布说明

## 仓库与 Release 的区别

仓库只保存项目说明、GUI 源码和构建脚本；MPV、VapourSynth、CUDA/TensorRT DLL、模型等完整运行文件放在 GitHub Releases 的附件中。不要把完整运行目录直接提交到仓库，否则会触发 GitHub 的大文件限制。

## 发布资源

推荐 Release 标题：`MPV AI Portable v1.0.8`

推荐附件：

- `MPV-AI-Portable-v1.0.8.7z.001`
- `MPV-AI-Portable-v1.0.8.7z.002`
- `MPV-AI-Portable-v1.0.8.7z.003`
- `MPV-AI-Portable-v1.0.8.7z.004`
- `MPV-AI-Portable-v1.0.8.sha256.txt`

下载者必须下载全部分卷，然后用 7-Zip 打开 `.7z.001` 解压。分卷缺少任何一个都无法解压。

## 校验

PowerShell：

```powershell
Get-FileHash .\MPV-AI-Portable-v1.0.8.7z -Algorithm SHA256
```

如果使用分卷，先按说明合并/打开分卷，再对合并后的 `.7z` 校验。

## 本机重新构建

在 `D:\mpv` 执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\构建-发布包.ps1
```

构建结果位于 `D:\mpv\发布包`。构建脚本会排除本机缓存、TensorRT engine 和开发工具。
