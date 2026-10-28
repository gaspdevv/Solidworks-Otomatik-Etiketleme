param([switch]$Dogrula)
$ErrorActionPreference='Stop'
function PaketiDogrula {
 $m=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'paket.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 $expected=@('Pronom.Copilot.ProductCard.dll','SolidWorks.Interop.sldworks.dll','SolidWorks.Interop.swconst.dll','SolidWorks.Interop.swpublished.dll','SolidWorksCopilot.Addin.dll')
 if($m.Version -ne '2026.10.27-Numarasiz1' -or @($m.Files).Count -ne 5){throw 'Paket surumu veya dosya sayisi gecersiz.'}
 $seen=@{}
 foreach($f in $m.Files){
  if($f.File -notmatch '^dist/addin-v24/([^/]+)$' -or $expected -notcontains $Matches[1] -or $seen.ContainsKey($f.File)){throw 'Gecersiz paket dosyasi.'}
  $seen[$f.File]=$true;$p=Join-Path $PSScriptRoot $f.File
  for($d=[IO.DirectoryInfo]::new((Split-Path $p -Parent));$null -ne $d;$d=$d.Parent){if($d.Exists -and ($d.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Yonlendirilmis paket klasoru kullanilamaz.'}}
  if(!(Test-Path -LiteralPath $p -PathType Leaf) -or ((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Dosya eksik veya yonlendirilmis: '+$f.File)}
  if((Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash -ine $f.SHA256){throw ('Dosya dogrulanamadi: '+$f.File)}
 }
 if(($m.Files | Where-Object {$_.File -eq 'dist/addin-v24/SolidWorksCopilot.Addin.dll'}).SHA256 -ne 'A6E17CDF62766DCFA49F2772D136F1458189D311BF2FC98D3929904A9F37E5E6'){throw 'Beklenmeyen eklenti surumu.'}
 if(($m.Files | Where-Object {$_.File -eq 'dist/addin-v24/Pronom.Copilot.ProductCard.dll'}).SHA256 -ne 'AB0554B861DBFC711B66640081BC4932AE9EDB40564673C189E4BC9B97250623'){throw 'Beklenmeyen urun karti surumu.'}
 return $m.Version
}
function KayitAraciniCalistir([string]$Arac,[string[]]$Argumanlar,[string]$Gunluk){
 # Native stderr, PS5.1 Stop tercihinde hata kaydina donusebilir.
 # Iki akis dosyaya yonlendirilir; basariyi surecin cikis kodu belirler.
 $out=$Gunluk+'.stdout.txt';$err=$Gunluk+'.stderr.txt'
 $process=Start-Process -FilePath $Arac -ArgumentList $Argumanlar -RedirectStandardOutput $out -RedirectStandardError $err -WindowStyle Hidden -Wait -PassThru
 $lines=@(('Cikis kodu: '+$process.ExitCode),'--- stdout ---')
 if(Test-Path -LiteralPath $out){$lines+=Get-Content -LiteralPath $out}
 $lines+='--- stderr ---'
 if(Test-Path -LiteralPath $err){$lines+=Get-Content -LiteralPath $err}
 $lines | Set-Content -LiteralPath $Gunluk -Encoding UTF8
 if($process.ExitCode -ne 0){throw ('COM kayit araci basarisiz. Cikis kodu: '+$process.ExitCode+'. Gunluk: '+$Gunluk)}
}
try {
 $surum=PaketiDogrula
 if($Dogrula){Write-Output ('DOGRULANDI: '+$surum+'; 5 dosya. COM/registry degistirilmedi.');exit 0}
 Add-Type -AssemblyName System.Windows.Forms
 if(![Environment]::Is64BitProcess){throw '64 bit Windows PowerShell ile KURULUM.cmd dosyasini calistirin.'}
 if(Get-Process -Name SLDWORKS,SolidWorksCopilot -ErrorAction SilentlyContinue){throw 'Calismalarinizi kaydedip SolidWorks un tum surumlerini ve Co-Pilot pencerelerini kapatin.'}
 if($PSScriptRoot.StartsWith([IO.Path]::GetTempPath(),[StringComparison]::OrdinalIgnoreCase)){throw 'ZIP dosyasini once kalici bir klasore cikarin. ZIP icinden kurulum yapmayin.'}
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
 if(!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
  $p=Start-Process -FilePath (Join-Path $PSHOME 'powershell.exe') -ArgumentList @('-NoProfile','-STA','-ExecutionPolicy','Bypass','-File',('"'+$PSCommandPath+'"')) -Verb RunAs -WindowStyle Hidden -Wait -PassThru
  exit $p.ExitCode
 }
 $null=PaketiDogrula
 if(Get-Process -Name SLDWORKS,SolidWorksCopilot -ErrorAction SilentlyContinue){throw 'SolidWorks acildi; kurulum durduruldu.'}
 $guid='{E590BEF5-25F6-4B1E-9B3E-9E37D20A6F36}'
 $dll=Join-Path $PSScriptRoot 'dist\addin-v24\SolidWorksCopilot.Addin.dll'
 $regasm=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe'
 if(!(Test-Path -LiteralPath $regasm)){throw '.NET Framework 4.x bulunamadi.'}
 $reg='Registry::HKEY_CLASSES_ROOT\CLSID\'+$guid+'\InprocServer32'
 $previous=Get-ItemProperty -LiteralPath $reg -ErrorAction SilentlyContinue
 $log=Join-Path $PSScriptRoot ('kurulum-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
 [pscustomobject]@{Surum=$surum;OncekiCodeBase=$previous.CodeBase;YeniDLL=$dll} | ConvertTo-Json | Set-Content -LiteralPath ($log+'.json') -Encoding UTF8
 KayitAraciniCalistir -Arac $regasm -Argumanlar @(('"'+$dll+'"'),'/codebase') -Gunluk $log
 $registered=[Uri](Get-ItemProperty -LiteralPath $reg).CodeBase
 if(!$registered.IsFile -or $registered.LocalPath -ine $dll){throw 'Yeni eklenti kaydi dogrulanamadi.'}
 $machine='HKLM:\SOFTWARE\SolidWorks\Addins\'+$guid;$startup='HKCU:\Software\SolidWorks\AddInsStartup\'+$guid
 $null=New-Item -Path $machine -Force;Set-Item -Path $machine -Value 0
 $null=New-ItemProperty -Path $machine -Name Title -Value 'SolidWorks Co-Pilot' -PropertyType String -Force
 $null=New-ItemProperty -Path $machine -Name Description -Value 'Urun karti, teknik resim ve BOM yardimcisi' -PropertyType String -Force
 $null=New-Item -Path $startup -Force;Set-Item -Path $startup -Value 1
 [void][Windows.Forms.MessageBox]::Show('Kurulum tamamlandi. SolidWorks u acin. Bu kurulum klasorunu tasimayin veya silmeyin.','SolidWorks Co-Pilot')
} catch {
 if($Dogrula){Write-Error $_;exit 1}
 Add-Type -AssemblyName System.Windows.Forms
 [void][Windows.Forms.MessageBox]::Show($_.Exception.Message,'Co-Pilot kurulumu tamamlanamadi')
 exit 1
}