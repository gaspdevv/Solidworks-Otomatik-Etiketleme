using Etiket.SolidWorks;
partial class Program
{
 static void Kaydet31Kurallari()
 {
  var tip=typeof(PronomEklentisi).Assembly.GetType("Etiket.SolidWorks.NativeAdDenemePenceresi",true);
  var ctx=new StandartTestBaglami();
  var pencere=System.Activator.CreateInstance(tip,new object[]{ctx.Y});
  var kapandi=tip.GetProperty("KapaliPencereSonucuBekliyor");
  Kontrol(!(bool)kapandi.GetValue(pencere),"Henuz izlenmis pencere yokken kapanis islenmemeli.");
  tip.GetField("pencereKapandi",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(pencere,true);
  Kontrol((bool)kapandi.GetValue(pencere),"Kendi pencere kapanisi tek sefer islenmek uzere saklanmali.");
  ((System.IDisposable)pencere).Dispose();
  Kontrol(!(bool)kapandi.GetValue(pencere),"Dispose kapanis kanitini tuketmeli; her idle COM sorgusu ve devir tekrari yapilmamali.");
  ((System.IDisposable)pencere).Dispose();
  Kontrol(!(bool)kapandi.GetValue(pencere),"Yinelenen Dispose yeni kapanis uretmemeli.");
  var d=new YuklemeKomutDurumu();d.KomutAcildi(0);
  Kontrol(!d.Guvenli&&!d.KomutDogrulanabilir,"Acma komutu tamamlama kaniti olmadan sorgulanmamali.");
  Kontrol(!d.CalisanKomutlaDogrula(false),"Yalniz idle acma komutunu kaldirmamali.");
  d.DosyaAcmaSonucuGeldi();
  Kontrol(!d.Guvenli&&d.KomutDogrulanabilir,"2020 FileOpenPost kapanissiz0 icin yalniz sorguya izin vermeli.");
  Kontrol(!d.CalisanKomutlaDogrula(true)&&!d.Guvenli,"Gercek etkin komut varsa dosya acma korumasi kalmali.");
  Kontrol(d.CalisanKomutlaDogrula(false)&&d.Guvenli,"FileOpenPost ve etkin komut yok kaniti eski0 engelini gidermeli.");
  d.KomutAcildi(0);Kontrol(!d.KomutDogrulanabilir,"Onceki acma basarisi sonraki acmaya tasinmamali.");
  d.DosyaAcmaSonucuGeldi();d.KomutAcildi(0);
  Kontrol(!d.KomutDogrulanabilir,"Ayni0 yeniden acilinca yeni FileOpenPost beklenmeli.");
  foreach(var k in new[]{1,2,620,3494})
  {
   d.Temizle();d.KomutAcildi(0);d.DosyaAcmaSonucuGeldi();d.KomutAcildi(k);
   Kontrol(!d.KomutDogrulanabilir&&!d.CalisanKomutlaDogrula(false),"Tamamlanmis acma baska yukleme/yazim komutunu serbest birakmamali: "+k);
   d.KomutKapandi(k);Kontrol(d.KomutDogrulanabilir,"Diger komut kapaninca post kanitli0 sorgulanabilir: "+k);
  }
  d.Temizle();d.KomutAcildi(0);d.DosyaAcmaSonucuGeldi();d.BilesenSecimiBasladi();
  Kontrol(!d.KomutDogrulanabilir&&!d.CalisanKomutlaDogrula(false),"Aktif bilesen yerlestirme icinden COM sorgusu yapilmamali.");
  d.BilesenSecimiBitti();Kontrol(d.CalisanKomutlaDogrula(false)&&d.Guvenli,"Secim bitince ayni post kaniti kullanilabilir.");
  d.DosyaAcmaSonucuGeldi();d.KomutAcildi(0);Kontrol(!d.KomutDogrulanabilir,"Sahipsiz FileOpenPost sonraki acmayi yetkilendirmemeli.");
  d.DosyaAcmaSonucuGeldi();d.Temizle();d.KomutAcildi(0);Kontrol(!d.KomutDogrulanabilir,"Temizle eski post kanitini silmeli.");
  d.DosyaAcmaSonucuGeldi();d.KomutKapandi(0);d.KomutAcildi(0);Kontrol(!d.KomutDogrulanabilir,"Normal CommandClose eski post kanitini tuketmeli.");
  d.Temizle();d.KomutAcildi(0);d.KomutAcildi(46);d.DosyaAcmaSonucuGeldi();
  Kontrol(d.CalisanKomutlaDogrula(false)&&d.Guvenli,"Gercek UI komutu yokken eski acma ve cizim bildirimleri birlikte sonuclanabilir.");
 }
}