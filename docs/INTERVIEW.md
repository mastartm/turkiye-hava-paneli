# Mülakat Soruları

Her aşamada yeni sorular eklenir. Cevaplar kısa ve kendi sözlerinle yeniden anlatılabilir olmalı.

## Mimari

**S: Neden katmanlı mimari?**
C: İş kuralları veritabanından ve HTTP'den bağımsız kalır, böylece kolay test edilir. Bağımlılıklar hep içe doğru akar.

**S: "Katman sağlayıcısı" arayüzü neden var?**
C: Yeni bir veri katmanı (örn. hava kalitesi) eklemek için mevcut kodu değiştirmem gerekmez, sadece yeni bir sağlayıcı yazarım. Buna açık/kapalı prensibi denir.

## Veri

**S: Neden Open-Meteo?**
C: Anahtarsız ve ücretsiz, veri lisansı CC BY 4.0 (atıf yapıyorum). Proje ticari olmadığı için kullanım şartına uygun.

**S: Neden MGM veya Kandilli'yi kullanmadın?**
C: Resmi, belgeli bir açık API'leri yok. Siteyi kazımak kırılgan olur ve kullanım izni belirsiz.

**S: Neden PostgreSQL?**
C: Docker'da hafif, ücretsiz barındırma seçenekleri var ve önceki projemden farklı bir teknolojiyi gösteriyor.

## Aşama 1: Open-Meteo istemcisi

**S: Neden `HttpClient`'ı doğrudan çağırmadın, arayüz koydun?**
C: Uygulama kaynağı bilmez. Testte gerçek internet yerine sahte yanıt veririm; kaynağı değiştirmek de tek sınıfı değiştirmek olur.

**S: Dış API'yi nasıl test ettin?**
C: `HttpMessageHandler`'ı taklit ettim. Gerçek bir yanıtın kısaltılmış halini sahte sunucu döndürüyor. Hata kodu, null değer ve bozuk yanıt senaryoları da test edildi.

**S: Türkçe Windows'ta çıkabilecek bir hata var mı?**
C: Evet, ondalık ayraç. Türkçe kültürde 39,93 yazılır, API 39.93 bekler. İstek kurarken InvariantCulture kullanıyorum.

**S: Neden `double?` (nullable)?**
C: Kaynak bazı günler veri vermeyebilir. Null'u 0'a çevirseydim yanlış sıcaklık göstermiş olurdum.

## Aşama 2: şehir listesi

**S: 81 ili neden veritabanına değil koda koydun?**
C: Değişmeyen referans veri. Tablo migration ve okuma kodu getirirdi, faydası olmazdı. Liste değişkense ya da kullanıcı ekliyorsa tablo olurdu.

**S: Koordinatların doğru olduğunu nasıl bildin?**
C: İki katman. Testlerle sayı, benzersizlik ve ülke sınır kutusunu koruyorum. Ayrıca bir kerelik betikle Open-Meteo'nun konum servisine karşı kontrol ettim. İki sapma çıktı, ikisi de servisin il adını başka bir yere eşlemesinden kaynaklanıyordu, merkez ilçe adıyla doğrulandı.

## Aşama 2: deprem sağlayıcısı

**S: GeoJSON'da dikkat ettiğin bir şey var mı?**
C: Koordinat sırası `[boylam, enlem]`, çoğu insanın alışkın olduğunun tersi. Enlem ile boylamı karıştırmamak için testte iki değeri ayrı ayrı doğruluyorum.

**S: Büyüklüğü `null` olan depremi ne yaptın?**
C: Atladım. 0 sayıp göstermek yanlış veri olurdu, "veri yok"u haritaya basmak da anlamsız.

**S: Koordinat kutusunda neden komşu ülkeler de var?**
C: Kıyıdaki depremler (Yunan adaları, Suriye sınırı) Türkiye'yi de etkiler. Kutuyu dar tutarsam bunları kaçırırım. Arayüzde ülkeye göre süzmek sonradan yapılabilir.

**S: USGS ne kadar veri döndürebilir?**
C: Tek sorguda en fazla 20.000. Bu bölge ve kısa süre için yeterli. Çok uzun süre sorgularsam sayfalama gerekir; şimdilik yok.

## Aşama 2: toplama mantığı

**S: Bir şehir hata verirse ne olur?**
C: Diğer şehirlere devam ederim ve hatayı sonuçta raporlarım. Kısmi veri, hiç veri olmamasından iyi. Toplayıcı hata varsa sıfırdan farklı kodla çıkar, böylece zamanlanmış işte görünür olur.

**S: `DateTime.Now` yerine `TimeProvider` neden?**
C: Zamanı dışarıdan verebilmek için. Testte sabit bir saat verip "son 7 gün" hesabını kesin olarak doğrulayabiliyorum.

**S: İptal (CancellationToken) neden özel ele alındı?**
C: Hataları yakalarken iptali de yakalarsam program durdurulamaz. Bu yüzden iptal istisnasını filtreyle dışarıda bırakıyorum.

## Aşama 2: veritabanı

**S: Testte neden SQLite/InMemory değil gerçek PostgreSQL?**
C: Upsert, tarih tipleri ve migration veritabanına göre farklı davranabilir. Sahte veritabanı bu hataları gizler. Bedeli: testler Docker istiyor.

**S: Tahmin tablosunun anahtarı neden (şehir, gün)?**
C: Bir şehrin bir günü için tek tahmin olmalı. Yeni tahmin gelince üzerine yazarım; geçmiş tahminleri tutmak istesem anahtara "çekilme zamanı" eklerdim.

**S: Zamanı neden UTC saklıyorsun?**
C: Saat dilimi karışıklığını önlemek için. Gösterirken yerel saate çeviririm. Testte +03:00 verilen zamanın UTC olarak aynen geri geldiğini doğruluyorum.

**S: Neden her çağrıda yeni DbContext?**
C: Uzun yaşayan bir context yüzlerce kaydı bellekte izleyip yavaşlar, hata sonrası bozulabilir. Kısa ömürlü context temiz bir başlangıç verir.

**S: USGS aynı depremi sonradan değiştirirse?**
C: Kimliğe göre güncelliyorum. İlk çekimde "automatic" olan büyüklük sonradan "reviewed" olunca değişebilir.

## Aşama 2: toplayıcı

**S: Neden API içinde arka plan servisi değil de ayrı program?**
C: Ücretsiz barındırma API'yi boştayken uyutuyor, içindeki arka plan servisi de duruyor. Toplayıcıyı ayrı bir "çalış ve bitir" programı yapıp zamanlanmış bir işle (GitHub Actions) çağırınca API uyusa bile veri akıyor.

**S: Toplayıcı hata verirse nasıl anlaşılır?**
C: Çıkış koduyla. 0 tamam, 1 kısmi hata, 2 yapılandırma hatası. Zamanlanmış iş bu kodu görüp kırmızıya döner.

**S: Veriyi iki kez çekersen ne olur?**
C: Çoğalmaz, upsert yapıyorum. Gerçek veritabanında iki kez çalıştırıp satır sayılarının aynı kaldığını doğruladım (87 şehir, 609 tahmin, 8 deprem).

**S: Bağlantı dizesi nerede duruyor?**
C: Sadece ortam değişkeninde. Repoda yok, loglarda da yazdırılmıyor.

## Aşama 3: iklim normali ve anomali

**S: "Normalden sıcak" ne demek, nasıl hesapladın?**
C: 1991-2020 dönemindeki ortalamayı yılın her günü için çıkardım. Bugünün sıcaklığından o günün normalini çıkarınca anomali elde ediyorum. Pozitifse normalden sıcak.

**S: Neden 1991-2020?**
C: Dünya Meteoroloji Örgütü'nün iklim normali için kullandığı standart 30 yıllık dönem.

**S: Tek bir günün 30 yıllık ortalaması yeterli değil mi?**
C: Sadece 30 örnek olur, gürültülü çıkar. Her günün iki yanındaki 7 günü de katıyorum, ~450 örnek oluyor.

**S: 29 Şubat'ı nasıl yönettin?**
C: Gün numarasını her zaman artık yıl olan 2000'e göre hesaplıyorum. Böylece 1 Mart her yıl aynı konumda, 29 Şubat'ın da kendi yeri var.

**S: Veri eksikse?**
C: Null günleri ortalamaya katmıyorum (0 sayarsam ortalama bozulur). Hiç veri yoksa hata veriyorum, sessizce yanlış değer üretmiyorum.

**S: Gerçek API'ye çıkınca karşılaştığın bir sorun oldu mu?**
C: Evet. 87 şehrin 30 yıllık verisini çekerken 16'dan sonra 429 aldım. Open-Meteo limiti istek sayısına değil ağırlığına bakıyor, 30 yıllık sorgu ağır sayılıyor. Sahte HTTP'li testlerim bunu göstermezdi, gerçek çalıştırmada yakaladım. Çözüm: "dakikalık limit" mesajında bekleyip yeniden deneyen bir HTTP işleyicisi; saatlik/günlük limitte beklemiyor, hatayı gösteriyor.

**S: İklim normalini neden ayrı mod olarak çalıştırıyorsun?**
C: 1991-2020 verisi değişmez. Bir kez hesaplayıp saklıyorum, saatlik toplamada tekrar çekmiyorum. Hesaplanmış şehirler atlanıyor; istersem `--force` ile yeniden hesaplarım.

**S: Normali neden dizi olarak, tek satırda sakladın?**
C: 366 değer her zaman birlikte okunuyor ve yazılıyor. PostgreSQL dizi tipini destekliyor, böylece tek sorguyla alıyorum; 366 ayrı satır gereksiz karmaşıklık olurdu.

## Aşama 3: sorgu servisleri

**S: Neden okuma ve yazma için ayrı arayüzler?**
C: Yazma toplayıcının, okuma API'nin işi. Ayırınca API yanlışlıkla veri yazamaz ve her taraf sadece ihtiyacı olan yüzeyi görür. Okuyucularda `AsNoTracking` kullanıyorum, çünkü veri sadece gösterilecek.

**S: URL'de neden "slug"?**
C: `/cities/İstanbul` Türkçe karakter yüzünden tarayıcıda kodlanır ve elle yazılınca sorun çıkarır. ASCII, küçük harfli kimlik (`istanbul`) güvenli ve tahmin edilebilir. Gelen değeri de aynı fonksiyondan geçiriyorum, `İSTANBUL` yazılsa da çalışır.

**S: `ToLower()` yerine neden Türkçe harfleri tek tek eşledin?**
C: `ToLower` kültüre göre farklı davranır. Türk kültüründe `I` küçük `ı` olur, `i` değil. Bu, "Isparta" gibi şehirlerde bile hata üretir. Açık eşleme her makinede aynı sonucu verir; testte `tr-TR` kültürüyle de deniyorum.

**S: "Bugün"ü hangi saat dilimine göre hesaplıyorsun?**
C: Sabit UTC+3. Türkiye yaz saati uygulamıyor, böylece Windows ve Linux'ta saat dilimi adı farkından etkilenmiyorum. Gece 00-03 arası UTC'ye göre hâlâ dünmüş gibi görünürdü, bunu testle koruyorum. KKTC yaz saatinde bir saatlik fark bilinen bir sınır.

**S: Veri yoksa API ne döner?**
C: Şehir yoksa 404. Şehir var ama verisi yoksa boş liste ve boş değerler; sıfır göstermem, çünkü "veri yok" ile "0 derece" farklı şeyler.

## Aşama 3: API

**S: Controller'larda neden iş mantığı yok?**
C: Controller sadece HTTP ile ilgilenir: parametre al, servisi çağır, sonucu JSON yap. İş kuralları Application'da olduğu için HTTP olmadan da test edebiliyorum.

**S: Neden DTO? Domain sınıfını doğrudan döndürsen olmaz mıydı?**
C: Domain sınıfının şekli değişince arayüzün beklediği JSON de kazara değişirdi. DTO API'nin sabit sözleşmesi.

**S: API'yi nasıl test ettin?**
C: `WebApplicationFactory` ile bellek içinde gerçek HTTP istekleri atıyorum, arkada gerçek PostgreSQL (Testcontainers). JSON şeklini, 404/400/405 durumlarını, Türkçe karakterli URL'yi ve yuvarlamayı kontrol ediyorum. Testlerin gerçekten bir şey yakaladığını görmek için bilerek hata soktum (deprem sıralamasını ters çevirdim), bir test kırmızıya döndü.

**S: Hata durumunda API ne döner?**
C: Standart ProblemDetails JSON'u: bilinmeyen şehir 404, geçersiz parametre 400, yanlış yöntem 405. İstisna ayrıntısı dışarı sızmaz.

**S: Neden CORS varsayılan kapalı?**
C: Güvenli varsayılan. Arayüzün adresini yapılandırmaya yazınca yalnızca o adrese ve yalnızca GET'e açılıyor.

**S: Veritabanı şemasını kim yönetiyor?**
C: Toplayıcı migration'ı uyguluyor. API sadece okuyor, yani yanlışlıkla şemayı değiştiremez.

**S: Ekranda `ortalama − normal` ile `anomali` bazen 0,1 farklı çıkabilir, neden?**
C: Üçünü ayrı ayrı bir ondalığa yuvarlıyorum. Hesap tam hassasiyetle yapılıyor, sadece gösterilen değer yuvarlanıyor.

## Aşama 4: harita verisi

**S: Sınır verisini nereden aldın, lisansı ne?**
C: Natural Earth. Kamu malı, atıf zorunlu değil, ama arayüzde yine de belirtiyorum. İlk denediğim küçük sürümde Türkiye illeri yoktu, ayrıntılı (10m) sürümü indirmem gerekti.

**S: Veriyi doğrulamadan kullandın mı?**
C: Hayır. Natural Earth'teki il adlarında yazım hataları var ve Türkçe karakter yok. Adlara güvenmek yerine, kendi şehir listemdeki koordinatın hangi ilin poligonunun içine düştüğüne baktım. 81 ilin 81'i eşleşti; iki kaynağın birbirini doğrulaması oldu. Kıyıdaki iki koordinatın (Tekirdağ, Girne) poligonun birkaç km dışında kaldığını ölçerek gördüm ve belgeledim.

**S: KKTC'nin ilçe sınırları neden haritada yok?**
C: Veri kaynağı KKTC'yi tek poligon olarak veriyor. Sahte sınır uydurmak yerine KKTC'yi tek bölge çizip 6 ilçeyi yan panelde gösteriyorum.

**S: 40 MB'lık dosyayı neden repoya koymadın?**
C: Büyük ve içinde dünyanın geri kalanı var. Bir betikle sadece Türkiye + KKTC'yi ayıklayıp basitleştirdim (109 KB). Betik repoda, çıktı tekrar üretilebilir.

## Aşama 4: harita arayüzü

**S: Hover'da ilin yükselmesini nasıl yaptın?**
C: Üzerine gelinen ilin bir kopyasını, fare olaylarını almayan ayrı bir katmanda çiziyorum ve CSS ile yukarı kaydırıp gölge veriyorum. Asıl yolu DOM'da öne taşısaydım, eleman fare altından kayacağı için mouseleave/mouseenter tetiklenip titreme olabilirdi.

**S: Neden Leaflet değil de d3-geo?**
C: Hover'da yükselme gibi etkileri SVG ve CSS ile tam kontrolle yapmak istedim, ayrıca altlık harita karosu (dış servis, limit, atıf) gerekmedi. Bedeli: yakınlaştırma ve kaydırmayı gerekirse ben yazarım.

**S: Hızlı tıklamada eski yanıtın yenisinin üstüne yazılmasını nasıl önledin?**
C: `useApi` kancası anahtar değişince önceki isteği `AbortController` ile iptal ediyor. İptal edilen istek hata olarak gösterilmiyor.

**S: Veri yoksa harita ne gösteriyor?**
C: Ayrı bir nötr gri ("veri yok"). "Normal" (0) farklı bir renk, ikisi karışmasın diye testle koruyorum.

**S: KKTC'yi nasıl gösterdin?**
C: Kaynak veri KKTC'yi tek poligon veriyor, ben de tek bölge çizdim ve rengini ilçe anomalilerinin ortalaması yaptım. Tıklayınca yan panelde 6 ilçe listeleniyor. Sahte ilçe sınırı uydurmadım.

**S: Arayüzü nasıl doğruladın, neyi doğrulayamadın?**
C: Gerçek API ve gerçek veriyle tarayıcıda hover, tıklama, KKTC akışı ve klavye seçimini denedim, yan panel değerlerini API ile karşılaştırdım. Ekran görüntüsü alamadığım için görsel görünümü gözle doğrulamadım; onu açıkça not ettim.

## Aşama 5a: normaller dosyası

**S: Neden hesaplanmış normalleri repoya koydun?**
C: 30 yıllık veri bir API'nin ağırlık limitine takılıyor, her kurulum 8 dakika sürüyor ve bazen iki ayrı çalıştırma istiyor. Normal değişmeyen bir veri, bir kez hesaplayıp küçük bir dosya olarak tutuyorum. Yeni kurulum dosyadan yükleniyor, API'ye hiç gitmiyor.

**S: Dosyayı neden şehir adıyla değil slug ile anahtarladın?**
C: Slug ASCII ve sabit. Adlarda Türkçe karakter ve yazım farkı riski var (İ/I gibi). Slug kimliği zaten API ve harita da kullanıyor.

**S: Eksik şehirli dosya yazılsaydı ne olurdu?**
C: Sonradan yüklenince bazı iller sessizce renksiz kalırdı. Bu yüzden dışa aktarma, veritabanında eksik şehir varsa dosyayı hiç yazmıyor ve hata veriyor.

**S: Dosya yazarken elektrik/çökme olursa?**
C: Önce geçici dosyaya yazıp tamamlanınca yerine taşıyorum; yarıda kesilirse eski dosya bozulmuyor.

**S: Bunu nasıl test ettin?**
C: Biçim için birim testleri (yuvarlama, sıralama, Türkçe kültür, geçersiz dosyalar) ve gerçek PostgreSQL'le "A veritabanı → dosya → boş B veritabanı" testi.

## Aşama 6: deprem katmanı

**S: Deprem dairelerinin boyutunu nasıl belirledin?**
C: Yarıçap büyüklükle doğrusal artıyor ve alt/üst sınırla kırpılıyor. Küçük depremler görünür kalıyor, çok büyük bir deprem haritayı kaplamıyor. Büyükten küçüğe çiziyorum ki küçük daire büyüğün altında kalıp tıklanamaz olmasın.

**S: Çerçevenin dışına düşen depremleri ne yaptın?**
C: Koordinat kutum komşu bölgeleri de kapsıyor. Haritanın dışına düşenleri çizmiyorum ama saklamıyorum da gizlemiyorum: haritanın altında "N deprem çerçevenin dışında kaldı" yazıyor.

**S: Katman kapalıyken API'ye istek gidiyor mu?**
C: Hayır. Veri çeken kancama anahtar olarak `null` verince hiçbir şey çekmiyor. Ayarlar değişince de önceki istek iptal ediliyor.

**S: Gerçek veriyle denerken ne buldun?**
C: "Son 30 gün" seçeneği yalnızca 7 günlük veri döndürüyordu, çünkü toplayıcı 7 gün çekiyordu. API ile toplayıcının aynı pencereyi kullanması gerektiğini fark edip toplayıcıyı 30 güne çıkardım.

**S: Neden depremler İngilizce yer adıyla?**
C: USGS metni olduğu gibi geliyor ("21 km ESE of Feke, Turkey"). Çevirmek bozuk çıktı riski taşırdı; ham veriyi göstermek ve kaynağı belirtmek daha dürüst.

## Aşama 6b: deprem hover kartı

**S: Depremin ilçesini neden göstermedin?**
C: İlçe sınır verim yok; elimdeki kaynak yalnızca il sınırı veriyor. İlçeyi tahmin etmek yerine USGS'nin yer metninden "en yakın yerleşim" bilgisini gösterdim ve adını "ilçe" değil "en yakın yerleşim" koydum ki yanlış bilgi izlenimi vermesin. İlçe sınırı verisi bulunursa aynı il bulma mantığı ilçeye genişletilebilir.

**S: Bir depremin hangi ilde olduğunu nasıl buldun?**
C: Haritada zaten il çokgenleri var; deprem koordinatının hangi çokgenin içinde olduğuna düz bir nokta-çokgen testiyle bakıyorum. d3-geo'nun küresel kontrolünü kullanmadım, çünkü çokgenin yön sırası yanlışsa tüm dünyayı içeride sayabiliyor.

**S: İç içe daireleri nasıl çözdün?**
C: İki dairenin merkezleri arası uzaklık yarıçaplar toplamından küçükse üst üste biniyor sayıyorum. Hover kartı fare altındaki depremle birlikte bindiği diğerlerini de listeliyor, böylece alttaki gizli kalmıyor.

**S: Gerçek denemede ne yakaladın?**
C: Telefon genişliğinde harita 350x160 piksel, kart ise 270x270+ piksel olduğu için kart kabın dışına taşıyordu. Sığmayan kapta kartı hiç göstermeyen bir kural ekledim; dokunmatikte zaten hover yok, dokununca yan panel açılıyor. Bir de kap boyutunu sadece ResizeObserver'la öğrenirsem gizli sekmede boyut sıfır kalıp kart hiç çıkmıyordu; ilk boyutu doğrudan ölçmeye başladım.

**S: Kart fareyi neden engellemiyor?**
C: `pointer-events: none`. Kart faredeki daireyi örtüp hover'ı bozarsa titreme olur; kart yalnızca bilgi gösteriyor, etkileşimli değil.

## Aşama 6c: deprem kümeleri

**S: İç içe giren depremleri nasıl çözdün?**
C: Üst üste binen daireleri bir küme işaretinde topladım (magenta, içinde sayı). Tıklayınca üyeler merkezin etrafına halka halinde yayılıyor ve her biri kendi gerçek konumuna çizgiyle bağlanıyor. Haritalarda bu desene "spiderfy" deniyor.

**S: Küme sınırını nasıl belirledin?**
C: Daireleri üst üste binenler kümeleniyor; A-B ve B-C üst üste biniyorsa A-C binmese de üçü aynı küme. Bunu union-find (birleştirme-bulma) ile yapıyorum. Küme kimliğini girdi sırasından bağımsız tuttum.

**S: Yayılmada dairelerin birbirine girmemesini nasıl sağladın?**
C: Halka kapasitesini komşular arası kiriş uzunluğundan hesapladım. İlk aklıma gelen yay uzunluğuydu, ama az üyede yay kirişten uzun olduğu için daireler değerdi. Üye çoksa dış halkalar ekleniyor. Testle 2'den 40 üyeye kadar hiçbirinin üst üste binmediğini doğruluyorum.

**S: Küme haritanın kenarındaysa?**
C: Önce yayılmayı döndürüyorum ki hepsi çerçevede kalsın. Köşede tam halka hiçbir açıda sığmıyor; bunu testi yazarken fark ettim, ilk sürüm sadece dönüyordu ve köşe testi başarısız oldu. Bu yüzden son çare olarak tüm yayılmayı çerçevenin içine kaydırıyorum. Çizgiler yine her üyeyi kendi gerçek konumuna bağlıyor.

**S: Kümeyi nasıl kapatıyorsun?**
C: Dört yol: merkezdeki halkaya tıklamak, Esc, başka bir kümeyi açmak ve deprem ayarlarını değiştirmek.

**S: Neden "ölü kod" sildin?**
C: Hover kartındaki "iç içe" listesi kümelemenin yerini aldı ve kullanılmaz oldu. Kullanılmayan fonksiyonu ve testlerini tuttum mu, ileride kafa karıştırırdı.
