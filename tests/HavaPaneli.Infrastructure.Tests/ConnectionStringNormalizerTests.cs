using HavaPaneli.Infrastructure;
using Npgsql;

namespace HavaPaneli.Infrastructure.Tests;

public class ConnectionStringNormalizerTests
{
    // Neon'un verdiği biçime benzeyen SAHTE bir dize (gerçek parola değil).
    private const string NeonLike =
        "postgresql://neondb_owner:abc123XYZ@ep-cool-name-123456.eu-central-1.aws.neon.tech/neondb?sslmode=require";

    private static NpgsqlConnectionStringBuilder Parse(string normalized) => new(normalized);

    [Fact]
    public void Neon_Bicimi_Npgsql_Bicimine_Cevrilir()
    {
        var b = Parse(ConnectionStringNormalizer.Normalize(NeonLike));

        Assert.Equal("ep-cool-name-123456.eu-central-1.aws.neon.tech", b.Host);
        Assert.Equal(5432, b.Port);
        Assert.Equal("neondb", b.Database);
        Assert.Equal("neondb_owner", b.Username);
        Assert.Equal("abc123XYZ", b.Password);
        Assert.Equal(SslMode.Require, b.SslMode);
    }

    [Fact]
    public void Postgres_Semasi_Ve_Buyuk_Harf_Sema_Kabul_Edilir()
    {
        Assert.Equal("db.example.com", Parse(ConnectionStringNormalizer.Normalize("postgres://u:p@db.example.com/x")).Host);
        Assert.Equal("db.example.com", Parse(ConnectionStringNormalizer.Normalize("POSTGRESQL://u:p@db.example.com/x")).Host);
    }

    [Fact]
    public void Acik_Port_Korunur()
    {
        Assert.Equal(6543, Parse(ConnectionStringNormalizer.Normalize("postgresql://u:p@h:6543/x")).Port);
    }

    [Fact]
    public void Yuzde_Kodlu_Ozel_Karakterli_Parola_Aynen_Geri_Okunur()
    {
        // p@ss;w=rd#1 : ';' ve '=' bağlantı dizesi ayraçlarıdır, '@' '#' URI ayraçlarıdır.
        var normalized = ConnectionStringNormalizer.Normalize("postgresql://kullanici:p%40ss%3Bw%3Drd%231@h/x?sslmode=require");

        // Çıktıyı yeniden ayrıştırınca parola bozulmamalı: Npgsql oluşturucusu doğru tırnaklar.
        Assert.Equal("p@ss;w=rd#1", Parse(normalized).Password);
        Assert.Equal("kullanici", Parse(normalized).Username);
    }

    [Theory]
    [InlineData("disable", SslMode.Disable)]
    [InlineData("allow", SslMode.Allow)]
    [InlineData("prefer", SslMode.Prefer)]
    [InlineData("require", SslMode.Require)]
    [InlineData("verify-ca", SslMode.VerifyCA)]
    [InlineData("verify-full", SslMode.VerifyFull)]
    [InlineData("REQUIRE", SslMode.Require)]
    public void Sslmode_Dogru_Eslenir(string value, SslMode expected)
    {
        Assert.Equal(expected, Parse(ConnectionStringNormalizer.Normalize($"postgresql://u:p@h/x?sslmode={value}")).SslMode);
    }

    [Fact]
    public void Taninmayan_Sslmode_Hata_Verir_Sessizce_Zayiflatmaz()
    {
        var ex = Assert.Throws<FormatException>(() => ConnectionStringNormalizer.Normalize("postgresql://u:SIRPAROLA@h/x?sslmode=sifresiz"));

        Assert.DoesNotContain("SIRPAROLA", ex.Message);
    }

    [Fact]
    public void Sslmode_Yoksa_Npgsql_Varsayilani_Kalir()
    {
        Assert.Equal(new NpgsqlConnectionStringBuilder().SslMode, Parse(ConnectionStringNormalizer.Normalize("postgresql://u:p@h/x")).SslMode);
    }

    [Fact]
    public void Diger_Sorgu_Anahtarlari_Yok_Sayilir_Cokmez()
    {
        var b = Parse(ConnectionStringNormalizer.Normalize("postgresql://u:p@h/x?sslmode=require&channel_binding=require&application_name=foo"));

        Assert.Equal(SslMode.Require, b.SslMode);
        Assert.Equal("h", b.Host);
    }

    [Fact]
    public void Anahtar_Deger_Bicimi_Aynen_Doner()
    {
        const string keyword = "Host=localhost;Port=5432;Database=hava;Username=u;Password=p";

        Assert.Same(keyword, ConnectionStringNormalizer.Normalize(keyword));
    }

    [Fact]
    public void Basta_Sonda_Bosluk_Ve_Satir_Sonu_Temizlenir()
    {
        var b = Parse(ConnectionStringNormalizer.Normalize("  \r\n" + NeonLike + "\r\n  "));

        Assert.Equal("neondb", b.Database);
        Assert.Equal("abc123XYZ", b.Password); // parolaya satır sonu yapışmamalı
    }

    // Girdi, sızmaması gereken sahte bir parola işaretçisi (SIRPAROLA) taşır: asıl amaç hata mesajının değeri yazdırmaması.
    [Theory]
    [InlineData("postgresql://u:SIRPAROLA@")]
    [InlineData("postgresql://:SIRPAROLA@/x")]
    [InlineData("postgres://u:SIRPAROLA@:5432/x")]
    public void Okunamayan_Uri_Anlasilir_Hata_Verir_Ve_Parolayi_Yazmaz(string bad)
    {
        var ex = Assert.Throws<FormatException>(() => ConnectionStringNormalizer.Normalize(bad));

        Assert.DoesNotContain("SIRPAROLA", ex.Message);
        Assert.Contains("okunamadı", ex.Message);
    }

    [Theory]
    [InlineData("postgresql://")]
    [InlineData("postgresql:///x")]
    public void Bos_Ya_Da_Hostsuz_Uri_Hata_Verir(string bad)
    {
        Assert.Throws<FormatException>(() => ConnectionStringNormalizer.Normalize(bad));
    }

    [Fact]
    public void Parolasiz_Kullanici_Cokmez()
    {
        var b = Parse(ConnectionStringNormalizer.Normalize("postgresql://sadecekullanici@h/x"));

        Assert.Equal("sadecekullanici", b.Username);
        Assert.Null(b.Password);
    }

    [Fact]
    public void Kullanici_Bilgisi_Yoksa_Cokmez()
    {
        var b = Parse(ConnectionStringNormalizer.Normalize("postgresql://h/x"));

        Assert.Equal("h", b.Host);
        Assert.Equal("x", b.Database);
    }
}
