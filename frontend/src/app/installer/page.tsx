"use client";

import React from "react";
import { Wrench } from "lucide-react";
import { InstallerWizard } from "@/features/installer/components/InstallerWizard";
import { PageHeader } from "@/shared/components/ui";

export default function InstallerPage() {
  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Instalador OCAP"
        description="Configura red, PostgreSQL, admin, Google Workspace e IA. El panel admin ya está conectado a la API."
        icon={<Wrench className="h-5 w-5 text-zinc-300" />}
      />
      <InstallerWizard />
    </div>
  );
}
