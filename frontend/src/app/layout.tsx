import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import './globals.css'
import Header from '@/components/Header'

const inter = Inter({ subsets: ['latin'] })

export const metadata: Metadata = {
  title: 'URLy — URL Shortener',
  description: 'Shorten any link instantly. No account required.',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className="dark">
      <body
        className={`${inter.className} min-h-screen bg-zinc-950 text-zinc-100 antialiased`}
      >
        {/* Decorative ambient glow — purely visual, no semantic content */}
        <div
          aria-hidden="true"
          className="pointer-events-none fixed inset-0 overflow-hidden"
        >
          <div className="absolute left-1/2 top-[-10%] h-[480px] w-[480px] -translate-x-1/2 rounded-full bg-indigo-600/20 blur-[120px]" />
          <div className="absolute bottom-[-15%] right-[-10%] h-[420px] w-[420px] rounded-full bg-violet-600/15 blur-[120px]" />
        </div>

        <div className="relative flex min-h-screen flex-col">
          <Header />
          <main className="flex-1">{children}</main>
        </div>
      </body>
    </html>
  )
}
