import type { Metadata } from "next";
import { connection } from "next/server";
import { IBM_Plex_Sans, IBM_Plex_Mono } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";

const plexSans = IBM_Plex_Sans({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-sans",
});

const plexMono = IBM_Plex_Mono({
  subsets: ["latin"],
  weight: ["400", "500"],
  variable: "--font-mono",
});

export const metadata: Metadata = {
  title: "OCAP",
  description:
    "Plataforma de automatización conversacional con agente principal, canales e IA.",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  // Render por petición: la CSP usa un nonce nuevo en cada una (src/proxy.ts) y una página
  // prerenderizada tendría scripts sin nonce que el navegador bloquearía.
  await connection();
  return (
    <html lang="es" className="h-full">
      <body
        className={`${plexSans.variable} ${plexMono.variable} flex h-full overflow-hidden bg-[var(--background)] text-[var(--foreground)] antialiased`}
      >
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
