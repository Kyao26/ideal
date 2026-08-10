# VWAP (Hacim Ağırlıklı Ortalama Fiyat) — Matriks Prime

Seans boyunca hacme göre ağırlıklandırılmış ortalama fiyatı gösteren, kurumsal bir adil değer referans noktası.

**Seviye:** Orta — formülü basittir, asıl zorluk başlangıç noktası (anchor) seçimi ve Prime'da seans sıfırlamasının kurulmasıdır.

> Bu dokümandaki formüller **Matriks Prime** formül dili içindir. Doğrulama durumu için [Doğrulama Notu](#doğrulama-notu) bölümüne bakın.

## Nedir

VWAP, bir menkul kıymetin belirli bir başlangıç noktasından bu yana işlem gördüğü ortalama fiyattır; her fiyat seviyesine eşit ağırlık veren basit bir ortalama değil, her seviyede ne kadar hacim işlem gördüğüne göre ağırlıklandırılmış bir ortalamadır.

Kurumlar onu bir uygulama (execution) referansı olarak kullanır: VWAP'a yakın ya da ondan iyi bir fiyattan gerçekleşen büyük emir iyi uygulama sayılır. Bunun önemli bir yan etkisi var: VWAP yaygın bir kıyas ölçütü olduğu için, gün boyunca onu hedefleyen execution algoritmaları fiyatı kısmen VWAP'a doğru çeker. Araç bir ölçüde kendi kendini gerçekleştirir.

VWAP kümülatif bir hesaptır — bir başlangıç noktasından itibaren biriken toplamlarla çalışır. En kritik tasarım kararı o başlangıç noktasının nerede olduğudur.

## Prime'da Tipik Fiyat Meselesi: `W` Kullanın

Ders kitabı VWAP anlatımları tipik fiyatı `(H+L+C)/3` diye tanımlar. Bu bir **yaklaşımdır** — bar içindeki gerçek hacim dağılımını bilmediğiniz için üç noktadan tahmin edersiniz.

Matriks Prime'da buna gerek yok. **`W` = Ağırlıklı Ortalama Fiyat (AOF)**, borsanın o bar için yayımladığı gerçek hacim ağırlıklı fiyattır. Yani `W` zaten bar içi VWAP'tır.

```
{ Bunu yapmayın — Prime'da gereksiz bir kalite kaybı }
tp := (H+L+C)/3;

{ Bunu yapın — bar içi gerçek hacim ağırlıklı fiyat }
tp := W;
```

`CUM(W*V)/CUM(V)` bu yüzden `(H+L+C)/3` tabanlı bir VWAP'tan **daha doğrudur**, yaklaşık değildir. TradingView'dan taşınan formülleri Prime'a çevirirken hlc3'ü olduğu gibi bırakmak yaygın bir hatadır.

İlgili not: `TLVOL` (TL cinsinden ciro) tanımı gereği ≈ `W*V` olduğundan, `CUM(TLVOL)/CUM(V)` da aynı sonucu verir ve daha kısadır. Kendi verinizde bir barda `TLVOL/V ≈ W` olduğunu doğrulayıp kullanabilirsiniz.

## Seans VWAP'ı — Temel Formül

Prime'da `CUM()` grafiğin başından birikir ve **seansta sıfırlanmaz**. Seans sıfırlaması, gün başındaki birikimi yakalayıp çıkararak taklit edilir:

```
{ Seans VWAP'ı — gün içi periyotlarda }
d := VALUEWHEN(1, DAYOFMONTH() <> REF(DAYOFMONTH(), -1), REF(CUM(W*V), -1));
f := VALUEWHEN(1, DAYOFMONTH() <> REF(DAYOFMONTH(), -1), REF(CUM(V), -1));

(CUM(W*V) - d) / (CUM(V) - f)
```

`d` ve `f`, günün ilk barından bir önceki bardaki (yani dünkü kapanıştaki) birikimi tutar. Şimdiki birikimden çıkarınca geriye yalnızca bugünkü birikim kalır. VWAP iki kümülatif toplamın oranı olduğu için bu hile temiz çalışır.

**Dikkat:** `DAYOFMONTH() <> REF(DAYOFMONTH(),-1)` sıfırlaması yalnızca gün içi barlarda anlamlıdır. Günlük periyotta her bar yeni gündür → her barda sıfırlanır → VWAP `W`'ye çöker. Bu aslında doğru davranıştır (seans VWAP'ı günlükte anlamsızdır) ama sessizce olur, hata vermez.

Haftalık veya aylık sıfırlama isterseniz koşulu değiştirin:

```
{ Aylık reset }
MONTH() <> REF(MONTH(), -1)
```

## Bantlar: İki Ayrı Yapı, İki Ayrı Anlam

Burada bir çatal var ve hangisini seçtiğiniz bandın **nasıl okunacağını** değiştirir.

### (a) Kümülatif hacim ağırlıklı bant

Ders kitabı versiyonu. Aynı çıkarma hilesini kareler toplamına da uygularsınız:

```
{ VWAP + kümülatif hacim ağırlıklı stdev bandı }
WP   := If(W > 0, W, (H+L+C)/3);
YGUN := DAYOFMONTH() <> Ref(DAYOFMONTH(), -1);

{ Gün başı referansı — kaydırma tabanı, gün içinde sabit }
KAY := ValueWhen(1, YGUN, Ref(C, -1));
XD  := WP - KAY;

d := ValueWhen(1, YGUN, Ref(Cum(XD*V), -1));
f := ValueWhen(1, YGUN, Ref(Cum(V), -1));
g := ValueWhen(1, YGUN, Ref(Cum(XD*XD*V), -1));

DHAC := Cum(V) - f;
ORTX := If(DHAC > 0, (Cum(XD*V) - d) / DHAC, 0);
vwap := If(DHAC > 0, KAY + ORTX, C);
VARY := If(DHAC > 0, (Cum(XD*XD*V) - g) / DHAC - ORTX*ORTX, 0);

vwap + 2 * Sqr(MAX(VARY, 0))
```

Alt bant için son satırı `vwap - 2*Sqr(MAX(VARY,0))` yapın.

**Neden `XD = WP - KAY` üzerinden hesaplanıyor?** Doğrudan `E[x²] - E[x]²` yazmak matematiksel olarak doğrudur ama sayısal olarak çöker: sonuç, iki büyük ve birbirine çok yakın sayının küçük farkıdır. X30YVADE 5dk / 7.516 bar üzerinde ölçüldüğünde float32'de **ortalama %85 hata** ve barların **%17-21'inde `sap = 0`** çıkıyor. Gün başına kaydırınca hata %0,26'ya, sıfırlanma oranı %0'a düşüyor. Varyans kaydırmaya duyarsız olduğu için sonuç birebir aynı, sadece büyüklükler ~6.800 kat küçülüyor.

Kaydırmanın algebrası şu yüzden bozulmaz: `Cum` tüm geçmişi toplarken her bar kendi gününün `KAY`'ını kullanır, ama gün başındaki değer çıkarılınca geriye yalnızca o gün kalır ve o gün içinde `KAY` sabittir.

Çökmenin **hata mesajı yoktur**: `MAX(VARY,0)` guard'ı negatif varyansı 0'a kırpar, bantlar VWAP çizgisine yapışır ve bant koşulları hiç tetiklenmez.

`vwap`'ın kendisi ham formülde bile sağlamdır — birinci moment oranında çıkarma hatası günün toplamına göre küçük kalır. Kırılgan olan yalnızca kareli terimdir.

**Söz dizimi uyarısı:** Karekök fonksiyonu Prime'da `Sqr()`'dir — `SQRT()` diye bir fonksiyon **yoktur**. Alternatifi `Power(VARY, 0.5)`.

**Okunuşu:** Bu gerçekten hacim ağırlıklı dağılımdır. Ama seans başından kümülatif olduğu için örneklem büyüdükçe her yeni barın etkisi azalır — **bandın tepkiselliği seans ilerledikçe söner**. Kapanışa yakın bandın hareketsizleşmesi bir volatilite sinyali değil, sadece paydanın büyümüş olmasıdır. Bollinger'ın squeeze okuması buraya **taşınmaz**.

### (b) Rolling stdev bandı — yani VWAP tabanlı Bollinger

```
{ VWAP + kayan pencere stdev bandı }
d  := VALUEWHEN(1, DAYOFMONTH() <> REF(DAYOFMONTH(), -1), REF(CUM(W*V), -1));
f  := VALUEWHEN(1, DAYOFMONTH() <> REF(DAYOFMONTH(), -1), REF(CUM(V), -1));

vw := (CUM(W*V) - d) / (CUM(V) - f);

vw + 2 * STDEV(vw, 20)
```

**Okunuşu:** Bunu yazdığınız anda elinizdeki şey, basis'i hareketli ortalama yerine VWAP olan bir **Bollinger Bandı**dır. Kayan pencere semantiği birebir geçerlidir — squeeze/expansion okuması çalışır, (a)'daki "tepkisellik söner" uyarısı çalışmaz.

**Ama bir taviz var:** `STDEV()` hacim ağırlıklı **değildir**. (b) melez bir şey üretir: merkez çizgi hacim ağırlıklı, dağılım ağırlıksız. Ne ders kitabı VWAP bandıdır ne de saf Bollinger. Okunaklı ve stabil bir banttır, kullanılabilir — ama "hacim ağırlıklı 2 sigma" diye yorumlamak yanlış olur.

### Hangisi?

| | (a) Kümülatif | (b) Rolling / BB |
|---|---|---|
| Dağılım hacim ağırlıklı mı | Evet | Hayır |
| Seans içinde davranış | Tepkisellik söner | Sabit tepkisellik |
| Squeeze/expansion okunur mu | Hayır | Evet |
| Yazması | Uzun | Kısa |

Gerçekten hacim ağırlıklı uçlar arıyorsanız (a). Tanıdık bir bant davranışı ve BB refleksi istiyorsanız (b) — yeter ki ne ölçtüğünü bilerek kullanın.

## Sabitlenmiş VWAP (Anchored)

Trader başlangıç noktasını kendi seçer — bir swing dip, bir bilanço tarihi — ve günlük sıfırlama olmadan oradan itibaren birikir. Ölçtüğü şey: *o olaydan bu yana işlem yapan herkesin ortalama maliyet tabanı*.

```
{ Belirli bir tarihe sabitlenmiş VWAP }
anc := YEAR()=2026 AND MONTH()=3 AND DAYOFMONTH()=10;
ilk := anc AND REF(anc, -1) = 0;          { o günün ilk barı }

d := VALUEWHEN(1, ilk, REF(CUM(W*V), -1));
f := VALUEWHEN(1, ilk, REF(CUM(V), -1));

(CUM(W*V) - d) / (CUM(V) - f)
```

`REF(anc,-1)=0` şartı önemli: onsuz `VALUEWHEN(1, ...)` o günün **son** barını yakalar, ilkini değil.

Anchored VWAP gün içi olmak zorunda değildir; günlük ve haftalık grafiklerde de anlamlıdır. Profesyonel kullanımın büyük kısmı da oradadır.

## Kayan VWAP (Rolling) — Karıştırmayın

Rolling VWAP'ın sabit başlangıç noktası **yoktur**; son N bar üzerinde kayan pencereyle hesaplanır:

```
{ 200 barlık rolling VWAP — anchored ile aynı şey DEĞİL }
SUM(W*V, 200) / SUM(V, 200)
```

Anchored ile rolling sık sık eş anlamlı sanılır; değildir. Rolling, "bir olaydan bu yana maliyet tabanı" sorusunu **yanıtlamaz** — hacim ağırlıklı bir hareketli ortalama gibi davranır.

Anchor seçimi metodolojisi basit: hangi soruyu sorduğunuza karar verin.
"Bugün alanlar nerede?" → seans VWAP'ı.
"Bilanço sonrası girenler nerede?" → o tarihe sabitlenmiş AVWAP.
"Son N barın hacim ağırlıklı eğilimi ne?" → rolling VWAP.

## Traderlar Nasıl Kullanır

Fiyat VWAP'ın üzerindeyse o gün alım yapanların ortalaması kârdadır; altındaysa zarardadır. VWAP'ın tekrar geçilmesi bu yüzden gün içi kontrolde bir değişim olarak izlenir.

Explorer/tarama için kesişim:

```
d := VALUEWHEN(1, DAYOFMONTH() <> REF(DAYOFMONTH(), -1), REF(CUM(W*V), -1));
f := VALUEWHEN(1, DAYOFMONTH() <> REF(DAYOFMONTH(), -1), REF(CUM(V), -1));

CROSS(C, (CUM(W*V) - d) / (CUM(V) - f))
```

Bantlı ve gürültü korumalı tam filtre, alternatif tarama koşullarıyla birlikte [`VWAP_Filtre.txt`](VWAP_Filtre.txt) dosyasında — doğrudan Explorer'a yapıştırılabilir.

Seans VWAP'ının yanında sık kullanılan diğer seviyeler: önceki günün VWAP'ı, haftalık ve aylık VWAP.

## Tuzaklar ve Yanlış Anlamalar

**"Seans VWAP'ı" ile "VWAP"ı karıştırmayın.** Gün içi kısıtı VWAP'ın kendisine değil, günlük sıfırlama mekanizmasına aittir. Seans VWAP'ı günlük grafikte anlamsızdır; sabitlenmiş VWAP ise günlük ve haftalıkta gayet anlamlıdır.

**Seansın tanımı bir parametredir.** Vadelide normal seans (RTH) mı uzatılmış seans (ETH) mı baz alındığı VWAP'ı tamamen değiştirir. Kriptoda "seans" zaten keyfîdir. Prime'da bunu `DAYOFMONTH()` reset koşulunuz belirler — yani seçim sizin, ve sessizdir.

**Güvenilir hacim şart.** BIST'te `W` ve `V` sağlamdır. Spot FX'te merkezi hacim yoktur; orada VWAP bir yaklaşımdır.

**İki uçtaki atalet.** Seansın ilk dakikalarında payda küçüktür, VWAP gürültülüdür ve zıplar. Sonunda payda büyümüştür, VWAP neredeyse çivilenmiştir. Aynı mekaniğin iki yüzü — pratik sonucu: erken seans kesişimi zayıf bilgidir, geç seans kesişimi güçlüdür, çünkü onu üretmek gerçekten büyük hacim gerektirmiştir.

**Yönlü bir gösterge değildir.** Güçlü bir trend gününde fiyat, hiçbir "dönüş" olmadan saatlerce çizginin bir tarafında kalabilir.

## Grafikte Deneyin

1. **5 veya 15 dakikalık** bir grafikte seans VWAP'ını yükleyin. Saatlik kullanmayın: BIST seansı saatlik grafikte seans başına çok az bar verir, kümülatif bir göstergenin davranışı okunmaz.
2. `W` tabanlı VWAP ile `(H+L+C)/3` tabanlısını aynı grafiğe koyup farkı görün — özellikle hacmin bar içinde tek tarafa yığıldığı barlarda ayrışırlar.
3. (a) ve (b) bantlarını üst üste çizin. Seans başında birbirine yakın, kapanışa doğru belirgin biçimde ayrışacaklardır — (a) donarken (b) nefes almaya devam eder.
4. Bant çarpanını 1'den 2'ye genişletin, fiyatın dış banda ne kadar daha seyrek dokunduğuna bakın.
5. Güçlü bir trend günü ile yatay bir günü karşılaştırın: trend günleri çizginin bir tarafına yapışır, yatay günler tekrar tekrar üzerinden geçer.

## Formül Özeti

```
Seans VWAP  = (CUM(W*V) - d) / (CUM(V) - f)
Kümülatif σ = Sqr( (CUM(W*W*V)-g)/(CUM(V)-f) - VWAP² )     → bant: VWAP ± k*σ
Rolling  σ  = STDEV(vw, n)                                   → bant: VWAP ± k*σ  (= BB)
Rolling VWAP = SUM(W*V, n) / SUM(V, n)
```

`d`, `f`, `g` = seçilen sıfırlama noktasındaki birikim değerleri.

## Önemli Noktalar

- VWAP hacim ağırlıklı ortalama fiyattır — hareketli ortalama değildir, bir başlangıç noktasından kümülatiftir.
- Prime'da tipik fiyat için `W` (AOF) kullanın; `(H+L+C)/3` gereksiz bir yaklaşımdır.
- `CUM()` sıfırlanmaz; seans reset'i `VALUEWHEN` + `REF` çıkarma hilesiyle kurulur.
- Bant seçimi anlamı değiştirir: kümülatif bant hacim ağırlıklıdır ama seans içinde donar; rolling bant donmaz ama hacim ağırlıklı değildir ve fiilen VWAP tabanlı Bollinger'dır.
- Gün içi kısıtı VWAP'a değil seans sıfırlamasına aittir; anchored VWAP günlük/haftalıkta da anlamlıdır.
- Rolling (kayan pencere) ile anchored (sabit başlangıç) aynı şey değildir, farklı soruları yanıtlarlar.
- VWAP'ın referans gücü kısmen refleksiftir — onu hedefleyen algoritmalar fiyatı ona doğru çeker.
- Erken seansta gürültülü, geç seansta atıl; kesişimlerin bilgi değeri seans içinde değişir.

## Doğrulama Notu

**Söz dizimi** Prime kullanım kılavuzuna göre teyit edildi:

| Öğe | Durum |
|---|---|
| `VALUEWHEN(N, koşul, değer)` | N=1 **en son** oluşumu verir. Parametre `1`, `1.` değil. |
| `CUM(1)` | Bar sayacı — grafiğin başından her bar 1 ekler. |
| `Sqr(Data)` | Karekök. **`SQRT()` yoktur.** Alternatif: `Power(Data, 0.5)`. |
| `MAX(Data1, Data2)` | İki değerin büyüğü. `If(x>0,x,0)` yerine `MAX(x,0)` kullanılabilir. |
| `AND` / `OR` | Mantıksal operatörler. **`&&` ve `||` yoktur.** |
| `W` | Yerleşik veri serisi (AOF), periyoda göre gelir. |

**Sayısal davranış** X30YVADE 5dk / 7.516 bar üzerinde ölçüldü (bkz. bant bölümü): ham `E[x²]-E[x]²` formu float32'de kullanılamaz, gün başına kaydırılmış form kullanılmalıdır.

**Çalıştırma** ise yapılmadı — bu ortamda Prime yok. Kalan belirsizlikler:

- Kaydırılmış formda bile float32'de %0,26 artık hata var. Mekanizma yok olmadı, küçüldü; biriken toplam grafik uzadıkça büyüdüğü için hata bar sayısıyla ölçeklenir. Çok daha uzun geçmişte yeniden ölçün.
- İlk gün `ValueWhen` henüz bir oluşum bulamaz. Filtrede `GSAY >= 2` ile eleniyor; doğrudan indikatör olarak çizerken ilk günü dikkate almayın.
- `TLVOL ≈ W*V` eşitliği tanımdan bekleniyor ama ölçülmedi.
- Taranan sembollerde `W` verisinin dolu geldiği gözle doğrulanmalı.

Kaynaklar: [Matriks Destek — VWAP indikatörü](https://destek.matriksdata.com/?qa=15741/vwap-indikatoru), [Matriks Destek — W (ağırlıklı ortalama fiyat) ile işlem yapma](https://destek.matriksdata.com/?qa=10350/wagirlikli-ortalama-fiyat-ile-islem-yapma)
