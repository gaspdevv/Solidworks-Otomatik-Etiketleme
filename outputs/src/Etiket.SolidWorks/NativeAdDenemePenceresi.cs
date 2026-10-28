using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace Etiket.SolidWorks
{
 internal sealed class NativeAdDenemePenceresi:IDisposable
 {
  readonly NativeAdDenemeIslemi islem;
  readonly HookIsleyici hookIsleyici;
  readonly AltPencereIsleyici altIsleyici;
  readonly HashSet<IntPtr> pencereler=new HashSet<IntPtr>();
  readonly Dictionary<IntPtr,PencereDurumu> oncekiDurumlar=new Dictionary<IntPtr,PencereDurumu>();
  sealed class PencereDurumu { public IntPtr Dialog,Edit,Kaydet;public string Metin;public bool SaltOkunur,KaydetAcik; }
  System.Windows.Forms.Timer hedefSayaci;
  NativeKaydetGirdiKoruyucusu girdiKoruyucusu;
  System.Threading.Tasks.Task<HedefPencereYaniti[]> hedefOkuma;
  Guid sorguId,sonSorguId;
  NativeAdAlaniDurumu adDurumu=new NativeAdAlaniDurumu();
  Action<KaydetmeIslemi,string> kayit;Action<string> bildir;
  bool kayitDogrulaniyor,devretHazir;string sonAd="";IntPtr kayitDialog;
  KaydetmeIslemi devredilen;
  bool standartKayit,nativeKomutYetkili,iptalIstendi,pencereKapandi;
  bool disAktarimYetkili,disAktarimAdi;string disAktarimSonAd="",disAktarimFiltre="";
  IntPtr izlenenDialog;
  IntPtr hook;string otomatikAd="",bekleyenKaynak="";
  static readonly UIntPtr AltKimlik=new UIntPtr(0x50524F4E);
  delegate IntPtr HookIsleyici(int kod,IntPtr w,IntPtr l);
  delegate IntPtr AltPencereIsleyici(IntPtr h,uint m,IntPtr w,IntPtr l,UIntPtr id,UIntPtr veri);
  delegate bool Gezici(IntPtr h,IntPtr veri);
  [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
  [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SetWindowsHookEx(int tur,HookIsleyici callback,IntPtr modul,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr h);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr h,int kod,IntPtr w,IntPtr l);
  [DllImport("user32.dll")]static extern bool EnumChildWindows(IntPtr h,Gezici callback,IntPtr veri);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder s,int boy);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder s,int boy);
  [DllImport("user32.dll")]static extern int GetDlgCtrlID(IntPtr h);
  [DllImport("user32.dll")]static extern IntPtr GetDlgItem(IntPtr h,int id);
  [DllImport("user32.dll")]static extern IntPtr GetParent(IntPtr h);
  [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")]static extern bool IsWindowEnabled(IntPtr h);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll",SetLastError=true)]static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("user32.dll",EntryPoint="GetWindowLongW")]static extern int GetWindowLong(IntPtr h,int index);
  [DllImport("user32.dll")]static extern bool EnableWindow(IntPtr h,bool acik);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,string l);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("comctl32.dll")]static extern bool SetWindowSubclass(IntPtr h,AltPencereIsleyici callback,UIntPtr id,UIntPtr veri);
  [DllImport("comctl32.dll")]static extern bool RemoveWindowSubclass(IntPtr h,AltPencereIsleyici callback,UIntPtr id);
  [DllImport("comctl32.dll")]static extern IntPtr DefSubclassProc(IntPtr h,uint m,IntPtr w,IntPtr l);
  public static bool ModAcik
  {get{try{return File.Exists(Path.Combine(EklentiAyarlari.KullaniciKlasoru,"NativeAdDenemesi.acik"));}catch(Exception){return false;}}}
  public static bool GercekKayitAcik=>File.Exists(Path.Combine(EklentiAyarlari.KullaniciKlasoru,"BagimsizKayit.acik"));
  public static bool StandartKayitAcik=>File.Exists(Path.Combine(EklentiAyarlari.KullaniciKlasoru,"StandartKayit.acik"));
  public bool Aktif=>islem.Aktif&&hook!=IntPtr.Zero;
  public bool RezervasyonBekliyor=>islem.Aktif;
  public bool KapaliPencereSonucuBekliyor=>pencereKapandi||(izlenenDialog!=IntPtr.Zero&&!IsWindow(izlenenDialog));
  public bool KapanisTakibiVar=>izlenenDialog!=IntPtr.Zero&&islem.Aktif;
  public string KapanisTanisi=>"devret="+devretHazir+" kapandi="+KapaliPencereSonucuBekliyor;
  public bool KaynakEslesiyor(string kaynak)=>string.Equals(bekleyenKaynak,kaynak??"",StringComparison.OrdinalIgnoreCase);
  public bool IptalEslesiyor(string kaynak)=>KaynakEslesiyor(kaynak)
   ||(!string.IsNullOrEmpty(otomatikAd)&&string.Equals(Path.GetFileNameWithoutExtension(kaynak??""),otomatikAd,StringComparison.OrdinalIgnoreCase));
  public NativeAdDenemePenceresi(KaydetmeIslemYoneticisi yonetici)
  {islem=new NativeAdDenemeIslemi(yonetici);hookIsleyici=Hook;altIsleyici=AltPencere;}
  public void Hazirla(string tur,string kaynak,Action<KaydetmeIslemi,string> kayit=null,Action<string> bildir=null,bool standartKayit=false)
  {
   try
   {
   islem.KayitSonucunuAl();
   if((islem.Aktif||oncekiDurumlar.Count>0)&&!KaynakEslesiyor(kaynak))throw new InvalidOperationException("Önce önceki belgenin kayıt penceresini tamamlayın.");
   if(islem.KaydaDevredildi)
   {
    KayitOlayTanisi.Yaz("NativeKayitBekliyor","islem="+islem.IslemId+" asama="+islem.BekleyenAsama);
    throw new InvalidOperationException("Önceki kayıt sonucu bekleniyor; işlem kimliği korundu. İşlem: "+islem.IslemId+". Aşama: "+islem.BekleyenAsama);
   }
   this.kayit=kayit;this.bildir=bildir;this.standartKayit=standartKayit;disAktarimYetkili=false;disAktarimAdi=false;nativeKomutYetkili=false;devretHazir=false;devredilen=null;iptalIstendi=false;pencereKapandi=false;izlenenDialog=IntPtr.Zero;
   bekleyenKaynak=kaynak??"";
   if(hook==IntPtr.Zero)
   {
    hook=SetWindowsHookEx(5,hookIsleyici,IntPtr.Zero,GetCurrentThreadId());
    if(hook==IntPtr.Zero)throw new IOException("Native pencere deneme koruması kurulamadı: "+Marshal.GetLastWin32Error());
   }
   if(girdiKoruyucusu==null)girdiKoruyucusu=new NativeKaydetGirdiKoruyucusu(KaydetIstendi);
   otomatikAd="";adDurumu=new NativeAdAlaniDurumu();
   var klasor=Path.GetDirectoryName(kaynak??"");
   if(!Directory.Exists(klasor))klasor=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
   otomatikAd=Path.GetFileNameWithoutExtension(islem.Hazirla(tur,klasor,kayit!=null,standartKayit));
   KayitOlayTanisi.Yaz("NativeAdAyrildi","islem="+islem.IslemId+" ad="+otomatikAd+" mod="+(standartKayit?"standart-kayit":kayit==null?"sadece-pencere":"bagimsiz-kayit"));
   }
   catch{Dispose();throw;}
  }
  IntPtr Hook(int kod,IntPtr w,IntPtr l)
  {
   if(kod==5)try{Uygula(w);}catch(Exception h){KayitOlayTanisi.Yaz("NativeAdHatasi",h.Message);Dispose();}
   return CallNextHookEx(hook,kod,w,l);
  }
  void Uygula(IntPtr dialog)
  {
   if(Sinif(dialog)!="#32770"||pencereler.Contains(dialog))return;
   var adaylar=new List<IntPtr>();
   EnumChildWindows(dialog,(h,v)=>{if(Sinif(h)=="Edit"&&GetDlgCtrlID(h)==1001&&Sinif(GetParent(h))=="ComboBox"&&GetDlgCtrlID(GetParent(h))==0)adaylar.Add(h);return true;},IntPtr.Zero);
   var kaydet=GetDlgItem(dialog,1);var iptal=GetDlgItem(dialog,2);
   if(adaylar.Count!=1||Sinif(kaydet)!="Button"||Sinif(iptal)!="Button")return;
   if(!NativeKayitPenceresiKurali.Eslesiyor(Metin(dialog),Metin(kaydet)))return;
   izlenenDialog=dialog;
   oncekiDurumlar.Add(dialog,new PencereDurumu{Dialog=dialog,Edit=adaylar[0],Kaydet=kaydet,Metin=Metin(adaylar[0]),SaltOkunur=(GetWindowLong(adaylar[0],-16)&0x0800)!=0,KaydetAcik=IsWindowEnabled(kaydet)});
   // HCBT_ACTIVATE: native pencere etkinleşmeden önce deneme Kaydet düğmesini kapatır.
   EnableWindow(kaydet,false);
   if(!SetWindowSubclass(dialog,altIsleyici,AltKimlik,UIntPtr.Zero))throw new IOException("Kaydet komutu deneme koruması kurulamadı; İptal seçin.");
   pencereler.Add(dialog);
   if(!SetWindowSubclass(kaydet,altIsleyici,AltKimlik,new UIntPtr(1)))throw new IOException("Kaydet düğmesi deneme koruması kurulamadı; İptal seçin.");
   pencereler.Add(kaydet);
   var edit=adaylar[0];
   girdiKoruyucusu.Bagla(dialog,kaydet,edit);
   AdAlaniniBagla(edit);
   if(!string.IsNullOrEmpty(otomatikAd))SendMessage(edit,0x000C,IntPtr.Zero,otomatikAd);
   SendMessage(edit,0x00CF,IntPtr.Zero,IntPtr.Zero);
   EnableWindow(kaydet,kayit!=null);
   var metin=new StringBuilder(256);GetWindowText(edit,metin,metin.Capacity);
   KayitOlayTanisi.Yaz("NativeAdYazildi","beklenen="+otomatikAd+" okunan="+metin+" Kaydet="+(kayit==null?"kapali":"acik"));
   // Hedef onizlemesi icin arka planda yardimci surec baslatilmaz.
  }
  void AdAlaniniBagla(IntPtr edit)
  {
   if(!SetWindowSubclass(edit,altIsleyici,AltKimlik,new UIntPtr(2)))throw new IOException("Otomatik ad alanı koruması kurulamadı; İptal seçin.");
   pencereler.Add(edit);
  }
  IntPtr AltPencere(IntPtr h,uint m,IntPtr w,IntPtr l,UIntPtr id,UIntPtr veri)
  {
   // Shell'in ilk kaynak adı güncellemesini düzelt; kullanıcının düzenlemesini koru.
   if(veri.ToUInt64()==2&&m==0x000C&&!string.IsNullOrEmpty(otomatikAd))
   {
    var gelen=Marshal.PtrToStringUni(l)??"";
    var uygulanacak=adDurumu.UygulanacakMetin(gelen,Path.GetFileNameWithoutExtension(bekleyenKaynak),otomatikAd);
    var ad=Marshal.StringToHGlobalUni(uygulanacak);
    try
    {
     var sonuc=DefSubclassProc(h,m,w,ad);
     if(!string.Equals(gelen,uygulanacak,StringComparison.Ordinal))
      KayitOlayTanisi.Yaz("NativeAdKorundu","gelen="+gelen+" beklenen="+otomatikAd+" okunan="+Metin(h));
     return sonuc;
    }
    finally{Marshal.FreeHGlobal(ad);}
   }
   if(veri.ToUInt64()==2&&(m==0x0102||m==0x0300||m==0x0302||m==0x0303||m==0x0304||m==0x00C2||(m==0x0100&&w.ToInt64()==0x2E)))adDurumu.KullaniciDuzenliyor();
   if(veri.ToUInt64()==1&&m==0x000A&&w!=IntPtr.Zero&&(kayit==null||kayitDogrulaniyor)){EnableWindow(h,false);return IntPtr.Zero;}
   if(m==0x0111&&(w.ToInt64()&0xffff)==1)
   {
    if(disAktarimYetkili)
    {
     disAktarimYetkili=false;
     try
     {
      if(!oncekiDurumlar.TryGetValue(h,out var d)||Metin(d.Edit)!=disAktarimSonAd||SeciliFiltre(h)!=disAktarimFiltre||!DisAktarimKurali.HariciFiltreMi(disAktarimFiltre))throw new IOException("Dışa aktarma biçimi veya adı değişti; yeniden Kaydet seçin.");
      return DefSubclassProc(h,m,w,l); // Yalniz bu dis format icin tek native komut.
     }
     catch(Exception hata){KayitHatasi(hata);return IntPtr.Zero;}
    }
    if(standartKayit&&nativeKomutYetkili)
    {
     nativeKomutYetkili=false;
     try
     {
      if(h!=kayitDialog||AdiOku(h)!=sonAd||islem.BekleyenAsama!="NativeYazimBasladi")throw new IOException("Tek kullanımlık native kayıt izni uyuşmuyor.");
      BicimiDogrula(h);EnableWindow(h,true);
      var edit=oncekiDurumlar[h].Edit;
      if(AdiOku(h)!=sonAd)throw new IOException("Native pencere etkinleşirken son ad değişti.");
      var gorunen=Metin(edit);NativeDosyaAdiAktarimi.Esitle(h,edit,gorunen);
      KayitOlayTanisi.Yaz("StandartAdEsitlendi","islem="+islem.IslemId+" gorunen="+gorunen+" hedef="+devredilen.GercekDosya);
      KayitOlayTanisi.Yaz("StandartNativeKaydet","islem="+islem.IslemId+" hedef="+devredilen.GercekDosya);
      return DefSubclassProc(h,m,w,l); // Özgün SolidWorks penceresi bütün kendi kayıt seçeneklerini uygular.
     }
     catch(Exception hata){KayitHatasi(hata);return IntPtr.Zero;}
    }
    if(kayit!=null)KaydetIstendi(h);else KayitOlayTanisi.Yaz("NativeAdKaydetEngellendi","Bu sürüm yalnızca pencere denemesidir.");return IntPtr.Zero;
   }
   // Format secimi native pencerede degistiyse ayrilan yeni CAD adi yerine
   // kaynak numarayi goster. Kaydet aninda filtre ve son ad yeniden okunur.
   if(m==0x0111&&((w.ToInt64()>>16)&0xffff)==1&&Sinif(l)=="ComboBox"&&!kayitDogrulaniyor&&!islem.KaydaDevredildi)
   {
    var sonuc=DefSubclassProc(h,m,w,l);
    try{FormatAdiniGuncelle(h);}catch(Exception hata){KayitOlayTanisi.Yaz("DisAktarimAdHatasi",hata.Message);}
    return sonuc;
   }
   // 2020, PromptForFilenameNotify/Cancelled=26 bildirimini tanımlamaz.
   // Yalnız kendi kayıt penceresindeki açık İptal veya X isteğini hatırla;
   // HTTP iptali native callback içinde değil, CommandClose sonrasında yapılır.
   if(oncekiDurumlar.ContainsKey(h)&&(m==0x0010||(m==0x0111&&(w.ToInt64()&0xffff)==2)))iptalIstendi=true;
   if(m==0x0082&&oncekiDurumlar.ContainsKey(h))pencereKapandi=true;
   if(m==0x0082){RemoveWindowSubclass(h,altIsleyici,AltKimlik);pencereler.Remove(h);oncekiDurumlar.Remove(h);}
   return DefSubclassProc(h,m,w,l);
  }
  public void IptalEt()
  {
   if(islem.KaydaDevredildi){KayitOlayTanisi.Yaz("NativeKayitPencereKapandi","Kayıt sırada; rezervasyon iptal edilmedi.");Dispose();return;}
   try{islem.IptalEt();KayitOlayTanisi.Yaz("NativeAdIptal","Deneme rezervasyonu iptal edildi; numara 005 kurulumu ile tekrar kullanılabilir.");}
   finally{Dispose();otomatikAd="";}
  }
  void KayitDogrulamaSayaciniBaslat()
  {
   HedefIzlemeyiDurdur();
   hedefSayaci=new System.Windows.Forms.Timer{Interval=100};hedefSayaci.Tick+=HedefTick;hedefSayaci.Start();
  }
  void HedefTick(object s,EventArgs e)
  {
   if(kayitDogrulaniyor)KayitDogrulamaTick();
  }
  string AdiOku(IntPtr dialog)
  {
   if(!oncekiDurumlar.TryGetValue(dialog,out var d)||!IsWindow(d.Edit))throw new IOException("Dosya adı alanı kapandı.");
   var ad=new StringBuilder(2048);var n=GetWindowText(d.Edit,ad,ad.Capacity);
   if(n>=ad.Capacity-1)throw new IOException("Dosya adı çok uzun.");
   return BagimsizDosyaAdiKurali.Tamamla(ad.ToString(),islem.BelgeTuru);
  }
  string SeciliFiltre(IntPtr dialog)
  {
   var filtreler=new List<string>();
   EnumChildWindows(dialog,(h,v)=>{if(IsWindowVisible(h)&&Sinif(h)=="ComboBox"){
    var metin=Metin(h);if(DisAktarimKurali.FiltreUzantilari(metin).Length>0)filtreler.Add(metin);
   }return true;},IntPtr.Zero);
   return filtreler.Count==1?filtreler[0]:"";
  }
  void FormatAdiniGuncelle(IntPtr dialog)
  {
   if(!oncekiDurumlar.TryGetValue(dialog,out var d))return;
   var filtre=SeciliFiltre(dialog);
   if(DisAktarimKurali.HariciFiltreMi(filtre))
   {
    var ad=DisAktarimKurali.KaynakAdiyla(bekleyenKaynak,filtre,Metin(d.Edit));
    adDurumu.KullaniciDuzenliyor();SendMessage(d.Edit,0x000C,IntPtr.Zero,ad);disAktarimAdi=true;
   }
   else if(disAktarimAdi&&filtre!="")
   {
    adDurumu.KullaniciDuzenliyor();SendMessage(d.Edit,0x000C,IntPtr.Zero,otomatikAd);disAktarimAdi=false;
   }
  }
  bool DisAktarimiBaslat(IntPtr dialog)
  {

   var filtre=SeciliFiltre(dialog);
   if(!DisAktarimKurali.HariciFiltreMi(filtre))return false;
   var d=oncekiDurumlar[dialog];
   var ad=DisAktarimKurali.KaynakAdiyla(bekleyenKaynak,filtre,Metin(d.Edit));
   adDurumu.KullaniciDuzenliyor();SendMessage(d.Edit,0x000C,IntPtr.Zero,ad);
   NativeDosyaAdiAktarimi.Esitle(dialog,d.Edit,ad);
   // Henuz CAD yazimi devredilmedi. Kullanilmayan rezervasyon iptal edilir;
   // ag hatasinda IptalBekliyor kalici kalir, export icin yeni numara alinmaz.
   try{islem.IptalEt();}catch(Exception hata){KayitOlayTanisi.Yaz("DisAktarimIptalBekliyor","islem="+islem.IslemId+" "+hata.Message);}
   disAktarimSonAd=ad;disAktarimFiltre=filtre;disAktarimYetkili=true;disAktarimAdi=true;
   KayitOlayTanisi.Yaz("DisAktarimNativeKaydet","kaynak="+bekleyenKaynak+" hedefAd="+ad+" merkeziKayit=yok");
   if(!PostMessage(dialog,0x0111,new IntPtr(1),IntPtr.Zero)){disAktarimYetkili=false;throw new IOException("Dışa aktarma Kaydet komutu gönderilemedi.");}
   return true;
  }
  void BicimiDogrula(IntPtr dialog)
  {
   var adet=0;var uzanti=islem.BelgeTuru=="P"?"*.sldprt":"*.sldasm";
   EnumChildWindows(dialog,(h,v)=>{if(IsWindowVisible(h)&&Sinif(h)=="ComboBox"&&Metin(h).IndexOf(uzanti,StringComparison.OrdinalIgnoreCase)>=0)adet++;return true;},IntPtr.Zero);
   if(adet!=1)throw new IOException("Kayıt türünü SOLIDWORKS parçası/montajı olarak seçin; dışa aktarma bu işlemde desteklenmiyor.");
  }
  void KaydetIstendi(IntPtr dialog)
  {
   if(kayitDogrulaniyor||islem.KaydaDevredildi)return;
   try
   {
    if(DisAktarimiBaslat(dialog))return;
    if(!islem.Aktif||islem.BekleyenAsama=="IptalBekliyor")
    {
     // Export secenekleri iptal edilip ayni pencerede yerel CAD'e donulebilir.
     var klasor=Path.GetDirectoryName(bekleyenKaynak??"");
     if(!Directory.Exists(klasor))klasor=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
     otomatikAd=Path.GetFileNameWithoutExtension(islem.Hazirla(Path.GetExtension(bekleyenKaynak??"").Equals(".sldasm",StringComparison.OrdinalIgnoreCase)?"M":"P",klasor,kayit!=null,standartKayit));
     adDurumu.KullaniciDuzenliyor();SendMessage(oncekiDurumlar[dialog].Edit,0x000C,IntPtr.Zero,otomatikAd);disAktarimAdi=false;
    }
    sonAd=AdiOku(dialog);BicimiDogrula(dialog);
    var gorunen=Metin(oncekiDurumlar[dialog].Edit);
    NativeDosyaAdiAktarimi.Esitle(dialog,oncekiDurumlar[dialog].Edit,gorunen);
    if(AdiOku(dialog)!=sonAd)throw new IOException("Ad eşitlemesi sırasında dosya adı değişti.");
    KayitDogrulamaSayaciniBaslat();kayitDialog=dialog;kayitDogrulaniyor=true;
    EnableWindow(dialog,false); // Yeni okuma sürerken kullanıcı hedefi değiştiremez; UI thread bekletilmez.
    sorguId=Guid.NewGuid();sonSorguId=Guid.NewGuid();
    var exe=Path.Combine(Path.GetDirectoryName(typeof(PronomEklentisi).Assembly.Location),"Araclar","Pronom.PencereTanisi.exe");
    hedefOkuma=DisHedefOkuyucu.OkuCift(exe,System.Diagnostics.Process.GetCurrentProcess().Id,dialog.ToInt64(),islem.IslemId,sorguId,sonSorguId);
    KayitOlayTanisi.Yaz("NativeKayitDogrulama","islem="+islem.IslemId+" ad="+sonAd);
   }
   catch(Exception h){KayitHatasi(h);}
  }
  void KayitDogrulamaTick()
  {
   if(hedefOkuma==null||!hedefOkuma.IsCompleted)return;
   try
   {
    if(!IsWindow(kayitDialog)||!oncekiDurumlar.ContainsKey(kayitDialog))throw new IOException("Kayıt penceresi doğrulama sırasında kapandı.");
    var klasor=HedefAdresKurali.CiftiDogrula(hedefOkuma.GetAwaiter().GetResult(),islem.IslemId,sorguId,sonSorguId,System.Diagnostics.Process.GetCurrentProcess().Id,kayitDialog.ToInt64());
    if(sonAd!=AdiOku(kayitDialog))throw new IOException("Dosya adı doğrulama sırasında değişti.");
    BicimiDogrula(kayitDialog);
    var hedef=Path.Combine(klasor,sonAd);
    if(!Directory.Exists(klasor))throw new IOException("Hedef klasör bulunamadı.");
    if(DosyaAdiKurali.AyniYol(hedef,bekleyenKaynak)||File.Exists(hedef)||Directory.Exists(hedef))throw new IOException("Hedef mevcut veya kaynak dosyayla aynı. Başka ad/klasör seçin; üzerine yazılmayacak.");
    var dialog=kayitDialog;HedefIzlemeyiDurdur();kayitDogrulaniyor=false;
    devredilen=islem.KaydaAktar(klasor,sonAd);
    if(standartKayit)
    {
     kayit(devredilen,sonAd); // Yalnızca model GUID/olay bağlamını hazırlar, dosya yazmaz.
     nativeKomutYetkili=true;
     if(!PostMessage(dialog,0x0111,new IntPtr(1),IntPtr.Zero)){nativeKomutYetkili=false;throw new IOException("Native Kaydet komutu gönderilemedi; işlem korundu.");}
     return;
    }
    // Orijinal native komut yalnızca pencereyi kapatır. Gerçek yazım CommandClose + OnIdle sonrasındadır.
    if(!PostMessage(dialog,0x0111,new IntPtr(2),IntPtr.Zero))throw new IOException("Kayıt penceresi kapatma isteği gönderilemedi; işlem korundu.");
    devretHazir=true;
    KayitOlayTanisi.Yaz("NativeKayitSirada","islem="+islem.IslemId+" hedef="+devredilen.GercekDosya);
   }
   catch(Exception h){KayitHatasi(h);}
  }
  void KayitHatasi(Exception h)
  {
   kayitDogrulaniyor=false;HedefIzlemeyiDurdur();
   if(oncekiDurumlar.ContainsKey(kayitDialog)&&IsWindow(kayitDialog))EnableWindow(kayitDialog,true);
   KayitOlayTanisi.Yaz("NativeKayitHatasi",h.ToString());
   bildir?.Invoke((standartKayit?"Standart":"Bağımsız")+" kayıt başlatılamadı: "+h.Message);
  }
  public Action KayitKapanisiniAl()
  {
   if(!devretHazir)return null;
   devretHazir=false;var i=devredilen;var ad=sonAd;var d=kayitDialog;var k=kayit;
   return ()=>
   {
    if(IsWindow(d))throw new IOException("Native kayıt penceresi kapanmadı; CAD yazımı başlatılmadı.");
    try{k(i,ad);}finally{islem.KayitSonucunuAl();}
   };
  }
  public void KayitSonucunuAl()=>islem.KayitSonucunuAl();
  public void KomutKapanisiniTamamla()
  {
   islem.KayitSonucunuAl();
   islem.PencereIptaliniTamamla(iptalIstendi);
  }
  void HedefIzlemeyiDurdur()
  {
   if(hedefSayaci!=null){hedefSayaci.Stop();hedefSayaci.Tick-=HedefTick;hedefSayaci.Dispose();hedefSayaci=null;}
   // Eski yardımcının sonucu bu oturuma bağlanmaz; araç kendi zaman sınırında çıkar.
   if(hedefOkuma!=null)hedefOkuma.ContinueWith(t=>{var hata=t.Exception;},System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted|System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
   hedefOkuma=null;sorguId=Guid.Empty;sonSorguId=Guid.Empty;
  }
  void HookuAyir(){if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}}
  static string Sinif(IntPtr h){var s=new StringBuilder(128);GetClassName(h,s,s.Capacity);return s.ToString();}
  static string Metin(IntPtr h){var s=new StringBuilder(512);GetWindowText(h,s,s.Capacity);return s.ToString();}
  public void Dispose()
  {
   if(girdiKoruyucusu!=null){girdiKoruyucusu.Dispose();girdiKoruyucusu=null;}
   kayitDogrulaniyor=false;
   nativeKomutYetkili=false;
   if(oncekiDurumlar.ContainsKey(kayitDialog)&&IsWindow(kayitDialog))EnableWindow(kayitDialog,true);
   HedefIzlemeyiDurdur();
   HookuAyir();
   foreach(var h in new List<IntPtr>(pencereler))if(IsWindow(h))RemoveWindowSubclass(h,altIsleyici,AltKimlik);
   pencereler.Clear();
   foreach(var durum in new List<PencereDurumu>(oncekiDurumlar.Values))
   {
    if(!IsWindow(durum.Dialog))continue;
    if(IsWindow(durum.Edit)&&Sinif(durum.Edit)=="Edit"&&GetDlgCtrlID(durum.Edit)==1001)
    {SendMessage(durum.Edit,0x000C,IntPtr.Zero,durum.Metin);SendMessage(durum.Edit,0x00CF,new IntPtr(durum.SaltOkunur?1:0),IntPtr.Zero);}
    if(IsWindow(durum.Kaydet)&&Sinif(durum.Kaydet)=="Button"&&GetDlgCtrlID(durum.Kaydet)==1)EnableWindow(durum.Kaydet,durum.KaydetAcik);
   }
   oncekiDurumlar.Clear();pencereKapandi=false;izlenenDialog=IntPtr.Zero;
   // Unload/crash, kaydetme iptali kanıtı değildir; kalan günlük otomatik iptal edilmez.
  }
 }
}
