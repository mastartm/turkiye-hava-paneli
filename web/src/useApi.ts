import { useEffect, useState } from 'react'

export interface ApiState<T> {
  data: T | null
  error: string | null
  loading: boolean
}

/**
 * Bir API çağrısını yürütür. `key` değişince önceki istek iptal edilir (hızlı art arda tıklamada
 * eski yanıtın yenisinin üstüne yazmaması için), yenisi başlar.
 */
export function useApi<T>(load: (signal: AbortSignal) => Promise<T>, key: string): ApiState<T> {
  const [state, setState] = useState<ApiState<T>>({ data: null, error: null, loading: true })

  useEffect(() => {
    const controller = new AbortController()
    setState({ data: null, error: null, loading: true })

    load(controller.signal)
      .then((data) => setState({ data, error: null, loading: false }))
      .catch((e: unknown) => {
        if (controller.signal.aborted) return // iptal hata değildir
        setState({ data: null, error: e instanceof Error ? e.message : 'Bir hata oluştu.', loading: false })
      })

    return () => controller.abort()
    // `load` her render'da yeniden oluşabilir; yeniden yükleme yalnızca `key` ile tetiklenir.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key])

  return state
}
