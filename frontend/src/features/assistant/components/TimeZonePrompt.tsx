"use client";

import React from "react";
import Link from "next/link";
import { Clock } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { usePreferences, useSavePreferences } from "@/features/settings/api/useOrganization";
import { Button, FormError } from "@/shared/components/ui";

/** Zona horaria que tiene configurada este equipo; el navegador la da sin pedir la ubicación. */
function deviceTimeZone(): string | null {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || null;
  } catch {
    return null;
  }
}

/**
 * Sin zona horaria el asistente no agenda citas (quedarían a otra hora). Se propone la de este equipo, pero no se usa
 * sin que la persona lo acepte: puede estar de viaje o administrar una organización de otro país.
 */
export function TimeZonePrompt() {
  const { hasPermission } = useAuth();
  const preferences = usePreferences();
  const save = useSavePreferences();
  const [dismissed, setDismissed] = React.useState(false);

  if (dismissed || !preferences.data || preferences.data.timeZone) return null;

  const current = preferences.data;
  const zone = deviceTimeZone();
  const canSave = hasPermission("Settings.Manage");

  return (
    <div role="status" className="w-full rounded-2xl border border-neutral-200 bg-white px-4 py-3 text-left text-sm text-neutral-800">
      <p className="flex items-start gap-2">
        <Clock className="mt-0.5 h-4 w-4 shrink-0 text-neutral-600" aria-hidden />
        <span>
          Para agendar citas a tu hora necesito la zona horaria de la organización.{" "}
          {canSave
            ? zone
              ? <>¿Uso la de este equipo, <strong className="font-semibold">{zone}</strong>?</>
              : "Elígela en Organización."
            : "Pídele a un administrador que la defina en Organización."}
        </span>
      </p>
      {canSave && (
        <div className="mt-3 flex flex-wrap items-center gap-2 pl-6">
          {zone && (
            <Button size="sm" loading={save.isPending} onClick={() => save.mutate({ ...current, timeZone: zone })}>
              Usar {zone}
            </Button>
          )}
          <Link href="/settings" className="focus-ring rounded-md px-2 py-1 text-sm font-medium text-neutral-900 underline underline-offset-2">
            Elegir otra
          </Link>
          <button
            type="button"
            onClick={() => setDismissed(true)}
            className="focus-ring rounded-md px-2 py-1 text-sm text-neutral-600 hover:text-neutral-950"
          >
            Ahora no
          </button>
        </div>
      )}
      <div className="pl-6">
        <FormError error={save.error} />
      </div>
    </div>
  );
}
