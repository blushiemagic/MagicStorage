param(
    [Parameter(Mandatory = $true)][string]$TerrariaExe,
    [Parameter(Mandatory = $true)][string]$OutFile
)
# Copies Re-Logic's ReLogic.dll out of your own Terraria.exe, where it is an embedded resource.
# It is only a compile-time reference for the mods and must never be committed or redistributed.
$ErrorActionPreference = "Stop"
$name = "Terraria.Libraries.ReLogic.ReLogic.dll"
# Reflection-only: inspects the (32-bit) exe without running any of its code.
$asm = [Reflection.Assembly]::ReflectionOnlyLoadFrom($TerrariaExe)
$res = $asm.GetManifestResourceStream($name)
if ($res -eq $null) { throw "Embedded resource '$name' not found in $TerrariaExe" }
$dir = Split-Path -Parent $OutFile
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
$fs = [IO.File]::Create($OutFile)
try { $res.CopyTo($fs) } finally { $fs.Dispose(); $res.Dispose() }
