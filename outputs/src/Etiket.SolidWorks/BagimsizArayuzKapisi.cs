using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Etiket.SolidWorks
{
 internal static class BagimsizArayuzKapisi
 {
  delegate bool Gezici(IntPtr pencere,IntPtr veri);
  [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")]static extern bool EnumThreadWindows(uint thread,Gezici gezici,IntPtr veri);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr pencere);
  [DllImport("user32.dll")]static extern bool IsWindowEnabled(IntPtr pencere);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr pencere,StringBuilder sinif,int boyut);
  // Sadece OnIdle'da ve kendi kayıt penceresi kapanışından sonra okunur.
  // Modal/bilinmeyen arayüz varsa COM sorgusu bile başlatılmaz.
  internal static bool Sorgulanabilir()
  {
   bool engel=false,gorunen=false;
   var sonuc=EnumThreadWindows(GetCurrentThreadId(),(h,v)=>{
    if(!IsWindowVisible(h))return true;gorunen=true;
    var sinif=new StringBuilder(256);if(GetClassName(h,sinif,sinif.Capacity)==0){engel=true;return true;}
    if(!IsWindowEnabled(h)||sinif.ToString()=="#32770")engel=true;
    return true;
   },IntPtr.Zero);
   return sonuc&&gorunen&&!engel;
  }
 }
}