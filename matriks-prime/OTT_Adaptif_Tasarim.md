# OTT-A — Karakter Uyarlamalı OTT (Matriks Prime)

> **Amaç:** OTT'yi kaldırmak değil, OTT'ye elle atadığımız **1 veya 2 opt parametresini**
> (yüzde ve periyot) sabit sayı olmaktan çıkarıp, canlı grafiğin ölçülen karakterinden
> matematiksel olarak türetmek. Sistem böylece yönünü, o anki grafiğin fraktal ve
> oynaklık karakterine göre kendisi tayin eder.

---

## 1. OTT'nin iki "opt"si tam olarak ne yapıyor?

Matriks'te dolaşan standart OTT:

```
opt:=1.4;  per:=2;
C1:=Mov(C,per,VAR);
q1:=Ref(C1,-2)*(1+opt/200);
q2:=Ref(C1,-2)*(1-opt/200);
If(Cum(1)=1,C1,If(q1<=PREV,q1,If(q2>=PREV,q2,PREV)));  C1
```

| Parametre | Matematiksel rolü | Sabit olunca ne olur? |
|---|---|---|
| `per` | VAR (VIDYA) ortalamasının temel hızı; α = 2/(per+1). Hafıza uzunluğunu belirler. | Grafiğin baskın salınım süresi değişince ortalama ya çok geç ya çok erken kalır. |
| `opt` | Ratchet bandının **yarı genişliği**, fiyatın oranı olarak: bant = C1 × opt/200. Trendin "geri çekilme toleransı". | Sabit % bir bant, oynaklık iki katına çıktığında aynı kalır → whipsaw. Oynaklık düşünce gereksiz geniş kalır → geç çıkış. |

Yani `opt` bir **eşik**tir ve her eşik, karşılaştırıldığı büyüklüğün birimiyle
ölçeklenmelidir. Sabit 1.4 sayısı, "bu hissenin gürültüsü şu kadardır" demenin dolaylı
ve donmuş halidir. Tasarımın çekirdek fikri: **o cümleyi grafiğe her bar yeniden
söyletmek.**

---

## 2. Grafiğin "karakteri" nasıl ölçülür? (kullanılan matematik)

### 2.1 Fraktal boyut D ve Hurst üsteli H — *kalıcılık ölçüsü*

Ehlers'in menzil-ikiye-katlama tahmincisi (FRAMA'nın çekirdeği). n barlık iki bitişik
pencerenin ve 2n barlık birleşik pencerenin normalize menzilleri:

```
N1 = (HHV(H,n)      − LLV(L,n))      / n           { son yarı  }
N2 = (HHV(H,n)[−n]  − LLV(L,n)[−n])  / n           { önceki yarı }
N3 = (HHV(H,2n)     − LLV(L,2n))     / 2n          { bütün      }

D  = ( log(N1+N2) − log(N3) ) / log 2
H  = 2 − D
```

Yorum: bir seriyi iki katı büyütecin altında baktığınızda menzili nasıl ölçekleniyor?
Düz bir çizgide D→1 (H→1, tam kalıcı/trendli); tam gürültüde D→2 (H→0, salınımlı);
rastgele yürüyüşte D≈1.5 (H≈0.5).

> Not: formül bir **log oranı** olduğu için logaritmanın tabanı sonucu değiştirmez —
> `Log` doğal ya da 10 tabanlı olsun, D aynıdır. Matriks'in `Log` tanımını
> araştırmaya gerek yok.

### 2.2 Rezidüel oynaklık σ_R — *gürültü genliği*

Bant, fiyatın kendi oynaklığıyla değil, **ortalamadan sapmanın** oynaklığıyla
ölçeklenmeli — çünkü OTT'nin bandı C1 etrafına kuruluyor:

```
Rz   = (C − C1) / C                              { boyutsuz artık }
σ_R  = 1.2533 · Sum(|Rz − ort(Rz)|, 2n) / 2n     { ortalama mutlak sapma → σ }
```

`Stdev()` yerine ortalama mutlak sapma kullanıldı: fonksiyon adı sürümden sürüme
değişebiliyor, `Sum` ve `Abs` ise her sürümde var. `1.2533 = √(π/2)` normal dağılımda
mutlak sapmayı standart sapmaya çeviren matematiksel dönüşüm sabitidir — ayarlanabilir
bir parametre değil. Yan faydası: MAD, aykırı barlara standart sapmadan daha dayanıklıdır.

Fiyata bölündüğü için TL cinsinden fiyat seviyesinden bağımsızdır: 5 TL'lik hisse ile
500 TL'lik hisse aynı formülü kullanır.

### 2.3 (Opsiyonel) Etkin uzunluk — *baskın hız*

Ehlers'in FRAMA katsayısı, fraktal boyutu doğrudan bir üstel yumuşatma hızına çevirir:

```
α = exp(−4.6 (D − 1))        { D=1 → α=1 (çok hızlı), D=2 → α≈0.01 (çok yavaş) }
L = 2/α − 1                  { α = 2/(L+1) tersinden etkin uzunluk }
```

---

## 3. Tasarımlar

### Tasarım 1 — **Adaptif `opt`** (tek parametre uyarlanır) — *başlangıç için önerilen*

`per` klasik değerinde (2) kalır, `opt` her bar yeniden hesaplanır:

```
bant_oranı = z · σ_R        ve      opt = 200 · bant_oranı
z = 1 / (2H)                →       opt = 100 · σ_R / H
```

**`z = 1/(2H)` neden?** Bant, gürültünün sıradan geri çekilmelerini yutmalı ama trendin
dönüşünü yutmamalı. H bu ikisinin oranını ölçer:

| Karakter | H | z | Bandın davranışı |
|---|---|---|---|
| Kalıcı / trendli | 0.7 | 0.71 | **Daralır** — trend gerçek, erken teyit et, kârı bırakma |
| Rastgele yürüyüş | 0.5 | 1.00 | 1σ — nötr referans |
| Salınımlı / testere | 0.3 | 1.67 | **Genişler** — kırılımların çoğu sahte, dokunma |

Bandın *seviyesi* σ_R'den (ölçülen gürültü), *modülasyonu* H'den (ölçülen kalıcılık)
gelir. Formülde elle konmuş çarpan yoktur.

- ➕ Tek satırlık değişiklik; mevcut OTT alışkanlığınızı bozmaz.
- ➕ Sembolden sembole, periyottan periyoda kendi kendine ölçeklenir; hisse başına
  ayrı `opt` tutmaya gerek kalmaz.
- ➖ `per=2` sabit kaldığı için çok yavaş rejimlerde (haftalık, düşük hacimli semboller)
  ortalama hâlâ hızlı kalabilir.

### Tasarım 2 — **Adaptif `per` + adaptif `opt`** (iki parametre de uyarlanır)

Tasarım 1'e ek olarak periyot da D'den türetilir ve **geometrik bir bankaya** yuvarlanır:

```
L = 2/exp(−4.6(D−1)) − 1
per ∈ {2, 3, 5, 8, 13, 21}   ← L'ye en yakın üye
eşikler: √(2·3)=2.45, √(3·5)=3.87, √(5·8)=6.32, √(8·13)=10.2, √(13·21)=16.5
```

Eşikler komşu üyelerin **geometrik ortalamasıdır** — yani seçilmiş değil, bankanın
kendisinden türemiştir.

Kodda `Exp()` çağrısı yok: yukarıdaki eşikler bir kez tersine çevrilip doğrudan
`Rr = 2^D` üzerinden yazıldı (`Rr < 2.171 → per 2`, … , `> 2.774 → per 21`). Aynı
matematik, bir fonksiyon eksiğiyle. Yön kontrolü: Rr büyüdükçe D büyür, grafik
testereleşir, seçilen periyot **uzar** — yani ortalama yavaşlar. Doğru davranış.

> **Neden banka?** Matriks formül dilinde `Mov(C, per, VAR)` çağrısının periyodu sabit
> olmak zorundadır; seri veremezsiniz. Bu yüzden 6 sabit VAR ortalaması önceden
> hesaplanıp iç içe `If()` ile seçilir. Dilin kısıtı tasarımı belirledi, tersi değil.

- ➕ Hem hız hem tolerans grafikten gelir; sistem gerçekten "karakter okur".
- ➖ Periyot bir bankadan diğerine atlarken C1'de küçük sıçramalar olur (ratchet bunu
  büyük ölçüde yutar, ama sinyal barında ufak gecikme yaratabilir).
- ➖ Formül uzar, hata ayıklaması zorlaşır.

### Tasarım 3 — **Yön tayini katmanı (rejim anahtarı)**

"Sistemin yönünü grafiğin karakterine göre tayin etmesi" isteğinin doğrudan karşılığı.
OTT sinyali her zaman uygulanmaz; H'nin kendisi bir **anahtar**tır:

| Ölçülen karakter | Rejim | Sistemin yönü |
|---|---|---|
| H > 0.5 (D < 1.5) | Kalıcı / trend | OTT yönü uygulanır (AL / SAT) |
| H ≤ 0.5 (D ≥ 1.5) | Salınımlı | **Yön alınmaz** (flat) — OTT bu rejimde matematiksel olarak bilgi taşımıyor |

0.5 eşiği keyfi bir tercih değil: rastgele yürüyüşün tam kendisidir. "Trend takibinin
beklenen değeri ancak seri kalıcıysa pozitiftir" ifadesinin sayısal karşılığı H > 0.5'tir.

- ➕ En büyük zarar kalemi olan yatay piyasa aşınmasını doğrudan keser.
- ➖ H tahmini gürültülüdür; sınır civarında sık moda giriş/çıkış olabilir (kodda
  histerezis için 0.5 yerine ±band uygulanabilir — bu bir ayar değil, tahmincinin
  gürültüsüne karşı önlemdir).

### Tasarım 4 — **Üstel ağırlıklı OTT ansamblı** (ileri seviye, opsiyonel)

Tek bir (per, opt) çifti seçmek yerine 3 varyant paralel çalışır; her varyantın ağırlığı
son performansının üstel fonksiyonudur (Hedge / multiplicative weights):

```
w_i,t ∝ w_i,t−1 · exp( η · getiri_i,t )        (ağırlıklar normalize edilir)
yön   = işaret( Σ w_i · yön_i )
```

Online öğrenme teorisinden gelen bu şemanın çekici yanı, "en iyi varyanta göre pişmanlık
(regret) sınırlıdır" garantisidir — yani hiçbir zaman geriye dönük en iyi sabit seçimden
anlamlı ölçüde kötü olamaz, üstelik geçmişe fit edilmeden.

- ➕ Parametre seçimi problemini teorik garantiyle ortadan kaldırır.
- ➖ Matriks formül dilinde ağırlık özyinelemesi ek `PREV` zinciri ister; tek formülde
  bir PREV zinciri sınırı yüzünden çok formüllü bir kuruluma ihtiyaç duyar. Bu yüzden
  bu depoda **kodlanmadı**, tasarım olarak bırakıldı.

### Karşılaştırma

| Kriter | T1 | T2 | T3 | T4 |
|---|:--:|:--:|:--:|:--:|
| Uygulama kolaylığı (Prime) | ★★★ | ★★☆ | ★★★ | ★☆☆ |
| Karaktere duyarlılık | ★★☆ | ★★★ | ★★★ | ★★★ |
| Sağlamlık (kırılganlık azlığı) | ★★★ | ★★☆ | ★★★ | ★★★ |
| Teorik dayanak | ★★☆ | ★★★ | ★★★ | ★★★ |

**Önerilen kurulum: T1 + T3.** Kararlı çalıştığını gördükten sonra `per`'i de T2 ile
serbest bırakın. T4'ü ancak çok formüllü kuruluma girmeye hazırsanız değerlendirin.

---

## 3.5 Hangi dil? — Prime ≠ Matriks IQ

Önemli bir ayrım: **Matriks IQ'nun formül dili tamamen farklıdır.** Prime, klasik
(MetaStock benzeri) Matriks dilini kullanır — `:=` atama, `PREV` özyinelemesi,
`Cum(1)` bar sayacı, `Mov(C,per,VAR)`. Bu depodaki formüller Prime lehçesindedir;
IQ'ya taşımak isterseniz yeniden yazım gerekir.

**Kullanılan fonksiyon kümesi bilinçli olarak dar tutuldu:**

| Kullanılan | `C H L` · `Mov(...,VAR)` · `Ref` · `HHV` · `LLV` · `Sum` · `Abs` · `Log` · `If` · `Cum` · `Cross` · `PREV` |
|---|---|
| **Kullanılmadı** | `Stdev` · `Exp` · `Sqr` · `Max` · `Min` — varlıkları/anlamları sürüme göre değişebildiği için `Sum`+`Abs` ile yeniden yazıldı ya da literal sabite çevrildi |

**Indicator Builder testiyle doğrulandı:** karekök fonksiyonunun adı `Sqrt` değil
**`Sqr`**, ve `Log()` mevcut (ekranda 4.6052).

İki belirsizlik kaldı ve **kod ikisinden de bağımsız kılındı**:

- `Sqr()` karekök mü, kare mi? (`Sqr(16)=4` → karekök; `Sqr(2)=4` → kare; ekrandaki
  tek başına 4 değeri ikisini de açıklıyor.) → Kodda `Sqr` hiç kullanılmıyor;
  karekök(20) gereken tek yere `4.4721` literali yazıldı.
- `Log` doğal mı, 10 tabanlı mı? → Kodda yalnızca `Log(Rr)/Log(2)` oranı içinde
  geçiyor; taban sadeleşir, ikisi de aynı sonucu verir.

Yani formüller artık sadece şu kümeye dayanıyor: `Mov(...,VAR)`, `Ref`, `HHV`, `LLV`,
`Sum`, `Abs`, `Log`, `If`, `Cum`, `Cross`, `PREV`.

Geriye doğrulanacak üç şey kalıyor — `00_SOZDIZIMI_TESTI.txt` tam olarak bunları
ölçer: **(a)** `Mov(C,2,VAR)` (VIDYA tipinin adı), **(b)** `PREV`'in atandığı
değişkenin geçmişine bağlanıp bağlanmadığı, **(c)** ratchet kalıbının bütün olarak
çalışması. (b) beklendiği gibi çıkmazsa sistem koşulları kendi kendine yeterli olmaz;
OTT-A'nın kayıtlı indikatör olarak çağrılması gerekir.

> **OTT zorunlu değil.** Ratchet motoru olarak OTT seçildi çünkü bu lehçede çalıştığı
> kanıtlanmış tek `PREV` kalıbı o. Karakter ölçüm katmanı (D → H → σ_R → opt) motordan
> bağımsızdır; SuperTrend, HalfTrend ya da düz bir yüzdesel iz süren stop da aynı
> `optA` serisiyle beslenebilir. Motoru değiştirmek isterseniz ölçüm katmanı aynen kalır.

## 4. Matriks Prime dilinin dayattığı kısıtlar

Tasarımı doğrudan şekillendirdikleri için açıkça yazıyorum:

1. **Döngü yok.** Argmax arayan (ör. otokorelasyon periodogramı ile baskın döngü bulan)
   yöntemler doğrudan yazılamaz. Bu yüzden karakter ölçümü `HHV/LLV/Sum/Abs` gibi
   kapalı-form, pencere tabanlı tahmincilerle yapıldı.
2. **Formül başına tek `PREV` zinciri.** OTT'nin ratchet'i zaten bir özyinelemedir; o
   yüzden diğer her şey (D, H, σ_R, per seçimi) **özyinelemesiz** tutuldu. T4'ün
   kodlanmamasının sebebi budur.
3. **Değişken periyot yok.** `Mov(C, per, VAR)` sabit periyot ister → T2'de banka + `If`
   seçimi.
4. **Sistem/Explorer'da PREV sorunu.** AL/SAT koşulunun içine OTT'yi doğrudan gömerseniz
   `PREV` koşulun kendi geçmişine bağlanır ve ratchet bozulur. Doğru yol: OTT-A'yı
   **kayıtlı indikatör** olarak oluşturup System Tester / Explorer içinde o kayıtlı
   indikatörü çağırmaktır. Kod dosyalarında bu iki kullanım ayrı ayrı belirtildi.

---

## 5. Dosyalar

| Dosya | İçerik |
|---|---|
| `OTT_Adaptif_v1_OptAdaptif.txt` | Tasarım 1 — `per=2` sabit, `opt` adaptif (indikatör) |
| `OTT_Adaptif_v2_PerVeOptAdaptif.txt` | Tasarım 2 — `per` ve `opt` birlikte adaptif (indikatör) |
| `OTT_Adaptif_Karakter_Gostergesi.txt` | Teşhis paneli: D, H, σ_R, hesaplanan `opt` ve `per` |
| `OTT_Adaptif_Sistem_Explorer.txt` | Tasarım 3 rejim anahtarıyla AL/SAT koşulları ve tarama |

---

## 6. Kurulum sırası

1. `OTT_Adaptif_Karakter_Gostergesi.txt` ile başlayın. **Önce ölçtüğünüz şeye bakın:**
   H makul aralıkta mı (çoğunlukla 0.3–0.7), hesaplanan `opt` sizin elle kullandığınız
   değere yakın bir bantta mı geziniyor, sıçramalar var mı?
2. Tatmin olduysanız `OTT_Adaptif_v1` indikatörünü kaydedin, grafiğe ekleyin, klasik
   OTT (per=2, opt=1.4) ile **aynı grafikte** karşılaştırın.
3. `OTT_Adaptif_Sistem_Explorer.txt` içindeki koşullarla System Tester'da test edin.
4. Ancak bundan sonra `v2`'ye geçin.

---

## 7. Doğrulama — ayar değil, sağlamlık

Bu tasarımın amacı optimize edilecek yüzeyi yok etmek olduğuna göre, doğrulama da
"en iyi değeri bulma" değil "sonucun ölçüm seçimlerine duyarsız olduğunu gösterme"dir:

1. **Ölçüm penceresi testi:** `n = 8, 10, 16, 20` ile çalıştırın. Sonuç karakteri
   değişmemeli. Değişiyorsa tahminci gürültülüdür, pencereyi büyütün — kârı değil
   tahmincinin kararlılığını izleyerek.
2. **Çapraz sembol:** 20–30 BIST hissesi. Aynı formül, sembol başına hiçbir değişiklik
   olmadan çalışmalı. Zaten tasarımın vaadi bu.
3. **Çapraz periyot:** günlük / 60dk / haftalık. `opt` otomatik olarak periyot başına
   farklı seviyelere oturmalı — oturmuyorsa σ_R ölçeklemesi hatalıdır.
4. **Klasik OTT ile eşleştirme:** T1'in ürettiği `opt` serisinin medyanı, elle
   kullandığınız sabit değere yakın çıkmalı. Sistematik olarak çok uzaksa **çarpan
   eklemeyin**; σ_R'nin penceresini veya artık tanımını gözden geçirin. Çarpan eklemek
   optimizasyona geri dönmektir.
5. **Rejim anahtarı katkısı:** T3'ü açık/kapalı iki koşu. Kapalıyken belirgin biçimde
   daha kötüyse anahtar gerçek bilgi taşıyor demektir.

---

## 8. Bilinen sınırlar

- H tahmini kısa pencerede gürültülüdür; menzil tabanlı tahminci boşluklu (gap) ve
  tavan/taban serilerinde şişer.
- T2'de periyot bankası değiştiğinde C1'de sıçrama olur.
- σ_R, C1'e bağlıdır; C1 de per'e bağlıdır → T2'de hafif bir geri besleme döngüsü vardır
  (aynı barda per değişirse σ_R bir bar gecikmeli tepki verir). Pratikte zararsız, ama
  bilinmesi gerekir.
- Tüm bant mantığı yüzdeseldir; çok düşük fiyatlı (kuruş) sembollerde tik boyutu bandı
  anlamsızlaştırabilir.
