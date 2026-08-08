# VWAP (Hacim Ağırlıklı Ortalama Fiyat)

Seans boyunca hacme göre ağırlıklandırılmış ortalama fiyatı gösteren, kurumsal bir adil değer referans noktası.

**Seviye:** Orta — formülü basittir, asıl zorluk başlangıç noktası (anchor) seçimindedir.

## Nedir

VWAP (Hacim Ağırlıklı Ortalama Fiyat), bir menkul kıymetin belirli bir başlangıç noktasından bu yana işlem gördüğü ortalama fiyattır; ancak her fiyat seviyesine eşit ağırlık veren basit bir ortalama değil, her seviyede ne kadar hacim işlem gördüğüne göre ağırlıklandırılmış bir ortalamadır.

Kurumlar onu bir uygulama (execution) referansı olarak kullanır: VWAP'a yakın ya da ondan daha iyi bir fiyattan gerçekleştirilen büyük bir emir iyi uygulama sayılır, ondan uzakta gerçekleşen bir emir kötü uygulama gibi görünür. Bunun önemli bir yan etkisi vardır: VWAP yaygın bir kıyas ölçütü olduğu için, gün boyunca onu hedefleyen execution algoritmaları fiyatı kısmen VWAP'a doğru çeker. Yani araç bir ölçüde kendi kendini gerçekleştirir — VWAP'ın "işe yaramasının" sebebi bir doğa yasası değil, ona göre ölçülen çok sayıda emir akışıdır.

VWAP kümülatif bir hesaptır: bir başlangıç noktasından itibaren biriken toplamlarla çalışır. Bu yüzden en kritik tasarım kararı, o başlangıç noktasının nerede olduğudur.

## Nasıl Hesaplanır

Her bar için tipik fiyat:

```
TP = (Yüksek + Düşük + Kapanış) / 3
```

Başlangıç noktasından itibaren biriktirilerek:

```
VWAP = Σ(TP × Hacim) / Σ(Hacim)
```

Standart sapma bantları için hacim ağırlıklı varyans kullanılır — ağırlıklandırılmış bir serinin sıradan standart sapması değil:

```
Var  = Σ(Hacim × TP²) / Σ(Hacim) − VWAP²
Bant = VWAP ± k × √Var
```

Burada `k` çarpandır (genellikle 1 ve 2 birlikte çizilir). Birçok platform stdev yerine yüzde tabanlı bant seçeneği de sunar: `Bant = VWAP × (1 ± %k)`. Bu ikisi farklı şeyler ölçer — stdev bandı o seansın gerçek dağılımına uyum sağlar, yüzde bandı sabit genişliktedir.

### Bollinger Bantlarıyla farkı

Benzetme yardımcıdır ama yarıda bırakılırsa yanıltır. Bollinger bantları sabit N barlık kayan bir pencerede hesaplanır; bu yüzden daralıp genişlerler ve daralma ("squeeze") volatilite sıkışması olarak okunur.

VWAP bantları ise seans başından itibaren kümülatif hesaplanır. Örneklem büyüdükçe her yeni barın toplam dağılıma etkisi azalır, dolayısıyla bantların **tepkiselliği seans ilerledikçe söner**. Seansın sonunda bantların hareketsizleşmesi bir volatilite sinyali değil, sadece paydanın büyümüş olmasıdır. Bollinger refleksini buraya taşımayın.

## Traderlar Nasıl Kullanır

Gün içi traderlar seans VWAP'ını günün adil değer eksen noktası olarak ele alır. Mekanizma şu: fiyat VWAP'ın üzerindeyse, o gün alım yapanların ortalaması kârdadır; altındaysa zarardadır. VWAP'ın tekrar geçilmesi bu yüzden gün içi kontrolde bir değişim olarak izlenir.

Stdev bantları fiyatın adil değerden ne kadar uzaklaştığını gösterir. 2-stdev bandına doğru bir itiş, aşırı alım/aşırı satım uç noktasına benzer okunur; ortalamaya dönüş scalp'leri için ya da VWAP'a yakın başlayan bir hareketin hedefi olarak kullanışlıdır.

Seans VWAP'ının yanında sık kullanılan diğer seviyeler: **önceki günün VWAP'ı** (bugünün açılışının dünün adil değerine göre nerede durduğunu gösterir), haftalık ve aylık VWAP.

## Yaygın Ayarlar ve Varyasyonlar

En önemli ayar başlangıç noktasıdır. Üç ayrı yapı vardır ve bunlar birbirinin eş anlamlısı **değildir**:

**Seans VWAP'ı.** Her işlem gününde açılışta sıfırlanır. Standart kurumsal referans budur.

**Sabitlenmiş VWAP (Anchored VWAP).** Trader başlangıç noktasını kendi seçer — bir swing dip, bir kazanç açıklama tarihi, bir haftanın başlangıcı — ve günlük sıfırlama olmadan o noktadan itibaren kümülatif hesaplanır. Ölçtüğü şey: *o olaydan bu yana işlem yapan herkesin ortalama maliyet tabanı*. Gün içi olmak zorunda değildir; günlük ve haftalık grafiklerde de anlamlıdır.

**Kayan VWAP (Rolling VWAP).** Sabit başlangıç noktası yoktur; son N bar üzerinde kayan pencereyle hesaplanır (örneğin 200 barlık rolling VWAP). Pencere sürekli ileri kayar, dolayısıyla "bir olaydan bu yana maliyet tabanı" sorusunu **yanıtlamaz** — hacim ağırlıklı bir hareketli ortalama gibi davranır.

Anchor seçimi metodolojisi basittir: hangi soruyu sorduğunuza karar verin. "Bugün alanlar nerede?" → seans VWAP'ı. "Bilanço sonrası girenler nerede?" → o tarihe sabitlenmiş AVWAP. "Son N barın hacim ağırlıklı eğilimi ne?" → rolling VWAP.

İkinci kaldıraç bant çarpanıdır: 1 stdev daha dar ve sık test edilen bir bant verir, 2 stdev fiyatın daha seyrek ulaştığı ama ulaştığında daha büyük bir ortalamaya dönüş potansiyeli sunan bir uç nokta verir.

## Tuzaklar ve Yanlış Anlamalar

**"Seans VWAP'ı" ile "VWAP"ı karıştırmayın.** Gün içi kısıtı VWAP'ın kendisine değil, günlük sıfırlama mekanizmasına aittir. Seans VWAP'ı günlük grafikte anlamsızdır — sıfırlanacak bir seans açılışı yoktur ve kümülatif toplamları seans sınırının ötesine taşıyan bir uygulama, giderek düzleşen anlamsız bir çizgi üretir. Buna karşılık sabitlenmiş VWAP günlük ve haftalık grafiklerde gayet anlamlıdır; profesyonel kullanımın büyük kısmı da oradadır.

**Seansın tanımı sabit değil, bir parametredir.** Vadeli işlemlerde hesabın normal seansla (RTH) mı yoksa uzatılmış seansla (ETH) mı başlatıldığı VWAP'ı tamamen değiştirir. Kriptoda "seans" zaten keyfîdir (çoğunlukla 00:00 UTC). Grafikteki çizgiye güvenmeden önce hangi açılışa göre sıfırlandığını bilin.

**Güvenilir hacim şart.** Spot FX'te merkezi bir hacim verisi yoktur; platformlar tick volume kullanır. Orada VWAP büyük ölçüde bir yaklaşımdır, kurumsal anlamda "hacim ağırlıklı ortalama fiyat" değildir.

**İki uçtaki atalet sorunu.** Seansın ilk dakikalarında payda küçüktür; VWAP gürültülüdür ve zıplar. Seansın sonunda payda büyümüştür; VWAP neredeyse çivilenmiştir. Bu aynı mekaniğin iki yüzüdür ve pratik sonucu şudur: erken seans VWAP kesişimi zayıf bilgidir, geç seans kesişimi güçlüdür — çünkü onu üretmek gerçekten büyük hacim gerektirmiştir.

**Yönlü bir gösterge değildir.** VWAP bir adil değer referansıdır. Güçlü bir trend gününde fiyat, hiçbir "dönüş" gerçekleşmeden saatlerce onun bir tarafında kalabilir.

## Grafikte Deneyin

1. **5 veya 15 dakikalık** bir AAPL grafiğinde seans VWAP'ını yükleyin. Saatlik kullanmayın: ABD seansı 6.5 saattir, yani saatlik grafikte seans başına yalnızca ~7 bar düşer — kümülatif bir göstergenin davranışını 7 veriyle okuyamazsınız.
2. Standart seans başlangıcını, yakın bir swing dipten sabitlenmiş bir AVWAP ile karşılaştırın; iki çizginin ne kadar farklı davrandığına bakın.
3. Bant çarpanını 1'den 2'ye genişletin ve fiyatın dış banda gerçekte ne kadar daha seyrek dokunduğunu gözlemleyin.
4. Güçlü bir trend günü ile dalgalı bir yatay gün seçip karşılaştırın: trend günleri çizginin bir tarafına yapışır, yatay günler tekrar tekrar üzerinden geçer.
5. Aynı seansı açılıştan kapanışa izleyip bantların tepkiselliğinin nasıl söndüğünü not edin.

## Formül Özeti

```
TP   = (Yüksek + Düşük + Kapanış) / 3
VWAP = Σ(TP × Hacim) / Σ(Hacim)          [başlangıç noktasından itibaren kümülatif]
Var  = Σ(Hacim × TP²) / Σ(Hacim) − VWAP²
Bant = VWAP ± k × √Var
```

Seans VWAP'ında toplamlar her seans açılışında sıfırlanır. Sabitlenmiş VWAP'ta sıfırlanmaz. Kayan VWAP'ta toplamlar N barlık pencere üzerinden alınır.

## Önemli Noktalar

- VWAP hacim ağırlıklı ortalama fiyattır — basit bir hareketli ortalama değildir. Belirli bir başlangıç noktasından itibaren kümülatiftir.
- Gün içi kısıtı VWAP'ın kendisine değil **seans VWAP'ının günlük sıfırlamasına** aittir. Sabitlenmiş VWAP günlük ve haftalık grafiklerde de anlamlıdır.
- Kayan (rolling) VWAP ile sabitlenmiş (anchored) VWAP aynı şey değildir: biri kayan pencere, diğeri sabit başlangıç noktası kullanır ve farklı soruları yanıtlarlar.
- Bantlar hacim ağırlıklı varyanstan hesaplanır. Bollinger'a benzer görünürler ama dinamikleri farklıdır: genişlik değişimi volatiliteden çok örneklem büyümesinden gelir.
- VWAP'ın referans gücü kısmen refleksiftir — onu hedefleyen execution algoritmaları fiyatı ona doğru çeker.
- Erken seansta gürültülü, geç seansta atıl. Kesişimlerin bilgi değeri seans içinde değişir.
- Güvenilir hacim verisi gerektirir; spot FX'te tick volume üzerinden hesaplanır ve yalnızca bir yaklaşımdır.
