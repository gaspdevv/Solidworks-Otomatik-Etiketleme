using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Etiket.SolidWorks;
partial class Program
{
 sealed class IptalServis34:IAdServisi
 {
  public int Ayirma,Onay,Iptal;public bool Kopuk,SonucKaybi;
  public ManualResetEventSlim Giris,Kapi;public Guid SonId;
  public AyrilanAd Ayir(Guid id,string tur){Ayirma++;return new AyrilanAd{IslemId=id,DosyaAdi=tur+"00634."+(tur=="P"?"SLDPRT":"SLDASM"),Durum="Ayrildi"};}
  public void Onayla(Guid id,string ad){Onay++;if(Kopuk)throw new IOException("ag yok");}
  public void IptalEt(Guid id){Interlocked.Increment(ref Iptal);SonId=id;Giris?.Set();if(Kapi!=null&&!Kapi.Wait(5000))throw new Exception("test kapisi");if(Kopuk||SonucKaybi)throw new IOException("yanit yok");}
 }
 static void Kaydet34Kurallari()
 {
  var alan=Path.Combine(Path.GetTempPath(),"k34-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(alan);
  var g=new KaliciIslemGunlugu(Path.Combine(alan,"j"));var s=new IptalServis34();var y=new KaydetmeIslemYoneticisi(s,g);
  var pencere=new NativeAdDenemeIslemi(y,y.IptaliYereldeBeklet);
  var ad=pencere.Hazirla("P",alan,true,true);var id=pencere.IslemId;
  s.Kopuk=true;pencere.IptalEt();
  Kontrol(!pencere.Aktif&&s.Iptal==0,"34 pencere iptali HTTP beklemeden ayrilmali");
  Kontrol(g.Oku(id).Asama=="IptalBekliyor","34 iptal niyeti UI ayrilmadan kalici olmali");
  Kontrol(!File.Exists(Path.Combine(alan,ad)),"34 iptal CAD yazmamali");
  using(var isci=new IptalGonderimSirasi(g,()=>s))
  {
   isci.Baslat().GetAwaiter().GetResult();
   Kontrol(g.Oku(id).Asama=="IptalBekliyor"&&s.Iptal==1,"34 kesinti gunlugu ve ayni GUIDyi korumali");
   Kontrol(s.SonId==id&&s.Ayirma==1&&s.Onay==0,"34 iptal iscisi numara/onay uretmemeli");
  }
  // Yeniden baslatma ve commit sonrasi yanit kaybi.
  var yeniG=new KaliciIslemGunlugu(Path.Combine(alan,"j"));
  s.Kopuk=false;s.SonucKaybi=true;
  using(var isci=new IptalGonderimSirasi(yeniG,()=>s))
  {
   isci.Baslat().GetAwaiter().GetResult();Kontrol(yeniG.Oku(id).Asama=="IptalBekliyor","34 commit yaniti yoksa yerel basari yazilmamali");
   s.SonucKaybi=false;isci.Baslat().GetAwaiter().GetResult();
   Kontrol(yeniG.Oku(id).Asama=="Iptal"&&s.SonId==id,"34 restart ayni GUID ile terminale gelmeli");
   var say=s.Iptal;isci.Baslat().GetAwaiter().GetResult();Kontrol(s.Iptal==say,"34 terminal tekrar HTTP yapmamali");
  }
  // Eski bellek nesnesi, diskteki terminal toparlanmayi gormeli.
  var p2=new NativeAdDenemeIslemi(y);p2.Hazirla("P",alan,true,true);var id2=p2.IslemId;
  var i2=p2.KaydaAktar(alan,"P00634.SLDPRT");i2.Asama="Iptal";g.Yaz(i2);p2.KayitSonucunuAl();
  Kontrol(!p2.Aktif&&!p2.KaydaDevredildi,"34 terminal disk sonucu eski pencere nesnesini serbest birakmali");
  foreach(var asama in new[]{"NativeYazimBasladi","BagimsizYazimBasladi","BagimsizKuyrukta","Kaydedildi","Onaylandi","OzelAdIptalBekliyor"})
  {
   var i=y.Baslat("P",alan,KaydetmeBicimi.StandartPencere);i.Asama=asama;g.Yaz(i);
   Hata(()=>y.IptaliYereldeBeklet(i),"34 belirsiz/yazilmis asama yerel resetlenmemeli: "+asama);
   Kontrol(g.Oku(i.IslemId).Asama==asama,"34 reddedilen reset gunlugu degistirmemeli");
  }
  var hatali=y.Baslat("M",alan);hatali.Asama="Ayrildi";hatali.GercekDosya=Path.Combine(alan,"M00001.SLDASM");g.Yaz(hatali);
  Hata(()=>y.IptaliYereldeBeklet(hatali),"34 beklenmeyen yazim bilgisi korunmali");
  // Kanitli dosya onayi: reset CAD yazmadan sadece ayni sonucu tamamlamali.
  var kayit=y.Baslat("M",alan,KaydetmeBicimi.StandartPencere);y.AdiAl(kayit);kayit.GercekDosya=kayit.HedefDosya;File.WriteAllText(kayit.GercekDosya,"kanit");
  KayitDosyaKaniti.Sakla(kayit,kayit.GercekDosya);kayit.Asama="Kaydedildi";g.Yaz(kayit);
  var toparla=new KayitKurtarmaIslemleri(g,()=>s);var oncesi=File.ReadAllBytes(kayit.GercekDosya);
  toparla.Toparla();Kontrol(g.Oku(kayit.IslemId).Asama=="Onaylandi","34 kanitli sonuc toparlanmali");
  Kontrol(oncesi.SequenceEqual(File.ReadAllBytes(kayit.GercekDosya)),"34 toparlama CAD yeniden yazmamali");
  var onay=s.Onay;toparla.Toparla();Kontrol(s.Onay==onay,"34 terminal toparlama tekrar onaylamamali");
  var bozuk=y.Baslat("M",alan,KaydetmeBicimi.StandartPencere);bozuk.HedefDosya=Path.Combine(alan,"M00635.SLDASM");bozuk.AyrilanDosyaAdi="M00635.SLDASM";bozuk.GercekDosya=bozuk.HedefDosya;
  File.WriteAllText(bozuk.GercekDosya,"ilk");KayitDosyaKaniti.Sakla(bozuk,bozuk.GercekDosya);bozuk.Asama="Kaydedildi";g.Yaz(bozuk);File.AppendAllText(bozuk.GercekDosya,"degisti");
  toparla.Toparla();Kontrol(g.Oku(bozuk.IslemId).Asama=="Kaydedildi"&&s.Onay==onay,"34 degismis CAD kaniti reddedilmeli");
  Kontrol(toparla.Listele().Contains("belirsiz")&&toparla.Listele().Contains(bozuk.IslemId.ToString()),"34 belirsizler UI listesinde kalmali");
  // Ayni anda isci baslatma ve ag beklemesinde UI cagrisi donmeli.
  var tek=y.Baslat("P",alan);y.IptaliYereldeBeklet(tek);s.Giris=new ManualResetEventSlim();s.Kapi=new ManualResetEventSlim();
  using(var isci=new IptalGonderimSirasi(g,()=>s))
  {
   var t=isci.Baslat();Kontrol(s.Giris.Wait(3000),"34 sahte ag baslamali");
   Kontrol(ReferenceEquals(t,isci.Baslat()),"34 ikinci baslatma paralel isci uretmemeli");
   isci.Dispose();s.Kapi.Set();t.GetAwaiter().GetResult();
   Kontrol(isci.Baslat().IsCompleted,"34 dispose yeni is baslatmamali");
  }
 }
}
