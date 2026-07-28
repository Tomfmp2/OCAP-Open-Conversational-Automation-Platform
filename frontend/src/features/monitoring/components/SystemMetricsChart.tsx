"use client";

import React from "react";
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer, LineChart, Line } from "recharts";
import { Activity, Cpu, HardDrive } from "lucide-react";
import { SystemMetricPoint } from "../api/useMonitoringData";

interface SystemMetricsChartProps {
  metrics: SystemMetricPoint[];
}

export function SystemMetricsChart({ metrics }: SystemMetricsChartProps) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* CPU Usage Panel */}
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
        <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
          <div className="flex items-center gap-2">
            <Cpu className="w-4 h-4 text-blue-500" />
            <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Uso de CPU Núcleo (%)</h3>
          </div>
          <span className="text-xs font-mono font-bold text-blue-500">Promedio: 19.3%</span>
        </div>
        <div className="h-48 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={metrics} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <defs>
                <linearGradient id="cpuGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#3B82F6" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#3B82F6" stopOpacity={0} />
                </linearGradient>
              </defs>
              <XAxis dataKey="timestamp" stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} unit="%" />
              <Tooltip contentStyle={{ backgroundColor: "#09090B", borderColor: "#27272A", color: "#FAFAFA", fontSize: "12px" }} />
              <Area type="monotone" dataKey="cpuPercent" stroke="#3B82F6" strokeWidth={2} fill="url(#cpuGrad)" />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Memory RAM Usage Panel */}
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
        <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
          <div className="flex items-center gap-2">
            <HardDrive className="w-4 h-4 text-purple-500" />
            <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Memoria RAM Utilizada (MB)</h3>
          </div>
          <span className="text-xs font-mono font-bold text-purple-500">Pico: 580 MB</span>
        </div>
        <div className="h-48 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={metrics} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <XAxis dataKey="timestamp" stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="#71717A" fontSize={11} tickLine={false} axisLine={false} unit=" MB" />
              <Tooltip contentStyle={{ backgroundColor: "#09090B", borderColor: "#27272A", color: "#FAFAFA", fontSize: "12px" }} />
              <Line type="monotone" dataKey="memoryMb" stroke="#A855F7" strokeWidth={2} dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}
