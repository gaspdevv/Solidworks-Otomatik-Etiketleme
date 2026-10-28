using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class MasaustuAdresTestleri
{
 delegate bool Gezici(IntPtr h,IntPtr v);
 [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")]static extern bool EnumThreadWindows(uint t,Gezici g,IntPtr v);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 static string Metin(IntPtr h){var b=new StringBuilder(256);GetWindowText(h,b,b.Capacity);return b.ToString();}
 public static int Calistir()
 {
  try
  {
   Application.EnableVisualStyles();
   var desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
   var sanalAdres="Adres: "+MasaustuAdresCozucu.GorunenAd();
   if(Etiket.SolidWorks.HedefAdresKurali.Coz(sanalAdres)!=null)throw new Exception("Eski okuyucunun sanal adresi reddettigi dogrulanamadi.");
   if(!string.Equals(MasaustuAdresCozucu.Coz(sanalAdres),desktop,StringComparison.OrdinalIgnoreCase))throw new Exception("Windows Shell adi gercek Masaustu yoluna cozumlenemedi.");
   Console.WriteLine("GECTI: Windows sanal adresi "+sanalAdres+" -> "+desktop);
   foreach(var path in new[]{desktop,AppContext.BaseDirectory})
   using(var dialog=new SaveFileDialog{Title="Pronom Masaustu Adres Testi",InitialDirectory=path,FileName="Pronom-test-yazilmayacak.txt",Filter="Test|*.txt",AutoUpgradeEnabled=true})
   using(var timer=new Timer{Interval=100})
   {
    var hwnd=IntPtr.Zero;Task<string> okuma=null;Exception hata=null;string sonuc=null;
    var son=DateTime.UtcNow.AddSeconds(8);
    timer.Tick+=(s,e)=>
    {
     try
     {
      if(DateTime.UtcNow>son)throw new TimeoutException("Test adres okuma zaman asimi.");
      EnumThreadWindows(GetCurrentThreadId(),(h,v)=>{if(Metin(h)==dialog.Title)hwnd=h;return true;},IntPtr.Zero);
      if(hwnd==IntPtr.Zero)return;
      if(okuma==null){var hedef=hwnd;okuma=Task.Run(()=>Etiket.SolidWorks.HedefOkumaTekrari.Oku(()=>Program.AdresAlaniniOku(Process.GetCurrentProcess().Id,hedef),ms=>System.Threading.Thread.Sleep(ms)));return;}
      if(!okuma.IsCompleted)return;
      sonuc=okuma.GetAwaiter().GetResult();timer.Stop();PostMessage(hwnd,0x0010,IntPtr.Zero,IntPtr.Zero);
     }
     catch(Exception ex){hata=ex;timer.Stop();if(hwnd!=IntPtr.Zero)PostMessage(hwnd,0x0010,IntPtr.Zero,IntPtr.Zero);}
    };
    timer.Start();dialog.ShowDialog();timer.Stop();if(hata!=null)throw hata;
    if(!string.Equals(Path.GetFullPath(path).TrimEnd('\\'),(sonuc??"").TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new IOException("Gercek Windows kayit penceresi hedefi eslesmedi: "+sonuc);
    Console.WriteLine("GECTI: gercek Windows kayit penceresi -> "+sonuc+". Dosya yazilmadi.");
   }
   return 0;
  }
  catch(Exception h){Console.Error.WriteLine(h);return 1;}
 }
}
