using HavaPaneli.Domain;

namespace HavaPaneli.Domain.Tests;

public class ClimateNormalTests
{
    private static IEnumerable<DailyMean> Years(int from, int to, Func<DateOnly, double?> value)
    {
        for (var d = new DateOnly(from, 1, 1); d <= new DateOnly(to, 12, 31); d = d.AddDays(1))
            yield return new DailyMean(d, value(d));
    }

    [Fact]
    public void Sabit_Sicaklikta_Normal_Ayni_Sabit_Olur()
    {
        var normal = ClimateNormalCalculator.Compute(Years(1991, 2020, _ => 12.5));

        Assert.All(normal.DailyMeansC, v => Assert.Equal(12.5, v, 6));
        Assert.Equal(366, normal.DailyMeansC.Count);
    }

    [Fact]
    public void Gun_Numarasi_Artik_Yil_Olmayinca_Kaymaz()
    {
        // 1 Mart: artık olmayan yılda 60. gün, artık yılda 61. gün olurdu. Bizde hep aynı konum.
        Assert.Equal(ClimateNormal.DayIndex(new DateOnly(2021, 3, 1)), ClimateNormal.DayIndex(new DateOnly(2020, 3, 1)));
        Assert.Equal(59, ClimateNormal.DayIndex(new DateOnly(2020, 2, 29)));
        Assert.Equal(365, ClimateNormal.DayIndex(new DateOnly(2021, 12, 31)));
        Assert.Equal(0, ClimateNormal.DayIndex(new DateOnly(2021, 1, 1)));
    }

    [Fact]
    public void Mevsimsel_Desen_Korunur_Yazi_Kistan_Sicak_Bulur()
    {
        // Basit mevsim dalgası: temmuz ortası sıcak, ocak ortası soğuk.
        double Wave(DateOnly d) => 15 - 12 * Math.Cos(2 * Math.PI * (d.DayOfYear - 15) / 365.0);
        var normal = ClimateNormalCalculator.Compute(Years(1991, 2020, d => Wave(d)));

        Assert.True(normal.NormalFor(new DateOnly(2021, 7, 15)) > 25);
        Assert.True(normal.NormalFor(new DateOnly(2021, 1, 15)) < 5);
    }

    [Fact]
    public void Null_Gunler_Ortalamaya_Katilmaz()
    {
        // Yarısı null olsa da kalanların ortalaması 10 olduğu için normal 10 çıkmalı (0 sayılsaydı ~5 olurdu).
        var normal = ClimateNormalCalculator.Compute(Years(1991, 2020, d => d.Year % 2 == 0 ? null : 10.0));

        Assert.All(normal.DailyMeansC, v => Assert.Equal(10.0, v, 6));
    }

    [Fact]
    public void Pencere_Yil_Sonu_Ile_Basini_Birlestirir()
    {
        // Her gün 0 derece, sadece 25 Aralık 100 derece. 1 Ocak'ın penceresi (±7 gün) 25 Aralık'ı içerir
        // (yıl sınırını aşarak); 9 Ocak'ın penceresi içermez.
        var normal = ClimateNormalCalculator.Compute(
            Years(1991, 2020, d => d.Month == 12 && d.Day == 25 ? 100.0 : 0.0));

        // 1 Ocak penceresi 15 gün x 30 yıl = 450 örnek, bunların 30'u 100 derece: 3000 / 450.
        Assert.Equal(3000.0 / 450.0, normal.NormalFor(new DateOnly(2021, 1, 1)), 6);
        Assert.Equal(0.0, normal.NormalFor(new DateOnly(2021, 1, 9)), 6);
    }

    [Fact]
    public void Yeterli_Veri_Yoksa_Anlasilir_Hata_Verir()
    {
        var onlyOneDay = new[] { new DailyMean(new DateOnly(2020, 6, 1), 20.0) };

        Assert.Throws<InvalidOperationException>(() => ClimateNormalCalculator.Compute(onlyOneDay));
    }

    [Fact]
    public void Anomali_Pozitif_Normalden_Sicak()
    {
        var normal = ClimateNormalCalculator.Compute(Years(1991, 2020, _ => 10.0));

        Assert.Equal(4.5, normal.AnomalyFor(new DateOnly(2026, 10, 10), 14.5), 6);
        Assert.Equal(-3, normal.AnomalyFor(new DateOnly(2026, 10, 10), 7.0), 6);
    }

    [Fact]
    public void Yanlis_Uzunlukta_Normal_Reddedilir()
    {
        Assert.Throws<ArgumentException>(() => new ClimateNormal(new double[365]));
    }

    [Fact]
    public void NaN_Iceren_Normal_Reddedilir()
    {
        var values = Enumerable.Repeat(1.0, 366).ToArray();
        values[10] = double.NaN;

        Assert.Throws<ArgumentException>(() => new ClimateNormal(values));
    }
}
