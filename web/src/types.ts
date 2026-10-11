// API'nin (src/HavaPaneli.Api/Dtos.cs) döndürdüğü şekiller. Null: "veri yok" (0 değil).

export type CountryName = 'Turkiye' | 'Kktc'

export interface CityDto {
  id: string
  name: string
  country: CountryName
  latitude: number
  longitude: number
}

export interface MapPointDto {
  city: CityDto
  minC: number | null
  maxC: number | null
  meanC: number | null
  weatherCode: number | null
  normalC: number | null
  anomalyC: number | null
}

export interface MapDto {
  date: string
  points: MapPointDto[]
}

export interface DayDto {
  date: string
  minC: number | null
  maxC: number | null
  meanC: number | null
  precipitationMm: number | null
  weatherCode: number | null
  normalC: number | null
  anomalyC: number | null
}

/** Şu anki durum (Open-Meteo `current`, ~15 dk'da bir). Günlük ortalamadan ayrı bir değerdir. */
export interface CurrentDto {
  observedAt: string
  temperatureC: number | null
  weatherCode: number | null
  precipitationMm: number | null
}

export interface CityForecastDto {
  city: CityDto
  days: DayDto[]
  /** Hiç çekilmediyse null. */
  current: CurrentDto | null
  /** Verinin bizim tarafımızdan en son çekildiği an (ISO, UTC); "veri ne kadar eski" göstermek için. */
  updatedAt: string | null
}

export interface SourceDto {
  name: string
  url: string
  license: string
  note: string
}

/** Haritadaki bölge kimliği: 81 il + 'kktc' (6 ilçe tek bölge olarak çizilir). */
export const KKTC_REGION_ID = 'kktc'

export interface EarthquakeDto {
  id: string
  /** ISO 8601, UTC ofsetli ("2026-10-06T23:50:36.679+00:00"). */
  occurredAt: string
  magnitude: number
  /** USGS'nin İngilizce yer metni ("27 km WSW of Fry, Greece"); çevrilmeden gösterilir. */
  place: string
  latitude: number
  longitude: number
  depthKm: number | null
}
