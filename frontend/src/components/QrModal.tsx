'use client'

import { useEffect } from 'react'
import QrCode from '@/components/QrCode'

interface QrModalProps {
  value: string
  onClose: () => void
}

export default function QrModal({ value, onClose }: QrModalProps) {
  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [onClose])

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-label="QR code for this link"
      onClick={onClose}
    >
      <div
        className="w-full max-w-xs rounded-2xl border border-white/10 bg-zinc-900 p-5 shadow-glow"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-4 flex items-center justify-between gap-3">
          <p className="truncate font-mono text-sm text-zinc-200">{value}</p>
          <button
            onClick={onClose}
            className="shrink-0 rounded-md p-1 text-zinc-400 transition hover:bg-white/10 hover:text-zinc-100"
            aria-label="Close"
          >
            ✕
          </button>
        </div>
        <QrCode value={value} />
      </div>
    </div>
  )
}
