using System;
using System.IO;
using System.Runtime.InteropServices;

internal static class MasaustuAdresCozucu
{
 [DllImport("shell32.dll")]static extern int SHGetFolderLocation(IntPtr owner,int csidl,IntPtr token,uint reserved,out IntPtr pidl);
 [DllImport("shell32.dll")]static extern int SHGetNameFromIDList(IntPtr pidl,uint sigdn,out IntPtr name);
 [DllImport("shell32.dll")]static extern int SHGetKnownFolderPath(ref Guid id,uint flags,IntPtr token,out IntPtr path);
 internal static string GorunenAd()
 {
  IntPtr pidl=IntPtr.Zero,name=IntPtr.Zero;
  try
  {
   Marshal.ThrowExceptionForHR(SHGetFolderLocation(IntPtr.Zero,0,IntPtr.Zero,0,out pidl));
   Marshal.ThrowExceptionForHR(SHGetNameFromIDList(pidl,0,out name));
   return Marshal.PtrToStringUni(name);
  }
  finally{if(name!=IntPtr.Zero)Marshal.FreeCoTaskMem(name);if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);}
 }
 internal static string Coz(string adres)
 {
  IntPtr pidl=IntPtr.Zero,name=IntPtr.Zero,path=IntPtr.Zero;
  try
  {
   // CSIDL_DESKTOP=0: Shell'in sanal Masaüstü kökü ve yerelleştirilmiş adı.
   Marshal.ThrowExceptionForHR(SHGetFolderLocation(IntPtr.Zero,0,IntPtr.Zero,0,out pidl));
   Marshal.ThrowExceptionForHR(SHGetNameFromIDList(pidl,0,out name));
   var ad=Marshal.PtrToStringUni(name);
   if(string.IsNullOrWhiteSpace(ad))return null;
   var metin=(adres??"").Trim();
   if(!string.Equals(metin,"Adres: "+ad,StringComparison.OrdinalIgnoreCase)
    &&!string.Equals(metin,"Address: "+ad,StringComparison.OrdinalIgnoreCase))return null;
   // FOLDERID_Desktop: OneDrive/UNC/yönlendirme dahil kullanıcının
   // Windows tarafından tanımlı gerçek kayıt klasörü; sabit kullanıcı yolu yok.
   var id=new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
   Marshal.ThrowExceptionForHR(SHGetKnownFolderPath(ref id,0,IntPtr.Zero,out path));
   var sonuc=Etiket.SolidWorks.HedefAdresKurali.MasaustuCoz(adres,ad,Marshal.PtrToStringUni(path));
   if(sonuc==null||!Directory.Exists(sonuc))throw new IOException("Windows Masaüstü kayıt klasörü doğrulanamadı.");
   return sonuc;
  }
  finally
  {
   if(path!=IntPtr.Zero)Marshal.FreeCoTaskMem(path);
   if(name!=IntPtr.Zero)Marshal.FreeCoTaskMem(name);
   if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);
  }
 }
}
