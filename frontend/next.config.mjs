/** @type {import('next').NextConfig} */
const nextConfig = {
  // Static export: outputs to /out, served by ASP.NET Core as wwwroot.
  // All pages are 'use client' so SSR is not needed.
  output: 'export',
  trailingSlash: false, // generates dashboard.html (not dashboard/index.html)
}

export default nextConfig
