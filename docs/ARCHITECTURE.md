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

## K-023: Arayüz iskeleti: React + TypeScript + Vite, harita için d3-geo
- **Neden bu üçlü:** Önceki projeyle (seat-reservation-engine) aynı yığın; Vite hızlı geliştirme sunucusu ve basit derleme verir.
- **Sürümler (şablonun getirdiği güncel):** React 19, Vite 8, TypeScript 6. `npm run build` başarılı (tarih: 2026-10-10).
- **`d3-geo` neden:** Yalnızca GeoJSON'u SVG yoluna çeviren projeksiyon ve yol üretici kısmı (tüm D3 değil). Hazır harita kütüphanesi (Leaflet) yerine seçildi, çünkü "üzerine gelince ilin yükselmesi" etkisi SVG + CSS ile tam kontrolle yapılır ve altlık harita karosu (dış servis, limit, atıf) gerekmez. **Bedel:** yakınlaştırma/kaydırmayı gerekirse biz yazarız.
- **Not:** Şablondaki demo içeriği (sayaç, örnek görseller) bir sonraki adımda gerçek içerikle değiştirilecek; bu commit yalnızca iskelet.

## K-024: Harita arayüzü: SVG + d3-geo, hover'da yükselen kopya, yan panel
- **Çizim:** `TurkeyMap.tsx` GeoJSON'u `d3-geo` (`geoMercator().fitExtent` + `geoPath`) ile SVG yoluna çevirir. 81 il + KKTC = 82 `<path>`. Projeksiyon yalnızca sınır verisi değişince hesaplanır (`useMemo`).
- **Hover'da yükselme:** Üzerine gelinen ilin **kopyası** ayrı bir üst katmanda (`pointer-events: none`) çizilir; CSS ile yukarı kayar ve gölge alır. **Neden kopya:** Asıl yolu DOM'da öne taşımak, eleman fare altından kayınca `mouseleave/mouseenter` tetikleyip titreme (flicker) yapabilir. Kopya fare olaylarını almadığı için asıl yol yerinde kalır. `prefers-reduced-motion` ile animasyon kapanır.
- **KKTC:** Haritada tek bölge (kaynak tek poligon, K-022). Rengi, ilçe anomalilerinin ortalaması (verisi olmayanlar hariç). Tıklayınca yan panel 6 ilçeyi listeler; bir ilçe seçilince o ilçenin tahmini gelir, "← KKTC" geri döner.
- **Anomali renkleri:** 5 kova (mavi → gri → turuncu/kırmızı) + "veri yok" (nötr gri). Veri yok ile "normal (0)" ayrı renk; test korur. `anomaly.ts` saf fonksiyonlardan oluşur.
- **Erişilebilirlik:** Her il `role="button"`, klavyeyle odaklanır, Enter/Boşluk ile seçilir; `aria-label` il adı ve sapmayı söyler.
- **API'ye erişim:** Geliştirmede Vite vekili (`/api` → `127.0.0.1:5099`), CORS gerekmez. Canlıda `VITE_API_URL` (derleme zamanı) ve API'de `Cors__AllowedOrigins`.
- **Yarış durumu:** `useApi` anahtar değişince önceki isteği iptal eder; hızlı art arda tıklamada eski yanıt yenisinin üstüne yazmaz.
- **Sayı biçimi:** `toLocaleString` yerine elle (virgül, gerçek eksi işareti `−`), çünkü ICU/tarayıcı farkından bağımsız ve deterministik olmalı.
- **Yeni paket gerekçesi:** `vitest` (Vite ile aynı yapılandırma, ek derleme adımı yok). `d3-geo` için bkz. K-023.
- **Doğrulama (2026-10-10, gerçek API + gerçek veri, tarayıcıda):** 82 bölge çizildi; renk dağılımı: 10 bölge "veri yok" (normali henüz hesaplanamayan 9 il + KKTC). Hover başlığı, yükselen kopyanın oluşup silinmesi, tıklama, yan panel değerleri (Ankara 15,1° / normal 16,2° / sapma −1,1°, API ile birebir), KKTC → ilçe listesi → Girne → geri akışı ve klavyeyle seçim denendi. Coğrafi yerleşim sayısal olarak kontrol edildi (İstanbul batıda, Erzurum doğuda, KKTC Mersin'in güneybatısında).
- **Doğrulanamayan:** Ekran görüntüsü alınamadığı için görünümü (renkler, gölge, yerleşim) gözle doğrulamadım. Mobil görünümü ve koyu modu denemedim. Bileşenler için otomatik test yok (yalnızca saf mantık testleri: 17 test).

### K-021 ek notu (2026-10-10): API'nin yerel portu
- API'nin `http` profili başta `launchSettings.json` şablonundaki rastgele porta (5129) bakıyordu; arayüzün Vite vekili (`vite.config.ts`) ise 5099'a gidiyordu. Kullanıcı kendi bilgisayarında çalıştırınca ikisi birbirini bulamadı (haritada "Veriler şu an alınamadı" görünecekti). Düzeltme: profilin portu 5099 yapıldı, `ASPNETCORE_URLS` ayarına gerek kalmadı. **Ders:** "benim makinemde çalışıyor" yetmez; yerel çalıştırma adımları temiz bir kullanıcıyla denenmeli (README'de adım adım yazılacak).

## K-025: İklim normalleri repoda dosya olarak (`data/climate-normals.json`), dışa/içe aktarma modları
- **Sorun:** 87 şehrin 30 yıllık verisi Open-Meteo'nun ağırlık limitine takılıyor (K-019): bir kurulum 8+ dakika sürer ve saatlik limit yüzünden bazen iki ayrı çalıştırma ister. Her yeni kurulumda (geliştirici, canlı ortam, projeyi indiren biri) bu yaşanırdı.
- **Çözüm:** Normal değişmeyen bir veri. Bir kez hesaplanıp küçük bir dosya olarak repoya girer; yeni kurulum `--import-normals data/climate-normals.json` ile API'ye hiç gitmeden yükler. `--normals` (API'den hesapla) yedek olarak durur.
- **Toplayıcı modları:** `--export-normals <dosya>` (veritabanı → dosya), `--import-normals <dosya>` (dosya → veritabanı), `--normals` (API → veritabanı).
- **Dosya biçimi (`ClimateNormalFile`):** Sürümlü (`version: 1`), şehir başına tek satır (git farkı okunur), şehirler kimliğe göre sıralı (aynı veri → aynı dosya), değerler 0,01 °C'ye yuvarlı (~200 KB). Anahtar şehir **slug**'ı (ASCII, sabit), ad değil. `InvariantCulture` ile yazılır/okunur, `-0` normalize edilir.
- **Doğrulama:** Okuma sürümü, 366 değeri, sayı olduğunu ve NaN olmadığını denetler; geçersiz dosya anlaşılır `InvalidDataException` verir, yarım veri yüklenmez.
- **Güvenli dışa aktarma:** Veritabanında eksik normalli şehir varsa dosya **yazılmaz** (eksik dosya sonradan sessizce eksik veri üretirdi). Yazma geçici dosyaya yapılıp taşınır: yarıda kesilirse eski dosya bozulmaz.
- **İçe aktarma:** Mevcut normalin üzerine yazar (tekrar çalıştırmak zararsız). Katalogda olup dosyada olmayan şehir hata (çıkış kodu 1), dosyada olup katalogda olmayan kimlik uyarı.
- **Test:** Sahte depoyla servis testleri; gerçek PostgreSQL'le "veritabanı A → dosya → boş veritabanı B" uçtan uca testi.
- **Yuvarlama bedeli:** 0,01 °C sapma; anomali tek ondalıkla gösterildiği için görünmez.
- **Lisans:** Dosya Open-Meteo/ERA5-Land verisinden türetilmiştir (CC BY 4.0); dosya başlığındaki `source` alanı ve arayüz atfı bunu belirtir.

### K-025 doğrulama notu (2026-10-10)
- **Üretilen dosya:** `data/climate-normals.json`, 174 KB, 87 şehir, her biri 366 değer. Katalogla birebir eşleşiyor (eksik/fazla kimlik yok), NaN yok, `-0` yok, şehirler sıralı. Değerler makul: Ankara 14 Ocak 1,0 °C / 14 Temmuz 24,3 °C; Erzurum −7,6 / 17,7; Girne 11,7 / 29,8; Van −3,9 / 22,7.
- **Gerçek komut denemesi:** Geçici PostgreSQL konteynerinde (boş veritabanı) `--import-normals data/climate-normals.json`: 87 şehir yüklendi, Ankara 15 Ocak değeri dosyayla aynı (0,97); ikinci çalıştırmada satır sayısı değişmedi (çoğalma yok). Hata yolları: dosya yok / bozuk JSON / yol verilmemiş → anlaşılır mesaj, çıkış kodu 2.
- **Hata dersi:** İlk sürümde `data/` klasörü yoksa dışa aktarma `DirectoryNotFoundException` ile çöktü; yazma mantığı `Program.cs` içindeydi ve test edilmemişti. Düzeltme: yazma `ClimateNormalFile.WriteToFile`'a taşındı, olmayan klasörü oluşturur, testle korunur.

## K-026: Deprem katmanı (arayüz)
- **Karar:** Anomali renkli iller aynen kalır; üstüne büyüklüğe göre boyutlanan yarı saydam daireler gelir (A seçeneği; B "ayrı sekmeler" reddedildi: iki katmanı birlikte görmek asıl değer). Sağ üstteki kutudan açılıp kapanır, "son 1/7/30 gün" ve "en az M 2,5/3/4/5" seçilir.
- **Daire boyutu:** Yarıçap büyüklükle doğrusal artar, [3, 18] aralığına sınırlanır (`radiusFor`). Büyükten küçüğe çizilir; küçük daire büyüğün altında kalıp tıklanamaz olmasın.
- **Harita dışı depremler:** Koordinat kutusu komşu ülkeleri de içerir (K-011). Projeksiyon çerçevesinin dışına düşenler çizilmez, sayılır ve haritanın altında "N deprem harita çerçevesinin dışında kaldı" notu çıkar (sessizce atlanmaz).
- **Zaman:** Türkiye saatiyle (UTC+3, elle hesaplanır; tarayıcının saat dilimine bağımlı değil). Yer metni USGS'nin İngilizce metni olarak, çevrilmeden gösterilir.
- **İstek yönetimi:** `useApi` anahtarı `null` ise hiç istek atılmaz (katman kapalıyken boşuna API'ye gidilmez); ayar değişince eski istek iptal edilir.
- **Toplama penceresi 7 → 30 gün:** API sorguya 30 güne kadar izin veriyor ama toplayıcı 7 gün çekiyordu; "son 30 gün" seçeneği 7 günlük veri döndürüyordu (gerçek denemede fark edildi). `DataCollectionService.EarthquakeWindow` 30 gün yapıldı. Bedel: ilk çalıştırmada daha fazla deprem çekilir (USGS 20.000 sınırının çok altında).
- **Atıf ve uyarı:** Altbilgide ve deprem panelinde "USGS verisi, küçük depremler eksik olabilir; resmi kayıt için AFAD ve Kandilli".
- **Doğrulama (2026-10-10/11, gerçek API + gerçek veri, tarayıcıda):** katman açıldı: 9 daire = API'deki 9 deprem; yarıçaplar sıralı; hover başlığı, tıklayınca panel (zaman, derinlik, konum), "← Kapat"; M4+ süzgeci 8; 1 gün 1; katman kapanınca daire ve seçenekler kalktı. Normaller dosyadan yüklenince tüm 82 bölge renkli geldi (gri yok).
- **Doğrulanamayan:** Ekran görüntüsü alınamadığı için dairelerin görsel yerleşimini/rengini gözle doğrulamadım; "harita dışı" notu bu veride tetiklenmedi (9 deprem de çerçevede), o dal yalnızca birim testle doğrulandı. Bileşenler için otomatik arayüz testi yok, saf mantık testleri var (29).

## K-027: Deprem hover kartı (kılavuz çizgili), iç içe depremler, il bulma
- **İstek:** Deprem dairesinin üstüne gelince saat, yer, büyüklük ayrıntısı; daireden karta çizgi; üst üste binen depremler; il bilgisi.
- **Kart ve kılavuz çizgisi:** Daireye gelince HTML kart açılır, dairenin **kenarından** (merkezinden değil) karta ince bir çizgi uzanır. Kart noktanın sağına, sığmazsa soluna, o da olmazsa altına/üstüne yerleşir ve her zaman kabın içinde kalır (`placeCard`, saf fonksiyon, testli). Kart `pointer-events: none`: fareyi çalmaz, hover titremesi yapmaz.
- **İç içe depremler:** Merkezler arası uzaklık yarıçaplar toplamından küçükse daireler üst üste biniyor sayılır (`overlappingWith`). Kart, fare altındakini ve onunla bineni "Bu noktada N deprem iç içe" diye listeler (en çok 6 satır, fazlası "+N daha"). Büyükten küçüğe çizim sırası korunur (küçük daire büyüğün altında kalmaz).
- **İl bulma:** Depremin hangi ilin (ya da KKTC'nin) sınırları içinde olduğu, haritadaki il çokgenlerinden **düz (planar) nokta-çokgen testiyle** bulunur. d3-geo'nun küresel `geoContains` fonksiyonu çokgen yön sırasına (winding) duyarlı ve yanlış yönde tüm dünyayı "içinde" sayabildiği için kullanılmadı. Deniz ya da komşu ülkedeyse "Türkiye sınırları dışı (deniz/komşu bölge)".
- **İLÇE YOK (bilinen sınır):** Elimizde ilçe sınırı verisi yok (Natural Earth yalnızca il verir, bkz. K-022), bu yüzden depremin ilçesi **hesaplanamaz**. Bunun yerine USGS yer metni ayrıştırılıp "En yakın yerleşim: Feke, Turkey" ve "Uzaklık, yön: 21 km, doğu-güneydoğu" gösterilir. Bu "ilçe" değil "en yakın yerleşim" olarak adlandırıldı; karıştırılmasın diye. Metin ayrıştırılamazsa ham metin gösterilir (uydurma yok).
- **Dar ekran:** Telefon genişliğinde harita kabı ~350x160 px, kart ise ~270x270+ px; hiçbir konumda sığmaz ve taşardı (gerçek denemede bulundu). `cardFits` kuralı: kap kartı sığdıramıyorsa kart gösterilmez. Dokunmatikte zaten hover yoktur; dokununca yan panel açılır ve aynı ayrıntıyı verir.
- **`useElementSize`:** Kap boyutu `ResizeObserver` yanında ilk anda doğrudan da ölçülür; gözlemci yalnızca çizim adımında tetiklenir, gizli/arka plan sekmede boyut sıfır kalır ve kart hiç çıkmazdı.
- **Doğrulama (2026-10-11, gerçek veri, tarayıcıda, 1400x900 ve 390x850 emüle):** Masaüstünde 9 depremin 9'unda kart çıktı, hepsi kabın içinde, hepsinde kılavuz çizgisi karta bağlı ve dairenin kenarından başlıyor. Feke/Saimbeyli depremleri "Adana" olarak bulundu; Girit açığındakiler "Türkiye sınırları dışı". İç içe: Feke kümesinde 6, Fry kümesinde 3 deprem listelendi. Telefonda kart çıkmadı, taşma yok, dokunmayla yan panel açıldı.
- **Doğrulanamayan:** Ekran görüntüsü alınamadığı için kartın/çizginin **görsel güzelliğini** (renk, gölge, çizginin görünümü) gözle doğrulamadım; sayısal yerleşim doğrulandı. Sağ kenara yakın bir deprem (kartın sola dönmesi) bu veride yok; yalnızca birim testle doğrulandı.

## K-031: "Şu an" ile "günlük ortalama" ayrımı, veri tazeliği, doğru hava kodu etiketleri (kullanıcı gözleminden)
_(Numara eklenme sırasını gösterir; bu bölüm K-028'den önce durur.)_
- **Sorun (kullanıcı bulgusu):** Panelde Şanlıurfa için "Sağanak" ve "Bugün 22,2°" yazıyordu; arkadaşı "güneşli" diyor, termometre 27-28° gösteriyordu. İkisi de **bizim etiketleme hatamızdı**:
  1. Open-Meteo'nun günlük `weather_code` değeri günün **en kötü saatinin** kodudur. Gerçek veride yalnızca 11:00'de 0,2 mm'lik kısa bir sağanak vardı (toplam 0,4 mm); şu an kod 2/3 (parçalı bulutlu/kapalı). Tüm gün "Sağanak" gösteriliyordu. Ayrıca 80/81/82 kodlarının hepsi aynı "Sağanak" etiketiyle gösteriliyordu.
  2. "Bugün 22,2°" **günlük ortalamaydı** (gece serinliği dahil); şu an 28,3° idi.
  3. Veri **2 saat bayattı**: toplayıcı elle çalıştırılıyordu (13:03), Open-Meteo `current` ise 15 dakikada bir güncellenir.
- **Karar (proje fikri değişmedi):** Ana fikir **anomali haritası** kalır (günlük ortalamanın 1991-2020 normalinden sapması), bu uygulama bir "anlık hava durumu" uygulaması değildir. Etiketler bunu açıkça söylemeli.
- **Yapılanlar:**
  - `current=temperature_2m,weather_code,precipitation` aynı istekte çekilir (ek istek yok); yeni `current_weather` tablosu (şehir başına tek satır, her çekişte üzerine yazılır) ve çekilme zamanı (`FetchedAt`).
  - API `forecast` ucu `current` ve `updatedAt` döner. Eski veride `current` yoksa `null`: arayüz "Anlık durum henüz çekilmedi" der, çökmez.
  - Panel: **"Şu an"** ve **"Günlük ort."** ayrı kutular; "Veri güncelleme: 15:19 (az önce)"; 3 saatten eski veride "eski olabilir" uyarısı; sapmanın günlük ortalamaya göre olduğu yazılı.
  - Hava kodu etiketleri: her WMO kodu kendi adını taşır (hafif sağanak / sağanak / şiddetli sağanak ayrı). Yağış kodu olup günlük toplam 1 mm'nin altındaysa "Gün içinde kısa süreli hafif sağanak (0,4 mm)": tüm gün yağışlıymış gibi gösterilmez.
- **`current.time` ofseti:** Open-Meteo `time` değerini şehrin yerel saatiyle ofsetsiz verir ("2026-10-11T15:00"); yanıttaki `utc_offset_seconds` ile mutlak ana çevrilir (testle korunur: 15:00 yerel = 12:00 UTC). Ayrıştırılamayan zaman ya da eksik `current` günlük tahmini engellemez (null).
- **Saat yönetimi:** "Şimdi" bileşende `useState` + dakikada bir güncelleme; render sırasında `Date.now()` çağrısı bileşeni saf olmaktan çıkarırdı (lint de uyardı). Panel açık kaldıkça "2 saat önce" yazısı ilerler.
- **Yeni geliştirme kolaylığı:** Vite vekili `API_PROXY` ortam değişkeniyle ayarlanır (varsayılan 5099).
- **Henüz yapılmayan:** Veri hâlâ elle çalıştırılan toplayıcıyla yenileniyor; **otomatik saatlik toplama canlıya alma aşamasında (GitHub Actions) kurulacak.** Haritanın renkleri günlük ortalamaya göre kalır, "şu an" sıcaklığına göre değil.
- **Doğrulama (2026-10-11, gerçek veri):** Geçici veritabanı + yeni API + tarayıcı. API'nin Şanlıurfa değerleri Open-Meteo'nun aynı andaki cevabıyla birebir: şu an 28,3° kod 3, günlük kod 80, yağış 0,4 mm; ölçüm 12:15 UTC = 15:15 Türkiye saati. Arayüzde: "Bugün: Gün içinde kısa süreli hafif sağanak (0,4 mm)", "Şu an 28,3° / Günlük ort. 22,3°", "Veri güncelleme: 15:19 (az önce)". Ankara ve Girne (KKTC) panelleri de doğru.
- **Doğrulanamayan:** "Eski olabilir" uyarısı (3 saatten eski veri) tarayıcıda gerçek bayat veriyle görülmedi, yalnızca birim testle kanıtlı. `Api.Tests` (21 test) bu değişikliklerden sonra çalıştırılamadı: kullanıcının çalışan API'si DLL'leri kilitliyordu (API kodu derlendi, DTO değişikliği küçük). Ekran görüntüsü alınamadığı için panelin görsel düzeni (4 kutu, yeni satırlar) gözle doğrulanmadı.

## K-028: Deprem kümeleri: iç içe depremler tek işaret, tıklayınca etrafa yayılır ("spiderfy")
- **Sorun (kullanıcı gözlemi):** Feke bölgesinde 6 deprem dairesi birbirinin içine girmiş, hangisi hangisi belli değildi, alttakilere tıklanamıyordu. Hover kartında "iç içe" listesi yetersizdi (K-027).
- **Çözüm:** İç içe giren daireler tek bir **küme işaretinde** (magenta, içinde deprem sayısı) toplanır. Kümeye tıklayınca üyeler küme merkezinin etrafında halka(lar) üzerine **yayılır**; her üyeden depremin **gerçek konumuna** ince bir çizgi ve küçük bir nokta çizilir. Yayılmışken her daireye gelince kart, tıklayınca yan panel çalışır.
- **Kapatma (4 yol):** merkezdeki sönük halkaya tıklamak, `Esc`, başka bir kümeyi açmak (öncekini kapatır), deprem ayarlarını değiştirmek (küme kimliği değişince kapanır).
- **Küme tanımı:** Daireleri üst üste binenler **zincirleme** aynı kümedir (A-B ve B-C iç içeyse A-C değilken bile üçü bir küme; birleştirme-bulma/union-find). Yalnızca değmek (uzaklık = yarıçaplar toplamı) üst üste binmek sayılmaz. Küme kimliği: en büyük üyenin kimliği + üye sayısı; girdi sırasından bağımsız.
- **Yayılma yerleşimi (`fanLayout`):** Halka kapasitesi komşular arası **kiriş** uzunluğundan hesaplanır (yay uzunluğundan değil: az üyede yay yanıltıcı, daireler değerdi); çok üyede dış halkalar eklenir (yarım adım kaydırılmış). Başlangıç açısı daireler çerçevede kalacak şekilde döndürülür; yetmezse (küme köşede/kenardayken etrafında tam halka hiçbir açıda sığmaz) tüm yayılma çerçevenin içine **kaydırılır**. Test yazarken bu eksik görüldü: ilk sürüm yalnızca dönüyordu, köşe testi başarısız oldu. Kaydırmada çizgiler üyeleri gerçek konumlarına bağlamaya devam eder.
- **Küme kartı:** Küme işaretine gelince "N deprem, M aralığı, il, üyelerin listesi (en çok 6, fazlası "+N daha")". Kart `pointer-events: none`.
- **Eski kod temizlendi:** Önceki `overlappingWith` (kartta "iç içe" listesi) kümelemenin yerini aldığı için silindi.
- **Doğrulama (2026-10-11, gerçek veri, tarayıcıda 1400x900 emüle):** Kapalıyken 9 deprem 2 küme ("6" ve "3"), tek daire yok. Küme kartı: "6 deprem, M 4,2–5,0, Adana", liste, kılavuz çizgisi. Tıklayınca 6 daire eşit aralıkla yayıldı (hepsi çerçevede, daireler arası en küçük boşluk 15 px), 6 çizgi + 6 gerçek konum noktası. Üyeye gelince tek deprem kartı, tıklayınca yan panel (yayılma yerinde kalır). Merkez halkaya tıklayınca ve `Esc` ile toplandı; ikinci kümeyi açınca birincisi kapandı.
- **Doğrulanamayan:** Ekran görüntüsü alınamadığı için görsel güzelliği (magenta rengi, çizgi ve nokta görünümü, halkanın estetiği) gözle doğrulamadım; yerleşim sayısal doğrulandı. Telefon genişliğinde yayılmanın tıklanabilirliği denenmedi. Küme eşiği ekrandaki daire boyutuna bağlı: pencere boyutu değişince kümeler değişebilir.
