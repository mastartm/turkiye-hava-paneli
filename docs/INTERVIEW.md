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
