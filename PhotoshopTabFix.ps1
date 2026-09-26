# Photoshop Tab Fix - show ONLY the file name in document tabs
# (removes "@ zoom% (Layer, RGB/8)" from tab titles)
# Works with every installed Photoshop version and language.
# Run PhotoshopTabFix.bat, or:  powershell -ExecutionPolicy Bypass -File PhotoshopTabFix.ps1 [-Restore]
param([switch]$Restore)

# self-elevate
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
  $args = @('-ExecutionPolicy','Bypass','-File',"`"$PSCommandPath`"")
  if ($Restore) { $args += '-Restore' }
  Start-Process powershell -Verb RunAs -ArgumentList $args
  exit
}

Write-Host ""
Write-Host "Photoshop Tab Fix" -ForegroundColor Cyan
Write-Host "-----------------"

if (Get-Process Photoshop -ErrorAction SilentlyContinue) {
  Write-Host "Close Photoshop first, then run this again." -ForegroundColor Yellow
  Read-Host "Press Enter to exit"; exit
}

$files = Get-ChildItem "C:\Program Files\Adobe\Adobe Photoshop*\Locales\*\Support Files\tw10428_*.dat" -ErrorAction SilentlyContinue
if (-not $files) {
  Write-Host "No Photoshop language files found under C:\Program Files\Adobe." -ForegroundColor Red
  Read-Host "Press Enter to exit"; exit
}

foreach ($f in $files) {
  $p = $f.FullName
  $ver = ($p -split '\\')[3]
  if ($Restore) {
    if (Test-Path "$p.bak") {
      Copy-Item "$p.bak" $p -Force
      Write-Host "[$ver] restored original tabs." -ForegroundColor Green
    } else {
      Write-Host "[$ver] no backup found - nothing to restore."
    }
    continue
  }
  try {
    if (-not (Test-Path "$p.bak")) { Copy-Item $p "$p.bak" -ErrorAction Stop }
    $t = [System.Text.Encoding]::Unicode.GetString([System.IO.File]::ReadAllBytes($p))
    $new = [regex]::Replace($t, '("\$\$\$/ImageWindow/TitleTemplate[^=]*)=[^"]*"', '$1=^0"')
    if ($new -eq $t) {
      Write-Host "[$ver] already patched (or template not found)."
    } else {
      [System.IO.File]::WriteAllBytes($p, [System.Text.Encoding]::Unicode.GetBytes($new))
      Write-Host "[$ver] patched - tabs now show only the file name." -ForegroundColor Green
    }
  } catch {
    Write-Host "[$ver] ERROR: $($_.Exception.Message)" -ForegroundColor Red
  }
}

Write-Host ""
Write-Host "Done. Start Photoshop to see the result."
Write-Host "Note: a Photoshop update restores the default tabs - just run this again."
Read-Host "Press Enter to exit"
