import { describe, expect, it } from 'vitest'
import {
  MAX_RADIUS,
  MIN_RADIUS,
  describeQuake,
  formatDepth,
  formatMagnitude,
  formatWhen,
  isInView,
  radiusFor,
  sortForDrawing,
} from './earthquakes'
import type { EarthquakeDto } from './types'

function quake(id: string, magnitude: number): EarthquakeDto {
  return { id, occurredAt: '2026-10-06T23:50:36.679+00:00', magnitude, place: 'X', latitude: 38, longitude: 27, depthKm: 10 }
}

describe('radiusFor', () => {
  it('büyüklükle artar', () => {
    expect(radiusFor(4)).toBeGreaterThan(radiusFor(3))
    expect(radiusFor(5)).toBeGreaterThan(radiusFor(4))
  })

  it('alt ve üst sınırla kırpılır', () => {
    expect(radiusFor(0)).toBe(MIN_RADIUS)
    expect(radiusFor(-3)).toBe(MIN_RADIUS)
    expect(radiusFor(9)).toBeCloseTo(17.4, 6) // M9'da henüz sınıra ulaşmaz
    expect(radiusFor(9.5)).toBe(MAX_RADIUS)   // gerçek depremlerin en büyükleri (M9+) sınırda durur
    expect(radiusFor(10)).toBe(MAX_RADIUS)
  })

  it('tipik değerler görünür aralıkta', () => {
    expect(radiusFor(2.5)).toBeGreaterThanOrEqual(MIN_RADIUS)
    expect(radiusFor(7)).toBeLessThan(MAX_RADIUS + 0.0001)
  })
})

describe('sortForDrawing', () => {
  it('büyükten küçüğe sıralar (küçükler üste çizilir)', () => {
    expect(sortForDrawing([quake('a', 3), quake('b', 5), quake('c', 4)]).map((q) => q.id)).toEqual(['b', 'c', 'a'])
  })

  it('girdi dizisini değiştirmez', () => {
    const input = [quake('a', 3), quake('b', 5)]
    sortForDrawing(input)
    expect(input.map((q) => q.id)).toEqual(['a', 'b'])
  })
})

describe('isInView', () => {
  it('çerçeve içindeki ve sınırdaki noktayı kabul eder', () => {
    expect(isInView(10, 10, 960, 440)).toBe(true)
    expect(isInView(0, 0, 960, 440)).toBe(true)
    expect(isInView(960, 440, 960, 440)).toBe(true)
  })

  it('dışındakini reddeder; tolerans payı varsa kıyıdakini kabul eder', () => {
    expect(isInView(-5, 10, 960, 440)).toBe(false)
    expect(isInView(-5, 10, 960, 440, 10)).toBe(true)
    expect(isInView(10, 500, 960, 440, 10)).toBe(false)
  })

  it('NaN ve sonsuzu reddeder (projeksiyon dışı noktalar)', () => {
    expect(isInView(Number.NaN, 5, 960, 440)).toBe(false)
    expect(isInView(5, Number.POSITIVE_INFINITY, 960, 440)).toBe(false)
  })
})

describe('biçimler', () => {
  it('büyüklük virgülle', () => {
    expect(formatMagnitude(4.2)).toBe('4,2')
    expect(formatMagnitude(3)).toBe('3,0')
  })

  it('derinlik yuvarlanır, yoksa bilinmiyor der', () => {
    expect(formatDepth(10)).toBe('10 km derinlik')
    expect(formatDepth(7.6)).toBe('8 km derinlik')
    expect(formatDepth(null)).toBe('Derinlik bilinmiyor')
  })

  it('zaman Türkiye saatiyle (UTC+3) ve gün sınırını aşar', () => {
    // 23:50 UTC = ertesi gün 02:50 TSİ
    expect(formatWhen('2026-10-06T23:50:36.679+00:00')).toBe('7 Eki 2026, 02:50')
    expect(formatWhen('2026-12-31T21:30:00Z')).toBe('1 Oca 2027, 00:30') // yıl sınırı
  })

  it('başlık metni', () => {
    expect(describeQuake({ ...quake('a', 4.2), place: '27 km WSW of Fry, Greece' })).toBe('M 4,2 · 27 km WSW of Fry, Greece')
  })
})
