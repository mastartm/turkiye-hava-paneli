export interface QuakeSettings {
  enabled: boolean
  days: number
  minMagnitude: number
}

export const DEFAULT_QUAKE_SETTINGS: QuakeSettings = { enabled: false, days: 7, minMagnitude: 2.5 }

export const DAY_OPTIONS = [1, 7, 30]
export const MAGNITUDE_OPTIONS = [2.5, 3, 4, 5]
