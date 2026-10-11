/**
 * WMO hava kodu → Türkçe ad (Open-Meteo `weather_code`). Her kod kendi adını taşır: "hafif sağanak" ile
 * "şiddetli sağanak" aynı etiketle gösterilirse kullanıcı yanılır. Bilinmeyen kod için nötr metin.
 */
const KODLAR: Record<number, string> = {
  0: 'Açık',
  1: 'Çoğunlukla açık',
  2: 'Parçalı bulutlu',
  3: 'Kapalı',
  45: 'Sisli',
  48: 'Kırağılı sis',
  51: 'Hafif çisenti',
  53: 'Çisenti',
  55: 'Yoğun çisenti',
  56: 'Hafif donan çisenti',
  57: 'Yoğun donan çisenti',
  61: 'Hafif yağmur',
  63: 'Yağmur',
  65: 'Şiddetli yağmur',
  66: 'Hafif donan yağmur',
  67: 'Şiddetli donan yağmur',
  71: 'Hafif kar',
  73: 'Kar',
  75: 'Yoğun kar',
  77: 'Kar taneleri',
  80: 'Hafif sağanak',
  81: 'Sağanak',
  82: 'Şiddetli sağanak',
  85: 'Hafif kar sağanağı',
  86: 'Şiddetli kar sağanağı',
  95: 'Gök gürültülü fırtına',
  96: 'Hafif dolulu fırtına',
  99: 'Şiddetli dolulu fırtına',
}

export function weatherLabel(code: number | null): string {
  if (code === null) return 'Veri yok'
  return KODLAR[code] ?? 'Bilinmiyor'
}

/** Yağış (yağmur, kar, sağanak, fırtına...) içeren kod mu? 0-3 ve sis kodları yağış değildir. */
export function isPrecipitationCode(code: number | null): boolean {
  return code !== null && ((code >= 51 && code <= 67) || (code >= 71 && code <= 77) || (code >= 80 && code <= 86) || code >= 95)
}

const fmtMm = (mm: number) => mm.toFixed(1).replace('.', ',')

/**
 * Günlük özet metni. Open-Meteo'nun günlük hava kodu, günün EN KÖTÜ saatinin kodudur: 11:00'de 0,2 mm'lik
 * kısa bir sağanak, tüm günü "sağanak" gösterirdi. Yağış toplamı 1 mm'nin altındaysa bunu açıkça "kısa süreli" diye
 * yazar ve miktarı gösteririz; kullanıcı gün boyu yağmur beklemesin.
 */
export function dailySummary(code: number | null, precipitationMm: number | null): string {
  const label = weatherLabel(code)
  if (!isPrecipitationCode(code) || precipitationMm === null) return label
  if (precipitationMm < 1) return `Gün içinde kısa süreli ${label.charAt(0).toLowerCase()}${label.slice(1)} (${fmtMm(precipitationMm)} mm)`
  return `${label} (${fmtMm(precipitationMm)} mm)`
}

const GUNLER = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt']

/** "2026-10-10" → "Cmt 10". Saat dilimi kaymasın diye tarihi UTC olarak ayrıştırırız. */
export function shortDay(isoDate: string): string {
  const [y, m, d] = isoDate.split('-').map(Number)
  const dow = new Date(Date.UTC(y, m - 1, d)).getUTCDay()
  return `${GUNLER[dow]} ${d}`
}

/** "13:03" biçiminde Türkiye saati (UTC+3, yaz saati yok; tarayıcının saat dilimine bağlı değil). */
export function formatClock(isoUtc: string): string {
  const d = new Date(new Date(isoUtc).getTime() + 3 * 3600_000)
  return `${String(d.getUTCHours()).padStart(2, '0')}:${String(d.getUTCMinutes()).padStart(2, '0')}`
}

/** "az önce", "12 dk önce", "2 saat önce", "3 gün önce". Gelecekteki zaman (saat farkı) "az önce" sayılır. */
export function formatAge(isoUtc: string, nowMs: number): string {
  const minutes = Math.floor((nowMs - new Date(isoUtc).getTime()) / 60_000)
  if (!Number.isFinite(minutes) || minutes < 1) return 'az önce'
  if (minutes < 60) return `${minutes} dk önce`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} saat önce`
  return `${Math.floor(hours / 24)} gün önce`
}

/** Veri bu kadar saatten eskiyse kullanıcıya "eski olabilir" uyarısı gösterilir. */
export function isStale(isoUtc: string, nowMs: number, hours = 3): boolean {
  return nowMs - new Date(isoUtc).getTime() > hours * 3600_000
}
