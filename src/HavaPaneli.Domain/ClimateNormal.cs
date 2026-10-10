namespace HavaPaneli.Domain;

/// <summary>Bir günün ortalama sıcaklığı. Değer null olabilir: kaynakta o gün için veri olmayabilir.</summary>
public sealed record DailyMean(DateOnly Date, double? MeanTemperatureC);

/// <summary>
/// Bir şehrin iklim normali: yılın her günü (29 Şubat dahil, 366 konum) için uzun dönem ortalama sıcaklık.
/// Bugünkü sıcaklığın "normalden ne kadar sapma" olduğunu (anomali) hesaplamak için kullanılır.
/// </summary>
public sealed record ClimateNormal
{
    public const int DaysInYear = 366;

    public IReadOnlyList<double> DailyMeansC { get; }

    public ClimateNormal(IReadOnlyList<double> dailyMeansC)
    {
        if (dailyMeansC.Count != DaysInYear)
            throw new ArgumentException($"İklim normali tam {DaysInYear} gün içermeli.", nameof(dailyMeansC));
        if (dailyMeansC.Any(double.IsNaN))
            throw new ArgumentException("İklim normali NaN içeremez.", nameof(dailyMeansC));
        DailyMeansC = dailyMeansC;
    }

    /// <summary>
    /// Gün numarasını her zaman artık yıl olan 2000'e göre alırız. Böylece 1 Mart her yıl aynı konuma düşer
    /// ve 29 Şubat'ın kendi yeri olur; artık olmayan yıllarda konum kaymaz.
    /// </summary>
    public static int DayIndex(DateOnly date) => new DateOnly(2000, date.Month, date.Day).DayOfYear - 1;

    public double NormalFor(DateOnly date) => DailyMeansC[DayIndex(date)];

    /// <summary>Gözlenen (ya da tahmin edilen) sıcaklığın normalden farkı; pozitif = normalden sıcak.</summary>
    public double AnomalyFor(DateOnly date, double observedC) => observedC - NormalFor(date);
}

public static class ClimateNormalCalculator
{
    /// <summary>Her gün için, o günün iki yanındaki bu kadar günü de ortalamaya katarız (gürültüyü azaltmak için).</summary>
    public const int WindowDays = 7;

    /// <exception cref="InvalidOperationException">Bir gün için pencerede hiç veri yoksa.</exception>
    public static ClimateNormal Compute(IEnumerable<DailyMean> history)
    {
        var sums = new double[ClimateNormal.DaysInYear];
        var counts = new int[ClimateNormal.DaysInYear];

        foreach (var d in history)
        {
            if (d.MeanTemperatureC is not { } value) continue; // null'u 0 saymak ortalamayı bozardı
            var i = ClimateNormal.DayIndex(d.Date);
            sums[i] += value;
            counts[i]++;
        }

        var result = new double[ClimateNormal.DaysInYear];
        for (var i = 0; i < ClimateNormal.DaysInYear; i++)
        {
            double sum = 0;
            var count = 0;
            for (var offset = -WindowDays; offset <= WindowDays; offset++)
            {
                // Dairesel pencere: yıl sonu ile yıl başı birbirine komşudur.
                var j = ((i + offset) % ClimateNormal.DaysInYear + ClimateNormal.DaysInYear) % ClimateNormal.DaysInYear;
                sum += sums[j];
                count += counts[j];
            }

            if (count == 0)
                throw new InvalidOperationException("İklim normali için yeterli geçmiş veri yok.");
            result[i] = sum / count;
        }
        return new ClimateNormal(result);
    }
}
