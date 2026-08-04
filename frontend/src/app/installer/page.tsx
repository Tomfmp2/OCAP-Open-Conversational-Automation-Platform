"use client";

import React from "react";
import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { InstallerWizard } from "@/features/installer/components/InstallerWizard";

export default function InstallerPage() {
  return (
    <div className="min-h-dvh w-full bg-neutral-50 text-neutral-950">
      <header className="border-b border-neutral-200 bg-white">
        <div className="mx-auto flex w-full max-w-3xl items-center justify-between gap-4 px-4 py-4 sm:px-6">
          <Link href="/login" className="text-sm font-semibold tracking-tight">
            OCAP
          </Link>
          <Link
            href="/login"
            className="inline-flex items-center gap-1.5 text-xs font-medium text-neutral-600 hover:text-neutral-950"
          >
            Ir al login
            <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>
      </header>

      <main className="mx-auto w-full max-w-3xl space-y-6 px-4 py-8 sm:px-6 sm:py-10">
        <div className="space-y-2">
          <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-neutral-500">
            Instalador
          </p>
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            Configura OCAP
          </h1>
          <p className="max-w-2xl text-sm leading-relaxed text-neutral-500">
            Modo Dev (sin Docker): panel :3000 y API :5229 con{" "}
            <code className="font-mono text-neutral-800">.\scripts\ocap-dev.ps1</code>
            . Docker Local usa :5000 y{" "}
            <code className="font-mono text-neutral-800">./scripts/ocap-up.sh</code>
            . Aquí defines admin, IA y Google; los canales se conectan después en el panel.
          </p>
        </div>

        <InstallerWizard />
      </main>
    </div>
  );
}
