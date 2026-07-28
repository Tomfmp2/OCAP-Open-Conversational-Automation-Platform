"use client";

import React from "react";
import { useRouter } from "next/navigation";
import {
  Search,
  MessageSquare,
  Cpu,
  Bot,
  GitFork,
  ShieldCheck,
  Settings,
  Sparkles,
  X,
  ArrowRight,
} from "lucide-react";

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
}

const COMMANDS = [
  { label: "Ir a Resumen General", href: "/", icon: Sparkles, category: "Navegación" },
  { label: "Conectar Telegram Bot", href: "/channels?provider=Telegram", icon: MessageSquare, category: "Canales" },
  { label: "Conectar WhatsApp Cloud API", href: "/channels?provider=WhatsApp", icon: MessageSquare, category: "Canales" },
  { label: "Configurar API Key de OpenAI", href: "/intelligence#providers", icon: Cpu, category: "IA" },
  { label: "Probar Conectividad Gemini", href: "/intelligence#test", icon: Cpu, category: "IA" },
  { label: "Ver Enterprise Assistant Agent", href: "/agents", icon: Bot, category: "Agentes" },
  { label: "Diseñar Nuevo Workflow Visual", href: "/workflows", icon: GitFork, category: "Workflows" },
  { label: "Administrar API Keys & Permisos", href: "/security", icon: ShieldCheck, category: "Seguridad" },
  { label: "Ajustes de Plataforma", href: "/settings", icon: Settings, category: "Configuración" },
];

export function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const router = useRouter();
  const [query, setQuery] = React.useState("");

  if (!open) return null;

  const filtered = COMMANDS.filter((cmd) =>
    cmd.label.toLowerCase().includes(query.toLowerCase()) ||
    cmd.category.toLowerCase().includes(query.toLowerCase())
  );

  const handleSelect = (href: string) => {
    router.push(href);
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-start justify-center pt-24 px-4">
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl shadow-2xl w-full max-w-xl overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-150">
        {/* Search Input Bar */}
        <div className="p-3 border-b border-zinc-200 dark:border-zinc-800 flex items-center gap-3">
          <Search className="w-4 h-4 text-zinc-400 shrink-0" />
          <input
            type="text"
            autoFocus
            placeholder="Escribe un comando o busca en OCAP (Ctrl K)..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="flex-1 bg-transparent text-sm text-zinc-900 dark:text-zinc-100 placeholder:text-zinc-400 focus:outline-none"
          />
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200">
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Results List */}
        <div className="max-h-80 overflow-y-auto p-2 space-y-1">
          {filtered.length === 0 ? (
            <p className="p-4 text-center text-xs text-zinc-500">No se encontraron comandos para &quot;{query}&quot;</p>
          ) : (
            filtered.map((item, idx) => {
              const Icon = item.icon;
              return (
                <button
                  key={idx}
                  onClick={() => handleSelect(item.href)}
                  className="w-full text-left px-3 py-2.5 rounded-lg flex items-center justify-between hover:bg-blue-50 dark:hover:bg-blue-950/40 hover:text-blue-600 dark:hover:text-blue-400 transition-colors group"
                >
                  <div className="flex items-center gap-3">
                    <Icon className="w-4 h-4 text-zinc-400 group-hover:text-blue-500" />
                    <div>
                      <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100 group-hover:text-blue-600 dark:group-hover:text-blue-400">
                        {item.label}
                      </p>
                      <p className="text-[10px] text-zinc-400">{item.category}</p>
                    </div>
                  </div>
                  <ArrowRight className="w-3.5 h-3.5 opacity-0 group-hover:opacity-100 text-blue-500 transition-opacity" />
                </button>
              );
            })
          )}
        </div>

        {/* Footer shortcuts */}
        <div className="p-2.5 bg-zinc-50 dark:bg-zinc-950/60 border-t border-zinc-200 dark:border-zinc-800 flex items-center justify-between text-[11px] text-zinc-400">
          <span>Usa las flechas para navegar</span>
          <span className="font-mono">ESC para cerrar</span>
        </div>
      </div>
    </div>
  );
}
