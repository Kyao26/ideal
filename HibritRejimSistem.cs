            // ==========================================================
            //  HibritRejimSistem
            //  Trend Takip + Mean Reversion tek sistemde.
            //  Rejim olcumu : Kaufman Efficiency Ratio (ER)
            //  Esik         : sabit sayi degil, ER'in kendi tarihsel
            //                 dagilimindaki yuzdelik sirasi (adaptif)
            //  Stabilizasyon: histerezis (cift esik) + minimum kalma suresi
            //  Panelde rejim bazli K/Z ayristirmasi ve rejim degisim
            //  sayisi yazdirilir; filtrenin ise yarayip yaramadigi
            //  ancak bu iki sayiyla olculur.
            // ==========================================================

            var V = Sistem.GrafikVerileri;
            var C = Sistem.GrafikFiyatSec("Kapanis");
            var H = Sistem.GrafikFiyatSec("Yuksek");
            var L = Sistem.GrafikFiyatSec("Dusuk");

            // ---------------- Parametreler ----------------
            var ERPeriyot     = 20;     // Efficiency Ratio penceresi
            var RankPeriyot   = 250;    // Esigin uyarlandigi tarihsel pencere
            var TrendGirisPct = 0.70;   // ER yuzdeligi bunun ustundeyse TREND
            var RangeDonusPct = 0.45;   // ER yuzdeligi bunun altindaysa YATAY
            var MinRejimBar   = 10;     // Rejimde minimum kalma suresi (bar)

            var MAKisa        = 20;     // Trend modu hizli ortalama
            var MAUzun        = 50;     // Trend modu yavas ortalama
            var BBPeriyot     = 20;     // MR modu bant periyodu
            var BBKatsayi     = 2.0;    // MR modu bant katsayisi
            var ATRPeriyot    = 14;
            var TrendStopATR  = 3.0;    // Trend modu izleyen stop (ATR kati)
            var MRStopATR     = 1.5;    // MR modu sert stop (ATR kati)
            var MinBantATR    = 2.0;    // Bant bu kadar ATR'den darsa MR calismaz

            // ---------------- Gostergeler ----------------
            var MAF = Sistem.MA(C, "Exp", MAKisa);
            var MAS = Sistem.MA(C, "Exp", MAUzun);
            var BBU = Sistem.BollingerUp(C, "Simple", BBPeriyot, BBKatsayi);
            var BBD = Sistem.BollingerDown(C, "Simple", BBPeriyot, BBKatsayi);
            var BBM = Sistem.MA(C, "Simple", BBPeriyot);
            var ATR = Sistem.AverageTrueRange(ATRPeriyot);

            // ---------- 1) Rejim olcumu : Efficiency Ratio ----------
            // ER = |net yol| / |toplam yol| . 1'e yakin = temiz trend,
            // 0'a yakin = ayni yerde savrulan yatay piyasa.
            var ER = Sistem.Liste(0);
            for (int i = ERPeriyot; i < V.Count; i++)
            {
                double yon = Math.Abs(C[i] - C[i - ERPeriyot]);
                double gurultu = 0;
                for (int k = 0; k < ERPeriyot; k++) gurultu += Math.Abs(C[i - k] - C[i - k - 1]);
                ER[i] = gurultu == 0 ? 0 : (float)(yon / gurultu);
            }

            // ---------- 2) Adaptif esik : ER'in yuzdelik sirasi ----------
            // "ADX > 25" gibi sabit bir sayi her enstrumanda / her periyotta
            // ayni anlama gelmez. Burada esik, enstrumanin kendi son
            // RankPeriyot barlik ER dagilimina gore belirlenir.
            var ERRank = Sistem.Liste(0);
            for (int i = ERPeriyot + RankPeriyot; i < V.Count; i++)
            {
                int kucuk = 0;
                for (int k = 1; k <= RankPeriyot; k++) if (ER[i - k] <= ER[i]) kucuk++;
                ERRank[i] = (float)kucuk / RankPeriyot;
            }

            // ---------- 3) Rejim : histerezis + minimum kalma suresi ----------
            // Tek esik kullanilirsa esigin dibinde rejim her barda yanip soner.
            // Girise ve cikisa iki ayri esik + minimum bekleme sarti konur.
            var Rejim = Sistem.Liste(0);   // 1 = TREND , 0 = YATAY
            int aktifRejim = 0;
            int rejimBar = 0;
            for (int i = 0; i < V.Count; i++)
            {
                rejimBar++;
                if (rejimBar >= MinRejimBar)
                {
                    if (aktifRejim == 0 && ERRank[i] >= TrendGirisPct) { aktifRejim = 1; rejimBar = 0; }
                    else if (aktifRejim == 1 && ERRank[i] <= RangeDonusPct) { aktifRejim = 0; rejimBar = 0; }
                }
                Rejim[i] = aktifRejim;
            }

            // ---------- 4) Sinyal uretimi ----------
            var Sinyal = "";
            var SonYon = "";
            double girisFiyat = 0;
            double izleyenStop = 0;
            int poz = 0;          //  1 = long , -1 = short , 0 = flat
            int pozRejim = 0;     // pozisyonun acildigi rejim

            for (int i = 0; i < V.Count; i++) Sistem.Yon[i] = "";

            int basla = ERPeriyot + RankPeriyot + MAUzun;
            if (basla < 2) basla = 2;

            for (int i = basla; i < V.Count; i++)
            {
                Sinyal = "";
                var bantGenislik = BBU[i] - BBD[i];
                var mrUygun = ATR[i] > 0 && bantGenislik >= MinBantATR * ATR[i];

                // --- Acik pozisyonun cikis kontrolu (rejimden bagimsiz) ---
                if (poz == 1)
                {
                    if (pozRejim == 1)
                    {
                        // Trend pozisyonu: izleyen stop, trend bitene kadar tasinir
                        izleyenStop = Math.Max(izleyenStop, C[i] - TrendStopATR * ATR[i]);
                        if (C[i] <= izleyenStop || MAF[i] < MAS[i]) Sinyal = "F";
                    }
                    else
                    {
                        // MR pozisyonu: hedef orta bant, sert ATR stop
                        if (C[i] >= BBM[i]) Sinyal = "F";
                        else if (C[i] <= girisFiyat - MRStopATR * ATR[i]) Sinyal = "F";
                        // Rejim trende dondu: MR pozisyonu tasima, gecis riski burada
                        else if (Rejim[i] == 1) Sinyal = "F";
                    }
                }
                else if (poz == -1)
                {
                    if (pozRejim == 1)
                    {
                        izleyenStop = Math.Min(izleyenStop, C[i] + TrendStopATR * ATR[i]);
                        if (C[i] >= izleyenStop || MAF[i] > MAS[i]) Sinyal = "F";
                    }
                    else
                    {
                        if (C[i] <= BBM[i]) Sinyal = "F";
                        else if (C[i] >= girisFiyat + MRStopATR * ATR[i]) Sinyal = "F";
                        else if (Rejim[i] == 1) Sinyal = "F";
                    }
                }

                // --- Pozisyon yoksa yeni giris ---
                if (poz == 0)
                {
                    if (Rejim[i] == 1)
                    {
                        // TREND MODU : ortalama kesisimi, trenin onune gecilmez
                        if (MAF[i] > MAS[i] && MAF[i - 1] <= MAS[i - 1]) Sinyal = "A";
                        if (MAF[i] < MAS[i] && MAF[i - 1] >= MAS[i - 1]) Sinyal = "S";
                    }
                    else if (mrUygun)
                    {
                        // MEAN REVERSION MODU : bant disindan iceri donus
                        // (bant disinda beklemek degil, geri girisi teyit etmek)
                        if (C[i - 1] < BBD[i - 1] && C[i] > BBD[i]) Sinyal = "A";
                        if (C[i - 1] > BBU[i - 1] && C[i] < BBU[i]) Sinyal = "S";
                    }
                }

                if (Sinyal != "" && Sinyal != SonYon)
                {
                    SonYon = Sinyal;
                    Sistem.Yon[i] = Sinyal;
                    if (Sinyal == "A")
                    {
                        poz = 1; girisFiyat = C[i]; pozRejim = (int)Rejim[i];
                        izleyenStop = C[i] - TrendStopATR * ATR[i];
                    }
                    else if (Sinyal == "S")
                    {
                        poz = -1; girisFiyat = C[i]; pozRejim = (int)Rejim[i];
                        izleyenStop = C[i] + TrendStopATR * ATR[i];
                    }
                    else
                    {
                        poz = 0; girisFiyat = 0; izleyenStop = 0; pozRejim = 0;
                    }
                }
            }

            // ---------- 5) Gorsel : rejim serisi ----------
            var RenkListesi = new List<Color>();
            for (int i = 0; i < V.Count; i++)
                RenkListesi.Add(Rejim[i] == 1 ? Color.DodgerBlue : Color.Orange);
            Sistem.Cizgiler[0].Deger = ERRank;
            Sistem.Cizgiler[0].RenkListesi = RenkListesi;
            Sistem.Cizgiler[1].Deger = Sistem.Liste(TrendGirisPct);

            // ---------- 6) Getiri ve rejim bazli ayristirma ----------
            var cizgino = 2;
            var panel = 3;
            Sistem.GetiriHesapla("01/01/2000", 0);
            Sistem.GetiriMaxDDHesapla("01/01/2000", "01/01/2200");
            Sistem.Cizgiler[cizgino].Deger = Sistem.GetiriKZ;

            double trendKZ = 0, rangeKZ = 0;
            int trendBar = 0, rangeBar = 0, rejimDegisim = 0;
            for (int i = 1; i < V.Count; i++)
            {
                var d = Sistem.GetiriKZ[i] - Sistem.GetiriKZ[i - 1];
                if (Rejim[i] == 1) { trendKZ += d; trendBar++; } else { rangeKZ += d; rangeBar++; }
                if (Rejim[i] != Rejim[i - 1]) rejimDegisim++;
            }
            var toplamBar = trendBar + rangeBar;
            var trendOran = toplamBar == 0 ? 0 : 100.0 * trendBar / toplamBar;
            var ortKalis = rejimDegisim == 0 ? toplamBar : (double)toplamBar / rejimDegisim;

            var Renk1a = Sistem.Renk(150, 255, 255, 0);
            var Renk2a = Sistem.Renk(255, 0, 255, 0);

            Sistem.GradientYaziEkle("HIBRIT REJIM SISTEMI", panel, 10, 20, Color.Gold, Color.Gold, "Tahoma", 12);
            Sistem.ZeminYazisiEkle("Trend rejimi bar orani   \t= " + "% " + trendOran.ToString("0.0"), panel, 10, 45, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("Yatay rejimi bar orani   \t= " + "% " + (100 - trendOran).ToString("0.0"), panel, 10, 60, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("Rejim degisim sayisi     \t= " + rejimDegisim.ToString("0"), panel, 10, 75, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("Ort. rejim omru (bar)    \t= " + ortKalis.ToString("0.0"), panel, 10, 90, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("TREND modu K/Z           \t= " + trendKZ.ToString("0.000"), panel, 10, 110, Renk2a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("YATAY modu K/Z           \t= " + rangeKZ.ToString("0.000"), panel, 10, 125, Renk2a, "Tahoma", 10);

            Sistem.ZeminYazisiEkle("Toplam Islem Sayisi      \t= " + Sistem.GetiriToplamIslem.ToString("0"), panel, 340, 45, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("Kazandiran Islem         \t= " + Sistem.GetiriKarIslem.ToString("0"), panel, 340, 60, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("Kaybettiren Islem        \t= " + Sistem.GetiriZararIslem.ToString("0"), panel, 340, 75, Renk1a, "Tahoma", 10);
            Sistem.ZeminYazisiEkle("Karli Islem Orani        \t= " + "% " + Sistem.GetiriKarIslemOran.ToString("0.00"), panel, 340, 90, Renk1a, "Tahoma", 10);

            Sistem.ZeminYazisiEkle("Net Kar                  \t= " + Sistem.GetiriNetKar.ToString("0.000"), panel, 640, 45, Renk2a, "Tahoma", 12);
            Sistem.ZeminYazisiEkle("Profit Factor            \t= " + Sistem.ProfitFactor.ToString("0.00"), panel, 640, 65, Renk2a, "Tahoma", 12);
            Sistem.ZeminYazisiEkle("MaxDD                    \t= " + Sistem.GetiriMaxDD.ToString("-0.000"), panel, 640, 85, Renk2a, "Tahoma", 12);
