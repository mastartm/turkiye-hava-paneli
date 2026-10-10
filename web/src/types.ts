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

export interface CityForecastDto {
  city: CityDto
  days: DayDto[]
}

export interface SourceDto {
  name: string
  url: string
  license: string
  note: string
}

/** Haritadaki bölge kimliği: 81 il + 'kktc' (6 ilçe tek bölge olarak çizilir). */
export const KKTC_REGION_ID = 'kktc'
