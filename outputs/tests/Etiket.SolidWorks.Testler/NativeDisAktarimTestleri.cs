using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Etiket.SolidWorks;
partial class Program
{
 delegate bool AktarimGezici(IntPtr h,IntPtr veri);
 [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")]static extern bool EnumThreadWindows(uint thread,AktarimGezici callback,IntPtr veri);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 static string AktarimMetni(IntPtr h){var s=new StringBuilder(512);GetWindowText(h,s,s.Capacity);return s.ToString();}
 static void NativeDisAktarimTestleri()
 {
  foreach(var standart in new[]{true,false})foreach(var tur in new[]{"P","M"})foreach(var ext in new[]{"x_t","step","dxf","pdf"})
  {
   var kok=new DirectoryInfo(AppContext.BaseDirectory);while(kok!=null&&!File.Exists(Path.Combine(kok.FullName,"outputs","SolidWorksEtiketleme.slnx")))kok=kok.Parent;
   var alan=Path.Combine(kok.FullName,"work","native-aktarim32",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(alan);
   var kaynak=Path.Combine(alan,tur+"00128"+(tur=="P"?".SLDPRT":".SLDASM"));File.WriteAllText(kaynak,"Test verisi; CAD degil.");
   var servis=new SahteServis();var gunluk=new KaliciIslemGunlugu(Path.Combine(alan,"gunluk"));var y=new KaydetmeIslemYoneticisi(servis,gunluk);
   var tip=typeof(PronomEklentisi).Assembly.GetType("Etiket.SolidWorks.NativeAdDenemePenceresi",true);var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
   var nesne=Activator.CreateInstance(tip,new object[]{y});Exception hata=null;IntPtr dialog=IntPtr.Zero;bool denendi=false;
   try
   {
    Action<KaydetmeIslemi,string> yazma=(i,ad)=>{throw new Exception("Export CAD yazimina devredilmemeli.");};Action<string> bildir=m=>{hata=new Exception(m);if(dialog!=IntPtr.Zero)PostMessage(dialog,0x0010,IntPtr.Zero,IntPtr.Zero);};
    tip.GetMethod("Hazirla",flags).Invoke(nesne,new object[]{tur,kaynak,yazma,bildir,standart});
    using(var d=new SaveFileDialog{Title="Farklı Kaydet",InitialDirectory=alan,FileName=Path.GetFileName(kaynak),Filter="SOLIDWORKS (*."+(tur=="P"?"sldprt":"sldasm")+")|*."+(tur=="P"?"sldprt":"sldasm")+"|Dışa aktar (*."+ext+")|*."+ext,FilterIndex=2,DefaultExt=ext})
    using(var saat=new Timer{Interval=150})
    {
     var son=DateTime.UtcNow.AddSeconds(8);
     saat.Tick+=(s,e)=>{try{
      if(dialog==IntPtr.Zero)EnumThreadWindows(GetCurrentThreadId(),(h,v)=>{var c=new StringBuilder(128);GetClassName(h,c,c.Capacity);if(c.ToString()=="#32770"&&AktarimMetni(h)=="Farklı Kaydet")dialog=h;return true;},IntPtr.Zero);
      if(DateTime.UtcNow>son)throw new TimeoutException("Dış format native test zaman aşımı.");
      if(dialog==IntPtr.Zero||denendi)return;
      tip.GetMethod("Uygula",flags).Invoke(nesne,new object[]{dialog});
      var izlenen=(System.Collections.IDictionary)tip.GetField("oncekiDurumlar",flags).GetValue(nesne);if(!izlenen.Contains(dialog))return;
      Console.WriteLine("Native test filtre="+tip.GetMethod("SeciliFiltre",flags).Invoke(nesne,new object[]{dialog}));
      denendi=true;
      tip.GetMethod("FormatAdiniGuncelle",flags).Invoke(nesne,new object[]{dialog});
      tip.GetMethod("KaydetIstendi",flags).Invoke(nesne,new object[]{dialog});
     }catch(Exception ex){hata=ex;PostMessage(dialog,0x0010,IntPtr.Zero,IntPtr.Zero);}};
     saat.Start();var sonuc=d.ShowDialog();saat.Stop();
     if(hata!=null)throw hata;
     Kontrol(sonuc==DialogResult.OK,"Gercek Windows dis format kayit penceresi native devam etmeli.");
     Kontrol(Path.GetFileName(d.FileName)==tur+"00128."+ext,"Gorunen ad ve Shell ic hedefi kaynagin ayni numarasini tasimali: "+d.FileName);
     Kontrol(servis.IptalCagrisi==0&&servis.OnayCagrisi==0&&gunluk.BekleyenleriOku().Single().Asama=="IptalBekliyor","Native export iptali agi beklemeden kalici siraya alinmali.");
     using(var isci=new IptalGonderimSirasi(gunluk,()=>servis))isci.Baslat().GetAwaiter().GetResult();
     Kontrol(servis.IptalCagrisi==1&&!gunluk.BekleyenleriOku().Any(),"Export iptali arka planda ayni kimlikle bir kez tamamlanmali.");
     Kontrol(Directory.GetFiles(alan).Length==1&&File.ReadAllText(kaynak)=="Test verisi; CAD degil.","Pencere testi hicbir export/CAD dosyasi yazmamali; kaynak korunmali.");
    }
   }finally{((IDisposable)nesne).Dispose();}
  }
  Console.WriteLine("GECTI: "+sayi+" gercek Windows dis aktarim pencere kontrolu (CAD yazilmadi).");
 }
}