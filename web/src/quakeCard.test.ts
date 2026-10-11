import type { FeatureCollection } from 'geojson'
import { describe, expect, it } from 'vitest'
import {
  CARD_WIDTH,
  MAX_LISTED,
  cardFits,
  cardMinHeight,
  directionName,
  parsePlace,
  placeCard,
} from './earthquakes'
import { inGeometry, regionNameAt } from './location'
import type { RegionProps } from './TurkeyMap'

describe('parsePlace / directionName', () => {
  it('USGS yer metnini ayrıştırır', () => {
    expect(parsePlace('21 km ESE of Feke, Turkey')).toEqual({ distanceKm: 21, direction: 'ESE', settlement: 'Feke, Turkey' })
    expect(parsePlace('7.5 km N of Hisarcik, Turkey')).toEqual({ distanceKm: 7.5, direction: 'N', settlement: 'Hisarcik, Turkey' })
  })

  it('beklenen biçimde olmayan metin için null (uydurma yapmaz)', () => {
    expect(parsePlace('Aegean Sea')).toBeNull()
    expect(parsePlace('')).toBeNull()
    expect(parsePlace('21 km XYZ of Somewhere')).toBeNull()
  })

  it('yön kısaltmalarını Türkçeye çevirir, bilinmeyeni olduğu gibi bırakır', () => {
    expect(directionName('ESE')).toBe('doğu-güneydoğu')
    expect(directionName('N')).toBe('kuzey')
    expect(directionName('WNW')).toBe('batı-kuzeybatı')
    expect(directionName('ABC')).toBe('ABC')
  })

  it('16 yönün hepsi tanımlı', () => {
    const codes = ['N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE', 'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW']
    for (const c of codes) expect(directionName(c)).not.toBe(c)
  })
})

describe('placeCard', () => {
  const container = { width: 1000, height: 500 }
  const card = { width: CARD_WIDTH, height: 200 }

  it('yer varsa noktanın sağına koyar ve çizgiyi kartın sol kenarına bağlar', () => {
    const p = placeCard({ x: 200, y: 250 }, 8, container, card)

    expect(p.side).toBe('right')
    expect(p.left).toBe(200 + 8 + 20)
    expect(p.anchorX).toBe(p.left)
    expect(p.anchorY).toBeGreaterThanOrEqual(p.top)
    expect(p.anchorY).toBeLessThanOrEqual(p.top + card.height)
  })

  it('sağa sığmıyorsa sola çevirir ve çizgiyi kartın sağ kenarına bağlar', () => {
    const p = placeCard({ x: 900, y: 250 }, 8, container, card)

    expect(p.side).toBe('left')
    expect(p.anchorX).toBe(p.left + CARD_WIDTH)
    expect(p.left).toBeGreaterThanOrEqual(0)
  })

  it('kartı her zaman kabın içinde tutar (alt ve üst kenar)', () => {
    const bottom = placeCard({ x: 200, y: 495 }, 8, container, card)
    const topEdge = placeCard({ x: 200, y: 2 }, 8, container, card)

    expect(bottom.top + card.height).toBeLessThanOrEqual(container.height)
    expect(topEdge.top).toBeGreaterThanOrEqual(0)
  })

  it('dar ekranda (yana sığmıyorsa) altına koyar ve yatayda kabın içinde kalır', () => {
    const narrow = { width: 320, height: 600 }
    const p = placeCard({ x: 160, y: 100 }, 8, narrow, card)

    expect(p.side).toBe('below')
    expect(p.left).toBeGreaterThanOrEqual(0)
    expect(p.left + CARD_WIDTH).toBeLessThanOrEqual(narrow.width)
    expect(p.top).toBeGreaterThan(100)
  })

  it('dar ekranda altta yer yoksa üstüne koyar', () => {
    const narrow = { width: 320, height: 300 }
    const p = placeCard({ x: 160, y: 280 }, 8, narrow, card)

    expect(p.side).toBe('above')
    expect(p.top).toBeGreaterThanOrEqual(0)
    expect(p.top + card.height).toBeLessThanOrEqual(280)
  })
})

describe('cardMinHeight', () => {
  it('tek depremde sabit, iç içe depremde listeye göre artar', () => {
    expect(cardMinHeight(1)).toBeLessThan(cardMinHeight(2))
    expect(cardMinHeight(3)).toBeGreaterThan(cardMinHeight(2))
  })

  it('görünen satır sayısı sınırlı: 6 ile 50 arasındaki fark yalnızca "+N daha" satırı', () => {
    expect(cardMinHeight(50) - cardMinHeight(MAX_LISTED)).toBe(20)
  })
})

describe('regionNameAt (planar nokta-çokgen)', () => {
  // 0..10 x 0..10 kare "Ankara"; içinde 4..6 delik; sağda 20..30 kare "KKTC".
  const square = (x0: number, y0: number, x1: number, y1: number) => [[x0, y0], [x1, y0], [x1, y1], [x0, y1], [x0, y0]]
  const regions: FeatureCollection<import('geojson').Geometry, RegionProps> = {
    type: 'FeatureCollection',
    features: [
      { type: 'Feature', properties: { id: 'ankara', name: 'Ankara', country: 'Turkiye' }, geometry: { type: 'Polygon', coordinates: [square(0, 0, 10, 10), square(4, 4, 6, 6)] } },
      { type: 'Feature', properties: { id: 'kktc', name: 'KKTC', country: 'Kktc' }, geometry: { type: 'MultiPolygon', coordinates: [[square(20, 0, 30, 10)]] } },
    ],
  }

  it('içindeki noktanın bölge adını döner', () => {
    expect(regionNameAt(regions, 2, 2)).toBe('Ankara')
    expect(regionNameAt(regions, 25, 5)).toBe('KKTC')
  })

  it('delikteki nokta o bölgenin değildir', () => {
    expect(regionNameAt(regions, 5, 5)).toBeNull()
  })

  it('deniz/komşu ülke (hiçbir bölgenin içinde değil) için null', () => {
    expect(regionNameAt(regions, 15, 5)).toBeNull()
    expect(regionNameAt(regions, -50, 80)).toBeNull()
  })

  it('nokta/çizgi geometrisi bölge sayılmaz', () => {
    expect(inGeometry(0, 0, { type: 'Point', coordinates: [0, 0] })).toBe(false)
  })
})

describe('cardFits', () => {
  const card = { width: CARD_WIDTH, height: 266 }

  it('geniş masaüstü kabına sığar', () => {
    expect(cardFits({ width: 1002, height: 459 }, card)).toBe(true)
  })

  it('telefon genişliğindeki küçük kabı (348x160) reddeder: kart taşardı', () => {
    expect(cardFits({ width: 348, height: 160 }, card)).toBe(false)
  })

  it('yeterince geniş ama kısa kapta da reddeder', () => {
    expect(cardFits({ width: 1000, height: 200 }, card)).toBe(false)
  })

  it('kart tam sığıyorsa kabul eder (yatayda 40 px pay ile)', () => {
    expect(cardFits({ width: CARD_WIDTH + 40, height: 266 }, card)).toBe(true)
    expect(cardFits({ width: CARD_WIDTH + 39, height: 266 }, card)).toBe(false)
  })
})
