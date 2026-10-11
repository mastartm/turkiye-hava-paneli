import { useEffect, useRef, useState } from 'react'

export interface ElementSize {
  width: number
  height: number
}

/**
 * Bir öğenin güncel piksel boyutunu izler (pencere boyutu ya da yerleşim değişince güncellenir).
 * Harita SVG'si ekrana göre ölçeklenir; HTML kartı ve kılavuz çizgisi piksel olarak yerleştirilir,
 * bu yüzden ölçeği (kap genişliği / viewBox genişliği) bilmemiz gerekir.
 */
export function useElementSize<T extends HTMLElement>() {
  const ref = useRef<T | null>(null)
  const [size, setSize] = useState<ElementSize>({ width: 0, height: 0 })

  useEffect(() => {
    const el = ref.current
    if (!el) return

    // İlk boyutu hemen, doğrudan ölç: gözlemci yalnızca çizim adımında tetiklenir; sekme gizliyse/arka plandaysa
    // ya da ilk çizim gecikirse boyut sıfır kalır ve boyuta bağlı arayüz (kart) hiç görünmezdi.
    setSize({ width: el.offsetWidth, height: el.offsetHeight })

    const observer = new ResizeObserver(([entry]) => {
      setSize({ width: entry.contentRect.width, height: entry.contentRect.height })
    })
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  return [ref, size] as const
}
