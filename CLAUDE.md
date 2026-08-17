# CLAUDE.md

## Depo yapısı

| Konum | Platform | Dil |
|---|---|---|
| Kök dizindeki `.001` dosyaları | iDeal Terminal | C# (binary .NET serialize; kaynak kod dosyanın içine gömülü) |
| `matriks-prime/` | **Matriks Prime** | Klasik (MetaStock benzeri) Matriks formül dili |

Aktif çalışma **Matriks Prime** üzerinde. iDeal tarafına yeni geliştirme yapılmıyor.

---

## Matriks Prime — doğrulanmış sözdizimi

> Aşağıdakilerin hepsi kullanıcının kendi Indicator Builder'ında **test edilerek**
> doğrulandı. Tahmin değil. Yeni bir varsayım eklemeden önce aynı şekilde test ettir.

### Çıktı yazımı (zorunlu kural)

Çizgiler **tek satırda**, `;` ile ayrılır ve satır `;` ile **biter**:

```
vv;tt;ref;
```

Her çıktıyı ayrı satıra yazmak veya son `;`'i atmak doğru yazım değildir.

### Doğrulanmış fonksiyonlar ve davranışlar

| Öğe | Durum | Not |
|---|---|---|
| `Mov(C,2,VAR)` | ✅ çalışıyor | VIDYA/değişken ortalama tipinin adı `VAR`. Periyot **sabit** olmalı, seri verilemez |
| `PREV` | ✅ atandığı değişkenin geçmişine bağlanır | `tt:=If(Cum(1)=1,0,PREV+1);` testi `Cum(1)-1` ile birebir örtüştü. OTT ratchet'i ve sistem koşulları bu sayede kendi kendine yeterli |
| `Cum(1)` | ✅ bar sayacı | |
| `Sqr()` | ✅ var | **`Sqrt` değil `Sqr`.** Karekök mü kare mi olduğu doğrulanmadı (`Sqr(9)` → 3 ise karekök, 81 ise kare). Bu yüzden kodda kullanılmıyor |
| `Log()` | ✅ var | Doğal mı 10 tabanlı mı doğrulanmadı. Kodda yalnızca `Log(x)/Log(2)` oranı içinde kullanılıyor → taban sadeleşir, fark etmez |
| `Ref` `HHV` `LLV` `Sum` `Abs` `If` `Cross` | ✅ kullanımda | |
| `Stdev` `Exp` `Max` `Min` | ❓ test edilmedi | Kodda **kullanılmıyor**; `Sum`+`Abs` ile veya literal sabitle yazıldı |

### Dil kısıtları

1. **Döngü yok.** Argmax/iteratif yöntemler yazılamaz; kapalı-form pencere tahmincileri kullanılır.
2. **Formül başına tek `PREV` zinciri.** Birden fazla bağımsız özyineleme isteyen tasarımlar (ör. online ağırlık güncellemesi) tek formüle sığmaz.
3. **`Mov` periyodu sabit.** Adaptif periyot ancak sabit periyotlu bir ortalama bankası + `If` seçimi ile yapılır.
4. **Matriks IQ'nun dili tamamen farklıdır.** Buradaki hiçbir formül IQ'ya doğrudan taşınamaz.

---

## Çalışma ilkeleri (bu depo için)

- **Doğrulanmamış fonksiyon adı kullanma.** Şüpheliyse `Sum`/`Abs` ile yeniden yaz veya literal sabite çevir.
- **Sabitler ya yapısal ya evrensel olacak.** "Backtest'te bu değer daha iyi çıktı" gerekçesiyle sayı konmaz; parametreler grafiğin ölçülen karakterinden türetilir.
- Doğrulama = **duyarlılık düzlüğü**, en yüksek kâr değil. Test geçmezse tasarım reddedilir, sayı ayarlanmaz.
