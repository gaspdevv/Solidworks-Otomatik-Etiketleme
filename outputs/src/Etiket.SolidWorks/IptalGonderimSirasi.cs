using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Etiket.SolidWorks
{
 // Yalniz yerelde kesinlesmis iptal niyetlerini arka planda merkeze iletir.
 // COM/CAD/numara ayirma yok; eksik ag yaniti yerel gunlugu silmez.
 public sealed class IptalGonderimSirasi:IDisposable
 {
  readonly KaliciIslemGunlugu gunluk; readonly Func<IAdServisi> servisAc;
  readonly object kapisi=new object(); bool kapali; Task isci;
  public string SonDurum {get;private set;}="Henüz kontrol edilmedi.";
  public IptalGonderimSirasi(KaliciIslemGunlugu gunluk,Func<IAdServisi> servisAc){this.gunluk=gunluk;this.servisAc=servisAc;}
  public Task Baslat()
  {
   lock(kapisi){if(kapali)return Task.FromResult(0);if(isci!=null&&!isci.IsCompleted)return isci;return isci=Task.Run((Action)Calis);}
  }
  void Calis()
  {
   try
   {
    var adaylar=gunluk.BekleyenleriOku().Where(i=>i.Asama=="IptalBekliyor").Select(i=>i.IslemId).ToArray();
    if(adaylar.Length==0){SonDurum="Gönderilecek iptal yok.";return;}
    var elle=0;var s=servisAc();
    try
    {
     foreach(var id in adaylar)
     {
      lock(kapisi){if(kapali)return;}
      var i=gunluk.Oku(id);if(i.Asama!="IptalBekliyor")continue;
      // Asama yazim baslamadan once kalici iptal istegidir; CAD varligi bu niyetle celisir.
      if(KayitDosyaKaniti.Var(i)||!string.IsNullOrEmpty(i.GercekDosya)){elle++;KayitOlayTanisi.Yaz("IptalElleInceleme","islem="+id);continue;}
      s.IptalEt(id); // Ayni GUID: commit sonrasi yanit kaybi da yeni islem yaratmaz.
      i.Asama="Iptal";gunluk.Yaz(i);
      KayitOlayTanisi.Yaz("ArkaPlanIptalTamamlandi","islem="+id);
     }
     SonDurum=elle==0?"Bekleyen iptaller merkezde tamamlandı.":"Güvenli iptaller tamamlandı; "+elle+" çelişkili işlem inceleme için korundu.";
    }
    finally{(s as IDisposable)?.Dispose();}
   }
   catch(Exception h){SonDurum="Merkez iptali bekliyor: "+h.Message;KayitOlayTanisi.Yaz("ArkaPlanIptalBekliyor",h.ToString());}
  }
  public void Dispose(){lock(kapisi)kapali=true;}
 }
 public sealed class KayitKurtarmaIslemleri
 {
  readonly KaliciIslemGunlugu gunluk;readonly Func<IAdServisi> servisAc;
  public KayitKurtarmaIslemleri(KaliciIslemGunlugu gunluk,Func<IAdServisi> servisAc){this.gunluk=gunluk;this.servisAc=servisAc;}
  public string Listele()
  {
   var liste=gunluk.BekleyenleriOku().ToArray();
   if(liste.Length==0)return "Bekleyen işlem yok.";
   return string.Join(Environment.NewLine+Environment.NewLine,liste.Select(i=>i.IslemId+" | "+i.AyrilanDosyaAdi+" | "+i.Asama+Environment.NewLine+
    (i.Asama=="IptalBekliyor"?"Dosya yazımı başlamamış iptal, merkez yanıtını bekliyor.":KayitSonucuToparlama.Uygun(i)?"Dosya kanıtı eşleşirse aynı kimlikle toparlanabilir.":"Yazım/işlem durumu belirsiz. Otomatik silinmez veya yeniden kaydedilmez.")));
  }
  public string Toparla()
  {
   var ids=gunluk.BekleyenleriOku().Where(KayitSonucuToparlama.Uygun).Select(i=>i.IslemId).ToArray();
   if(ids.Length==0)return "Kanıtla toparlanabilecek kayıt yok. İptaller arka planda gönderiliyor; belirsiz yazımlar korundu.";
   var s=servisAc();var sonuclar=new List<string>();
   try
   {
    var y=new KaydetmeIslemYoneticisi(s,gunluk);
    foreach(var id in ids)
     try{var i=new KayitSonucuToparlama(gunluk,y).Tamamla(id);sonuclar.Add(id+" : "+i.Asama);}
     catch(Exception h){sonuclar.Add(id+" : Korundu. "+h.Message);}
   }finally{(s as IDisposable)?.Dispose();}
   return string.Join(Environment.NewLine+Environment.NewLine,sonuclar);
  }
 }
}
