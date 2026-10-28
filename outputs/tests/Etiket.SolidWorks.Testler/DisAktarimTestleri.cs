using System;
using System.IO;
using System.Reflection;
using Etiket.SolidWorks;
partial class Program
{
 static void DisAktarimTestleri()
 {
  var tip=typeof(PronomEklentisi);var eklenti=new PronomEklentisi();
  var prompt=tip.GetMethod("DosyaAdiIstendi",BindingFlags.Instance|BindingFlags.NonPublic);
  var bagTipi=tip.Assembly.GetType("Etiket.SolidWorks.BelgeOlaylari",true);
  var bag=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(bagTipi);
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  bagTipi.GetField("programatikBagimsiz",flags).SetValue(bag,(Func<bool>)(()=>false));
  // Model, servis ve gunluk bilerek null. Export bu nesnelere erismemeli.
  foreach(var uzanti in new[]{"DXF","x_t","step","SLDDRW","yeni-format"})
  {
   var ad="P00080."+uzanti;
   foreach(var metod in new[]{"FarkliKaydet","NormalKaydet"})
    Kontrol((int)bagTipi.GetMethod(metod,flags).Invoke(bag,new object[]{ad})==0,"Dis format olayinda model/servis/kimlik sorgusu yok: "+metod);
   Kontrol((int)bagTipi.GetMethod("Kaydedildi",flags).Invoke(bag,new object[]{2,ad})==0,"Export post olayi bekleyen CAD islemine onay kaniti olamaz.");
  }
  foreach(var ext in new[]{"dxf","dwg","x_t","x_b","step","stp","iges","igs","stl","3mf","obj","pdf","png","jpg","eprt","easm","slddrw","zip","yeni-format"})
  {
   foreach(var ad in new[]{"P00080."+ext,@"D:\İmalat\M00080."+ext,@"\\Sunucu\CAD\P00080."+ext})
   {
    Kontrol(DisAktarimKurali.HariciDosyaMi(ad),"Tum yerel olmayan formatlar ayni yoldan serbest: "+ad);
    // Uygulama/yonetici/journal null: COM veya HTTP'ye dokunursa hata verir.
    foreach(var tur in new[]{1,2,3})Kontrol((int)prompt.Invoke(eklenti,new object[]{0,ad,tur,1})==0,"Parca/montaj/cizim export Prompt yolunda COM/HTTP/rezervasyon yok: "+ad);
   }
  }
  foreach(var ad in new[]{"P1.SLDPRT","M1.sldasm",@"D:\CAD\P1.SlDpRt","P1.SLDPRT ","M1.SLDASM...",null,"","P00080","P00080.","P00080.DXF:veri"})
   Kontrol(!DisAktarimKurali.HariciDosyaMi(ad),"Yerel/belirsiz/gecersiz ad export muafiyeti alamaz: "+ad);
  foreach(var filtre in new[]{"SOLIDWORKS (*.sldprt;*.prt)","SOLIDWORKS (*.sldasm;*.asm)","Hepsi (*.*)","Parasolid",null,""})
   Kontrol(!DisAktarimKurali.HariciFiltreMi(filtre),"Yerel veya belirsiz filtre CAD korumasini kaldiramaz: "+filtre);
  foreach(var ext in new[]{"x_t","x_b","step","stl","DXF","yeni-format"})
  {
   var filtre="Disa aktar (*."+ext+")";
   Kontrol(DisAktarimKurali.HariciFiltreMi(filtre),"Uzanti listesi yerine secili filtre okunur.");
   foreach(var kaynak in new[]{@"C:\CAD\P00128.SLDPRT",@"D:\CAD\M00079.SLDASM",@"\\Sunucu\CAD\Özel.Mil.SLDPRT","Parça1.SLDPRT"})
    Kontrol(DisAktarimKurali.KaynakAdiyla(kaynak,filtre,"P00999")==Path.GetFileNameWithoutExtension(kaynak)+"."+ext,"Ayrilan yeni CAD adi yerine kaynak adi, diskten bagimsiz korunur.");
  }
  Kontrol(DisAktarimKurali.KaynakAdiyla(@"C:\CAD\P00080.SLDPRT","Parasolid (*.x_t; *.x_b)","P00999.X_B")=="P00080.x_b","Secili alternatif uzanti korunur.");
  Hata(()=>DisAktarimKurali.KaynakAdiyla("P00080.SLDPRT","SOLIDWORKS (*.sldprt)","P00081"),"Native model baska numarayla export yoluna giremez.");
  var baglam=new StandartTestBaglami();
  // Native pencere acildiginda ayrilmis fakat yazima devredilmemis rezervasyon.
  // Kullanici format degistirince native yazim/onay baslamadan iptal olur.
  var g=new KaliciIslemGunlugu(Path.Combine(Path.GetDirectoryName(baglam.Kaynak),"export-journal"));
  var hizmet=new SahteServis();var y=new KaydetmeIslemYoneticisi(hizmet,g);var d=new NativeAdDenemeIslemi(y);
  d.Hazirla("P",Path.GetDirectoryName(baglam.Kaynak),true,true);d.IptalEt();
  Kontrol(!d.Aktif&&hizmet.IptalCagrisi==1&&hizmet.OnayCagrisi==0,"Format degisimi kullanilmayan rezervasyonu tek iptal eder; Documents onayi yok.");
  d.IptalEt();Kontrol(hizmet.IptalCagrisi==1,"Export sonrasi yinelenen kapanis iptal istegini cogaltmaz.");
  d.Hazirla("M",Path.GetDirectoryName(baglam.Kaynak),true,true);
  Kontrol(d.Aktif&&d.BelgeTuru=="M","Exporttan sonra yeni native kayit tekrar normal rezervasyon alabilir.");d.IptalEt();
 }
}