param(
 [Parameter(Mandatory=$true)][string]$SetupDosyasi,
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$BeklenenSHA256,
 [string[]]$WebKlasoru,
 [string]$YedekKlasoru
)
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if(!$WebKlasoru){$WebKlasoru=@((Join-Path $root 'outputs\src\Etiket.Api\wwwroot'),(Join-Path $root 'Merkez\Api\wwwroot'))}
if(!$YedekKlasoru){$YedekKlasoru=Join-Path $root 'work\setup-yayin-yedekleri'}
if(!(Test-Path -LiteralPath $SetupDosyasi -PathType Leaf)){throw 'Yayınlanacak setup dosyası bulunamadı.'}
$SetupDosyasi=[IO.Path]::GetFullPath($SetupDosyasi)
if([IO.Path]::GetExtension($SetupDosyasi) -ine '.exe'){throw 'Yalnızca setup EXE dosyası yayınlanabilir.'}
if((Get-FileHash -LiteralPath $SetupDosyasi -Algorithm SHA256).Hash -ine $BeklenenSHA256){throw 'Setup SHA256 uyuşmuyor; yayın değiştirilmedi.'}
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo($SetupDosyasi)
if($version.ProductName -ne 'Pronom SolidWorks Co-Pilot Kurulumu'){throw 'Pronom setup ürünü doğrulanamadı; yayın değiştirilmedi.'}
function DizinDogrula([string]$path){
 for($d=[IO.DirectoryInfo]::new([IO.Path]::GetFullPath($path));$null -ne $d;$d=$d.Parent){
  if($d.Exists -and ($d.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Yönlendirilmiş yayın klasörü kullanılamaz: '+$d.FullName)}
 }
}
function IndirmePaketiOlustur([string]$hedef){
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $zip=[IO.Compression.ZipFile]::Open($hedef,[IO.Compression.ZipArchiveMode]::Create)
 try{
  $null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$SetupDosyasi,'Otomatik-Etiketleme-Kurulum.exe',[IO.Compression.CompressionLevel]::Optimal)
  $entry=$zip.CreateEntry('README-KURULUM.txt')
  $writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($true))
  try{
   $writer.WriteLine('OTOMATIK ETIKETLEME - KURULUM VE GUNCELLEME')
   $writer.WriteLine('Surum: '+$version.FileVersion)
   $writer.WriteLine('')
   $writer.WriteLine('1. ZIP dosyasinin tumunu bir klasore cikarin.')
   $writer.WriteLine('2. SolidWorks un tum surumlerini kapatin.')
   $writer.WriteLine('3. Otomatik-Etiketleme-Kurulum.exe dosyasini calistirin.')
   $writer.WriteLine('4. Ilk kullanimda Kur, mevcut kurulumda Guncelle secenegini kullanin.')
   $writer.WriteLine('5. Ilk kurulumda merkez API adresini girin. Bu adres, bu paketi indirdiginiz web sitesinin adresidir (ornek: http://MERKEZ:5082). Diger bilgisayarda 127.0.0.1 kullanmayin.')
   $writer.WriteLine('6. Kurulum tamamlaninca SolidWorks u acin. Eklentiler listesinde eklentinin Etkin ve Baslangic kutularini kontrol edin.')
   $writer.WriteLine('')
   $writer.WriteLine('Ilk parca/montaj kaydinda otomatik P/M numarasi verilir. Normal Kaydet mevcut numarayi korur. Farkli Kaydet ve Bagimsiz Yap yeni dosya icin numara alir. Disa aktarmada yeni numara ayrilmaz.')
   $writer.WriteLine('')
   $writer.WriteLine('Bu paketteki EXE mevcut Pronom kurulumuyla uyumludur; kurulum penceresinde ve SolidWorks eklenti listesinde eski Pronom adi gorunebilir. Eski kurulumu kaldirmayin, Guncelle secenegini kullanin.')
   $writer.WriteLine('Kurulum dosyasi SHA256: '+$BeklenenSHA256.ToUpperInvariant())
  }finally{$writer.Dispose()}
 }finally{$zip.Dispose()}
 # Paketin tasidigi EXE ayni dogrulanmis kurulum olmalidir.
 $read=[IO.Compression.ZipFile]::OpenRead($hedef)
 try{
  if($read.Entries.Count -ne 2 -or !$read.GetEntry('README-KURULUM.txt')){throw 'Indirme paketi icerigi eksik.'}
  $stream=$read.GetEntry('Otomatik-Etiketleme-Kurulum.exe').Open();$sha=[Security.Cryptography.SHA256]::Create()
  try{$hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','');if($hash -ine $BeklenenSHA256){throw 'ZIP icindeki kurulum dogrulanamadi.'}}finally{$sha.Dispose();$stream.Dispose()}
 }finally{$read.Dispose()}
}
DizinDogrula $YedekKlasoru
$id=[Guid]::NewGuid().ToString('N')
$stages=@()
try{
 foreach($web in ($WebKlasoru | Select-Object -Unique)){
  if(!(Test-Path -LiteralPath $web -PathType Container)){throw ('Web klasörü bulunamadı: '+$web)}
  $folder=Join-Path ([IO.Path]::GetFullPath($web)) 'indir';DizinDogrula $folder
  $target=Join-Path $folder 'Pronom-Kurulum.exe'
  if((Test-Path -LiteralPath $target) -and ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Yönlendirilmiş setup dosyası değiştirilemez.'}
  $null=New-Item -ItemType Directory -Path $folder -Force
  $temp=Join-Path $folder ('Pronom-Kurulum.'+$id+'.tmp')
  $stages+= [pscustomobject]@{Target=$target;Temp=$temp}
  [IO.File]::Copy($SetupDosyasi,$temp,$false)
  if((Get-FileHash -LiteralPath $temp).Hash -ine $BeklenenSHA256){throw 'Yayın kopyası doğrulanamadı.'}
  $zipTarget=Join-Path $folder 'Otomatik-Etiketleme-Kurulum.zip'
  if((Test-Path -LiteralPath $zipTarget) -and ((Get-Item -LiteralPath $zipTarget).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Yonlendirilmis ZIP degistirilemez.'}
  $zipTemp=Join-Path $folder ('Otomatik-Etiketleme.'+$id+'.tmp')
  $stages+= [pscustomobject]@{Target=$zipTarget;Temp=$zipTemp}
  IndirmePaketiOlustur $zipTemp
 }
 foreach($stage in $stages){
  if(Test-Path -LiteralPath $stage.Target){
   $backup=Join-Path $YedekKlasoru ($id+'-'+[Guid]::NewGuid().ToString('N')+[IO.Path]::GetExtension($stage.Target))
   $null=New-Item -ItemType Directory -Path $YedekKlasoru -Force
   [IO.File]::Replace($stage.Temp,$stage.Target,$backup)
  }else{[IO.File]::Move($stage.Temp,$stage.Target)}
  [pscustomobject]@{Surum=$version.FileVersion;SHA256=(Get-FileHash -LiteralPath $stage.Target).Hash;Dosya=$stage.Target;IndirmeYolu=('/indir/'+[IO.Path]::GetFileName($stage.Target))}
 }
}finally{
 foreach($stage in $stages){if(Test-Path -LiteralPath $stage.Temp){Remove-Item -LiteralPath $stage.Temp}}
}
