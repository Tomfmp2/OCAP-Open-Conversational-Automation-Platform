"use client";

import React from "react";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend } from "recharts";
import { MessageSquare } from "lucide-react";
import { ChannelThroughputDataPoint } from "../api/useDashboardData";

interface ExecutionMetricsChartWidgetProps {
  data: ChannelThroughputDataPoint[];
}

export function ExecutionMetricsChartWidget({ data }: ExecutionMetricsChartWidgetProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <div className="p-1.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 text-emerald-500">
            <MessageSquare className="w-4 h-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Flujo de Mensajes por Canales</h2>
            <p className="text-[11px] text-zinc-400">Rendimiento en tiempo real por adaptador (mensajes/hora)</p>
          </div>
        </div>
      </div>

      <div className="h-56 w-full">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
            <XAxis dataKey="time" stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} />
            <YAxis stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} />
            <Tooltip
              contentStyle={{
                backgroundColor: "#09090B",
                borderColor: "#27272A",
                borderRadius: "8px",
                color: "#FAFAFA",
                fontSize: "12px",
              }}
            />
            <Legend wrapperStyle={{ fontSize: "11px", paddingTop: "8px" }} />
            <Bar dataKey="telegram" name="Telegram" fill="#0EA5E9" radius={[4, 4, 0, 0]} />
            <Bar dataKey="whatsapp" name="WhatsApp" fill="#10B981" radius={[4, 4, 0, 0]} />
            <Bar dataKey="google" name="Google Workspace" fill="#F59E0B" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
