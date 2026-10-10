# Mimari Kararlar

Her karar: **ne**, **neden**, **reddedilen alternatif**. Aşamalar ilerledikçe güncellenir.

## Kapsam (v1)
- Türkiye (81 il) ve KKTC (6 ilçe) haritası.
- Katmanlar: hava durumu ve deprem. Yan panelde grafikler ve görseller.
- v2 (sonra): hava kalitesi, deniz/dalga, orman yangını.

## K-001: Katmanlı backend (Domain → Application → Infrastructure → Api)
- **Neden:** İş kuralları dış dünyadan (veritabanı, HTTP) bağımsız test edilir. Mülakatta savunması kolay, önceki projeyle tutarlı.
- **Alternatif:** Tek proje. Küçük işler için hızlı, ama bağımlılıklar karışır ve test zorlaşır.

## K-002: Harita katmanları "sağlayıcı" arayüzüyle eklenir
- **Neden:** Yeni katman (örn. hava kalitesi) mevcut kodu değiştirmeden eklenir. Bu, açık/kapalı prensibidir (genişlemeye açık, değişikliğe kapalı).
- **Alternatif:** Her katman için ayrı if/else. Katman sayısı arttıkça kod karışır.

## K-003: Veritabanı PostgreSQL
- **Neden:** SQL Server'dan farklı bir teknolojiyi göstermek. Docker'da hafif. Ücretsiz, kart istemeyen barındırma seçenekleri var (Neon).
- **Alternatif:** SQL Server. Önceki projede kullanıldı; yeni bir şey göstermezdi.

## K-004: Dış kaynak seçimi
- **Hava durumu:** Open-Meteo. Anahtarsız, ücretsiz, CC BY 4.0 (atıf zorunlu), ticari olmayan kullanım. Portföy projesi ticari olmadığı için uygun.
- **Deprem:** USGS/EMSC (belgeli, resmi). AFAD için resmi kullanım şartı doğrulanana kadar kullanılmaz.
- **Reddedilen:** MGM ve Kandilli. Resmi açık API yok, siteyi kazımak kırılgan ve izin belirsiz.
- **Doğrulanacak:** Open-Meteo verisini kendi veritabanımızda saklamanın şartlara uygunluğu (aşama 1).

## K-005: Canlıya alma planı (kart istemeyen yol)
- Veritabanı: Neon. API: Render (Docker). Arayüz: Cloudflare Pages. Güncel şartlar kayıttan önce kullanıcı tarafından kontrol edilir.
- Açık sorun: Render ücretsiz örneği boşta uyuyor; arka plan veri toplama işi bununla birlikte durur. Çözüm aşama 2'de karara bağlanır (zamanlanmış GitHub Actions işi veya "sunucu uyanıyor" mesajı).

## K-006: Hava durumu sağlayıcısı arayüzü Application'da, Open-Meteo uygulaması Infrastructure'da
- **Neden:** Uygulama `IWeatherProvider`'ı bilir, Open-Meteo'yu bilmez. Testte sahte HTTP sunucusu (`HttpMessageHandler` taklidi) kullanılır, CI internete bağlı kalmaz.
- **Alternatif:** Doğrudan `HttpClient` çağırmak. Daha kısa, ama test ve kaynak değiştirme zorlaşır.

## K-007: JSON için yerleşik System.Text.Json, ek kütüphane yok
- **Neden:** Yanıt küçük ve düz. Newtonsoft ya da başka paket gerekmedi.

## K-008: İstek kurulurken InvariantCulture
- **Neden:** Türkçe kültürde ondalık ayracı virgüldür (39,93). API nokta bekler. `FormattableString.Invariant` ile kültür sabitlenir; testte tr-TR kültürü zorlanır.
- **Doğrulanamayan:** Bu makinede InvariantGlobalization açıksa tr-TR testi gerçek bir Türkçe kültür koşmuyor olabilir. Test geçiyor ama hatayı yakalayıp yakalamayacağını kodu bozarak denemedim.

## K-009: Tahmin değerleri nullable
- **Neden:** "Veri yok" ile "0 derece" farklıdır. Kaynak bazı günlerde null verebilir (testlerle korunuyor).
- **Doğrulandı:** `temperature_2m_mean` parametresi hem tahmin hem geçmiş (archive) API'sinde çalışıyor (10 Ekim 2026'da gerçek istekle denendi).
