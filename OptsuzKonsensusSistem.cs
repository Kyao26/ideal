// ============================================================================
//  OPT'SUZ KONSENSÜS SİSTEMİ  (OKS v1)  -  iDeal Terminal Sistem Kodu
// ----------------------------------------------------------------------------
//  Optimize edilecek parametresi YOKTUR. Sistem.Parametreler[] bilerek
//  kullanılmamıştır; Opt sekmesine hiç girilmez.
//
//  Fikir: "Hangi periyot en iyi?" sorusunu sormak yerine bir periyot merdiveni
//  kurup hepsine aynı anda oy verdirmek. Pozisyon = oyların toplamının işareti.
//  Üye sayısı TEK olduğu için toplam asla 0 olamaz -> keyfi bir kararsızlık
//  eşiğine gerek kalmaz.
//
//  Tasarım dokümanı: OptsuzKonsensusSistem_Tasarim.md
// ============================================================================

// --- 0) YAPISAL AYARLAR (optimizasyon parametresi DEĞİLDİR) -----------------
bool AcigaSatisVar = false;   // Spot hisse: false | VIOP: true  (enstrüman gerçeği)
bool RejimFiltresi = true;    // Girişleri Kaufman ER ile filtrele
bool KoruyucuStop  = true;    // En hızlı merdiven üyesi erken çıkış görevi görsün
double Komisyon    = 0.0;     // Kendi gerçek maliyetinizi girin (0 ile test kendinizi kandırır)

// Geometrik merdiven: adım = 2 (oktav). Üye sayısı TEK olmalıdır.
int[] Merdiven = new int[] { 10, 20, 40, 80, 160 };

// --- 1) VERİ ----------------------------------------------------------------
var V = Sistem.GrafikVerileri;
var H = Sistem.GrafikFiyatOku(V, "Yuksek");
var L = Sistem.GrafikFiyatOku(V, "Dusuk");
var C = Sistem.GrafikFiyatOku(V, "Kapanis");

int UyeSayisi = Merdiven.Length;
int EnUzun    = Merdiven[UyeSayisi - 1];

// --- 2) KANAL MERDİVENİ VE OYLAMA -------------------------------------------
//  Her üye Donchian kırılımına göre +1 / -1 oy verir, kırılım yoksa son oyunu korur.
var Skor     = Sistem.Liste(0);   // oyların toplamı  (-UyeSayisi .. +UyeSayisi)
var HizliOy  = Sistem.Liste(0);   // en hızlı üyenin (Merdiven[0]) oyu
var OyVeren  = Sistem.Liste(0);   // o barda oy vermiş üye sayısı (ısınma kontrolü)

for (int k = 0; k < UyeSayisi; k++)
{
    var hh = Sistem.HHV(V, Merdiven[k], "Yuksek");
    var ll = Sistem.LLV(V, Merdiven[k], "Dusuk");

    float oy = 0f;
    for (int i = 1; i < V.Count; i++)
    {
        if (hh[i - 1] != 0 && C[i] > hh[i - 1]) oy =  1f;
        else if (ll[i - 1] != 0 && C[i] < ll[i - 1]) oy = -1f;

        Skor[i] += oy;
        if (oy != 0f) OyVeren[i] += 1f;
        if (k == 0) HizliOy[i] = oy;
    }
}

// --- 3) REJİM FİLTRESİ ------------------------------------------------------
//  Kaufman Efficiency Ratio (yön / gürültü). Eşik sabit bir sayı değil, ER'in
//  KENDİ genişleyen ortalamasıdır -> sembolden sembole kendi kendine ölçeklenir
//  ve yalnızca geçmiş veriyi gördüğü için look-ahead içermez.
var ER      = Sistem.Liste(0);
var EREsik  = Sistem.Liste(0);

float erToplam = 0f;
int   erAdet   = 0;
for (int i = EnUzun + 1; i < V.Count; i++)
{
    float gurultu = 0f;
    for (int k = 0; k < EnUzun; k++)
        gurultu += Math.Abs(C[i - k] - C[i - k - 1]);

    ER[i] = (gurultu != 0f) ? Math.Abs(C[i] - C[i - EnUzun]) / gurultu : 0f;

    erToplam += ER[i];
    erAdet++;
    EREsik[i] = erToplam / erAdet;
}

// --- 4) POZİSYON KURALLARI --------------------------------------------------
for (int i = 0; i < V.Count; i++)
    Sistem.Yon[i] = "";

int poz = 0;   // 0 = düz, 1 = long, -1 = short
for (int i = EnUzun + 1; i < V.Count; i++)
{
    if (OyVeren[i] != UyeSayisi) continue;                 // ısınma tamamlanmadı

    bool rejim   = (!RejimFiltresi) || (ER[i] >= EREsik[i]);
    bool hizliAl = (!KoruyucuStop)  || (HizliOy[i] > 0);
    bool hizliSt = (!KoruyucuStop)  || (HizliOy[i] < 0);

    if (poz == 0)
    {
        if (Skor[i] > 0 && hizliAl && rejim)
        {
            poz = 1;  Sistem.Yon[i] = "A";
        }
        else if (AcigaSatisVar && Skor[i] < 0 && hizliSt && rejim)
        {
            poz = -1; Sistem.Yon[i] = "S";
        }
    }
    else if (poz == 1)
    {
        if (Skor[i] < 0 || (KoruyucuStop && HizliOy[i] < 0))
        {
            if (AcigaSatisVar && Skor[i] < 0 && hizliSt && rejim)
            {
                poz = -1; Sistem.Yon[i] = "S";             // doğrudan ters pozisyon
            }
            else
            {
                poz = 0;  Sistem.Yon[i] = "F";
            }
        }
    }
    else
    {
        if (Skor[i] > 0 || (KoruyucuStop && HizliOy[i] > 0))
        {
            if (Skor[i] > 0 && hizliAl && rejim)
            {
                poz = 1;  Sistem.Yon[i] = "A";             // doğrudan ters pozisyon
            }
            else
            {
                poz = 0;  Sistem.Yon[i] = "F";
            }
        }
    }
}

// --- 5) ÇİZGİLER ------------------------------------------------------------
var SkorRenk = new List<Color>();
for (int i = 0; i < V.Count; i++)
    SkorRenk.Add(Color.Gold);
for (int i = 0; i < V.Count; i++)
{
    SkorRenk[i] = (Skor[i] >= UyeSayisi)      ? Sistem.Renk(255, 0, 255, 0)
                : (Skor[i] > 0)               ? Color.Green
                : (Skor[i] <= -UyeSayisi)     ? Sistem.Renk(255, 217, 0, 0)
                : (Skor[i] < 0)               ? Color.OrangeRed
                                              : Color.DarkGray;
}

Sistem.Cizgiler[0].Deger        = Skor;          // Konsensüs skoru
Sistem.Cizgiler[0].RenkListesi  = SkorRenk;
Sistem.Cizgiler[0].ActiveBool   = true;
Sistem.Cizgiler[0].Panel        = 1;

Sistem.Cizgiler[1].Deger        = ER;            // Kaufman Efficiency Ratio
Sistem.Cizgiler[1].ActiveBool   = true;
Sistem.Cizgiler[1].Panel        = 3;

Sistem.Cizgiler[2].Deger        = EREsik;        // ER'in genişleyen ortalaması (eşik)
Sistem.Cizgiler[2].ActiveBool   = true;
Sistem.Cizgiler[2].Panel        = 3;

// --- 6) GETİRİ VE RAPOR -----------------------------------------------------
Sistem.GetiriHesapla("01/01/2000", Komisyon);
Sistem.GetiriMaxDDHesapla("01/01/2000", "01/01/2200");

Sistem.Cizgiler[3].Deger      = Sistem.GetiriKZ;
Sistem.Cizgiler[3].ActiveBool = true;
Sistem.Cizgiler[3].Panel      = 2;

var panel  = 2;
var Renk1a = Sistem.Renk(150, 255, 255, 0);
var Renk2a = Sistem.Renk(255, 0, 255, 0);
var Renk3a = Sistem.Renk(255, 255, 0, 80);

var Sure  = (DateTime.Now - V[0].Date).TotalDays / 30.4;
var gunkz = Sistem.GetiriKZGunSonu[Sistem.GetiriKZGunSonu.Count - 1]
          - Sistem.GetiriKZGun[Sistem.GetiriKZGun.Count - 1];

// Konsensüs istatistikleri: sistemin fit değil, uyum üzerinden çalıştığını gösterir.
float uyumToplam = 0f;
float piyasadaBar = 0f;
float sayilanBar  = 0f;
for (int i = EnUzun + 1; i < V.Count; i++)
{
    if (OyVeren[i] != UyeSayisi) continue;
    uyumToplam += Math.Abs(Skor[i]) / UyeSayisi;
    if (RejimFiltresi == false || ER[i] >= EREsik[i]) piyasadaBar += 1f;
    sayilanBar += 1f;
}
var UyumOrani  = (sayilanBar > 0) ? 100f * uyumToplam / sayilanBar : 0f;
var RejimOrani = (sayilanBar > 0) ? 100f * piyasadaBar / sayilanBar : 0f;

Sistem.GradientYaziEkle("OPT'SUZ KONSENSÜS SİSTEMİ", panel, 10, 20, Color.White, Color.White, "Tahoma", 12);
Sistem.ZeminYazisiEkle("Merdiven                 \t= " + string.Join("-", Merdiven), panel, 10, 45, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Optimize Parametre       \t= 0", panel, 10, 60, Renk3a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Üye Uyum Oranı           \t= % " + UyumOrani.ToString("0.0"), panel, 10, 75, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Rejim Uygun Bar Oranı    \t= % " + RejimOrani.ToString("0.0"), panel, 10, 90, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Test Süresi              \t= " + Sure.ToString("0.0") + " Ay", panel, 10, 105, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Bugünkü K/Z              \t= " + gunkz.ToString("0.000"), panel, 10, 120, Renk1a, "Tahoma", 10);

Sistem.ZeminYazisiEkle("Toplam İşlem Sayısı      \t= " + Sistem.GetiriToplamIslem.ToString("0"), panel, 320, 45, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Kazandıran İşlem Sayısı  \t= " + Sistem.GetiriKarIslem.ToString("0"), panel, 320, 60, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Kaybettiren İşlem Sayısı \t= " + Sistem.GetiriZararIslem.ToString("0"), panel, 320, 75, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Karlı İşlem Oranı        \t= % " + Sistem.GetiriKarIslemOran.ToString("0.00"), panel, 320, 90, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Karlı İşlem Miktarı      \t= " + Sistem.GetiriKarMiktar.ToString("0.000"), panel, 320, 105, Renk1a, "Tahoma", 10);
Sistem.ZeminYazisiEkle("Zararlı İşlem Miktarı    \t= " + Sistem.GetiriZararMiktar.ToString("0.000"), panel, 320, 120, Renk1a, "Tahoma", 10);

Sistem.ZeminYazisiEkle("Net Kar                  \t= " + Sistem.GetiriNetKar.ToString("0.000"), panel, 660, 45, Renk2a, "Tahoma", 12);
Sistem.ZeminYazisiEkle("Profit Factor            \t= " + Sistem.ProfitFactor.ToString("0.00"), panel, 660, 65, Renk2a, "Tahoma", 12);
Sistem.ZeminYazisiEkle("MaxDD                    \t= " + Sistem.GetiriMaxDD.ToString("-0.000"), panel, 660, 85, Renk2a, "Tahoma", 12);
Sistem.ZeminYazisiEkle("30 Gün / Toplam          \t= " + Sistem.GetiriBirAy.ToString("0.000") + "  /  "
    + Sistem.GetiriKZ[Sistem.GetiriKZ.Count - 1].ToString("0.000"), panel, 660, 105, Renk2a, "Tahoma", 12);
