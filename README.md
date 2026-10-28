# Solidworks Co-Pilot

### SolidWorks için otomatik etiketleme ve merkezi kayıt takibi

**Otomatik Etiketleme**, parça ve montajlara ortak bir merkezden dosya numarası verir. SolidWorks içindeki normal kayıt akışıyla çalışır; başarılı kayıtlar web arayüzünden aranır ve takip edilir. Küçük ekiplerin aynı numarayı iki kez kullanmadan, dosya adlarını elle yönetmeden çalışması için geliştirilmiştir.

**Güncel sürüm:** Kaydet33 · `2026.10.27.2`
**Kaynak güncellemesi:** 28 Ekim 2026
**Hedef:** Windows x64 · SolidWorks 2020–2023 · SQL Server

> Otomatik Etiketleme ile yapay zekâ destekli **SolidWorks Co-Pilot** ayrı eklentilerdir. Bu depo otomatik etiketleme uygulamasının kaynaklarını ve iki paket için kullanılan web indirme arayüzünü içerir. Ayrı Co-Pilot ürününün derlenmiş DLL/EXE dosyaları burada dağıtılmaz.

## Özellikler

| İşlem | Sonuç |
| --- | --- |
| İlk parça / montaj kaydı | Merkezden `P00001.SLDPRT` / `M00001.SLDASM` biçiminde ad alınır. |
| Normal Kaydet / Ctrl+S | Mevcut numara korunur, yeni belge kaydı oluşmaz. |
| Yeni dosya oluşturan Farklı Kaydet | Yeni dosya için ayrı numara alınır. |
| Bağımsız Yap | Bileşen yeni dosyaya kaydedilir; montaj referansı ve merkezi sonuç doğrulanır. |
| Parasolid, STEP, DXF ve diğer dış formatlar | SLDPRT/SLDASM dışındaki dışa aktarmalarda yeni etiket veya web kaydı oluşturulmaz. |
| Bağlantı kesintisi | Aynı işlem kimliği ve yerel işlem günlüğü korunur; sonuç sonradan toparlanabilir. |
| Web arayüzü | Arama, parça/montaj filtresi, sayfalama, TSİ tarihleri ve dosya adı kopyalama. |
| İndirmeler | Otomatik Etiketleme ve ayrı Co-Pilot paketleri için düzenli indirme sayfası. |

Kaydet33, bağımsız kaydı genel belge tarama kuyruğundan ayırır. Kendi kayıt penceresi kapandıktan sonra güncel arayüz iki ayrı boşta gözleminde uygun olmalıdır; CAD yazımı tek kez yürütülür. Önceki komutlardan kalmış işaretler bu sırayı tek başına engellemez. Belirsiz işlemler körlemesine silinmez veya yeni numarayla tekrarlanmaz.

## Mimari

```mermaid
flowchart LR
    SW[SolidWorks + Otomatik Etiketleme] -->|Rezervasyon / onay / iptal| API[Merkezi API]
    API --> SQL[(SQL Server)]
    WEB[Web kayıt listesi ve indirmeler] --> API
    SW --> CAD[Yerel veya paylaşılan CAD klasörü]
    SW --> LOG[Yerel işlem günlüğü]
```

API ve SQL merkez bilgisayarda; eklenti SolidWorks kullanılan bilgisayarlarda çalışır. CAD dosyaları merkezi API'ye yüklenmez. Dosyaların konumunu kullanıcı belirler.

Uygulama güvenilen yerel ağ kullanımı içindir. API'de kullanıcı kimlik doğrulaması bulunmaz; çalışan servisi doğrudan internete açmayın. Kaynakların GitHub'da açık olması, şirket verilerinin veya çalışan API'nin yayımlanması anlamına gelmez.

## Kullanıcı kurulumu

1. Merkezin web sayfasında **İndirmeler** bölümünü açın.
2. **Etiketleme eklentisini indir** düğmesiyle ZIP'i alın ve çıkarın.
3. SolidWorks'ün tüm sürümleri kapalıyken ZIP'teki kurulum EXE'sini çalıştırın. Mevcut kurulumda **Güncelle** seçeneğini kullanın.
4. Merkez API adresini doğrulayın; başka bilgisayardan bağlanırken `127.0.0.1` yerine merkezin erişilebilir adresini kullanın.
5. SolidWorks'te eklentiyi etkinleştirin ve ilk test kaydını web listesinden kontrol edin.

Her indirme paketi `README-KURULUM.txt` içerir. Kurulum/eklenti listesinde eski **Pronom** adı görülebilir; mevcut kurulumlarla uyumluluk korunmuştur.

### Ayrı Co-Pilot paketi

Webdeki **Co-Pilot’u indir** bağlantısı tam kurulum/güncelleme ZIP'ine yönelir. Kullanıcı ZIP'i kalıcı klasöre çıkarır, SolidWorks'ü kapatır ve `KURULUM.cmd` dosyasını çalıştırır. Bu klasör kurulumdan sonra taşınmamalı veya silinmemelidir.

[`outputs/copilot-kurulum`](outputs/copilot-kurulum) yalnız bize ait kurulum betiğini, README'yi ve dosya hash manifestini içerir; lisanslı eklenti DLL'leri dahil değildir. Kendi yetkili dağıtımınızı hazırlamak için manifestteki beş DLL'yi `dist/addin-v24` altına yerleştirin ve `Kurulum.ps1 -Dogrula` ile doğrulayın. Co-Pilot'un bu sürümünde dosya numaralandırması kaldırılmıştır. Sohbet için mevcut hesap/Codex bağlantısı ayrıca gerekir; bu paket o bağımlılığı kaldırmaz.

**Kurulum R2:** RegAsm uyarıları işlem çıktısında saklanır; kurulum kararı süreç çıkış kodu ve gerçek CodeBase kontrolüyle verilir. Yeni R2 kurulumunun istemci kabulü hâlâ beklemektedir. Co-Pilot için burada SolidWorks 2020 uyumluluk garantisi verilmez.

## Kaynaktan derleme

Gerekenler: Windows x64, .NET SDK 10, .NET Framework 4.8 Developer Pack ve lisanslı SolidWorks kurulumunuzdan alınan API referansları. SQL Server merkezi API için gerekir.

Aşağıdaki üç dosyayı kendi referans klasörünüzde bulundurun:

```text
SolidWorks.Interop.sldworks.dll
SolidWorks.Interop.swconst.dll
SolidWorks.Interop.swpublished.dll
```

Depo kökünde:

```powershell
& .\outputs\kurulum\GitHubKaynakDerle.ps1 -SolidWorksApiKlasoru 'C:\SolidWorks-API-Referanslari'
```

Betik eklentiyi, hedef pencere okuyucusunu ve kurulumu derler; setup'ın kendi kontrollerini çalıştırır. Sonuç `work/github-build/bin/Etiket.Kurulum/release/Pronom.Kurulum.exe` konumundadır. Betik bilgisayara eklenti kurmaz, COM kaydı yapmaz, API başlatmaz veya veritabanını değiştirmez. API referansları ve oluşturulan ikili dosyalar Git dışında kalır.

2020–2023 desteği için temel derleme 2020 API referanslarıyla yapılır. Daha yeni API referanslarıyla derlemek, eski sürüm uyumluluğunu tek başına kanıtlamaz.

### Merkezi API

Önce kendi SQL veritabanınızın şemasını hazırlayın. `outputs/sql` betiklerini inceleyip hedefi doğrulayın; hepsini mevcut veritabanında sırayla çalıştırmayın. Temiz başlangıç ve yedekleme betikleri mevcut veriler için ayrıca planlanmalıdır.

Örnek Windows oturumu bağlantısı:

```powershell
$env:ConnectionStrings__MerkeziVeritabani = 'Server=localhost;Initial Catalog=Pronom_Kullanim;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project .\outputs\src\Etiket.Api --urls http://127.0.0.1:5082
```

Bu başlatma veritabanı oluşturmaz veya sıfırlamaz. Yerel web adresi `http://127.0.0.1:5082`, sağlık kontrolü `/api/health` olur. Sunucu, sertifika ve ağ ayarlarını kendi ortamınıza göre düzenleyin. Depodaki `192.0.2.10` yalnız örnek adrestir.

### Web indirmelerini yayımlama

`SetupYayinla.ps1` doğrulanmış setup EXE'sini ve aynı EXE + README içeren `Otomatik-Etiketleme-Kurulum.zip` paketini webin `indir` klasörüne yayımlar. Co-Pilot kurulumu ayrı hazırlanır. Web sayfasındaki bağlantılar, ilgili dosyalar merkezde yerleştirildikten sonra kullanılabilir; bu GitHub deposu hazır kurulum dosyaları barındırmaz.

`PronomSetupHazirla.ps1` geliştirici ortamındaki tarihsel güncelleme girdilerine dayanır. Temiz klon için `GitHubKaynakDerle.ps1` kullanın.

## Doğrulama ve sınırlar

- Kaydet33 kaynakları 2020 ve 2023 API referanslarıyla daha önce ayrı ayrı derlendi; 921 çekirdek kontrol geçti.
- SolidWorks 2020 SP4 ve 2023 SP0.1 ana kayıt grupları önceki sürümlerde doğrulandı. Masaüstüne kayıt ve dış format kontrolleri iki sürümde kullanıcı kabulünden geçti.
- Kaydet33 Dell / SolidWorks 2023 üzerinde üç bağımsız kayıt, iptal-sonrası tekrar, tek web kaydı, montajı kapatıp açma ve normal Kaydet kontrollerini geçti.
- Kullanıcı Kaydet33'ü daha önce sorun yaşanan bilgisayara da kurdu; Bağımsız Yap testlerinde sorun bildirmedi. Normal kayıt ve bağımsız kayıt süreleri yaklaşık aynı, bir saniyenin biraz üzerindeydi.
- Her servis paketi, dosya biçimi ve ağ ortamı fiziksel olarak ayrı ayrı sınanmış değildir. Kaydet33 için yeni bir gerçek 2020 kabul turu bu yayında yapılmadı. Önceki bileşen yükleme donmasının nedeni kesinleştirilmiş değildir.

## Depo düzeni

```text
outputs/
├── src/                 # API/web, SolidWorks eklentisi, okuyucu ve setup kaynakları
├── tests/               # Çekirdek, HTTP, pencere ve kurtarma kontrolleri
├── sql/                 # Şema ve kontrollü geçiş betikleri
├── kurulum/             # Derleme, dağıtım ve bakım araçları
└── copilot-kurulum/     # Ayrı Co-Pilot için kurulum betikleri; DLL içermez
```

## Paylaşım kapsamı

Gerçek CAD belgeleri, SQL verileri/yedekleri, işlem günlükleri, kişisel yollar, gerçek bağlantı ayarları, hesap/oturum bilgileri ve lisanslı ikili bağımlılıklar bu depoya dahil değildir. Yerel dosyalar ve hassas ayarlar `.gitignore` ile kapsam dışında tutulur.

Bu güncelleme mevcut herkese açık deponun geçmişini korur. Şu aşamada açık kaynak lisansı eklenmemiştir; yeniden kullanım ve dağıtım izinleri için depo sahibiyle iletişime geçin.
