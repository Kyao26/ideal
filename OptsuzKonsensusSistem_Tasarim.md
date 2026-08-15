# Opt'suz Sistem Tasarımı — Optimizasyon Gerektirmeyen Konsensüs Sistemi

> **Amaç:** iDeal Terminal'de, hiçbir parametresi optimize edilmeyen (`Opt` modülüne hiç
> girilmeyen), buna rağmen tek başına ayakta duran bir al/sat sistemi tasarlamak.

---

## 1. Problem: Optimizasyon neden bir tuzak?

iDeal'de bir sistem yazdığınızda `Sistem.Parametreler[0..n]` tanımlar, sonra
`OptBaslangic / OptBitis / OptAdim` ile parametre taraması yaparsınız. Tarama sonunda
"en yüksek net kâr" veren kombinasyonu seçmek üç sorun doğurur:

| Sorun | Açıklama |
|---|---|
| **Aşırı uyum (overfitting)** | 3 parametre × 20 değer = 8.000 kombinasyon. Rastgele bir seride bile bunların en iyisi çok iyi görünür. Seçtiğiniz şey sinyal değil, gürültünün fotoğrafıdır. |
| **Tepe kırılganlığı** | Optimum genelde dar bir tepede oturur. Piyasa rejimi biraz kayınca (BIST'te bu 2-3 ayda bir olur) tepeden düşersiniz. |
| **Yeniden fit bağımlılığı** | "Her 6 ayda bir yeniden optimize ederim" demek, sistemi sürekli geçmişe göre yamamak demektir; canlıda hiçbir zaman test ettiğiniz sistemi kullanmazsınız. |

**Tasarım hedefi:** parametre uzayında bir *tepe seçmek* yerine, parametre uzayının
*tamamının ortalamasını almak*. Optimize edilecek bir şey kalmazsa, bozulacak bir fit de
kalmaz.

---

## 2. Tasarım ilkeleri (bu sistemin anayasası)

1. **Her sabit ya yapısal ya evrensel olacak.**
   Yapısal = veriden/takvimden türetilen (örn. "geometrik merdivenin adımı 2'dir").
   Evrensel = literatürde onlarca yıldır aynı kalan (örn. ATR periyodu 14/20).
   *"Backtest'te 37 daha iyi çıktı"* gerekçesiyle konmuş tek bir sayı olmayacak.
2. **Tek periyot seçme, periyot merdiveni kullan.**
   10-20-40-80-160 hepsi aynı anda oy kullanır. "Hangi periyot en iyi?" sorusu ortadan
   kalkar, çünkü soruyu sormuyoruz.
3. **Eşikler serinin kendi dağılımından gelsin.**
   "ER > 0.25" değil, "ER > kendi genişleyen ortalaması". Sembolden sembole, periyottan
   periyoda kendi kendine ölçeklenir.
4. **Geleceğe bakma (look-ahead) yasak.**
   Tüm eşikler *genişleyen pencere* ile hesaplanır — i. bardaki eşik yalnızca 0..i
   verisini görür. (Serinin tamamının medyanını eşik yapmak sinsi bir look-ahead'dir;
   bilerek kaçınıldı.)
5. **Doğrulama = duyarlılık düzlüğü, en yüksek kâr değil.**
   Merdiven üyelerini ±%25 kaydırdığınızda sonuç değişmiyorsa sistem sağlamdır. Amaç
   eğrinin tepesi değil, platonun ortasıdır.

---

## 3. Üç tasarım önerisi

### Öneri A — Takvim Ankrajlı Kırılım
Sinyaller *bar sayısından* değil *takvimden* gelir: kapanış bir önceki **ayın** en
yükseğini geçerse al, bir önceki **haftanın** en düşüğünün altına inerse çık.

- ➕ Sıfır sayısal parametre. Periyot değiştirseniz bile (5dk / günlük) mantık aynı kalır.
- ➕ Anlatması ve denetlemesi çok kolay.
- ➖ Tek bir zaman ölçeğine bağımlı; ay içi geniş salınımlarda geç kalır.
- ➖ Kısa vadeli (intraday) çalışmada ay ankrajı çok kaba kalır.

### Öneri B — Çoklu Periyot Konsensüsü (**önerilen**)
Geometrik bir kanal merdiveni (10, 20, 40, 80, 160) kurulur. Her üye Donchian kırılımına
göre `+1 / -1` oy verir; pozisyon **oyların toplamının işaretidir**. Üye sayısı **tek**
seçildiği için toplam asla 0 olamaz — yön her zaman tanımlıdır, "kararsız bölge" eşiği
gibi uydurma bir sayıya gerek kalmaz.

- ➕ "Hangi periyot" sorusu tasarımdan silinir; tüm ölçekler aynı anda temsil edilir.
- ➕ Tek bir üyenin bozulması sistemi bozmaz (5 üyeden 1'i yanılsa yön değişmez).
- ➕ Skor (-5…+5) tek başına okunabilir bir trend gücü göstergesidir.
- ➖ Konsensüs doğası gereği yavaştır; sert dönüşlerde ilk hareketi kaçırır.
- ➖ Yatay piyasada üyeler bölünür, skor 1/-1 arası zıplar → koruyucu stop şart.

### Öneri C — İstatistiksel Normalizasyon (z-skor)
Fiyatın kendi dağılımına göre z-skoru hesaplanır; eşik yine kendi geçmiş dağılımının
yüzdeliğinden alınır.

- ➕ Sembolden bağımsız, tamamen ölçeksiz.
- ➖ Ortalamaya dönüş varsayar; BIST'te güçlü trendlerde erken satar.
- ➖ Dağılım tahmini için uzun ısınma gerekir, kırılgan tarafı istatistik varsayımlarıdır.

### Karar

| Kriter | A | **B** | C |
|---|:--:|:--:|:--:|
| Parametresizlik | ★★★ | ★★★ | ★★☆ |
| Rejim değişimine dayanıklılık | ★★☆ | ★★★ | ★☆☆ |
| Periyot bağımsızlığı (5dk→haftalık) | ★☆☆ | ★★★ | ★★☆ |
| Yatay piyasa davranışı | ★★☆ | ★★☆ | ★★★ |
| iDeal'de uygulanabilirlik | ★★★ | ★★★ | ★★☆ |

**Seçim: B.** A'nın takvim fikri gerekirse merdivene bir üye olarak eklenebilir; C ise
trend sistemine ters çalıştığı için birleştirilmedi.

---

## 4. Seçilen mimari

```
                 ┌───────────────────────────────────────────┐
   Fiyat (H,L,C) │  KANAL MERDİVENİ (10-20-40-80-160)        │
        ─────────▶  her üye: C > HHV(n)[i-1] → +1            │
                 │            C < LLV(n)[i-1] → -1           │
                 │            aksi halde → son oyu korur     │
                 └───────────────┬───────────────────────────┘
                                 │ oylar
                                 ▼
                 ┌───────────────────────────────────────────┐
                 │  SKOR = Σ oy   (tek üye sayısı → ≠ 0)     │
                 └───────────────┬───────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        ▼                        ▼                        ▼
┌────────────────┐   ┌────────────────────┐   ┌────────────────────┐
│ REJİM FİLTRESİ │   │  YÖN = işaret(SKOR)│   │  KORUYUCU STOP     │
│ ER ≥ ER_ort    │   │                    │   │  en hızlı üye (10) │
│ (genişleyen)   │   │                    │   │  ters kırılırsa düz│
│ → sadece GİRİŞ │   │                    │   │  → GİRİŞ+ÇIKIŞ     │
└────────────────┘   └────────────────────┘   └────────────────────┘
                                 │
                                 ▼
                        Sistem.Yon[i] = "A" / "S" / "F"
```

### Kural tablosu

| Durum | Koşul | Aksiyon |
|---|---|---|
| Pozisyonsuz | `Skor > 0` **ve** hızlı üye `+1` **ve** rejim uygun | **A** (Long) |
| Pozisyonsuz | `Skor < 0` **ve** hızlı üye `-1` **ve** rejim uygun **ve** açığa satış açık | **S** (Short) |
| Long | `Skor < 0` **veya** hızlı üye `-1` | **F** (veya doğrudan **S**) |
| Short | `Skor > 0` **veya** hızlı üye `+1` | **F** (veya doğrudan **A**) |
| Isınma | merdivenin tüm üyeleri henüz oy vermemiş | işlem yok |

### Neden bu seçimler?

- **Neden 2'nin katları?** Ölçek merdiveninde adım seçmek zorundayız; 2 katı, "ölçek"
  kavramının kendisinden gelen doğal adımdır (oktav). 1.5 veya 3 de olurdu — bu yüzden
  §6'daki sağlamlık testinde adımın sonucu değiştirmediği gösterilmelidir.
- **Neden tek sayıda üye?** Toplamın işareti daima tanımlı olsun diye. Çift üye olsaydı
  "toplam 0 iken ne yapılır?" sorusunun cevabı keyfi bir kural olurdu.
- **Neden rejim filtresi sadece girişte?** Filtreyi çıkışa da koymak, trend sağlamken
  geçici bir gürültü artışında pozisyonu attırır. Girişte seçici, çıkışta sadık.
- **Neden en hızlı üye ayrıca stop?** Konsensüs 5 üyeden 3'ü dönene kadar bekler; bu
  bekleme zararın büyük kısmıdır. En hızlı üye erken uyarı görevi görür ve zaten
  merdivenin bir parçası olduğu için sisteme yeni bir sabit sokmaz.

---

## 5. Risk yönetimi

Kod tarafında **fiyat bazlı bir kâr al/zarar kes seviyesi yoktur** — bilinçli bir tercih:
her sabit çarpan (`2.5 × ATR` gibi) optimize edilmeye açık yeni bir parametredir. Riski
üç yerden yönetiyoruz:

1. **Yapısal stop:** en hızlı kanalın ters kırılımı (yukarıda).
2. **Pozisyon büyüklüğü:** iDeal'in kendi lot/portföy ayarları veya ATR bazlı birim
   (Turtle mantığı, repodaki `_TurtleSistem.001` ile aynı `N = ATR(20)` yaklaşımı).
   Sistem sinyal üretir, ölçeklendirmeyi terminal yapar.
3. **Komisyon/kayma gerçekçiliği:** `Sistem.GetiriHesapla(tarih, komisyon)` çağrısına
   gerçek maliyetinizi girin. Konsensüs sistemleri işlem sayısı düşük olduğu için
   maliyete dayanıklıdır; yine de sıfır komisyonla test etmek kendini kandırmaktır.

---

## 6. Doğrulama protokolü (optimizasyon değil, sağlamlık)

Bu sistemde "en iyi parametreyi bulma" adımı **yoktur**. Bunun yerine:

1. **Merdiven kaydırma testi:** `{10,20,40,80,160}` yerine `{8,16,32,64,128}` ve
   `{12,25,50,100,200}` ile çalıştırın. Net kâr ±%25 bandında kalmalı. Kalmıyorsa sistem
   merdivene fit olmuştur, kullanmayın.
2. **Üye çıkarma testi:** 5 üyeden herhangi birini çıkarıp 3 üyeyle çalıştırın. Sonuç
   yönü değişmemeli (kâr düşebilir, işaret değişmemeli).
3. **Çapraz sembol testi:** aynı kodu değiştirmeden 20-30 BIST hissesi + XU030 üzerinde
   çalıştırın. Sistem sembollerin çoğunda artıda olmalı; tek bir sembolde parlayıp
   diğerlerinde batıyorsa o bir tesadüftür.
4. **Çapraz periyot testi:** günlük / 60dk / haftalık. Parametre yok, dolayısıyla kod
   aynen çalışmalı; sonuç işareti korunmalı.
5. **Dönem bölme:** 2015-2019 ve 2020-bugün ayrı ayrı. Fit olmadığı için ikisi de benzer
   karakterde olmalı (getiri seviyesi değil, kazanma oranı ve profit factor karakteri).

> Not: bu testlerin hiçbirinin sonucunda **kodda sayı değiştirmek yoktur**. Test geçmezse
> tasarım reddedilir; ayarlanmaz. Ayarlamaya başladığınız an optimizasyona dönersiniz.

---

## 7. Bilinen sınırlar

- Trend takip sistemidir: uzun yatay dönemlerde küçük ama sürekli aşınma yaşar
  (rejim filtresi bunu azaltır, sıfırlamaz).
- Sert boşluklu (gap) açılışlarda kırılım kapanışta teyit edildiği için giriş fiyatı
  bozulur; BIST'te tavan/taban serilerinde bu belirgindir.
- Isınma süresi en uzun üye kadardır (160 bar). Kısa geçmişli semboller için merdiveni
  kısaltmak yerine sembolü elemek doğrudur.
- Açığa satış varsayılan olarak **kapalıdır** (spot hisse). VIOP için koddaki
  `AcigaSatisVar = true` yapılır — bu bir optimizasyon parametresi değil, enstrüman
  gerçeğidir.

---

## 8. Kurulum (iDeal Terminal)

1. `OptsuzKonsensusSistem.cs` içeriğini kopyalayın.
2. iDeal → **Sistem** → yeni sistem → kod alanına yapıştırın → derleyin.
3. Çizgi ayarları: `Cizgiler[0]` = Skor (Panel 1), `Cizgiler[1]` = ER,
   `Cizgiler[2]` = ER eşiği, `Cizgiler[3]` = Kümülatif K/Z (Panel 2).
4. **Opt sekmesine hiç girmeyin.** Bu sistemin optimize edilecek parametresi yoktur;
   `Sistem.Parametreler[...]` bilerek kullanılmamıştır.
5. Getiri hesabında komisyon/kayma alanını kendi maliyetinizle doldurun.

---

## 9. Sonraki adım seçenekleri

- **Robot sürümü:** aynı sinyal çekirdeğini `_TurtleRobotSpot.001` kalıbıyla emir gönderen
  bir robota taşımak.
- **Sorgu (tarama) sürümü:** skoru bir tarama indikatörü olarak yazıp
  (`Sorgu_IndikatorOlarak.001` kalıbı) BIST genelinde skoru en yüksek hisseleri listelemek.
- **Takvim ankrajlı üye:** Öneri A'yı merdivene 6. üye olarak eklemek (üye sayısı çift
  olacağı için beraberlik kuralı gerekir — bu yüzden 7 üyeye çıkarmak daha temizdir).
