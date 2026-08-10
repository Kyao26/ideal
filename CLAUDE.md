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

## Aktif iş: VWAP kapısı ve ST_ADP_RMI sistemi

Bu repodaki güncel iş bu üç dosyadır. Yeni bir seansta VWAP ya da sistem
konusu açılırsa **önce bunları oku**, sıfırdan türetme:

| Dosya | İçerik |
|---|---|
| `ST_ADP_RMI_VWAP_Sistem.txt` | Canlı sistem. Dört kutu (ALIŞ/SATIŞ/AÇIĞA SATIŞ/POZ.KAPAMA), tasarım kararları, System Tester ayarları, inceleme notları. |
| `VWAP_Filtre.txt` | Tek başına Explorer/tarama filtresi + alternatif tarama koşulları. |
| `VWAP.md` | VWAP kavramsal dokümanı, bant varyantları, Prime formülleri. |

Hedef enstrüman **X30YVADE 5dk**. Ölçümler bu veri üzerinde yapıldı.

### Pahalıya öğrenilen kurallar — bozmayın

**1. Varyans gün başına kaydırılarak hesaplanır.** Ham `E[x²]-E[x]²` formu
katastrofik iptale girer. X30YVADE 5dk / 7.516 barda ölçüldü: float32'de
ortalama %85 hata, barların %17-21'inde `sap=0`. Kaydırılmış formda %0,26 ve
%0. Kaydırma tabanı `KAY := ValueWhen(1,YGUN,Ref(C,-1))` — gün içinde sabit
olduğu için cebir birebir korunur. `V` payda ve paydada sadeleşir, yani
**düşük hacimli sembollerde de aynı düzeltme gerekir**.

**2. Dört kutunun eşleşme kuralı.** ALIŞ ile POZ.KAPAMA `Cross(C,ST)`,
SATIŞ ile AÇIĞA SATIŞ `Cross(ST,C)` paylaşır. 1↔4 veya 2↔3 tutmuyorsa hata
vardır. Bu hata bir kez gerçekten yapıldı: POZ.KAPAMA `Cross(ST,C)` yazılmıştı,
short'lar ralli boyunca açık kalıyordu.

**3. Çıkış kutularına TF ve VWAP kapısı EKLENMEZ.** Kapı çıkışa da eklenirse
`LFIL=0` iken hem giriş hem çıkış kapanır, pozisyon süresiz kilitlenir.
Seans sonu kapama (`OR T>=1080`) istisnadır: o çıkışı kısıtlamaz, genişletir.

**4. TF üst sınırı < seans sonu kapama eşiği olmalı.** Aksi halde aynı barda
hem giriş hem çıkış üretilir. Ayrıca sınır periyoda bağlıdır — bir periyot için
doğru sabit başka periyotta sessizce yanlıştır.

**5. Kıyas koşusunda yalnızca VWAP koşulu düşürülür:** `LFIL:=SOK`
(test: `SOK AND C>vwap`). TF, `SOK` ve seans sonu kapama iki koşuda da aynı
kalmalı; biri değişirse ölçülen fark kapının etkisi olmaz.

**6. Kapısız kıyas bitmeden yeni aday açılmaz.** Bant mesafesi, Z-skoru ve
VWAP eğimi hazır ama kapalı. Aynı anda birden fazlası açılırsa hangisinin ne
yaptığı ayırt edilemez.

**7. Optimizasyon kaydırılmış formda yapılır.** Ham formda optimize edilen
eşik float32 gürültüsüne fit eder ve başka sembolde anlamsız çıkar.

## Repodaki eski dosyalar

Kökteki `.001` dosyaları eski **iDeal** script'leridir (binary .NET serileştirme,
içleri C#: `Sistem.BollingerUp(...)`, `Sistem.GrafikFiyatSec(...)`). Tarihsel
içeriktir; yeni iş için idiom kaynağı olarak kullanılmaz.

## Doküman dili

Dokümanlar Türkçe yazılır.

## Doğrulanmamış kod işaretlenir

Bu ortamda Matriks Prime çalıştırılamıyor. Test edilmemiş formüller doküman
içinde açıkça "doğrulanmadı" notuyla işaretlenir; çalışıyormuş gibi sunulmaz.
