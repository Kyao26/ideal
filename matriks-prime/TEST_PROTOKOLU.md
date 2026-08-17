# Test Protokolü — ayakta kalan üç parça

OTT-A v1 parameterlemesi ölçümle düştü. Ayakta kalan üç şey var ve **üçü de
birbirinden bağımsız test edilebilir**. Aşağıdaki sıra rastgele değil: en sağlam
parçadan en spekülatife doğru.

Ortak koşullar: aynı sembol/periyot, komisyon binde 0.8/tur, kayma 2 p/tur,
sinyal barı kapanışında dolum.

---

## TEST 1 — Hurst kapısı, motordan bağımsız mı?

**Neden ilk sırada:** ölçümün en sağlam parçası. 98 işlem elendi, elenenlerin
ortalaması −35 p. Tek işleme dayanmıyor ve OTT-A omurgasıyla hiç ilgisi yok.

**Hipotez:** `Hrs > 0.5` kapısı, önüne konduğu **her** trend motorunun performansını
iyileştirir. Eğer bu doğruysa kapı gerçek bir bilgi taşıyor; yanlışsa OTT-A'ya özgü
bir tesadüftü.

**Kurulum:** `Hurst_Kapisi_Klasik_OTT_AL/SAT.txt`. Motor kasıtlı olarak **sabit
`opt`'lu klasik OTT** — adaptif bant tamamen devre dışı, yani kapı yalnız başına
ölçülüyor. Dosyanın ilk satırı `opt:=0.60;` — bu bir OPT parametresidir, **görünür ve
taranabilir**. Kapının katkısını farklı `opt` değerlerinde tekrarlayın.

| Koşu | AL koşulu |
|---|---|
| A (kapısız referans) | `Cross(C1,Ott)` — `AND Hrs>0.5` kısmını silin |
| B (kapılı) | dosyadaki hali |

**Bunu `opt` = 0.10 / 0.40 / 0.60 / 1.40 için ayrı ayrı yapın.**

**Geçme ölçütü:** B, A'yı **dört `opt` değerinin en az üçünde** geçmeli — ve elenen
işlemlerin ortalaması negatif kalmalı. Tek bir `opt` değerinde iyileşme tesadüftür.

**Kalırsa:** kapı OTT-A'ya özgü bir tesadüftü, tüm hat kapanır.

---

## TEST 2 — Kalibre edilmiş bant: ölen parameterleme miydi, fikir mi?

**Neden bu hiç test edilmedi:** `optA` medyanı 0.278 idi → yarı bant %0.139. 5 dakikalık
grafikte bu, ortalama **22 dakikalık tutuş** demek. O bir trend takip sistemi değil,
gürültü takibi. Geniş bantla hiç koşulmadı.

**Kurulum:** `OTT_A_v3_UfukKalibreli.txt` (ve sistem için `OTT_A_v3_Sistem_AL/SAT.txt`).
Tek fark, ilk satırdaki görünür sabit:

```
kN:=2.1;
```

`kN`, R/S ölçekleme yasasındaki ufuk çarpanıdır: N barlık sapma = 1 barlık sapma × N^H.
`kN = √N`. Varsayılan 2.1, ölçülen tutuş ufkundan gelir (22 dk ÷ 5 dk ≈ 4.4 bar → √4.4 ≈ 2.1).

| `kN` | karşılık gelen ufuk | optA medyanı ≈ |
|---|---|---|
| 1.0 | 1 bar (eski hâli) | 0.28 |
| 2.1 | 4.4 bar (ölçülen tutuş) | 0.58 |
| 3.2 | 10 bar | 0.88 |
| 4.6 | 21 bar (= pB) | 1.27 |

**`kN`'i OPT ile tarayın.** Bu bilinçli bir tercih: önceki sürümün asıl kusuru bu sabiti
formülün içine gömüp görünmez kılmasıydı. Artık dışarıda ve duyarlılığı ölçülebilir.

**Geçme ölçütü (sizin kabul kriterleriniz):**
- Brüt/işlem ≥ **30.4 p**
- En iyi **5** işlem çıkarıldığında net hâlâ pozitif
- `kN` taramasında sonuç **düz bir plato** oluşturmalı; tek bir `kN`'de sivrilip
  komşularında çökerse bu gürültüdür, kenar değil

### ⚠ Bu testin döngüsellik tuzağı

`kN = 2.1` seçimi, sizin en iyi ölçtüğünüz sabit bölgeye (0.40–0.60) denk geliyor.
**Bu, kendi başına bir kanıt değil — aksine bir uyarıdır.** "Backtest'te iyi çıkan yere
denk gelen ufku seçmek" gizli optimizasyondur; tam olarak bu tasarımın ilk sürümünde
yapılan hata.

Tek meşru savunma şu ikisidir, ikisi de test edilmeli:

1. **`N` ölçülür, seçilmez.** İşlem başına ortalama bar sayısını backtest çıktısından
   okuyun. `kN = √N` oradan gelmeli.
2. **Sabit nokta kararlı olmalı.** Yeni `kN` ile koştuktan sonra tutuş süresi uzayacak.
   Yeni tutuş süresini tekrar ölçün, `kN`'i güncelleyin, tekrar koşun. İki-üç turda
   sabitlenmeli. Sabitlenmiyor, salınıyorsa geri besleme kararsızdır ve tasarım kalır.

Eğer `kN`'i kâr maksimize edecek şekilde seçtiğinizi fark ederseniz test başarısızdır —
sonucun ne olduğundan bağımsız olarak.

---

## TEST 3 — pB eşik türetmesi (yalnız saklanacak, şu an test edilmeyecek)

Beş eşiğin beşi de FRAMA α = exp(−4.6(D−1)) eşlemesinden bağımsız olarak doğrulandı.
Bu, yeniden kullanılabilir doğru bir iş — ama v2 ölçümde v1'den kötü çıktı
(pB'nin %55.9'u 21'e yapışıyor), yani **eşlemenin bu enstrüman/periyot için ölçeği
yanlış**, türetme değil.

Şu an kullanmayın. Saklanma sebebi: başka bir bağlamda (farklı periyot, farklı motor)
D → periyot eşlemesi gerektiğinde eşikleri yeniden türetmeye gerek yok.

Eğer ileride kullanılacaksa önce ölçülmesi gereken: `pB` dağılımı bankaya **yayılıyor
mu**? Tek üyede toplanıyorsa banka aralığı o periyot için yanlıştır.

---

## Karar tablosu

| Sonuç | Ne yapılır |
|---|---|
| TEST 1 geçer, TEST 2 kalır | Ürün **kapıdır**. Sabit `opt`'lu klasik OTT + Hurst kapısı; adaptif bant terk edilir |
| TEST 1 geçer, TEST 2 geçer | OTT-A v3 yaşar; ama `kN` görünür bir parametre olarak kalır, "parametresiz" denmez |
| TEST 1 kalır | Tüm hat kapanır. Kapı da OTT-A'ya özgü tesadüfmüş demektir |

Hiçbir sonuçta sayı ayarlanmaz. Kalırsa tasarım reddedilir.
