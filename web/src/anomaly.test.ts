import { describe, expect, it } from 'vitest'
import {
  BUCKETS,
  NO_DATA_COLOR,
  bucketFor,
  colorFor,
  formatAnomaly,
  formatC,
  regionAnomalies,
  regionIdOf,
} from './anomaly'
import type { MapPointDto } from './types'

function point(id: string, country: 'Turkiye' | 'Kktc', anomalyC: number | null): MapPointDto {
  return {
    city: { id, name: id, country, latitude: 39, longitude: 33 },
    minC: null, maxC: null, meanC: null, weatherCode: null, normalC: null, anomalyC,
  }
}

describe('bucketFor / colorFor', () => {
  it('veri yoksa (null ya da NaN) kova yok, renk nötr', () => {
    expect(bucketFor(null)).toBeNull()
    expect(bucketFor(Number.NaN)).toBeNull()
    expect(colorFor(null)).toBe(NO_DATA_COLOR)
  })

  it('sınır değerleri: alt sınır dahil, üst sınır hariç', () => {
    expect(bucketFor(-2)?.label).toBe('−2° ile −0,5°') // -2 üst kovanın değil, bu kovanın alt sınırı
    expect(bucketFor(-2.01)?.label).toBe('−2° ve altı')
    expect(bucketFor(-0.5)?.label).toBe('normal')
    expect(bucketFor(0.5)?.label).toBe('+0,5° ile +2°')
    expect(bucketFor(2)?.label).toBe('+2° ve üstü')
  })

  it('0 "normal" kovasındadır; "veri yok" ile karışmaz', () => {
    expect(bucketFor(0)?.label).toBe('normal')
    expect(colorFor(0)).not.toBe(NO_DATA_COLOR)
  })

  it('uç değerler de bir kovaya düşer', () => {
    expect(bucketFor(-40)).toBe(BUCKETS[0])
    expect(bucketFor(40)).toBe(BUCKETS[BUCKETS.length - 1])
  })

  it('kovalar boşluksuz ve çakışmasız birbirini izler', () => {
    for (let i = 1; i < BUCKETS.length; i++) expect(BUCKETS[i].min).toBe(BUCKETS[i - 1].max)
  })
})

describe('regionAnomalies', () => {
  it('iller kendi kimliğiyle gelir', () => {
    const m = regionAnomalies([point('ankara', 'Turkiye', -1.1), point('mersin', 'Turkiye', 4.7)])

    expect(m.get('ankara')).toBe(-1.1)
    expect(m.get('mersin')).toBe(4.7)
  })

  it('KKTC ilçeleri tek bölgede ortalanır', () => {
    const m = regionAnomalies([point('girne', 'Kktc', 2), point('lefkosa', 'Kktc', 4)])

    expect(m.get('kktc')).toBe(3)
    expect(m.has('girne')).toBe(false) // ilçe ayrı bölge değil
  })

  it('KKTC ortalaması verisi olmayan ilçeleri saymaz', () => {
    const m = regionAnomalies([point('girne', 'Kktc', 2), point('lefke', 'Kktc', null)])

    expect(m.get('kktc')).toBe(2)
  })

  it('KKTC ilçelerinin hiçbirinde veri yoksa bölge null (0 değil)', () => {
    expect(regionAnomalies([point('girne', 'Kktc', null)]).get('kktc')).toBeNull()
    expect(regionAnomalies([]).get('kktc')).toBeNull()
  })

  it('verisiz il null kalır', () => {
    expect(regionAnomalies([point('van', 'Turkiye', null)]).get('van')).toBeNull()
  })
})

describe('regionIdOf', () => {
  const pts = [point('ankara', 'Turkiye', 0), point('girne', 'Kktc', 0)]

  it('il kendi kimliğidir, KKTC ilçesi kktc bölgesidir', () => {
    expect(regionIdOf('ankara', pts)).toBe('ankara')
    expect(regionIdOf('girne', pts)).toBe('kktc')
  })

  it('bilinmeyen kimlik olduğu gibi döner', () => {
    expect(regionIdOf('kktc', pts)).toBe('kktc')
  })
})

describe('Türkçe sayı biçimi', () => {
  it('sıcaklık virgül ve gerçek eksi işaretiyle', () => {
    expect(formatC(15.1)).toBe('15,1°')
    expect(formatC(-7.6)).toBe('−7,6°')
    expect(formatC(null)).toBe('—')
  })

  it('sapma işaretli; sıfıra yuvarlanan sapmada işaret yok', () => {
    expect(formatAnomaly(1.24)).toBe('+1,2°')
    expect(formatAnomaly(-1.07)).toBe('−1,1°')
    expect(formatAnomaly(0.04)).toBe('0,0°')
    expect(formatAnomaly(-0.04)).toBe('0,0°') // "−0,0°" görünmemeli
    expect(formatAnomaly(null)).toBe('—')
  })
})
