"use client";

import React from "react";
import { SetupWizard } from "@/features/setup/components/SetupWizard";

export default function SetupPage() {
  return (
    <div className="min-h-dvh w-full bg-neutral-50 text-neutral-950">
      <header className="border-b border-neutral-200 bg-white">
        <div className="mx-auto flex w-full max-w-2xl items-center justify-between gap-4 px-4 py-4 sm:px-6">
          <span className="text-sm font-semibold tracking-tight">OCAP</span>
          <span className="text-xs text-neutral-600">Configuración inicial</span>
        </div>
      </header>

      <main className="mx-auto w-full max-w-2xl px-4 py-10 sm:px-6 sm:py-14">
        <SetupWizard />
      </main>
    </div>
  );
}
