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
- **Saklama kararı:** Open-Meteo lisans sayfası önbellek/saklamayı açıkça yazmıyor; CC BY 4.0 kopyalamaya izin veriyor. Bu yüzden veriyi saklıyoruz, ama her gösterimde Open-Meteo linki var ve ham veriyi toplu indirmeye açmıyoruz. Kesin hukuki teyit değil, makul yorum.
- **USGS:** Resmi kullanım şartı sayfasını okumadım; sorgu parametreleri doğrulandı (koordinat kutusu, zaman, büyüklük, GeoJSON, en fazla 20.000 sonuç).

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

## K-010: Şehir listesi kodda sabit (`CityCatalog`), veritabanı tablosu değil
- **Neden:** 81 il ve 6 KKTC ilçesi değişmeyen referans veridir. Tabloya koymak migration, seed ve okuma kodu ekler, kazancı yok.
- **Alternatif:** `cities` tablosu. Liste sık değişseydi ya da kullanıcı şehir ekleyebilseydi doğru olurdu.
- **Doğrulama:** Koordinatları elle yazdım. Open-Meteo konum arama servisiyle çapraz kontrol ettim: 87 şehrin 85'i en yakın sonuca 25 km içinde. Kocaeli ve Sakarya'yı servis il adıyla başka yer olarak buldu; merkez ilçe adlarıyla (İzmit, Adapazarı) aratınca 1 km içinde eşleşti. Testler sayıyı, benzersizliği ve kaba sınır kutusunu korur. Hatay için Antakya merkezi kullanıldı.

## K-011: Deprem katmanı: `IEarthquakeProvider` + USGS uygulaması
- **Neden:** Hava durumundaki desenin aynısı (K-002, K-006). USGS resmi ve belgeli bir API; AFAD ve Kandilli'nin belgeli açık API'si yok (K-004).
- **Koordinat kutusu:** `GeoBounds.TurkiyeAndKktc` (34.5–42.5 enlem, 25–45 boylam). Kıyıya yakın komşu bölgeleri de içerir (Yunan adaları, Suriye sınırı). Bu kasıtlı: kıyıdaki depremler Türkiye'yi de etkiler. Gerekirse arayüzde ülkeye göre süzeriz.
- **GeoJSON tuzağı:** Koordinat sırası `[boylam, enlem, derinlik]`. Testte enlem ve boylamın yerleri ayrı ayrı doğrulanıyor.
- **Büyüklüğü `null` kayıtlar atlanır:** 0 sayıp göstermek yanlış veri olurdu.
- **Sınır:** USGS tek sorguda en fazla 20.000 sonuç döner. Bu bölge ve kısa süre için yeterli; çok uzun süre istenirse sayfalama gerekir (şimdilik yok).
- **Doğrulanmayan:** Sağlayıcı kodunu gerçek USGS'ye karşı C# ile çalıştırmadım. Yanıt biçimini `curl` ile gerçek yanıttan doğruladım, testler o yanıtın kısaltılmışıyla yazıldı. İlk gerçek çağrı aşama 2'de veri toplayıcıyla olacak.
- **USGS atıf/şart:** Resmi kullanım şartı sayfasını okumadım; arayüzde USGS'ye atıf göstereceğiz.

## K-012: Toplama mantığı Application'da (`DataCollectionService`), saklama arayüzleri (`IWeatherStore`, `IEarthquakeStore`)
- **Neden:** Servis dış kaynakları ve veritabanını yalnızca arayüzlerle bilir; sahtelerle hızlı test edilir. Toplayıcı program (K-013) bu servisi çağıran ince bir kabuk olur.
- **Hata yönetimi:** Bir şehir ya da deprem kaynağı hata verirse diğerleri devam eder (kısmi veri, hiç veri olmamasından iyidir). Hatalar sonuçta listelenir, toplayıcı sıfırdan farklı çıkış koduyla bildirir.
- **İptal istisnası yutulmaz:** `catch ... when (ex is not OperationCanceledException)`. Aksi halde program durdurulamazdı.
- **Zaman `TimeProvider` ile:** `DateTime.Now` yerine enjekte edilen zaman; testte sabit saat verilir.
- **Kibar istek hızı:** Şehirler arası kısa bekleme (ücretsiz API'ye nazik davranmak için).
