"use client";

import React from "react";
import Link from "next/link";
import { ArrowRight, Sparkles, Wrench } from "lucide-react";
import { InstallerWizard } from "@/features/installer/components/InstallerWizard";

export default function InstallerPage() {
  return (
    <div className="min-h-dvh w-full bg-zinc-950 text-zinc-50">
      <div
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 70% 50% at 0% 0%, rgba(255,255,255,0.07), transparent 55%), radial-gradient(ellipse 50% 40% at 100% 100%, rgba(255,255,255,0.04), transparent 50%)",
        }}
        aria-hidden
      />

      <header className="relative border-b border-zinc-800/80">
        <div className="mx-auto flex w-full max-w-3xl items-center justify-between gap-4 px-4 py-4 sm:px-6">
          <Link href="/login" className="flex items-center gap-2.5">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-zinc-50 text-zinc-950">
              <Sparkles className="h-4 w-4" />
            </span>
            <span className="text-sm font-semibold tracking-tight">OCAP</span>
          </Link>
          <Link
            href="/login"
            className="inline-flex items-center gap-1.5 text-xs font-medium text-zinc-400 hover:text-zinc-100"
          >
            Ir al login
            <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>
      </header>

      <main className="relative mx-auto w-full max-w-3xl space-y-6 px-4 py-8 sm:px-6 sm:py-10">
        <div className="space-y-3">
          <div className="inline-flex items-center gap-2 rounded-full border border-zinc-800 bg-zinc-900/70 px-3 py-1 text-[11px] uppercase tracking-[0.16em] text-zinc-400">
            <Wrench className="h-3.5 w-3.5" />
            Instalador
          </div>
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            Configura OCAP
          </h1>
          <p className="max-w-2xl text-sm leading-relaxed text-zinc-400">
            El stack ya debe estar montado con <code className="text-zinc-200">./scripts/ocap-up.sh</code>.
            Aquí defines admin, Google e IA. En Local los puertos quedan en :3000 / :5000.
          </p>
        </div>

        <InstallerWizard />
      </main>
    </div>
  );
}
