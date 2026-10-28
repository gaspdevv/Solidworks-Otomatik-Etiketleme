using System;
using System.IO;
using Etiket.SolidWorks;

partial class Program
{
 static void Kaydet29Kurallari()
 {
  const string ad="Masaüstü";const string yol=@"C:\Pronom\Masaüstü";
  Kontrol(HedefAdresKurali.Coz("Adres: "+ad)==null,"Eski tam yol okuyucusu sanal Masaustu adini reddeder; yeni yol ayri olmalidir.");
  Kontrol(HedefAdresKurali.MasaustuCoz("Adres: "+ad,ad,yol)==yol,"Windows'un tam Masaustu etiketi cozumlenmeli.");
  Kontrol(HedefAdresKurali.MasaustuCoz("Address: Desktop","Desktop",yol)==yol,"Ingilizce Windows'un kendi etiketi cozumlenmeli.");
  Kontrol(HedefAdresKurali.MasaustuCoz("Adres: "+ad,ad,@"C:\Pronom\OneDrive\Masaüstü")==@"C:\Pronom\OneDrive\Masaüstü","Yonlendirilmis Masaustu yolu sabit kullanici yoluyla degistirilmemeli.");
  Kontrol(HedefAdresKurali.MasaustuCoz("Adres: "+ad,ad,@"\\sunucu\CAD\Masaüstü")==@"\\sunucu\CAD\Masaüstü","Windows UNC Masaustu yolu korunmali.");
  foreach(var adres in new[]{null,"","Adres: ","Masaüstü","Adres: Belgeler","Adres: Masaüstü\\Alt","Adres: Masaüstü - Kısayol","Adres: C:\\CAD\\Masaüstü"})
   Kontrol(HedefAdresKurali.MasaustuCoz(adres,ad,yol)==null,"Belirsiz/baska klasor Masaustu varsayilmamali: "+adres);
  foreach(var kotu in new[]{null,"","Masaüstü",@"C:\Pronom\..\Baska",@"\\.\C:\Masaüstü"})
   Kontrol(HedefAdresKurali.MasaustuCoz("Adres: "+ad,ad,kotu)==null,"Windows'un bildirdigi yol da tam yol kurallarindan gecmeli.");
  Kontrol(HedefAdresKurali.MasaustuCoz("Adres: "+ad,"",yol)==null,"Bos Shell adi kabul edilmemeli.");
  var id=Guid.NewGuid();var ilk=Guid.NewGuid();var son=Guid.NewGuid();
  var a=new HedefPencereYaniti{IslemId=id,SorguId=ilk,SurecId=123,Pencere=456,Klasor=yol};
  var b=new HedefPencereYaniti{IslemId=id,SorguId=son,SurecId=123,Pencere=456,Klasor=yol};
  Kontrol(HedefAdresKurali.CiftiDogrula(new[]{a,b},id,ilk,son,123,456)==yol,"Masaustu hedefi de iki taze okumayla dogrulanmali.");
  b.Klasor=@"C:\Pronom\Baska";
  Hata(()=>HedefAdresKurali.CiftiDogrula(new[]{a,b},id,ilk,son,123,456),"Okumalar arasinda Masaustunden ayrilma reddedilmeli.");
 }
}
