# Proje Bağlamı

## Platform kararı (verildi, yeniden açılmayacak)

Hedef **Matriks Prime**. MatriksIQ ve iDeal değerlendirildi, seçilmedi. Karar
bilinçli; aşağıdaki bilinen takas kabul edilmiş durumda:

- Prime formülleri MatriksIQ'ya doğrudan taşınmaz, dönüştürme gerekir.
- Prime, MetaStock türevi bir DSL kullanır; iDeal ve MatriksIQ AlgoTrader ise C#.

Bu konuyu tekrar gündeme getirme; Prime'a göre üret.

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
  `Sqr()` `Power()` `MAX()` `CROSS()` `If()` `HHV()` `LLV()` `SUM()`
  `DAYOFMONTH()` `MONTH()` `YEAR()`

### Kılavuzdan teyitli söz dizimi tuzakları

- **`SQRT()` YOKTUR.** Karekök `Sqr(Data)`'dır. Alternatif: `Power(Data, 0.5)`.
- **`&&` ve `||` YOKTUR.** Mantıksal operatörler `AND` / `OR`.
- `VALUEWHEN(N, koşul, değer)` — `N=1` **en son** (en yakın geçmiş) oluşumu
  verir, en eskisini değil. Parametre `1` yazılır, `1.` değil; kılavuz eksik/
  fazla parametre ve noktalama konusunda özellikle uyarıyor.
- `CUM(1)` bar sayacıdır — grafiğin başından her bar için 1 ekler.
- `MAX(Data1, Data2)` iki değerin büyüğünü verir; `If(x>0, x, 0)` yerine
  `MAX(x, 0)` aynı işi tek satırda yapar.
- `CUM()` grafiğin başından itibaren birikir ve **seansta sıfırlanmaz**. Seans
  bazlı kümülatif hesaplar, gün başındaki birikimi `VALUEWHEN` + `REF` ile
  yakalayıp çıkararak kurulur.

### Prime referans dokümanı

Kullanım kılavuzu bu ortamda **yok** ve `matriksdata.com` egress allowlist'i
tarafından bloklu. Söz dizimi sorusu çıktığında tahmin etme — kullanıcıdan
ilgili kılavuz bölümünü iste.

## Repodaki eski dosyalar

Kökteki `.001` dosyaları eski **iDeal** script'leridir (binary .NET serileştirme,
içleri C#: `Sistem.BollingerUp(...)`, `Sistem.GrafikFiyatSec(...)`). Tarihsel
içeriktir; yeni iş için idiom kaynağı olarak kullanılmaz.

## Doküman dili

Dokümanlar Türkçe yazılır.

## Doğrulanmamış kod işaretlenir

Bu ortamda Matriks Prime çalıştırılamıyor. Test edilmemiş formüller doküman
içinde açıkça "doğrulanmadı" notuyla işaretlenir; çalışıyormuş gibi sunulmaz.
