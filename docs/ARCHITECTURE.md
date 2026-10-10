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

## K-013: Saklama: EF Core + Npgsql, tablo sınıfları Domain'den ayrı
- **Neden:** Domain kayıtları (`City`, `DailyForecast`, `Earthquake`) saf kalır; EF'nin ihtiyaçları (anahtar, ilişki, tablo adı) `Persistence/` altındaki `*Row` sınıflarında. Dönüşüm bilerek elle yapılıyor, küçük ve açık.
- **Tablolar:** `cities` (ad benzersiz), `daily_forecasts` (bileşik anahtar: şehir + gün), `earthquakes` (anahtar: USGS kimliği, zamana indeks).
- **Upsert:** Aynı şehir+gün ya da aynı deprem tekrar gelirse güncellenir, çoğalmaz. Tahmin her gün değişir; USGS de sonradan büyüklüğü düzeltebilir.
- **Zaman:** Hep UTC saklanır (`timestamptz`). Testte +03:00 verilen zamanın UTC olarak aynen geri geldiği doğrulanıyor.
- **`IDbContextFactory`:** Her çağrıda kısa ömürlü context. Uzun yaşayan tek context yüzlerce kaydı izleyip yavaşlar, hatadan sonra bozuk kalabilir.
- **Migration:** `Persistence/Migrations/`. Testler gerçek migration'ı çalıştırır; yani migration dosyası da test ediliyor.

## K-014: Yeni paketler ve gerekçeleri
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11: EF Core'un PostgreSQL sürücüsü. Sürüm .NET 8 ile uyumlu olsun diye 8.0.x'e sabitlendi (makinede .NET 10 SDK var, varsayılan olarak 10.x paket gelirdi).
- `Microsoft.EntityFrameworkCore.Design` 8.0.11 + yerel `dotnet-ef` aracı (`dotnet-tools.json`): migration üretmek için. Genel (global) kurulum yerine repoya özel kurulum: herkes aynı sürümü kullanır.
- `Testcontainers.PostgreSql`: testte geçici gerçek PostgreSQL açar. **Alternatif:** EF InMemory/SQLite. Reddedildi çünkü upsert, tarih tipleri ve migration PostgreSQL'de farklı davranabilir; sahte veritabanı bu hataları gizler. **Bedel:** testler Docker ister ve ilk çalışmada imaj indirir (~yirmi-otuz sn).

## K-015: Toplayıcı ayrı konsol programı ("çalış ve bitir"), zamanlama GitHub Actions'ta
- **Neden:** K-005'teki sorun: Render ücretsiz katmanı 15 dk sessizlikte API'yi uyutur, API içindeki `BackgroundService` de durur. Toplayıcı API'den bağımsız bir program olursa, zamanlanmış bir iş (GitHub Actions, public repoda ücretsiz olmalı) onu saatte bir çalıştırır; API uyusa da veri akar.
- **Alternatif:** API içinde `BackgroundService`. Daha basit, ama ücretsiz barındırmada güvenilmez. Reddedildi.
- **Çıkış kodları:** 0 tamam, 1 kısmi hata (veri yine de yazıldı), 2 yapılandırma hatası. Zamanlanmış işte hata görünür olur.
- **Güvenlik:** Bağlantı dizesi yalnızca `ConnectionStrings__Default` ortam değişkeninden okunur ve hiçbir zaman yazdırılmaz.
- **Şema sahibi:** Toplayıcı açılışta migration'ları uygular. API gelince şema sorumluluğunu yeniden değerlendiririz.
- **Henüz yapılmadı:** GitHub Actions zamanlaması ve canlı veritabanı (Neon) aşama 6'da; hesap açma adımları kullanıcıya ait.

## K-016: Yerel veritabanı Docker Compose ile, parola `.env`'den
- `docker-compose.yml` sadece `db` servisi. Parola `${POSTGRES_PASSWORD:?...}` ile zorunlu: tanımlı değilse compose hata verir, boş/varsayılan parolayla çalışmaz. Port yalnızca `127.0.0.1`'e açılır.
- **Doğrulandı (2026-10-10):** Geçici konteynerde gerçek uçtan uca deneme: 87/87 şehir, 609 tahmin satırı (87×7 gün), 8 deprem; ikinci çalıştırmada sayılar aynı kaldı (upsert çalışıyor). Ankara'nın bugünkü değeri (7,7 / 24,1 °C) başta `curl` ile alınan yanıtla birebir eşleşti.
- **Bilinen kozmetik tutarsızlık:** Tablo adları küçük harf (`cities`), sütun adları EF varsayılanıyla BüyükHarf (`"Name"`). Çalışmayı etkilemiyor; elle SQL yazarken sütunları tırnak içinde yazmak gerekir. İstenirse snake_case adlandırma paketiyle düzeltilebilir (yeni bağımlılık olduğu için şimdilik yapılmadı).

## K-017: İklim normali ve anomali (Domain'de saf hesap)
- **Tanım:** Normal = 1991-2020 ortalama sıcaklığı, yılın her günü için. Anomali = gözlenen/tahmin edilen sıcaklık − o günün normali (pozitif = normalden sıcak). 1991-2020, Dünya Meteoroloji Örgütü'nün standart "iklim normali" dönemidir.
- **±7 günlük pencere:** Tek bir takvim günü için 30 örnek gürültülü. Her günün iki yanındaki 7 günü de ortalamaya katıyoruz (~450 örnek). Bedel: ani mevsim geçişleri biraz yumuşar; normal için kabul edilebilir.
- **366 konum, artık yıla göre indeks:** Gün numarasını her zaman artık yıl olan 2000'e göre alırız; 1 Mart her yıl aynı konumda, 29 Şubat'ın kendi yeri var.
- **Dairesel pencere:** Yıl sonu ile yıl başı komşu (3 Ocak'ın penceresi Aralık sonunu da içerir). Testle korunuyor.
- **Null günler ortalamaya katılmaz.** Yetersiz veri varsa sessizce yanlış değer üretmek yerine anlaşılır hata verir.
- **Alternatif:** Her gün için tek takvim günü ortalaması (pencere yok): daha gürültülü. Ya da kayan 31 günlük ortalama: gerçeğe daha az sadık.

## K-018: İklim normali hesabı ayrı mod (`--normals`), bir kez hesaplanır
- **Neden:** Normal 1991-2020 verisine dayanır, değişmez. Her saatlik toplamada yeniden çekmek anlamsız ve pahalı. Bu yüzden toplayıcının ayrı bir modu: `--normals` (hesaplanmış şehirleri atlar), `--force` (hepsini yeniden hesaplar).
- **Saklama:** Şehir başına tek satır, `climate_normals.DailyMeansC` bir PostgreSQL `double precision[]` dizisi (366 değer). 366 ayrı satır yerine: her zaman birlikte okunur/yazılır, tek sorgu yeter.
- **Model açıkça seçildi:** `models=era5_land`. Varsayılan "best_match" zamanla değişebilir; normal tekrarlanabilir olmalı. ERA5-Land ~11 km çözünürlük.

## K-019: 429 (dakikalık limit) için bekleyip yeniden deneme (`RateLimitRetryHandler`)
- **Sorun (gerçek denemede bulundu):** Open-Meteo limiti istek sayısına değil istek **ağırlığına** bakıyor. 30 yıllık günlük veri istemek ağır sayılıyor: ilk çalıştırmada 16 şehirden sonra "Minutely API request limit exceeded" geldi ve kalan şehirlerin hepsi hemen 429 aldı. Sahte HTTP ile yazılan testler bunu gösteremezdi.
- **Çözüm:** `DelegatingHandler`. 429 gövdesinde "Minutely" geçiyorsa 65 sn bekleyip yeniden dener (en fazla 4 kez). Saatlik/günlük limitte beklemenin faydası yok, yanıt olduğu gibi döner ve çağıran hata görür.
- **Test edilebilirlik:** Bekleme fonksiyonu enjekte edilir; testte gerçekten beklemeden süre kaydedilir.
- **Bilinen sınır:** Yanıt gövdesindeki "Minutely" metnine bakıyoruz; Open-Meteo mesajı değiştirirse yeniden deneme devreye girmez (hata görünür olur, sessizce yanlış çalışmaz).

### K-019 sonuç notu (2026-10-10, gerçek çalıştırma)
- Yeniden deneme işleyicisiyle `--normals` çalıştırıldı: 8 dk 20 sn'de toplam 72/87 şehrin normali saklandı (ilk denemede 16, ikincide 56 daha). Kalan 15 şehirde **saatlik** limit aşıldı ("Hourly API request limit exceeded"); işleyici bilerek beklemedi ve şehirleri hata olarak raporladı. Çıkış kodu 1.
- **Pratik sonuç:** İlk kurulumda normal hesabı bir saat arayla iki kez çalıştırılır. Komut tekrarlanabilir (saklı şehirler atlanır), yani aynı komut kaldığı yerden devam eder. Normal bir kez hesaplandıktan sonra bir daha gerekmez.
- **Doğrulama:** Saklanan değerler iklimsel olarak makul: Ankara 14 Ocak ≈ 1,0 °C, 14 Temmuz ≈ 24,2 °C; Erzurum 14 Ocak ≈ −7,6 °C; Antalya 14 Temmuz ≈ 28,6 °C. Dizi uzunluğu hepsinde 366.
- **Henüz doğrulanmayan:** Kalan 15 şehrin (KKTC dahil) normali gerçek veriyle hesaplanmadı; sınır kontrolü saatlik limit sıfırlanınca yapılacak.

## K-020: Sorgu servisleri Application'da, okuma/yazma arayüzleri ayrı
- **Neden:** Yazma (`IWeatherStore`, `IEarthquakeStore`) toplayıcının işi; okuma (`IForecastReader`, `IEarthquakeReader`) API'nin işi. Arayüzleri ayırmak, API'nin yanlışlıkla veri yazamamasını sağlar ve her tarafın ihtiyacı kadar yüzey bırakır. Okuyucular `AsNoTracking` kullanır (veri sadece gösterilecek, izleme gereksiz).
- **Servisler:** `ForecastQueryService` (şehir tahmini + anomali, harita özeti), `EarthquakeQueryService` (parametre doğrulama + sorgu). Controller'lar sadece bunları çağırır.
- **Anomali tahminler için de hesaplanır:** `anomali = ortalama sıcaklık − o günün normali`. Normal ya da ortalama yoksa anomali `null` (0 değil): "veri yok" ile "normal" karışmasın.
- **Şehir kimliği (slug):** `City.Slug`, ASCII ve küçük harf (`istanbul`). Arama `City.ToSlug` ile normalleştirilir, yani `İstanbul`, `ISTANBUL`, `istanbul` aynı şehre gider. Türkçe harfleri açıkça eşliyoruz; `ToLower` kültüre göre farklı davranır (İ/I/ı) ve güvenilmez.
- **"Bugün" = Türkiye saati (sabit UTC+3):** Türkiye 2016'dan beri yaz saati uygulamıyor. Sabit ofset, işletim sistemine göre değişen saat dilimi adlarına (Windows `Turkey Standard Time`, Linux `Europe/Istanbul`) bağımlı olmaz. Test: 22:30 UTC, Türkiye'de ertesi güne düşer. **Bilinen sınır:** KKTC yaz saati uyguladığı için yazın bir saatlik fark olabilir.
- **Harita özeti verisiz şehri de listeler** (boş değerlerle): arayüz "veri yok" gösterebilsin, nokta kaybolmasın.
- **Deprem sorgusu sınırları:** 1-30 gün, büyüklük 0-10, en fazla 500 sonuç. Sınırsız sorgu veritabanını yorabilir.

## K-021: API: ASP.NET Core, ince controller'lar, DTO'lar
- **Uçlar (hepsi GET):** `/health`, `/api/cities`, `/api/cities/{id}/forecast`, `/api/map`, `/api/earthquakes?days=&minMagnitude=`, `/api/sources`.
- **Controller'lar ince:** Parametreyi alır, Application servisini çağırır, DTO'ya çevirir. İş mantığı yok.
- **DTO'lar (`Dtos.cs`):** API'nin sözleşmesi. Domain sınıflarını doğrudan vermiyoruz; Domain değişince arayüzün beklediği JSON kazara bozulmasın.
- **Çıktı yuvarlaması:** Sıcaklıklar tek ondalığa yuvarlanır (`16.1`), `16.174888888888887` değil. Hesap tam hassasiyetle yapılır, yalnızca dışarı verilen değer yuvarlanır. Bedel: `ortalama − normal` ile `anomali` ekranda 0,1 sapabilir (yuvarlama), bu gösterim düzeyinde zararsız.
- **Hata biçimi:** Standart `ProblemDetails` JSON'u. Bilinmeyen şehir 404, geçersiz sorgu parametresi 400, POST 405. İstisna ayrıntısı dışarı sızmaz (`UseExceptionHandler`).
- **CORS varsayılan kapalı:** `Cors:AllowedOrigins` tanımlıysa yalnızca o adreslere ve yalnızca GET'e açılır. Arayüz farklı bir adreste yayınlanınca (aşama 4-6) ayarlanır.
- **Bağlantı dizesi:** Yalnızca `ConnectionStrings__Default` ortam değişkeninden; tanımlı değilse başlangıçta anlaşılır bir hatayla durur, değeri asla yazdırılmaz.
- **API şemayı yönetmez:** Migration'ı toplayıcı uygular (K-015). API sadece okur (`IForecastReader`, `IEarthquakeReader`).
- **Atıf:** `/api/sources` Open-Meteo (CC BY 4.0) ve USGS bilgisini verir; arayüz her sayfada gösterecek.
- **Test:** `WebApplicationFactory` ile bellek içi test sunucusu + gerçek PostgreSQL (Testcontainers). Saat sabitlenir (`TimeProvider` değiştirilir). JSON şekli, 404/400/405, Türkçe karakterli URL, yuvarlama test edilir. Bilerek hata sokarak (deprem sıralamasını tersleyerek) testlerin gerçekten yakaladığı doğrulandı.
- **Gerçek veriyle elle doğrulandı (2026-10-10):** Yerel PostgreSQL + gerçek toplanmış veri. Ankara bugün 15,1 °C, normal 16,2 °C, anomali −1,1 °C. Harita: 87 nokta, 72'sinde anomali (normali hesaplanan şehirler), 15'inde yok. 8 deprem, yeniden eskiye.

## K-022: Harita sınırları (Natural Earth, kamu malı) ve KKTC
- **Karar:** Sınırlı (choropleth) harita: iller tıklanabilir, üzerine gelince yükselir, tıklayınca yan panel açılır. Noktalı harita yerine bu seçildi (kullanıcı kararı).
- **Kaynak:** Natural Earth 10m `admin_1_states_provinces` (kamu malı: atıf zorunlu değil, ticari kullanım dahil serbest; yine de arayüzde "Made with Natural Earth" yazılır). 50m sürümünde Türkiye illeri **yoktu** (yalnızca birkaç ülke var); bu yüzden 40,7 MB'lık 10m dosyası kullanıldı. İndirilen ham dosya repoya girmez.
- **Üretilen dosya:** `web/public/data/turkiye-kktc.geojson` (~109 KB, 82 özellik: 81 il + KKTC). Üretim betiği `scripts/build-geojson.mjs`: Douglas-Peucker ile basitleştirme (~500 m tolerans), 3 ondalık koordinat, minik ada/delik atma. Betiği temizledikten sonra aynı çıktıyı verdiği doğrulandı.
- **İl adlarına güvenilmedi:** Natural Earth adlarında yazım hataları var (`Kinkkale`, `Zinguldak`) ve Türkçe karakter yok. Bunun yerine her ilin içine `CityCatalog` koordinatının düşüp düşmediğine bakılarak eşlendi; kimlik (`id`) katalogdaki slug'dır (API ile aynı, ör. `istanbul`).
- **Çapraz doğrulama sonucu:** 81/81 il eşleşti. Tekirdağ koordinatı poligonun ~1,5 km dışında (Marmara kıyısı), yedek eşleme (tek eşleşmeyen il + tek eşleşmeyen şehir, < 5 km) ile bağlandı. Girne koordinatı KKTC poligonunun ≤3,4 km dışında (köşe uzaklığı, gerçeği olduğundan büyük gösterir); sorun değil.
- **KKTC:** Natural Earth'te tek poligon (`Northern Cyprus`). Haritada **tek bölge** olarak çizilecek; 6 ilçe (Lefkoşa, Girne, Gazimağusa, Güzelyurt, İskele, Lefke) tıklanınca yan panelde ayrı ayrı gösterilecek. İlçe sınırları yok (kaynak vermiyor); bunu sahte sınır uydurarak kapatmıyoruz.
- **Bilinen sınır:** 10m çözünürlük ve ~500 m basitleştirme: ülke ölçeğinde keskin, il sınırında yakınlaştırınca kaba görünebilir.
