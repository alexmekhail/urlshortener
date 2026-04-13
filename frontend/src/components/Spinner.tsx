interface SpinnerProps {
  /** Tailwind size classes — defaults to h-4 w-4 */
  className?: string
}

/**
 * A lightweight CSS-only spinner.
 * Pass a Tailwind size class to override the default: <Spinner className="h-3 w-3" />
 */
export default function Spinner({ className = 'h-4 w-4' }: SpinnerProps) {
  return (
    <span
      className={`inline-block animate-spin rounded-full border-2 border-current border-t-transparent ${className}`}
      role="status"
      aria-label="Loading"
    />
  )
}
