$ErrorActionPreference = 'Stop'

$source = [IO.Path]::GetFullPath((Split-Path -Parent $MyInvocation.MyCommand.Path))
$release = [IO.Path]::GetFullPath((Join-Path $source '发布包'))
if (-not $release.StartsWith($source + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "发布目录必须位于工作目录内: $release"
}
if (-not (Test-Path -LiteralPath $release -PathType Container)) {
    New-Item -ItemType Directory -Path $release | Out-Null
}

$excludeDirs = @(
    'doc', 'installer', 'portable_config-configzip-backup', 'tools',
    'vsgenstubs4', 'mpv', '发布包', 'GitHub发布', 'docs'
)
$excludeFiles = @(
    '安装_AI组件.ps1', '检查环境.ps1',
    '启动-低负载.bat', '启动-普通播放.bat', '启动-选择档位.bat',
    '启动-RIFE+超分.bat', '启动-RIFE插帧.bat',
    'MPV-GUI.bat', 'MPV-GUI.cs', 'MPV-GUI.ps1',
    'mpv-register.bat', 'mpv-unregister.bat', 'updater.bat',
    'get-pip.py', 'MANIFEST.in', 'vsgenstubs.py', 'vsrepo.py',
    'external_player.js', 'mpv_manual.pdf', 'umpv.conf', 'umpv.exe',
    'Custom_Torrent_Real_Derbid_Streaming.lua', '7z.exe', '7z.dll',
    'README-整合包.md', 'README.MD', '构建-发布包.ps1', '构建-GitHub发布.ps1',
    '.gitignore', '.gitattributes'
)

Get-ChildItem -LiteralPath $source -Force |
    Where-Object {
        $_.Name -ne '发布包' -and
        -not ($_.PSIsContainer -and $excludeDirs -contains $_.Name) -and
        -not (-not $_.PSIsContainer -and $excludeFiles -contains $_.Name)
    } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $release -Recurse -Force
        Write-Host "复制: $($_.Name)"
    }

$cache = Join-Path $release 'portable_config\_cache'
if (Test-Path -LiteralPath $cache) { Remove-Item -LiteralPath $cache -Recurse -Force }
$savedProps = Join-Path $release 'portable_config\saved-props.json'
if (Test-Path -LiteralPath $savedProps) { Remove-Item -LiteralPath $savedProps -Force }

Get-ChildItem -LiteralPath $release -Recurse -Force -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -eq '.engine' -or $_.Name -like '*.engine.cache' } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

$readme = Join-Path $release 'README.md'
if (-not (Test-Path -LiteralPath $readme)) { throw "缺少发布说明: $readme" }
Write-Host '发布副本构建完成。'
