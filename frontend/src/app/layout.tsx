import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { PrimaryRail } from "@/shared/components/navigation/PrimaryRail";
import { SecondarySidebar } from "@/shared/components/navigation/SecondarySidebar";
import { Topbar } from "@/shared/components/navigation/Topbar";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "OCAP Enterprise Dashboard — Platform v1.6.0",
  description: "Plataforma empresarial inteligente de gestión de agentes autónomos, canales y flujos de automatización.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es" className="h-full">
      <body className={`${inter.variable} antialiased h-full flex overflow-hidden bg-zinc-50 dark:bg-zinc-950 text-zinc-900 dark:text-zinc-100`}>
        {/* Primary 64px Navigation Rail */}
        <PrimaryRail />

        {/* Secondary 240px Sidebar */}
        <SecondarySidebar />

        {/* Main Content Area */}
        <div className="flex-1 flex flex-col min-w-0 h-full overflow-hidden">
          <Topbar />
          <main className="flex-1 overflow-y-auto p-6 bg-zinc-100/50 dark:bg-zinc-900/30">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
