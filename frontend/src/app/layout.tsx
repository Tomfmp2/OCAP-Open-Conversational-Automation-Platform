import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "OCAP Enterprise Control Plane",
  description: "Plataforma empresarial inteligente de gestión de agentes autónomos, canales y flujos de automatización.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es" className="dark h-full">
      <body
        className={`${inter.variable} flex h-full overflow-hidden bg-[var(--background)] text-[var(--foreground)] antialiased`}
      >
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
