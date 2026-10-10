# turkiye-hava-paneli

Türkiye ve KKTC için açık veriyle çalışan, katmanlı harita paneli. v1: hava durumu + deprem katmanları, yan panelde grafikler. Portföy projesi; mülakatta sahibi (junior geliştirici) açıklayacak.

## Mimari kurallar
- Backend: .NET 8, katmanlar `Domain` → `Application` → `Infrastructure` → `Api`. Bağımlılıklar içe doğru akar; Domain hiçbir projeye bağlı değildir.
- İş kuralları Domain'de. Controller ince kalır, iş mantığı içermez. Kullanım senaryoları Application'da.
- Harita katmanları "sağlayıcı" arayüzüyle eklenir (hava durumu, deprem...). Yeni katman = yeni sağlayıcı, mevcut kod değişmez.
- Veritabanı: PostgreSQL, EF Core + Npgsql. SQL asla string birleştirmeyle kurulmaz.
- Frontend: React + TypeScript + Vite, harita için Leaflet. Dış API'ye tarayıcı değil backend gider.
- Yeni kütüphane eklemeden önce gerekçesi `docs/ARCHITECTURE.md`'ye yazılır. Gereksiz bağımlılık eklenmez.
- Dış veri kaynakları: Open-Meteo (CC BY 4.0, ticari olmayan kullanım, atıf zorunlu), deprem için USGS/EMSC. MGM ve Kandilli kazıması kullanılmaz. AFAD ancak resmi kullanım şartı doğrulanırsa. Atıf arayüzde görünür olmalı.

## Çalışma kuralları
- Her özellik ayrı commit, anlamlı mesaj. Her aşama ayrı dal + PR. Doğrudan `main`'e commit yok (ilk kurulum hariç).
- Her davranışın testi olur (xUnit). Dış API çağrıları testlerde sahte (mock) olur; CI internete bağlı kalmaz.
- `dotnet build` ve `dotnet test` (ve frontend testleri) yeşil olmadan "bitti" denmez. Doğrulanmayan şey "doğruladım" diye yazılmaz.
- Karmaşık kodda yorum "neden"i anlatır, "ne"yi değil.
- `docs/ARCHITECTURE.md` (karar + gerekçe + reddedilen alternatif) ve `docs/INTERVIEW.md` (mülakat soru-cevapları) her aşamada güncellenir.
- Push, PR, merge işlemlerini kullanıcı yapar; Claude komutları tek tek verir.

## Öğrenme kuralları (kullanıcı için)
- Önemli her karar için kısa "neden böyle" açıklaması.
- Basit Türkçe: kısa cümleler, her terim ilk kullanımda tek cümleyle açıklanır.
- Anlaşılmazsa daha basit anlat, yine olmazsa diyagram. Etkileşimli HTML'den önce kullanıcıya sor.
- Kod yazarken ne yapıldığı söylenir; "kopyala-yapıştır" moduna sokulmaz.

## Güvenlik
- Parola, API anahtarı, bağlantı dizesi sohbete yazılmaz, commit'lenmez. `.env` dosyaları okunmaz; sadece `.env.example` (sahte değerli) repoda durur. `.env` `.gitignore`'dadır.
- `appsettings.json`'da gerçek sır olmaz; ortam değişkeni kullanılır.
- Hesap açma, kart/telefon doğrulaması, parola girme kullanıcıya aittir; Claude sadece adımları anlatır. Kartlı/ücretli servise dayanan çözüm önerilmez.
- Bu klasörün dışındaki hiçbir klasöre (özellikle Obsidian vault'una) dokunulmaz.
