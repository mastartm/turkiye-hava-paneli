import { DAY_OPTIONS, MAGNITUDE_OPTIONS, type QuakeSettings } from './quakeSettings'

interface Props {
  settings: QuakeSettings
  onChange: (next: QuakeSettings) => void
  /** Durum satırı: "12 deprem", "Yükleniyor…" ya da hata metni. */
  status: string | null
}

export function QuakeControls({ settings, onChange, status }: Props) {
  return (
    <div className="quake-controls">
      <label className="toggle">
        <input
          type="checkbox"
          checked={settings.enabled}
          onChange={(e) => onChange({ ...settings, enabled: e.target.checked })}
        />
        Depremler
      </label>

      {settings.enabled && (
        <>
          <label>
            Son{' '}
            <select value={settings.days} onChange={(e) => onChange({ ...settings, days: Number(e.target.value) })}>
              {DAY_OPTIONS.map((d) => (
                <option key={d} value={d}>{d} gün</option>
              ))}
            </select>
          </label>
          <label>
            En az{' '}
            <select
              value={settings.minMagnitude}
              onChange={(e) => onChange({ ...settings, minMagnitude: Number(e.target.value) })}
            >
              {MAGNITUDE_OPTIONS.map((m) => (
                <option key={m} value={m}>M {String(m).replace('.', ',')}</option>
              ))}
            </select>
          </label>
          {status && <span className="muted quake-status" role="status">{status}</span>}
        </>
      )}
    </div>
  )
}
