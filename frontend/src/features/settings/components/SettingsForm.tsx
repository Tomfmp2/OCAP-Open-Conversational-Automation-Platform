"use client";

import React from "react";
import { Settings, Save, CheckCircle2 } from "lucide-react";
import { SettingsConfig } from "../api/useSettingsData";

interface SettingsFormProps {
  settings: SettingsConfig;
  onSave: (config: SettingsConfig) => void | Promise<void>;
  isSaving: boolean;
  saveSuccess?: boolean;
}

export function SettingsForm({ settings, onSave, isSaving, saveSuccess }: SettingsFormProps) {
  const [form, setForm] = React.useState<SettingsConfig>(settings);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    void onSave(form);
  };

  return (
    <form onSubmit={handleSubmit} className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-6 shadow-sm space-y-6">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-4">
        <div className="flex items-center gap-2">
          <Settings className="w-5 h-5 text-blue-500" />
          <h2 className="text-base font-bold text-zinc-900 dark:text-zinc-100">Configuración General de Plataforma</h2>
        </div>
        {saveSuccess && (
          <span className="inline-flex items-center gap-1 text-xs font-semibold text-emerald-500 bg-emerald-500/10 px-3 py-1 rounded-full border border-emerald-500/20 animate-in fade-in duration-200">
            <CheckCircle2 className="w-4 h-4" /> Cambios Guardados
          </span>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-xs">
        <div className="space-y-2">
          <label className="font-semibold text-zinc-700 dark:text-zinc-300 block">Nombre del Tenant Activo</label>
          <input
            type="text"
            value={form.tenantName}
            onChange={(e) => setForm({ ...form, tenantName: e.target.value })}
            className="w-full bg-zinc-100 dark:bg-zinc-950 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-zinc-900 dark:text-zinc-100 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold"
          />
        </div>

        <div className="space-y-2">
          <label className="font-semibold text-zinc-700 dark:text-zinc-300 block">Idioma por Defecto</label>
          <select
            value={form.defaultLocale}
            onChange={(e) => setForm({ ...form, defaultLocale: e.target.value as SettingsConfig["defaultLocale"] })}
            className="w-full bg-zinc-100 dark:bg-zinc-950 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-zinc-900 dark:text-zinc-100 focus:outline-none focus:ring-1 focus:ring-blue-500"
          >
            <option value="es">Español (ES)</option>
            <option value="en">English (EN)</option>
            <option value="de">Deutsch (DE)</option>
          </select>
        </div>

        <div className="space-y-2">
          <label className="font-semibold text-zinc-700 dark:text-zinc-300 block">Zona Horaria del Sistema</label>
          <input
            type="text"
            value={form.timezone}
            onChange={(e) => setForm({ ...form, timezone: e.target.value })}
            className="w-full bg-zinc-100 dark:bg-zinc-950 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-zinc-900 dark:text-zinc-100 focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono"
          />
        </div>

        <div className="space-y-2">
          <label className="font-semibold text-zinc-700 dark:text-zinc-300 block">Retención de Logs (Días)</label>
          <input
            type="number"
            value={form.auditLogRetentionDays}
            onChange={(e) => setForm({ ...form, auditLogRetentionDays: parseInt(e.target.value, 10) || 30 })}
            className="w-full bg-zinc-100 dark:bg-zinc-950 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-zinc-900 dark:text-zinc-100 focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono"
          />
        </div>
      </div>

      <div className="pt-4 border-t border-zinc-100 dark:border-zinc-800 flex justify-end">
        <button
          type="submit"
          disabled={isSaving}
          className="flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors disabled:opacity-50"
        >
          <Save className="w-4 h-4" />
          <span>{isSaving ? "Guardando..." : "Guardar Preferencias"}</span>
        </button>
      </div>
    </form>
  );
}
