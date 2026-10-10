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
