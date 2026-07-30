import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
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
