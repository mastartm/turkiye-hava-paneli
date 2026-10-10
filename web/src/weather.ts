/** WMO hava kodu → Türkçe kısa açıklama (Open-Meteo `weather_code`). Bilinmeyen kod için nötr metin. */
export function weatherLabel(code: number | null): string {
  if (code === null) return 'Veri yok'
  if (code === 0) return 'Açık'
  if (code <= 2) return 'Parçalı bulutlu'
  if (code === 3) return 'Kapalı'
  if (code === 45 || code === 48) return 'Sisli'
  if (code >= 51 && code <= 57) return 'Çisenti'
  if (code >= 61 && code <= 67) return 'Yağmurlu'
  if (code >= 71 && code <= 77) return 'Karlı'
  if (code >= 80 && code <= 82) return 'Sağanak'
  if (code === 85 || code === 86) return 'Kar sağanağı'
  if (code >= 95 && code <= 99) return 'Gök gürültülü fırtına'
  return 'Bilinmiyor'
}

const GUNLER = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt']

/** "2026-10-10" → "Cum 10". Saat dilimi kaymasın diye tarihi UTC olarak ayrıştırırız. */
export function shortDay(isoDate: string): string {
  const [y, m, d] = isoDate.split('-').map(Number)
  const dow = new Date(Date.UTC(y, m - 1, d)).getUTCDay()
  return `${GUNLER[dow]} ${d}`
}
