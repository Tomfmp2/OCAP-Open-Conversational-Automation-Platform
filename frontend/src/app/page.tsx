"use client";

import React from "react";
import {
  Activity,
  ArrowUpRight,
  Bot,
  Cpu,
  GitFork,
  MessageSquare,
  ShieldCheck,
  TrendingUp,
  Zap,
  Radio,
  CheckCircle2,
  RefreshCw,
  Plus,
} from "lucide-react";

const METRICS = [
  {
    title: "Ejecuciones Totales",
    value: "340",
    change: "+204%",
    trend: "up",
    period: "vs semana anterior",
    icon: Activity,
  },
  {
    title: "Canales Activos",
    value: "8 / 8",
    change: "100%",
    trend: "up",
    period: "Telegram, WA, Slack, Teams",
    icon: MessageSquare,
  },
  {
    title: "Costo Estimado IA",
    value: "$14.28",
    change: "-12%",
    trend: "down",
    period: "Últimos 30 días",
    icon: Cpu,
  },
  {
    title: "Salud del Sistema",
    value: "99.4%",
    change: "Óptimo",
    trend: "up",
    period: "Núcleo OCAP Operacional",
    icon: ShieldCheck,
  },
];

const RECENT_ACTIVITIES = [
  { id: 1, text: "Mensaje procesado en adaptador Telegram Native", time: "Hace 2 min", status: "success" },
  { id: 2, text: "EnterpriseAssistantAgent resolvió intención 'CreateReminder'", time: "Hace 5 min", status: "info" },
  { id: 3, text: "Credenciales de OpenAI actualizadas en Credential Vault", time: "Hace 14 min", status: "success" },
  { id: 4, text: "Ejecución de Workflow 'Onboarding Cliente' completada", time: "Hace 28 min", status: "success" },
];

export default function OverviewPage() {
  const [isRefreshing, setIsRefreshing] = React.useState(false);

  const handleRefresh = () => {
    setIsRefreshing(true);
    setTimeout(() => setIsRefreshing(false), 600);
  };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
            Resumen General OCAP
          </h1>
          <p className="text-xs text-zinc-500 mt-1">
            Plataforma empresarial de gestión de agentes autónomos, capacidades e integración omnichannel.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleRefresh}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? "animate-spin" : ""}`} />
            <span>Actualizar</span>
          </button>
          <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-medium transition-colors shadow-sm">
            <Plus className="w-3.5 h-3.5" />
            <span>Agregar Widget</span>
          </button>
        </div>
      </div>

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {METRICS.map((kpi, idx) => {
          const Icon = kpi.icon;
          return (
            <div
              key={idx}
              className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm hover:border-zinc-300 dark:hover:border-zinc-700 transition-all"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-medium text-zinc-500">{kpi.title}</span>
                <div className="p-2 rounded-lg bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400">
                  <Icon className="w-4 h-4" />
                </div>
              </div>
              <div className="mt-3 flex items-baseline justify-between">
                <span className="text-3xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">{kpi.value}</span>
                <span className="inline-flex items-center gap-0.5 text-xs font-semibold px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                  <TrendingUp className="w-3 h-3" />
                  {kpi.change}
                </span>
              </div>
              <p className="mt-1 text-[11px] text-zinc-400">{kpi.period}</p>
            </div>
          );
        })}
      </div>

      {/* Main Analytical Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Activity & Channel Matrix */}
        <div className="lg:col-span-2 space-y-6">
          {/* Channel Status Overview */}
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm">
            <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3 mb-4">
              <div className="flex items-center gap-2">
                <Radio className="w-4 h-4 text-blue-500 animate-pulse" />
                <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Estado de Canales de Comunicación</h2>
              </div>
              <span className="text-xs text-blue-500 hover:underline cursor-pointer">Ver todos (8)</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-sky-500/10 text-sky-500 flex items-center justify-center font-bold text-xs">
                    TG
                  </div>
                  <div>
                    <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100">Telegram Bot Native</p>
                    <p className="text-[10px] text-zinc-400">Adaptador Activo (CAP-01)</p>
                  </div>
                </div>
                <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
                  <CheckCircle2 className="w-3 h-3" /> Online
                </span>
              </div>

              <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-emerald-500/10 text-emerald-500 flex items-center justify-center font-bold text-xs">
                    WA
                  </div>
                  <div>
                    <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100">WhatsApp Business</p>
                    <p className="text-[10px] text-zinc-400">Cloud API & Web QR</p>
                  </div>
                </div>
                <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
                  <CheckCircle2 className="w-3 h-3" /> Online
                </span>
              </div>
            </div>
          </div>

          {/* Activity Feed */}
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm">
            <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100 border-b border-zinc-100 dark:border-zinc-800 pb-3 mb-4">
              Feed de Actividad en Vivo (Audit Timeline)
            </h2>
            <div className="space-y-3">
              {RECENT_ACTIVITIES.map((act) => (
                <div key={act.id} className="flex items-center justify-between text-xs py-2 border-b border-zinc-100 dark:border-zinc-800/60 last:border-none">
                  <div className="flex items-center gap-2.5">
                    <span className="w-2 h-2 rounded-full bg-blue-500" />
                    <span className="text-zinc-800 dark:text-zinc-200 font-medium">{act.text}</span>
                  </div>
                  <span className="text-zinc-400 text-[11px] font-mono">{act.time}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Right 1 Col: Agent Core Info */}
        <div className="space-y-6">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm">
            <div className="flex items-center gap-2 mb-3">
              <Bot className="w-5 h-5 text-blue-500" />
              <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Enterprise Assistant Agent</h2>
            </div>
            <p className="text-xs text-zinc-500 leading-relaxed">
              Agente global principal encargado de coordinar capacidades empresariales, herramientas y futuros agentes especializados.
            </p>

            <div className="mt-4 pt-4 border-t border-zinc-100 dark:border-zinc-800 space-y-2 text-xs">
              <div className="flex justify-between text-zinc-600 dark:text-zinc-400">
                <span>Proveedor IA Activo:</span>
                <span className="font-semibold text-zinc-900 dark:text-zinc-100">OpenAI (gpt-4o)</span>
              </div>
              <div className="flex justify-between text-zinc-600 dark:text-zinc-400">
                <span>Failover Configurado:</span>
                <span className="font-semibold text-zinc-900 dark:text-zinc-100">Gemini 1.5 / Local</span>
              </div>
              <div className="flex justify-between text-zinc-600 dark:text-zinc-400">
                <span>Herramientas Registradas:</span>
                <span className="font-semibold text-zinc-900 dark:text-zinc-100">12 Tools</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
