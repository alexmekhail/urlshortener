'use client'

import { useRef } from 'react'
import { QRCodeCanvas, QRCodeSVG } from 'qrcode.react'

interface QrCodeProps {
  value: string
  size?: number
}

function triggerDownload(href: string, filename: string) {
  const a = document.createElement('a')
  a.href = href
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
}

/**
 * Renders a QR code for `value` with PNG/SVG download buttons.
 * Dark modules on a light quiet zone (via marginSize) keep it scannable on the dark theme.
 */
export default function QrCode({ value, size = 160 }: QrCodeProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const svgRef = useRef<SVGSVGElement>(null)

  function downloadPng() {
    const canvas = canvasRef.current
    if (!canvas) return
    triggerDownload(canvas.toDataURL('image/png'), 'urly-qr-code.png')
  }

  function downloadSvg() {
    const svg = svgRef.current
    if (!svg) return
    const source = new XMLSerializer().serializeToString(svg)
    const blob = new Blob([source], { type: 'image/svg+xml' })
    const url = URL.createObjectURL(blob)
    triggerDownload(url, 'urly-qr-code.svg')
    URL.revokeObjectURL(url)
  }

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="rounded-xl bg-white p-3">
        <QRCodeCanvas
          ref={canvasRef}
          value={value}
          size={size}
          bgColor="#ffffff"
          fgColor="#18181b"
          level="M"
          marginSize={2}
        />
      </div>

      {/* Same QR rendered as SVG, kept off-screen — used only as the SVG download source. */}
      <div className="hidden" aria-hidden="true">
        <QRCodeSVG
          ref={svgRef}
          value={value}
          size={size}
          bgColor="#ffffff"
          fgColor="#18181b"
          level="M"
          marginSize={2}
        />
      </div>

      <div className="flex gap-2">
        <button
          type="button"
          onClick={downloadPng}
          className="rounded-lg border border-white/10 bg-white/5 px-3 py-1.5 text-xs font-semibold
            text-zinc-200 shadow-sm transition hover:bg-white/10 active:scale-95"
        >
          Download PNG
        </button>
        <button
          type="button"
          onClick={downloadSvg}
          className="rounded-lg border border-white/10 bg-white/5 px-3 py-1.5 text-xs font-semibold
            text-zinc-200 shadow-sm transition hover:bg-white/10 active:scale-95"
        >
          Download SVG
        </button>
      </div>
    </div>
  )
}
