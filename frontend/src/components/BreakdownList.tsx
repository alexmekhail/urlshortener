import type { CountBreakdown } from '@/types'

interface BreakdownListProps {
  title: string
  items: CountBreakdown[]
  emptyLabel?: string
}

/** Dark-themed horizontal-bar breakdown (referrers, countries, devices, browsers). */
export default function BreakdownList({ title, items, emptyLabel = 'No data yet' }: BreakdownListProps) {
  const max = Math.max(1, ...items.map((i) => i.count))

  return (
    <div className="rounded-xl border border-white/10 bg-zinc-900/60 p-4">
      <h3 className="mb-3 text-xs font-semibold uppercase tracking-wide text-zinc-500">{title}</h3>
      {items.length === 0 ? (
        <p className="text-sm text-zinc-600">{emptyLabel}</p>
      ) : (
        <ul className="space-y-2">
          {items.map((item) => (
            <li key={item.key} className="flex items-center gap-3 text-sm">
              <span className="w-24 shrink-0 truncate text-zinc-300" title={item.key}>
                {item.key}
              </span>
              <span className="h-1.5 flex-1 overflow-hidden rounded-full bg-white/5">
                <span
                  className="block h-full rounded-full bg-zinc-300"
                  style={{ width: `${(item.count / max) * 100}%` }}
                />
              </span>
              <span className="w-8 shrink-0 text-right text-zinc-500">{item.count}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
