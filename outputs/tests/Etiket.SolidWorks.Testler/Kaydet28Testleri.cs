using System;
using System.IO;
using Etiket.SolidWorks;

partial class Program
{
 static void Kaydet28Kurallari()
 {
  var kok=new DirectoryInfo(AppContext.BaseDirectory);
  while(kok!=null&&!File.Exists(Path.Combine(kok.FullName,"outputs","SolidWorksEtiketleme.slnx")))kok=kok.Parent;
  var alan=Path.Combine(kok.FullName,"work","kaydet28-iptal-testleri",Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(alan);
  foreach(var standart in new[]{false,true})
  {
   var g=new KaliciIslemGunlugu(Path.Combine(alan,standart?"standart":"bagimsiz"));
   var s=new SahteServis();var d=new NativeAdDenemeIslemi(new KaydetmeIslemYoneticisi(s,g));
   d.Hazirla("P",alan,true,standart);var id=d.IslemId;
   d.PencereIptaliniTamamla(false);
   Kontrol(d.Aktif&&s.IptalCagrisi==0,"Yalniz pencere/komut kapanisi iptal kaniti sayilmamali.");
   d.PencereIptaliniTamamla(true);
   Kontrol(!d.Aktif&&s.IptalEdilen.Contains(id)&&g.Oku(id).Asama=="Iptal","2020 iptal girdisi ayni GUID rezervasyonunu tamamlamali.");
   d.PencereIptaliniTamamla(true);
   Kontrol(s.IptalCagrisi==1&&s.OnayCagrisi==0,"Tekrar kapanis yeni HTTP/onay/CAD islemi yapmamali.");
   var ad=d.Hazirla("P",alan,true,standart);id=d.IslemId;
   d.KaydaAktar(alan,ad);
   d.PencereIptaliniTamamla(true);
   Kontrol(d.KaydaDevredildi&&d.IslemId==id&&s.IptalCagrisi==1,"Programatik ID2 kapanisi kuyruga devredilmis kaydi iptal etmemeli.");
   Kontrol(g.Oku(id).Asama==(standart?"NativeYazimBasladi":"BagimsizKuyrukta"),"Kuyruktaki veya native yazimdaki islem kimligi korunmali.");
  }
  {
   var g=new KaliciIslemGunlugu(Path.Combine(alan,"yanit-kaybi"));var s=new SahteServis();
   var d=new NativeAdDenemeIslemi(new KaydetmeIslemYoneticisi(s,g));
   d.Hazirla("P",alan,true);var id=d.IslemId;s.IptalKaybi=true;
   Hata(()=>d.PencereIptaliniTamamla(true),"2020 iptal yanit kaybi bildirilmeli.");
   Kontrol(d.IslemId==id&&g.Oku(id).Asama=="IptalBekliyor","Iptal kaybinda kimlik ve niyet korunmali.");
   s.IptalKaybi=false;d.PencereIptaliniTamamla(true);
   Kontrol(!d.Aktif&&s.YeniAdSayisi==1&&g.Oku(id).Asama=="Iptal","Iptal ayni GUID ile toparlanmali; yeni ad alinmamali.");
  }
 }
}
