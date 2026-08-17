# CLAUDE.md

## Depo yapısı

| Konum | Platform | Dil |
|---|---|---|
| Kök dizindeki `.001` dosyaları | iDeal Terminal | C# (binary .NET serialize; kaynak kod dosyanın içine gömülü) |
| `matriks-prime/` | **Matriks Prime** | Klasik (MetaStock benzeri) Matriks formül dili |

Aktif çalışma **Matriks Prime** üzerinde. iDeal tarafına yeni geliştirme yapılmıyor.

---

## Matriks Prime — INDICATOR BUILDER için doğrulanmış sözdizimi

> ⚠️ **Kapsam: yalnızca Indicator Builder.** Aşağıdakiler kullanıcının kendi
> Indicator Builder'ında test edilerek doğrulandı. **System Tester ve Explorer
> için geçerli değildir** — oraların yazım kuralları farklıdır ve henüz
> doğrulanmamıştır. Bir kuralı bir modülden diğerine taşımadan önce test ettir.

### Çıktı yazımı (Indicator Builder — zorunlu kural)

Çizgiler **tek satırda**, `;` ile ayrılır ve satır `;` ile **biter**:

```
vv;tt;ref;
```

Her çıktıyı ayrı satıra yazmak veya son `;`'i atmak doğru yazım değildir.

**Çıktılar seri olmalıdır — çıplak sabit sayı yazılamaz.** Sabit bir referans çizgisi
gerekiyorsa önce seriye çevir:

```
{ YANLIS }   Hrs;0.5;optA;
{ DOGRU  }   Sfr:=Hrs*0+0.5;   Hrs;Sfr;optA;
```

### Doğrulanmış fonksiyonlar ve davranışlar

| Öğe | Durum | Not |
|---|---|---|
| `Mov(C,2,VAR)` | ✅ çalışıyor | VIDYA/değişken ortalama tipinin adı `VAR`. Periyot **sabit** olmalı, seri verilemez |
| `PREV` | ✅ atandığı değişkenin geçmişine bağlanır | `tt:=If(Cum(1)=1,0,PREV+1);` testi `Cum(1)-1` ile birebir örtüştü → OTT ratchet'i Indicator Builder'da çalışır. **System Tester'da aynı davranış varsayılamaz** |
| `Cum(1)` | ✅ bar sayacı | |
| `Sqr()` | ✅ var | **`Sqrt` değil `Sqr`.** Karekök mü kare mi olduğu doğrulanmadı (`Sqr(9)` → 3 ise karekök, 81 ise kare). Bu yüzden kodda kullanılmıyor |
| `Log()` | ✅ var | Doğal mı 10 tabanlı mı doğrulanmadı. Kodda yalnızca `Log(x)/Log(2)` oranı içinde kullanılıyor → taban sadeleşir, fark etmez |
| `Ref` `HHV` `LLV` `Sum` `Abs` `If` `Cross` | ✅ kullanımda | |
| `Stdev` `Exp` `Max` `Min` | ❓ test edilmedi | Kodda **kullanılmıyor**; `Sum`+`Abs` ile veya literal sabitle yazıldı |

### System Tester / Explorer — DOĞRULANMADI

Bu modüllerin yazım kuralları Indicator Builder'dan farklı. Bilinmeyenler:

- Çıktı/koşul satırının nasıl bitirileceği (`;` var mı, tek ifade mi beklenir).
- `PREV`'in koşul içinde neye bağlandığı — Indicator Builder'daki davranış burada
  geçerli olmayabilir. Geçerli değilse ratchet koşul içinde bozulur.
- Kaydedilmiş bir indikatörün System Tester içinden hangi sözdizimiyle çağrıldığı.

**Bu üçü öğrenilene kadar** sistem tarafı için güvenli yol: mantığı Indicator
Builder'da (doğrulanmış ortamda) kurup kaydetmek, System Tester'ın o kayıtlı
indikatörü çağırmasını sağlamak.

### Dil kısıtları

0. **Bir değişkene yalnızca BİR KEZ atama yapılabilir.** (Kullanıcı tarafından hata
   mesajıyla doğrulandı.) "Önce hesapla, sonra sınırla" kalıbı **iki ayrı isim** ister:

   ```
   {  YANLIS - degisken tanimlama hatasi verir }
   Hrs:=2-Log(Rr)/Log(2);
   Hrs:=If(Hrs<0.2,0.2,If(Hrs>0.9,0.9,Hrs));

   {  DOGRU }
   Hr0:=2-Log(Rr)/Log(2);
   Hrs:=If(Hr0<0.2,0.2,If(Hr0>0.9,0.9,Hr0));
   ```

   Bu depoda kullanılan adlandırma: ham değer `X0`, sınırlanmış/nihai değer `X`
   (`Rr0`→`Rr`, `Hr0`→`Hrs`, `op0`→`optA`).

1. **Döngü yok.** Argmax/iteratif yöntemler yazılamaz; kapalı-form pencere tahmincileri kullanılır.
2. **Formül başına tek `PREV` zinciri.** Birden fazla bağımsız özyineleme isteyen tasarımlar (ör. online ağırlık güncellemesi) tek formüle sığmaz.
3. **`Mov` periyodu sabit.** Adaptif periyot ancak sabit periyotlu bir ortalama bankası + `If` seçimi ile yapılır.
4. **Matriks IQ'nun dili tamamen farklıdır.** Buradaki hiçbir formül IQ'ya doğrudan taşınamaz.

---

## Çalışma ilkeleri (bu depo için)

- **Doğrulanmamış fonksiyon adı kullanma.** Şüpheliyse `Sum`/`Abs` ile yeniden yaz veya literal sabite çevir.
- **Sabitler ya yapısal ya evrensel olacak.** "Backtest'te bu değer daha iyi çıktı" gerekçesiyle sayı konmaz; parametreler grafiğin ölçülen karakterinden türetilir.
- Doğrulama = **duyarlılık düzlüğü**, en yüksek kâr değil. Test geçmezse tasarım reddedilir, sayı ayarlanmaz.
