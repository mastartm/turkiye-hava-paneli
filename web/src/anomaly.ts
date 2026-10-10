import { KKTC_REGION_ID, type MapPointDto } from './types'

export interface Bucket {
  /** Alt sınır dahil, üst sınır hariç. */
  min: number
  max: number
  color: string
  label: string
}

// Soğuktan sıcağa tek yönlü, renk körlüğüne dayanıklı mavi → gri → turuncu/kırmızı skala.
export const BUCKETS: readonly Bucket[] = [
  { min: -Infinity, max: -2, color: '#378ADD', label: '−2° ve altı' },
  { min: -2, max: -0.5, color: '#85B7EB', label: '−2° ile −0,5°' },
  { min: -0.5, max: 0.5, color: '#B4B2A9', label: 'normal' },
  { min: 0.5, max: 2, color: '#EF9F27', label: '+0,5° ile +2°' },
  { min: 2, max: Infinity, color: '#D85A30', label: '+2° ve üstü' },
]

export const NO_DATA_COLOR = '#D3D1C7'

/** Anomaliye karşılık gelen kova; veri yoksa null. */
export function bucketFor(anomaly: number | null): Bucket | null {
  if (anomaly === null || Number.isNaN(anomaly)) return null
  return BUCKETS.find((b) => anomaly >= b.min && anomaly < b.max) ?? null
}

export function colorFor(anomaly: number | null): string {
  return bucketFor(anomaly)?.color ?? NO_DATA_COLOR
}

/**
 * Harita bölgesi → anomali. 81 ilin her biri kendi kimliğiyle; KKTC'nin 6 ilçesi ise tek bölge ('kktc')
 * olarak çizildiği için ilçe anomalilerinin ortalaması alınır (verisi olmayanlar hariç).
 */
export function regionAnomalies(points: readonly MapPointDto[]): Map<string, number | null> {
  const result = new Map<string, number | null>()
  const kktc: number[] = []

  for (const p of points) {
    if (p.city.country === 'Kktc') {
      if (p.anomalyC !== null) kktc.push(p.anomalyC)
    } else {
      result.set(p.city.id, p.anomalyC)
    }
  }
  result.set(KKTC_REGION_ID, kktc.length ? kktc.reduce((a, b) => a + b, 0) / kktc.length : null)
  return result
}

/** Bir şehir kimliğinin hangi harita bölgesine ait olduğu: KKTC ilçeleri 'kktc' bölgesidir. */
export function regionIdOf(cityId: string, points: readonly MapPointDto[]): string {
  const p = points.find((x) => x.city.id === cityId)
  return p?.city.country === 'Kktc' ? KKTC_REGION_ID : cityId
}

// --- Türkçe sayı biçimi: toLocaleString'e güvenmiyoruz (tarayıcı/ICU'ya göre değişir), elle ve deterministik ---

export function formatC(v: number | null): string {
  return v === null ? '—' : `${v.toFixed(1).replace('.', ',').replace('-', '−')}°`
}

/** İşaretli sapma: "+1,2°", "−1,1°", "0,0°"; veri yoksa "—". */
export function formatAnomaly(v: number | null): string {
  if (v === null) return '—'
  const rounded = Math.round(v * 10) / 10
  const sign = rounded > 0 ? '+' : rounded < 0 ? '−' : ''
  return `${sign}${Math.abs(rounded).toFixed(1).replace('.', ',')}°`
}
