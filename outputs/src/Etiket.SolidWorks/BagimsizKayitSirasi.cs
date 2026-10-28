using System;
namespace Etiket.SolidWorks
{
 // Yalnız UI thread'inde: geçmiş CommandOpen/Close bilançosundan bağımsız iş sahipliği.
 // Zaman aşımı çalışma izni değildir; iki ayrı OnIdle güncel arayüz kanıtı ister.
 public sealed class BagimsizKayitSirasi
 {
  Action bekleyen;bool calisiyor;int bosGozlem;
  public bool Bekliyor=>bekleyen!=null;
  public bool Calisiyor=>calisiyor;
  public void Ekle(Action islem,bool kendiPenceresiKapandi)
  {
   if(islem==null)throw new ArgumentNullException(nameof(islem));
   if(!kendiPenceresiKapandi)throw new InvalidOperationException("Bağımsız kayıt penceresinin kapanışı doğrulanmadı.");
   if(Bekliyor||calisiyor)throw new InvalidOperationException("Bağımsız kayıt zaten sırada.");
   bekleyen=islem;bosGozlem=0;
  }
  public void GozlemiGecersizKil(){bosGozlem=0;}
  public bool Dene(bool yerelPencereGuvenli,bool? etkinKomut)
  {
   if(!Bekliyor||calisiyor)return false;
   if(!yerelPencereGuvenli||etkinKomut!=false){bosGozlem=0;return false;}
   if(++bosGozlem<2)return false;
   // Sahiplik native çağrıdan önce tek sefer devredilir; hata yeniden CAD yazdıramaz.
   var islem=bekleyen;bekleyen=null;bosGozlem=0;calisiyor=true;
   try{islem();return true;}finally{calisiyor=false;}
  }
  public void Temizle(){if(calisiyor)throw new InvalidOperationException("Yazım sürerken kuyruk temizlenemez.");bekleyen=null;bosGozlem=0;}
 }
}