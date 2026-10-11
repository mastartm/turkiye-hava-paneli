using Npgsql;

namespace HavaPaneli.Infrastructure;

/// <summary>
/// Barındırma servisleri (Neon, Render, Heroku...) bağlantıyı "postgresql://kullanici:parola@host/veritabani?sslmode=require"
/// biçiminde verir; Npgsql ise "Host=...;Database=..." biçimini okur. Bu sınıf ilkini ikinciye çevirir, böylece verilen
/// dize olduğu gibi yapıştırılabilir (elle çeviri hataya açık ve sıkıcıydı). Zaten anahtar=değer biçimindeyse dokunmaz.
/// </summary>
public static class ConnectionStringNormalizer
{
    /// <exception cref="FormatException">
    /// URI biçimi okunamazsa ya da tanınmayan bir sslmode verilirse. Mesaj parolayı/dizeyi İÇERMEZ (loglara sızmasın).
    /// </exception>
    public static string Normalize(string connectionString)
    {
        // Yapıştırırken sona/başa boşluk ya da satır sonu eklenebilir.
        var text = connectionString.Trim();
        if (!text.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            && !text.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            throw new FormatException(
                "Bağlantı dizesi 'postgresql://kullanici:parola@host/veritabani' biçiminde okunamadı (güvenlik için değeri yazdırmıyoruz).");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        };

        // Kullanıcı bilgisi yüzde-kodlu gelir ("p%40ss" = "p@ss"); parolada ';' '=' gibi karakterler olsa bile
        // Npgsql'in kendi oluşturucusu doğru tırnaklar.
        var info = uri.UserInfo;
        if (info.Length > 0)
        {
            var colon = info.IndexOf(':');
            builder.Username = Uri.UnescapeDataString(colon < 0 ? info : info[..colon]);
            if (colon >= 0) builder.Password = Uri.UnescapeDataString(info[(colon + 1)..]);
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = Uri.UnescapeDataString(eq < 0 ? pair : pair[..eq]);
            var value = eq < 0 ? "" : Uri.UnescapeDataString(pair[(eq + 1)..]);

            // Yalnızca sslmode'u anlıyoruz; diğer anahtarlar (channel_binding vb.) yok sayılır.
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                builder.SslMode = ParseSslMode(value);
        }

        return builder.ConnectionString;
    }

    // Tanınmayan değeri sessizce varsayılana düşürmek güvenliği zayıflatırdı (şifresiz bağlantı): bunun yerine hata ver.
    private static SslMode ParseSslMode(string value) => value.ToLowerInvariant() switch
    {
        "disable" => SslMode.Disable,
        "allow" => SslMode.Allow,
        "prefer" => SslMode.Prefer,
        "require" => SslMode.Require,
        "verify-ca" => SslMode.VerifyCA,
        "verify-full" => SslMode.VerifyFull,
        _ => throw new FormatException("Bağlantı dizesindeki 'sslmode' değeri tanınmadı (beklenen: disable, allow, prefer, require, verify-ca, verify-full)."),
    };
}
