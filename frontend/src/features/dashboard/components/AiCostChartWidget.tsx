"use client";

import React from "react";
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";
import { Cpu, DollarSign } from "lucide-react";
import { CostUsageDataPoint } from "../api/useDashboardData";

interface AiCostChartWidgetProps {
  data: CostUsageDataPoint[];
}

export function AiCostChartWidget({ data }: AiCostChartWidgetProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <div className="p-1.5 rounded-lg bg-blue-50 dark:bg-blue-950/40 text-blue-500">
            <Cpu className="w-4 h-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Consumo & Costo de Modelos IA</h2>
            <p className="text-[11px] text-zinc-400">Gasto diario en tokens LLM acumulados</p>
          </div>
        </div>
        <div className="flex items-center gap-1 text-xs font-mono font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2.5 py-1 rounded-md border border-emerald-500/20">
          <DollarSign className="w-3.5 h-3.5" />
          <span>42.85 USD / Mes</span>
        </div>
      </div>

      <div className="h-56 w-full">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
            <defs>
              <linearGradient id="costGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#3B82F6" stopOpacity={0.4} />
                <stop offset="95%" stopColor="#3B82F6" stopOpacity={0} />
              </linearGradient>
            </defs>
            <XAxis dataKey="date" stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} />
            <YAxis stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} tickFormatter={(v) => `$${v}`} />
            <Tooltip
              contentStyle={{
                backgroundColor: "#09090B",
                borderColor: "#27272A",
                borderRadius: "8px",
                color: "#FAFAFA",
                fontSize: "12px",
              }}
              formatter={(value) => [`$${value ?? 0}`, "Costo USD"]}
            />
            <Area type="monotone" dataKey="costUsd" stroke="#3B82F6" strokeWidth={2} fillOpacity={1} fill="url(#costGradient)" />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
