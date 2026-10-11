import type { EarthquakeDto } from './types'

export const MIN_RADIUS = 3
export const MAX_RADIUS = 18

/**
 * Büyüklüğe göre daire yarıçapı (SVG birimi). Yarıçap büyüklükle doğrusal artar, alt/üst sınırlıdır:
 * küçük depremler görünür kalır, çok büyük bir deprem haritayı kaplamaz.
 */
export function radiusFor(magnitude: number): number {
  const r = 2 + (magnitude - 2) * 2.2
  return Math.min(MAX_RADIUS, Math.max(MIN_RADIUS, r))
}

/** Büyükten küçüğe: küçük daireler en son (üste) çizilir, büyüğün altında kalıp tıklanamaz olmaz. */
export function sortForDrawing(quakes: readonly EarthquakeDto[]): EarthquakeDto[] {
  return [...quakes].sort((a, b) => b.magnitude - a.magnitude)
}

/** Nokta, çerçevenin (margin kadar tolerans payıyla) içinde mi? Çerçeve dışına taşan komşu ülke depremleri çizilmez. */
export function isInView(x: number, y: number, width: number, height: number, margin = 0): boolean {
  return Number.isFinite(x) && Number.isFinite(y) && x >= -margin && y >= -margin && x <= width + margin && y <= height + margin
}

export function formatMagnitude(magnitude: number): string {
  return magnitude.toFixed(1).replace('.', ',')
}

export function formatDepth(depthKm: number | null): string {
  return depthKm === null ? 'Derinlik bilinmiyor' : `${Math.round(depthKm)} km derinlik`
}

const AYLAR = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara']

/**
 * "7 Eki 2026, 02:50" biçiminde Türkiye saatiyle (UTC+3, yaz saati yok). Tarayıcının yerel saat dilimine
 * bağlı olmasın diye elle hesaplanır: aynı deprem herkese aynı saatte görünür.
 */
export function formatWhen(isoUtc: string): string {
  const d = new Date(new Date(isoUtc).getTime() + 3 * 3600_000)
  const hh = String(d.getUTCHours()).padStart(2, '0')
  const mm = String(d.getUTCMinutes()).padStart(2, '0')
  return `${d.getUTCDate()} ${AYLAR[d.getUTCMonth()]} ${d.getUTCFullYear()}, ${hh}:${mm}`
}

/** Haritanın üstündeki başlık metni: "M 4,2 · 27 km WSW of Fry, Greece". */
export function describeQuake(q: EarthquakeDto): string {
  return `M ${formatMagnitude(q.magnitude)} · ${q.place}`
}
