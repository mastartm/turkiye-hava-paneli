namespace HavaPaneli.Domain;

/// <summary>
/// Uygulamanın bildiği şehirler: Türkiye'nin 81 ili (il merkezi) ve KKTC'nin 6 ilçesi.
/// Koordinatlar yaklaşık şehir merkezidir; hava verisi bu noktalar için sorgulanır.
/// Liste kodda sabit tutuldu: değişmeyen referans veridir, veritabanı tablosu gereksiz karmaşıklık olurdu.
/// </summary>
public static class CityCatalog
{
    private static City T(string name, double lat, double lon) => new(name, Country.Turkiye, lat, lon);
    private static City K(string name, double lat, double lon) => new(name, Country.Kktc, lat, lon);

    public static IReadOnlyList<City> All { get; } =
    [
        T("Adana", 37.00, 35.32), T("Adıyaman", 37.76, 38.28), T("Afyonkarahisar", 38.76, 30.54),
        T("Ağrı", 39.72, 43.05), T("Aksaray", 38.37, 34.03), T("Amasya", 40.65, 35.83),
        T("Ankara", 39.93, 32.86), T("Antalya", 36.90, 30.70), T("Ardahan", 41.11, 42.70),
        T("Artvin", 41.18, 41.82), T("Aydın", 37.85, 27.84), T("Balıkesir", 39.65, 27.89),
        T("Bartın", 41.64, 32.34), T("Batman", 37.89, 41.13), T("Bayburt", 40.26, 40.23),
        T("Bilecik", 40.14, 29.98), T("Bingöl", 38.88, 40.50), T("Bitlis", 38.40, 42.11),
        T("Bolu", 40.74, 31.61), T("Burdur", 37.72, 30.29), T("Bursa", 40.19, 29.06),
        T("Çanakkale", 40.15, 26.41), T("Çankırı", 40.60, 33.62), T("Çorum", 40.55, 34.95),
        T("Denizli", 37.78, 29.09), T("Diyarbakır", 37.91, 40.24), T("Düzce", 40.84, 31.16),
        T("Edirne", 41.68, 26.56), T("Elazığ", 38.67, 39.22), T("Erzincan", 39.75, 39.49),
        T("Erzurum", 39.90, 41.27), T("Eskişehir", 39.78, 30.52), T("Gaziantep", 37.07, 37.38),
        T("Giresun", 40.91, 38.39), T("Gümüşhane", 40.46, 39.48), T("Hakkâri", 37.58, 43.74),
        T("Hatay", 36.20, 36.16), T("Iğdır", 39.92, 44.05), T("Isparta", 37.76, 30.55),
        T("İstanbul", 41.01, 28.98), T("İzmir", 38.42, 27.14), T("Kahramanmaraş", 37.58, 36.94),
        T("Karabük", 41.20, 32.63), T("Karaman", 37.18, 33.22), T("Kars", 40.60, 43.10),
        T("Kastamonu", 41.38, 33.78), T("Kayseri", 38.73, 35.49), T("Kırıkkale", 39.85, 33.52),
        T("Kırklareli", 41.74, 27.23), T("Kırşehir", 39.15, 34.17), T("Kilis", 36.72, 37.12),
        T("Kocaeli", 40.77, 29.94), T("Konya", 37.87, 32.48), T("Kütahya", 39.42, 29.98),
        T("Malatya", 38.35, 38.31), T("Manisa", 38.61, 27.43), T("Mardin", 37.31, 40.74),
        T("Mersin", 36.81, 34.64), T("Muğla", 37.22, 28.36), T("Muş", 38.74, 41.49),
        T("Nevşehir", 38.62, 34.71), T("Niğde", 37.97, 34.68), T("Ordu", 40.98, 37.88),
        T("Osmaniye", 37.07, 36.25), T("Rize", 41.02, 40.52), T("Sakarya", 40.78, 30.40),
        T("Samsun", 41.29, 36.33), T("Siirt", 37.93, 41.94), T("Sinop", 42.03, 35.15),
        T("Sivas", 39.75, 37.02), T("Şanlıurfa", 37.16, 38.80), T("Şırnak", 37.52, 42.46),
        T("Tekirdağ", 40.98, 27.51), T("Tokat", 40.31, 36.55), T("Trabzon", 41.00, 39.72),
        T("Tunceli", 39.11, 39.55), T("Uşak", 38.68, 29.41), T("Van", 38.49, 43.38),
        T("Yalova", 40.65, 29.27), T("Yozgat", 39.82, 34.81), T("Zonguldak", 41.46, 31.80),

        K("Lefkoşa", 35.19, 33.36), K("Girne", 35.34, 33.32), K("Gazimağusa", 35.12, 33.94),
        K("Güzelyurt", 35.20, 32.99), K("İskele", 35.29, 33.89), K("Lefke", 35.11, 32.85),
    ];
}
