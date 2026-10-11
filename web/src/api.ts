import type { CityForecastDto, EarthquakeDto, MapDto, SourceDto } from './types'

// Geliştirmede boş: istekler Vite vekili üzerinden gider (vite.config.ts). Canlıda API'nin adresi (build zamanı ayarı).
const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? ''

export class ApiError extends Error {
  readonly status: number

  constructor(status: number) {
    // Kullanıcıya gösterilecek metin: ham HTTP/istisna ayrıntısı sızdırılmaz.
    super(status === 404 ? 'Şehir bulunamadı.' : 'Veriler şu an alınamadı. Biraz sonra yeniden deneyin.')
    this.status = status
  }
}

async function get<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${BASE}${path}`, { signal, headers: { Accept: 'application/json' } })
  if (!response.ok) throw new ApiError(response.status)
  return (await response.json()) as T
}

export const fetchMap = (signal?: AbortSignal) => get<MapDto>('/api/map', signal)

export const fetchForecast = (cityId: string, signal?: AbortSignal) =>
  get<CityForecastDto>(`/api/cities/${encodeURIComponent(cityId)}/forecast`, signal)

export const fetchSources = (signal?: AbortSignal) => get<SourceDto[]>('/api/sources', signal)

export const fetchEarthquakes = (days: number, minMagnitude: number, signal?: AbortSignal) =>
  get<EarthquakeDto[]>(`/api/earthquakes?days=${days}&minMagnitude=${minMagnitude}`, signal)
