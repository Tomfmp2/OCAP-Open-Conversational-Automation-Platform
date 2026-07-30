"use client";

import React from "react";
import { Settings, Save, CheckCircle2, AlertCircle } from "lucide-react";
import { SettingsConfig } from "../api/useSettingsData";
import { Badge, Button, Input, Surface } from "@/shared/components/ui";

interface SettingsFormProps {
  settings: SettingsConfig;
  onSave: (config: SettingsConfig) => void | Promise<void>;
  isSaving: boolean;
  saveSuccess?: boolean;
  saveError?: string;
}

export function SettingsForm({ settings, onSave, isSaving, saveSuccess, saveError }: SettingsFormProps) {
  const [form, setForm] = React.useState<SettingsConfig>(settings);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    void onSave(form);
  };

  return (
    <Surface variant="glass" glow>
      <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800/80 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-blue-500/10 p-2 text-blue-400">
            <Settings className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-100">Configuración del tenant</h2>
            <p className="text-[11px] text-zinc-500">Preferencias persistidas por la API.</p>
          </div>
        </div>
        {saveSuccess && (
          <Badge tone="success"><CheckCircle2 className="h-3 w-3" /> Cambios guardados</Badge>
        )}
      </div>

      {saveError && (
        <div role="alert" className="flex items-start gap-2 rounded-xl border border-red-500/20 bg-red-500/10 p-3 text-xs text-red-400">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {saveError}
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 text-xs md:grid-cols-2">
        <Input
          label="Nombre del tenant activo"
          value={form.tenantName}
          readOnly
          hint="Solo lectura: este endpoint no renombra la entidad Tenant."
          className="cursor-not-allowed opacity-70"
        />
        <div className="space-y-2">
          <label className="block text-xs font-semibold tracking-wide text-zinc-300">Idioma predeterminado</label>
          <select
            value={form.defaultLocale}
            onChange={(e) => setForm({ ...form, defaultLocale: e.target.value as SettingsConfig["defaultLocale"] })}
            className="focus-ring w-full rounded-xl border border-zinc-800 bg-zinc-950 px-3.5 py-2.5 text-sm text-zinc-100"
          >
            <option value="es">Español (ES)</option>
            <option value="en">English (EN)</option>
            <option value="de">Deutsch (DE)</option>
          </select>
        </div>

        <Input label="Zona horaria" value={form.timezone} onChange={(e) => setForm({ ...form, timezone: e.target.value })} />
        <Input
          label="Retención de logs (días)"
          type="number"
          min={1}
          value={form.auditLogRetentionDays}
          onChange={(e) => setForm({ ...form, auditLogRetentionDays: Number(e.target.value) })}
        />
      </div>

      <div className="grid gap-3 md:grid-cols-2">
        {[
          ["enableTelemetry", "Telemetría", "Permite recopilar telemetría operativa."],
          ["enableFailover", "Failover", "Activa la conmutación configurada por el backend."],
        ].map(([field, label, description]) => (
          <label key={field} className="flex cursor-pointer items-start gap-3 rounded-xl border border-zinc-800 bg-zinc-950/60 p-4">
            <input
              type="checkbox"
              checked={form[field as "enableTelemetry" | "enableFailover"]}
              onChange={(event) => setForm({ ...form, [field]: event.target.checked })}
              className="mt-0.5 h-4 w-4 accent-blue-500"
            />
            <span>
              <span className="block text-sm font-medium text-zinc-200">{label}</span>
              <span className="mt-0.5 block text-[11px] text-zinc-500">{description}</span>
            </span>
          </label>
        ))}
      </div>

      <div className="flex justify-end border-t border-zinc-800/80 pt-4">
        <Button type="submit" loading={isSaving}>
          <Save className="h-4 w-4" />
          Guardar preferencias
        </Button>
      </div>
      </form>
    </Surface>
  );
}
