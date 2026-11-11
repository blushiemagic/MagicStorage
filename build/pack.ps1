# Builds both mods in Release and writes ready-to-extract zips to dist\:
#   dist\magic-storage-<version>.zip     -> extract into <Terraria>\TerrariaModder\mods\
#   dist\vanilla-ui-skin-<version>.zip
# Only this project's own files go in; Terraria/ReLogic/TerrariaModder files are never packed.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item -Recurse -Force $dist }
New-Item -ItemType Directory -Path $dist | Out-Null

$mods = @(
    @{ Project = "MagicStorageTM"; Id = "magic-storage"; Files = @("MagicStorageTM.dll", "MagicStorageTM.pdb", "manifest.json", "icon.png"); Assets = $true },
    @{ Project = "VanillaUISkin";  Id = "vanilla-ui-skin"; Files = @("VanillaUISkin.dll", "VanillaUISkin.pdb", "manifest.json"); Assets = $false }
)

foreach ($m in $mods) {
    $proj = Join-Path $root $m.Project
    & dotnet build (Join-Path $proj ($m.Project + ".csproj")) -c Release -nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $($m.Project)" }
    $out = Join-Path $proj "bin\Release"
    $version = (Get-Content (Join-Path $proj "manifest.json") -Raw | ConvertFrom-Json).version

    $stage = Join-Path $dist ("stage\" + $m.Id)
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    foreach ($f in $m.Files) { Copy-Item (Join-Path $out $f) $stage }
    if ($m.Assets) { Copy-Item (Join-Path $out "Assets") (Join-Path $stage "Assets") -Recurse }
    Copy-Item (Join-Path $root "LICENSE") $stage

    # Safety net: refuse to pack anything that isn't ours.
    $bad = Get-ChildItem $stage -Recurse -File | Where-Object { $_.Name -match '^(ReLogic|Terraria|TerrariaModder|0Harmony|Microsoft\.Xna)' }
    if ($bad) { throw "Refusing to pack third-party files: $($bad.Name -join ', ')" }

    $zip = Join-Path $dist ("{0}-{1}.zip" -f $m.Id, $version)
    # Entries are written by hand with '/' separators: in Windows PowerShell both Compress-Archive and
    # ZipFile.CreateFromDirectory write '\', which the zip spec forbids and some tools mangle.
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $stageParent = Split-Path -Parent $stage
    $archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem $stage -Recurse -File) {
            $entryName = $file.FullName.Substring($stageParent.Length + 1).Replace('\', '/')
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $archive.Dispose() }
    "packed $zip"
}
Remove-Item -Recurse -Force (Join-Path $dist "stage")
