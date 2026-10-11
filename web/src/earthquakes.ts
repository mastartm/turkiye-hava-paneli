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

// ---------------- Hover kartı: iç içe depremler, yerleşim, yer metni ----------------

export interface PlacedQuake {
  quake: EarthquakeDto
  /** SVG koordinatı (viewBox birimi). */
  x: number
  y: number
  r: number
}

/** USGS "21 km ESE of Feke, Turkey" metninin ayrıştırılmış hali; ayrıştırılamazsa null. */
export interface ParsedPlace {
  distanceKm: number
  direction: string
  settlement: string
}

export function parsePlace(place: string): ParsedPlace | null {
  const m = /^(\d+(?:\.\d+)?) km ([NSEW]{1,3}) of (.+)$/.exec(place.trim())
  return m ? { distanceKm: Number(m[1]), direction: m[2], settlement: m[3] } : null
}

const YONLER: Record<string, string> = {
  N: 'kuzey', NNE: 'kuzey-kuzeydoğu', NE: 'kuzeydoğu', ENE: 'doğu-kuzeydoğu',
  E: 'doğu', ESE: 'doğu-güneydoğu', SE: 'güneydoğu', SSE: 'güney-güneydoğu',
  S: 'güney', SSW: 'güney-güneybatı', SW: 'güneybatı', WSW: 'batı-güneybatı',
  W: 'batı', WNW: 'batı-kuzeybatı', NW: 'kuzeybatı', NNW: 'kuzey-kuzeybatı',
}

/** "ESE" → "doğu-güneydoğu". Bilinmeyen kısaltma olduğu gibi döner (sessizce yanlış çeviri yapmaz). */
export function directionName(code: string): string {
  return YONLER[code] ?? code
}

// ---------------- Kart yerleşimi ----------------

export const CARD_WIDTH = 270

/** İç içe depremler kartta en çok bu kadar satır olarak listelenir; fazlası "+N daha" olur. */
export const MAX_LISTED = 6

/** Kartın en az yüksekliği (px): sabit satırlar + iç içe deprem listesi (en çok 6 satır görünür). */
export function cardMinHeight(groupSize: number): number {
  const base = 176
  if (groupSize <= 1) return base
  return base + 30 + Math.min(groupSize, MAX_LISTED) * 20 + (groupSize > MAX_LISTED ? 20 : 0)
}

export interface CardPlacement {
  left: number
  top: number
  side: 'right' | 'left' | 'below' | 'above'
  /** Kılavuz çizgisinin karta bağlandığı nokta (px, kabın sol üstüne göre). */
  anchorX: number
  anchorY: number
}

const clamp = (v: number, lo: number, hi: number) => Math.min(Math.max(v, lo), Math.max(lo, hi))

/**
 * Kartı noktanın yanına koyar: önce sağa, sığmazsa sola, o da olmazsa altına/üstüne (dar ekran).
 * Kart her zaman kabın içinde kalır. Tüm değerler piksel, kabın sol üst köşesine göre.
 */
export function placeCard(
  point: { x: number; y: number },
  radius: number,
  container: { width: number; height: number },
  card: { width: number; height: number },
  gap = 20,
): CardPlacement {
  const top = clamp(point.y - 24, 0, container.height - card.height)
  const sideAnchorY = clamp(point.y, top + 10, top + card.height - 10)

  const rightLeft = point.x + radius + gap
  if (rightLeft + card.width <= container.width) {
    return { left: rightLeft, top, side: 'right', anchorX: rightLeft, anchorY: sideAnchorY }
  }

  const leftLeft = point.x - radius - gap - card.width
  if (leftLeft >= 0) {
    return { left: leftLeft, top, side: 'left', anchorX: leftLeft + card.width, anchorY: sideAnchorY }
  }

  // Yana sığmıyor (dar ekran): altına ya da üstüne, yatayda kabın içine kenetlenerek.
  const left = clamp(point.x - card.width / 2, 0, container.width - card.width)
  const anchorX = clamp(point.x, left + 10, left + card.width - 10)
  const belowTop = point.y + radius + gap
  if (belowTop + card.height <= container.height) {
    return { left, top: belowTop, side: 'below', anchorX, anchorY: belowTop }
  }
  const aboveTop = Math.max(0, point.y - radius - gap - card.height)
  return { left, top: aboveTop, side: 'above', anchorX, anchorY: aboveTop + card.height }
}

/**
 * Kart kabın içine sığabilir mi? Dar ve kısa kaplarda (telefon: harita ~350x160 px) 270x270+ px'lik kart hiçbir
 * konumda sığmaz ve taşardı; o durumda kart gösterilmez. Dokunmatik ekranda zaten hover yoktur,
 * dokununca yan panel açılır ve aynı ayrıntıyı verir.
 */
export function cardFits(container: { width: number; height: number }, card: { width: number; height: number }): boolean {
  return container.width >= card.width + 40 && container.height >= card.height
}
