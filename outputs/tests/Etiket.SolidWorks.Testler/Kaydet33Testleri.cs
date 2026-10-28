using System;
using Etiket.SolidWorks;
partial class Program
{
 static void Kaydet33Kurallari()
 {
  // Replay P00107: çizim46 kapanmadı, bileşen seçimi eski46'ya bağlandı.
  var gecmis=new YuklemeKomutDurumu();gecmis.KomutAcildi(46);gecmis.BilesenSecimiBasladi();gecmis.SecimdeBelgeYuklendi(@"C:\CAD\P00106.SLDPRT");gecmis.KomutAcildi(3494);gecmis.KomutKapandi(3494,-3);
  Kontrol(!gecmis.Guvenli&&!gecmis.KomutDogrulanabilir,"Eski P107 gecmisi gercekten genel kuyrugu engellemeli.");
  var sira=new BagimsizKayitSirasi();int yazim=0;
  Hata(()=>sira.Ekle(()=>yazim++,false),"Kendi pencere kapanisi olmadan CAD isi alinmamali.");
  sira.Ekle(()=>yazim++,true);
  Kontrol(!sira.Dene(true,false)&&yazim==0,"Tek idle gozlemiyle yazim baslamamali.");
  Kontrol(sira.Dene(true,false)&&yazim==1,"Guncel UI iki kez bosken eski46/secim isareti isi kilitlememeli.");
  Kontrol(!gecmis.Guvenli&&!gecmis.KomutDogrulanabilir,"Genel yukleme/standart kopya korumalari temizlenmemeli.");
  Kontrol(!sira.Dene(true,false)&&yazim==1,"Ayni is tekrar yazilmamali.");
  foreach(var durum in new[]{0,1,2})
  {
   sira.Ekle(()=>yazim++,true);var once=yazim;
   Kontrol(!sira.Dene(true,false),"Ilk bosluk yalniz gozlem.");
   bool? komut=durum==0?(bool?)true:durum==1?(bool?)null:false;
   Kontrol(!sira.Dene(durum!=2,komut)&&yazim==once,"Aktif komut/bilinmeyen sorgu/modal yazimi engellemeli: "+durum);
   Kontrol(!sira.Dene(true,false)&&yazim==once,"Engelden sonra ilk gozlem yetmemeli.");
   Kontrol(sira.Dene(true,false)&&yazim==once+1,"Engel kalkinca ayni is bir kez ilerlemeli.");
  }
  sira.Ekle(()=>yazim++,true);sira.Dene(true,false);sira.GozlemiGecersizKil();var sayiOnce=yazim;
  Kontrol(!sira.Dene(true,false)&&yazim==sayiOnce,"Araya giren komut eski bosluk kanitini gecersiz kilmali.");
  Kontrol(sira.Dene(true,false)&&yazim==sayiOnce+1,"Komut sonrasi iki yeni gozlem ilerlemeli.");
  sira.Ekle(()=>{Kontrol(sira.Calisiyor,"Yazim sirasinda sahiplik gorunmeli.");Kontrol(!sira.Dene(true,false),"Ic ice idle yeniden yazamamali.");Hata(()=>sira.Ekle(()=>{},true),"Yazim sirasinda ikinci is alinmamali.");Hata(()=>sira.Temizle(),"Yazim sirasinda sahiplik silinmemeli.");throw new Exception("native sonucu belirsiz");},true);
  sira.Dene(true,false);Hata(()=>sira.Dene(true,false),"Native hata cagrana iletilmeli.");
  Kontrol(!sira.Calisiyor&&!sira.Bekliyor&&!sira.Dene(true,false),"Hata veren CAD isi sessizce yeniden baslamamali.");
  sira.Ekle(()=>yazim++,true);Hata(()=>sira.Ekle(()=>{},true),"Bekleyen is uzerine yazilmamali.");sira.Temizle();
  Kontrol(!sira.Bekliyor&&!sira.Dene(true,false),"Disconnect kuyruk isini calistirmadan birakmali.");
 }
}