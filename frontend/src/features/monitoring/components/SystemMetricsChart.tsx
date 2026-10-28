"use client";

import React from "react";
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer, LineChart, Line } from "recharts";
import { Cpu, HardDrive } from "lucide-react";
import { SystemMetricPoint } from "../api/useMonitoringData";
import { EmptyState, Surface } from "@/shared/components/ui";

interface SystemMetricsChartProps {
  metrics: SystemMetricPoint[];
}

export function SystemMetricsChart({ metrics }: SystemMetricsChartProps) {
  if (metrics.length === 0) {
    return (
      <EmptyState
        title="Sin muestras de telemetría"
        description="El servicio no devolvió mediciones de CPU o memoria."
      />
    );
  }

  const cpuAverage = metrics.reduce((total, point) => total + point.cpuPercent, 0) / metrics.length;
  const memoryPeak = Math.max(...metrics.map((point) => point.memoryMb));

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <Surface variant="glass" glow className="space-y-4">
        <div className="flex items-center justify-between border-b border-neutral-200 pb-3">
          <div className="flex items-center gap-2">
            <Cpu className="h-4 w-4 text-neutral-700" />
            <h3 className="text-sm font-semibold text-neutral-950">Uso de CPU</h3>
          </div>
          <span className="font-mono text-xs font-bold text-neutral-700">
            Promedio: {cpuAverage.toFixed(1)}%
          </span>
        </div>
        <div className="h-56 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={metrics} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id="cpuGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#171717" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#171717" stopOpacity={0} />
                </linearGradient>
              </defs>
              <XAxis dataKey="time" stroke="#737373" fontSize={12} tickLine={false} axisLine={false} />
              <YAxis stroke="#737373" fontSize={12} tickLine={false} axisLine={false} unit="%" width={56} />
              <Tooltip contentStyle={{ backgroundColor: "#09090B", borderColor: "#27272A", color: "#FAFAFA", fontSize: "12px" }} />
              <Area type="monotone" dataKey="cpuPercent" stroke="#171717" strokeWidth={2} fill="url(#cpuGrad)" />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </Surface>

      <Surface variant="glass" glow className="space-y-4">
        <div className="flex items-center justify-between border-b border-neutral-200 pb-3">
          <div className="flex items-center gap-2">
            <HardDrive className="h-4 w-4 text-neutral-700" />
            <h3 className="text-sm font-semibold text-neutral-950">Memoria utilizada</h3>
          </div>
          <span className="font-mono text-xs font-bold text-neutral-700">
            Pico: {memoryPeak.toFixed(1)} MB
          </span>
        </div>
        <div className="h-56 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={metrics} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
              <XAxis dataKey="time" stroke="#737373" fontSize={12} tickLine={false} axisLine={false} />
              <YAxis stroke="#737373" fontSize={12} tickLine={false} axisLine={false} unit=" MB" width={72} />
              <Tooltip contentStyle={{ backgroundColor: "#09090B", borderColor: "#27272A", color: "#FAFAFA", fontSize: "12px" }} />
              <Line type="monotone" dataKey="memoryMb" stroke="#525252" strokeWidth={2} dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </Surface>
    </div>
  );
}
