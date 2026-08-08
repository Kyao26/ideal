# Proje Bağlamı

## Hedef platform: Matriks Prime

Bu repoda üretilen **yeni** indikatör, sistem ve doküman içeriği **Matriks Prime**
formül dili içindir. iDeal ve Matriks IQ hedef değildir — örnek, formül ve
açıklamalar Prime'a göre yazılır.

Matriks Prime, MetaStock türevi bir formül dilidir:

- Atama `:=` ile yapılır, satırlar `;` ile biter, son satır çıktıdır.
- Yorum satırı `{ ... }` içine yazılır.
- Veri serisi kısaltmaları: `O` `H` `L` `C` `W` `V` `TLVOL`
  - **`W` = Ağırlıklı Ortalama Fiyat (AOF)** — borsanın o bar için yayımladığı
    gerçek hacim ağırlıklı fiyat. `(H+L+C)/3` gibi bir yaklaşım **değildir**;
    VWAP türü hesaplarda `W` tercih edilir.
  - `V` = lot cinsinden hacim, `TLVOL` = TL cinsinden işlem hacmi (ciro).
- Sık kullanılan fonksiyonlar: `CUM()` `REF()` `VALUEWHEN()` `STDEV()` `MOV()`
  `SQRT()` `CROSS()` `IF()` `HHV()` `LLV()` `DAYOFMONTH()` `MONTH()` `YEAR()`
- `CUM()` grafiğin başından itibaren birikir ve **seansta sıfırlanmaz**. Seans
  bazlı kümülatif hesaplar, gün başındaki birikimi `VALUEWHEN` + `REF` ile
  yakalayıp çıkararak kurulur.

## Repodaki eski dosyalar

Kökteki `.001` dosyaları eski **iDeal** script'leridir (binary .NET serileştirme,
içleri C#: `Sistem.BollingerUp(...)`, `Sistem.GrafikFiyatSec(...)`). Tarihsel
içeriktir; yeni iş için idiom kaynağı olarak kullanılmaz.

## Doküman dili

Dokümanlar Türkçe yazılır.

## Doğrulanmamış kod işaretlenir

Bu ortamda Matriks Prime çalıştırılamıyor. Test edilmemiş formüller doküman
içinde açıkça "doğrulanmadı" notuyla işaretlenir; çalışıyormuş gibi sunulmaz.
