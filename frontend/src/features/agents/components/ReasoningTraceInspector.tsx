import React from "react";
import { Terminal, Wrench, Clock } from "lucide-react";
import { ReasoningStep } from "../api/useAgentsData";
import { Surface } from "@/shared/components/ui/Surface";
import { EmptyState } from "@/shared/components/ui/EmptyState";

interface ReasoningTraceInspectorProps {
 traces: ReasoningStep[];
}

export function ReasoningTraceInspector({ traces }: ReasoningTraceInspectorProps) {
 return (
 <Surface variant="glass" className="space-y-4">
 <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
 <div className="flex items-center gap-2">
 <Terminal className="w-4 h-4 text-neutral-700" />
 <h2 className="text-sm font-semibold text-neutral-950">
 Inspector de Trazas de Razonamiento (Execution Traces)
 </h2>
 </div>
 <span className="text-xs font-mono text-neutral-500">Historial disponible</span>
 </div>

 {traces.length === 0 ? (
 <EmptyState
 title="Sin trazas disponibles"
 description="El backend no ha devuelto trazas de razonamiento recientes."
 icon={<Terminal className="h-5 w-5" />}
 />
 ) : (
 <div className="space-y-3">
 {traces.map((step) => (
 <div
 key={step.id}
 className="p-4 rounded-xl bg-white text-neutral-950 font-mono text-xs border border-neutral-200 space-y-2"
 >
 <div className="flex items-center justify-between text-[11px] text-neutral-500 border-b border-neutral-200 pb-2">
 <span className="text-neutral-600 font-bold">[{step.agentName}]</span>
 <span className="flex items-center gap-1">
 <Clock className="w-3 h-3 text-neutral-500" />
 {step.timestamp}
 </span>
 </div>

 <p className="text-neutral-700 font-semibold">&gt; Accion: {step.action}</p>
 <p className="text-neutral-700 leading-relaxed pl-3 border-l-2 border-neutral-300">{step.thought}</p>

 {step.toolUsed && (
 <div className="flex items-center gap-2 text-[11px] text-neutral-600 pt-1">
 <Wrench className="w-3 h-3" />
 <span>Tool Executed: {step.toolUsed}</span>
 </div>
 )}
 </div>
 ))}
 </div>
 )}
 </Surface>
 );
}
