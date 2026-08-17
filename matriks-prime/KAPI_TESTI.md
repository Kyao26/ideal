# Hurst Kapısı — motor-bağımsız doz testi

OTT-A ailesi standalone sistem olarak **elendi**. Adil ızgaranın (12 hücre, long-only)
en iyi hücresi bile iki eşiği de geçmiyor: brüt/işlem 26.59 (eşik 30.4), NET PF 1.26
(eşik 1.5), n=17, en iyi 5 çıkınca NET −735 p. Ve 12 hücreden en iyisini seçmek
kendisi bir optimizasyondur.

Ayakta kalan tek şey **kapının doz-tepki ilişkisi**: kapıyı sıkılaştırdıkça brüt/işlem
dört bağımsız kurulumun dördünde de monoton artıyor. Bandın böyle bir tepkisi yok
(ufuk üssü taraması da sabit optA taraması da monoton değildi).

Bu doküman, kapıyı **OTT'den tamamen kopararak** test eder.

---

## Ölçülen ızgara (kayıt)

| bant | kapı | n | BRÜT/i | NET/i | PF | tutuş |
|---|---|--:|--:|--:|--:|--:|
| optA | Hrs>0.5 | 55 | −1.40 | −16.63 | 0.50 | 20 dk |
| optA | +sH | 47 | 18.83 | +3.61 | 1.13 | 23 dk |
| optA | +σ | 27 | 20.37 | +5.19 | 1.12 | 47 dk |
| optH | Hrs>0.5 | 35 | 6.14 | −9.08 | 0.71 | 24 dk |
| optH | +sH | 30 | 7.67 | −7.55 | 0.76 | 24 dk |
| optH | **+σ** | 17 | **26.59** | **+11.40** | **1.26** | 52 dk |

optH'nin ölçek düzeltmesi tuttu (1.098), mekanizması tutmadı (%82 çıkış rejimden).

---

## Doz merdiveni tek parametreye indirildi

Üç kapı varyantı ayrı formüller değil; hepsi tek görünür sabitin değerleri:

```
kD:=1.0;
...
sg:=1.2533*Sum(Abs(Hrs-Hm),20)/20;     { Hurst'ün kendi standart sapması }
Esk:=0.5+kD*sg;
```

| `kD` | karşılığı |
|---|---|
| 0 | `Hrs>0.5` — çıplak kapı |
| 0.447 | eski `sH` (σ/√20 — kategori hatalı olan) |
| 1.0 | `+σ` — ızgaranın en iyisi |
| 1.5 / 2.0 | **hiç denenmedi** — doz-tepki sürüyor mu? |

`kD` doğrudan OPT ile taranır. Doz-tepki iddiasının sınanacağı yer burasıdır: etki
gerçekse `kD` arttıkça brüt/işlem artmaya devam etmeli ya da bir **platoya** oturmalı.
Tek bir `kD`'de sivrilip komşularında çökerse gürültüdür.

---

## Test 1 — Kapı motordan bağımsız mı? (asıl test)

Kapı üç farklı motorun önüne takıldı. **İlk ikisinde `PREV` yok** — yani System
Tester'daki `PREV` riski bu testlerde tamamen devre dışı, ölçüm o belirsizlikten
etkilenmez.

| Motor | Dosya | `PREV` |
|---|---|---|
| M1 — Donchian kırılım (20/10) | `Kapi_M1_Donchian_AL/SAT.txt` | yok |
| M2 — MA kesişimi (10/30) | `Kapi_M2_MAKesisim_AL/SAT.txt` | yok |
| M3 — klasik OTT (`opt:=0.60`) | `Kapi_M3_KlasikOTT_AL/SAT.txt` | var |

Her motor için `kD` = 0 / 0.447 / 1.0 / 1.5 / 2.0 koşulur. Hepsi long-only.

**Geçme ölçütü:** brüt/işlem, **üç motorun en az ikisinde** `kD` ile monoton artmalı
(ya da artıp platoya oturmalı). Tek motorda görülen etki, o motora özgüdür.

**Kalırsa:** doz-tepki OTT-A ailesine özgü bir yapıydı; kapı hattı da kapanır.

---

## Test 2 — Ortalama mı yükseliyor, dağılım mı kayıyor? (kritik)

Bu, kapı hakkındaki **tek ciddi açık soru** ve ızgarada ölçülmedi.

Kaybedenleri elediğinizde ortalama **mekanik olarak** yükselir. En iyi hücrede n=17 ve
en iyi 5 çıkınca NET −735 p olması, ortalamanın aynı şişman kuyruğa binmiş olabileceğini
gösteriyor.

Her `kD` seviyesinde **ortalamanın yanı sıra şunları da kaydedin:**

- **medyan işlem**
- **budanmış ortalama** (en iyi ve en kötü %10 atılarak)
- kazanan işlem oranı

**Ayırt edici ölçüt:** medyan de `kD` ile monoton artıyorsa kapı dağılımın tamamını
kaydırıyor demektir — gerçek etki. Yalnız ortalama artıyorsa aynı birkaç büyük işlem
yeniden seçiliyordur — kapı bir kuyruk seçicidir, filtre değil.

---

## Test 3 — Örneklem sorunu (bu ölçümle çözülemez)

Doz arttıkça n düşüyor: 55 → 47 → 27 ve 35 → 30 → 17. Etki ile örneklem **birlikte**
küçülüyor; `kD=1.5` veya `2.0` bu veride muhtemelen ölçülemeyecek kadar az işlem
bırakır.

Ayrıca toplam brüt monoton **değil** — optA'da −77 → +885 → +550. Yani kapıyı sH'nin
ötesine sıkmak, işlem başına değeri artırırken toplamda kaybettiriyor. Monotonluk
iddiası per-trade istatistiğiyle sınırlıdır.

Bu yüzden doz-tepki 2.5 aylık tek sembolde **kapatılamaz**. Gereken:

1. **Daha uzun geçmiş** (aynı sembol, 1–2 yıl), veya
2. **Havuzlanmış semboller** (10–20 sembolün işlemleri birleştirilerek), veya
3. **Daha yavaş periyot** (60 dk / günlük) — aynı takvim süresinde daha az ama daha
   uzun tutuşlu işlem; maliyet eşiğini geçmek de kolaylaşır.

Hedef: her `kD` seviyesinde **en az 30–50 işlem**. Bunun altındaki hiçbir sonuç
"kapı çalışıyor" demek için yeterli değil — nitekim şu ana kadarki en iyi hücre de
17 işleme dayanıyor.

---

## Karar tablosu

| Sonuç | Ne yapılır |
|---|---|
| Test 1 ✔ + Test 2 ✔ (medyan de artıyor) | Kapı gerçek bir filtre. Ürün budur; motor seçimi ayrı ve bağımsız iş |
| Test 1 ✔ + Test 2 ✘ (yalnız ortalama) | Kapı kuyruk seçici. Kullanılabilir ama "filtre" denemez, yoğunlaşma riski taşır |
| Test 1 ✘ | Hat kapanır. Doz-tepki OTT-A'ya özgüymüş |

Hiçbir sonuçta sayı ayarlanmaz. `kD`'yi kâr maksimize edecek şekilde seçmek testi
geçersiz kılar — 12 hücreden en iyisini seçmenin bir optimizasyon olması gibi.
