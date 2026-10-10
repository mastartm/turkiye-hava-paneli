import { describe, expect, it } from 'vitest'
import { shortDay, weatherLabel } from './weather'

describe('weatherLabel', () => {
  it('bilinen WMO kodlarını Türkçeye çevirir', () => {
    expect(weatherLabel(0)).toBe('Açık')
    expect(weatherLabel(2)).toBe('Parçalı bulutlu')
    expect(weatherLabel(3)).toBe('Kapalı')
    expect(weatherLabel(61)).toBe('Yağmurlu')
    expect(weatherLabel(95)).toBe('Gök gürültülü fırtına')
  })

  it('null veri yok, tanımsız kod bilinmiyor der', () => {
    expect(weatherLabel(null)).toBe('Veri yok')
    expect(weatherLabel(4)).toBe('Bilinmiyor')
  })
})

describe('shortDay', () => {
  it('tarihi gün adı ve gün numarasıyla kısaltır', () => {
    expect(shortDay('2026-10-10')).toBe('Cmt 10') // 10 Ekim 2026 Cumartesi
    expect(shortDay('2026-10-11')).toBe('Paz 11')
  })
})
