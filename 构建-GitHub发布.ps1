$ErrorActionPreference = 'Stop'

$source = [IO.Path]::GetFullPath((Split-Path -Parent $MyInvocation.MyCommand.Path))
$release = Join-Path $source '发布包'
$out = Join-Path $source 'GitHub发布'
$version = '1.0.8'
$appName = "MPV-AI-Portable-v$version"
$appStage = Join-Path $out $appName
$archive = Join-Path $out "$appName.7z"
$sevenZip = Join-Path $source '7z.exe'

if (-not (Test-Path -LiteralPath $release -PathType Container)) {
    $buildScript = Join-Path $source '构建-发布包.ps1'
    if (-not (Test-Path -LiteralPath $buildScript -PathType Leaf)) { throw "找不到发布目录，也找不到构建脚本: $release" }
    & $buildScript
    if ($LASTEXITCODE -ne 0) { throw "自动构建发布目录失败，退出码: $LASTEXITCODE" }
}
if (-not (Test-Path -LiteralPath $sevenZip -PathType Leaf)) { throw "找不到 7z.exe: $sevenZip" }
if (Test-Path -LiteralPath $appStage) { throw "输出目录已存在，请先移动旧目录: $appStage" }
if (Test-Path -LiteralPath $archive) { throw "输出压缩包已存在，请先移动旧文件: $archive" }

New-Item -ItemType Directory -Path $out -Force | Out-Null
Copy-Item -LiteralPath $release -Destination $appStage -Recurse -Force

$models = Join-Path $appStage 'vs-plugins\models'
$modelRoot = (Resolve-Path $models).Path
$keep = @(
    'RealESRGANv2-animevideo-xsx2.onnx',
    'rife\rife_v4.6.onnx',
    'rife_v2\rife_v4.6.onnx'
)
$unused = Get-ChildItem -LiteralPath $models -Recurse -Force -File |
    Where-Object {
        $relative = $_.FullName.Substring($modelRoot.Length).TrimStart('\')
        $keep -notcontains $relative
    }
$excludeArgs = @($unused | ForEach-Object {
    $relative = $_.FullName.Substring($modelRoot.Length).TrimStart('\').Replace('\', '/')
    "-x!$appName/vs-plugins/models/$relative"
})

Push-Location $out
try {
    & $sevenZip a -t7z '-mx=1' '-mmt=on' (Split-Path -Leaf $archive) $appName @excludeArgs
    if ($LASTEXITCODE -ne 0) { throw "7-Zip 创建压缩包失败，退出码: $LASTEXITCODE" }
} finally {
    Pop-Location
}

$partSize = 1800L * 1024L * 1024L
$buffer = New-Object byte[] (8 * 1024 * 1024)
$input = [IO.File]::OpenRead($archive)
try {
    $index = 1
    while ($input.Position -lt $input.Length) {
        $partPath = '{0}.{1:D3}' -f $archive, $index
        if (Test-Path -LiteralPath $partPath) { throw "分卷已存在: $partPath" }
        $output = [IO.File]::Create($partPath)
        try {
            $remaining = [math]::Min($partSize, $input.Length - $input.Position)
            while ($remaining -gt 0) {
                $wanted = [int][math]::Min($buffer.Length, $remaining)
                $read = $input.Read($buffer, 0, $wanted)
                if ($read -le 0) { break }
                $output.Write($buffer, 0, $read)
                $remaining -= $read
            }
        } finally {
            $output.Dispose()
        }
        $index++
    }
} finally {
    $input.Dispose()
}

$hashFile = Join-Path $out "$appName.sha256.txt"
$hashTargets = @($archive) + @(Get-ChildItem -LiteralPath $out -Filter "$appName.7z.*" -File | Sort-Object Name | Select-Object -ExpandProperty FullName)
$lines = @("# SHA-256 checksums for MPV AI Portable v$version", '')
foreach ($target in $hashTargets) {
    $hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToUpperInvariant()
    $lines += ('{0}  {1}' -f $hash, (Split-Path -Leaf $target))
}
$lines | Out-File -LiteralPath $hashFile -Encoding utf8

Write-Host "GitHub 发布包完成: $archive"
Write-Host "备用模型未进入压缩包: $($unused.Count) 个"
Write-Host "分卷和校验文件: $out"
