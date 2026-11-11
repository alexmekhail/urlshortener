import type { DailyClickCount } from '@/types'

interface ClicksChartProps {
  data: DailyClickCount[]
  /** Accessible label describing what the chart shows. */
  label: string
}

/**
 * Minimal hand-built SVG bar chart — no charting library dependency.
 * Renders one bar per day in `data`, scaled to the tallest day.
 */
export default function ClicksChart({ data, label }: ClicksChartProps) {
  const width = 640
  const height = 180
  const paddingBottom = 20
  const max = Math.max(1, ...data.map((d) => d.count))
  const barGap = 2
  const barWidth = data.length > 0 ? width / data.length - barGap : 0

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      className="h-40 w-full"
      role="img"
      aria-label={label}
      preserveAspectRatio="none"
    >
      {data.map((d, i) => {
        const barHeight = (d.count / max) * (height - paddingBottom)
        const x = i * (barWidth + barGap)
        const y = height - paddingBottom - barHeight
        return (
          <g key={d.date}>
            <rect
              x={x}
              y={y}
              width={Math.max(barWidth, 1)}
              height={Math.max(barHeight, 1)}
              rx={2}
              fill="url(#clicks-bar-gradient)"
            >
              <title>
                {d.date}: {d.count} click{d.count !== 1 ? 's' : ''}
              </title>
            </rect>
          </g>
        )
      })}
      <defs>
        <linearGradient id="clicks-bar-gradient" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#8b5cf6" />
          <stop offset="100%" stopColor="#6366f1" />
        </linearGradient>
      </defs>
      <line
        x1={0}
        y1={height - paddingBottom}
        x2={width}
        y2={height - paddingBottom}
        stroke="rgba(255,255,255,0.1)"
        strokeWidth={1}
      />
    </svg>
  )
}
