# Yol haritası

Durum notları: tamamlanan işler `ARCHITECTURE.md`'de karar olarak, yapılacaklar burada.

## Bir sonraki oturum (öncelik sırasıyla)
1. **Haritayı büyüt (yapıldı, görsel doğrulama bekliyor).** Sayfa 1560 px, yan panel 340 px sabit, harita kalan alanı kaplıyor. Kullanıcı geri bildirimi: harita, yan panelle yan yana olunca çok küçük kalıyor. Fikirler: harita sütununu genişlet / paneli daralt, yan paneli haritanın altına ya da üstüne açılan bir çekmeceye al, SVG yüksekliğini artır, KKTC için büyütülmüş küçük pencere (inset), isteğe bağlı yakınlaştırma. Görsel doğrulama için kullanıcıdan ekran görüntüsü iste (araç ekran görüntüsü alamadı).
2. **İklim normallerini tamamla.** Yerel veritabanında normaller henüz yok (Open-Meteo saatlik limiti). `--normals` bir saat arayla tekrar çalıştırılır. Kalıcı çözüm: hesaplanmış normalleri küçük bir veri dosyası olarak repoya koy (87 şehir × 366 değer), toplayıcı ilk açılışta oradan yüklesin; böylece kimse 30 yıllık veriyi çekmek zorunda kalmaz.
3. **Deprem katmanı** (haritada büyüklüğe göre daireler, açılıp kapanan). API ucu hazır (`/api/earthquakes`).
4. **Geçmişle karşılaştırma** (kullanıcı sorusundan): bir şehir için "bu tarih son 10 yılda nasıldı?" grafiği.
5. **`.env.example`**: izin ayarı yüzünden yazılamadı; kullanıcı oluşturacak, içerik `docker-compose.yml` başlığında ve sohbette.

## Aşama 5: cila
- `docker compose up` ile her şey tek komutla (db + toplayıcı + API + arayüz), Dockerfile'lar.
- GitHub Actions CI (build + testler: .NET ve web).
- README: ölçüm tablosu, mimari diyagramı, "temiz makinede çalıştırma" adımları. Yerel çalıştırma bir kez gerçek kullanıcıyla denendi ve port uyuşmazlığı çıktı (bkz. K-021 ek notu); README adımları temiz kurulumla doğrulanacak.
- Mobil ve koyu mod kontrolü, bileşen testleri.

## Aşama 6: canlıya alma (kartsız yol)
- Veritabanı: Neon, API: Render (Docker), arayüz: Cloudflare Pages. Hesapları kullanıcı açar.
- Toplayıcı zamanlaması: GitHub Actions (K-015). İlk kurulumda normaller için iki ayrı çalıştırma gerekebilir (saatlik limit).

## v2 (sonra)
Hava kalitesi, deniz/dalga, orman yangını; il/ülke bazlı süzme; AFAD (kullanım şartı doğrulanırsa).

## Güncel durum (2026-10-11)
- **Aşama 5 yapıldı:** Docker Compose ile tek komut, GitHub Actions CI (ilk koşu bekliyor), README (ölçüm tablosu dahil).
- **Yeni fikir (ölçümden çıktı):** `/api/map` her istekte 87 şehrin normal dizisini veritabanından okuyor (p50 8,6 ms; diğer uçlar 1-3 ms). Normaller değişmediği için bellekte önbelleğe alınabilir (başlangıçta bir kez yükle).
- **Sırada:** Aşama 6 canlıya alma (Neon + Render + Cloudflare Pages; hesapları kullanıcı açar), geçmişle karşılaştırma grafiği, telefon düzeni, ekran görüntüsü/GIF.
