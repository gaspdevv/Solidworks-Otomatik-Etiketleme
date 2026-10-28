using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace Etiket.SolidWorks
{
 // Etiketleme yalnizca iki yerel model formatina aittir. Dis formatlar icin
 // uzanti beyaz listesi tutulmaz; donusturme ve secenekler SolidWorks'e aittir.
 public static class DisAktarimKurali
 {
  public static bool HariciDosyaMi(string ad)
  {
   if(string.IsNullOrWhiteSpace(ad))return false;
   try
   {
    if(ad.IndexOfAny(Path.GetInvalidPathChars())>=0)return false;
    var dosya=Path.GetFileName(ad);
    if(dosya.IndexOfAny(Path.GetInvalidFileNameChars())>=0)return false;
    var ext=Path.GetExtension(dosya.TrimEnd(' ','.'));
    return ext.Length>1&&!YerelUzantiMi(ext);
   }
   catch(ArgumentException){return false;}
   catch(NotSupportedException){return false;}
   catch(PathTooLongException){return false;}
  }
  static bool YerelUzantiMi(string ext)=>string.Equals(ext,".sldprt",StringComparison.OrdinalIgnoreCase)||string.Equals(ext,".sldasm",StringComparison.OrdinalIgnoreCase);
  public static string[] FiltreUzantilari(string filtre)
   =>Regex.Matches(filtre??"",@"\*\.(?<ext>[\p{L}\p{N}_-]+)").Cast<Match>().Select(x=>"."+x.Groups["ext"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  public static bool HariciFiltreMi(string filtre)
  {
   var ext=FiltreUzantilari(filtre);
   return ext.Length>0&&!ext.Any(YerelUzantiMi);
  }
  public static string KaynakAdiyla(string kaynak,string filtre,string gorunen)
  {
   if(!HariciFiltreMi(filtre))throw new IOException("Dışa aktarma biçimi doğrulanamadı.");
   var ad=Path.GetFileNameWithoutExtension(kaynak??"");
   if(string.IsNullOrWhiteSpace(ad))throw new IOException("Kaynak dosya adı okunamadı.");
   var uzantilar=FiltreUzantilari(filtre);var mevcut=Path.GetExtension(gorunen??"");
   var ext=uzantilar.FirstOrDefault(x=>string.Equals(x,mevcut,StringComparison.OrdinalIgnoreCase))??uzantilar[0];
   return ad+ext;
  }
 }
}