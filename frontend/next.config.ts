import type { NextConfig } from "next";

// Cabeceras de seguridad de todas las respuestas; la CSP con nonce la pone src/proxy.ts.
const securityHeaders = [
  { key: "X-Frame-Options", value: "DENY" },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=(), payment=()" },
];

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  async headers() {
    return [{ source: "/(.*)", headers: securityHeaders }];
  },
  // Workflows quedan fuera de la v1: 307 antes de renderizar (con render por petición, redirect() dentro de la
  // página llegaba a mitad del streaming y dependía de que el cliente lo ejecutara).
  async redirects() {
    return [{ source: "/workflows/:path*", destination: "/", permanent: false }];
  },
  async rewrites() {
    // Evaluado en build-time. En Docker se inyecta API_INTERNAL_URL=http://ocap-api:5000.
    // En local (dev) cae a localhost:5229. Detrás de Nginx, /api también se proxifica allí.
    const backendUrl =
      process.env.API_INTERNAL_URL ||
      process.env.NEXT_PUBLIC_API_URL ||
      "http://localhost:5229";

    return [
      {
        source: "/api/:path*",
        destination: `${backendUrl.replace(/\/$/, "")}/api/:path*`,
      },
      {
        source: "/hubs/:path*",
        destination: `${backendUrl.replace(/\/$/, "")}/hubs/:path*`,
      },
    ];
  },
};

export default nextConfig;
