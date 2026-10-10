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
