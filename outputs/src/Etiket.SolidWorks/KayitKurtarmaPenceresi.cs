using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Etiket.SolidWorks
{
 internal sealed class KayitKurtarmaPenceresi:Form
 {
  readonly KayitKurtarmaIslemleri islemler;readonly IptalGonderimSirasi iptaller;
  readonly TextBox liste=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
  readonly Button dugme=new Button{Text="Güvenli işlemleri toparla",AutoSize=true};bool mesgul;
  public KayitKurtarmaPenceresi(KayitKurtarmaIslemleri islemler,IptalGonderimSirasi iptaller)
  {
   this.islemler=islemler;this.iptaller=iptaller;Text="Otomatik Etiketleme — Kayıtları toparla";
   Size=new Size(760,470);MinimumSize=new Size(550,350);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
   var bilgi=new Label{Dock=DockStyle.Top,Height=65,Padding=new Padding(10),Text="Günlükler ve CAD dosyaları silinmez. Dosya kanıtı doğrulanan sonuçlar aynı işlem kimliğiyle tamamlanır. Dosya kilitliyse ilgili belgeyi kapatıp yeniden deneyin."};
   var alt=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(8)};
   var yenile=new Button{Text="Yenile",AutoSize=true};yenile.Click+=(s,e)=>Yenile();
   alt.Controls.Add(dugme);alt.Controls.Add(yenile);Controls.Add(liste);Controls.Add(alt);Controls.Add(bilgi);
   dugme.Click+=async(s,e)=>{if(mesgul)return;mesgul=true;dugme.Enabled=false;
    try{liste.Text="Merkez ve dosya kanıtları kontrol ediliyor…";var iptal=iptaller.Baslat();var metin=await Task.Run(()=>islemler.Toparla());await iptal;if(!IsDisposed)liste.Text=metin+Environment.NewLine+Environment.NewLine+iptaller.SonDurum;}
    catch(Exception h){if(!IsDisposed)liste.Text="İşlemler korundu: "+h.Message;}
    finally{mesgul=false;if(!IsDisposed)dugme.Enabled=true;}};
   FormClosing+=(s,e)=>{if(mesgul){e.Cancel=true;return;}};Yenile();
  }
  void Yenile(){if(mesgul)return;try{liste.Text=islemler.Listele()+Environment.NewLine+Environment.NewLine+iptaller.SonDurum;}catch(Exception h){liste.Text="Günlük okunamadı; hiçbir işlem değiştirilmedi. "+h.Message;}}
 }
}
