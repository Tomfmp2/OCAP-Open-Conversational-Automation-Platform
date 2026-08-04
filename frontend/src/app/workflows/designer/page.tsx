"use client";

import React from "react";
import Link from "next/link";
import {
  GitFork,
  Plus,
  Save,
  CheckSquare,
  Play,
  Trash2,
  ArrowRight,
  AlertCircle,
  CheckCircle2,
} from "lucide-react";
import { apiClient } from "@/shared/api/apiClient";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { Surface } from "@/shared/components/ui/Surface";

export interface DesignerNode {
  id: string;
  stepId: string;
  name: string;
  type: string;
  configurationJson?: string;
}

export interface DesignerEdge {
  id: string;
  fromNodeId: string;
  toNodeId: string;
}

const PALETTE: Array<{ type: string; label: string }> = [
  { type: "start", label: "Start" },
  { type: "llm", label: "LLM" },
  { type: "condition", label: "Condition" },
  { type: "http", label: "HTTP" },
  { type: "tool", label: "Tool" },
  { type: "end", label: "End" },
];

function uid(prefix: string) {
  return `${prefix}-${Math.random().toString(36).slice(2, 9)}`;
}

interface WorkflowDesignerProps {
  initialId?: string;
  initialName?: string;
}

export function WorkflowDesigner({ initialId, initialName }: WorkflowDesignerProps) {
  const [workflowId, setWorkflowId] = React.useState(initialId || "");
  const [name, setName] = React.useState(initialName || "Nuevo workflow");
  const [nodes, setNodes] = React.useState<DesignerNode[]>([]);
  const [edges, setEdges] = React.useState<DesignerEdge[]>([]);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [status, setStatus] = React.useState<{ ok: boolean; message: string } | null>(null);
  const [busy, setBusy] = React.useState(false);

  React.useEffect(() => {
    if (!initialId) return;
    let cancelled = false;
    (async () => {
      try {
        const graph = await apiClient.get<{
          id?: string;
          name?: string;
          nodes?: Array<{
            id: string;
            stepId?: string;
            name: string;
            type: string;
            configurationJson?: string;
          }>;
          edges?: Array<{ id: string; fromNodeId: string; toNodeId: string }>;
        }>(`/api/workflows/${initialId}/designer`);
        if (cancelled || !graph) return;
        setWorkflowId(graph.id || initialId);
        setName(graph.name || initialName || "Workflow");
        setNodes(
          (graph.nodes || []).map((n) => ({
            id: n.id,
            stepId: n.stepId || n.id,
            name: n.name,
            type: n.type,
            configurationJson: n.configurationJson,
          }))
        );
        setEdges(graph.edges || []);
      } catch {
        /* definición sin grafo cargable */
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [initialId, initialName]);

  const addNode = (type: string, label: string) => {
    const id = uid(type);
    setNodes((prev) => [
      ...prev,
      { id, stepId: id, name: label, type, configurationJson: "{}" },
    ]);
    if (nodes.length > 0) {
      const from = nodes[nodes.length - 1];
      setEdges((prev) => [
        ...prev,
        { id: uid("edge"), fromNodeId: from.id, toNodeId: id },
      ]);
    }
  };

  const removeSelected = () => {
    if (!selectedId) return;
    setNodes((prev) => prev.filter((n) => n.id !== selectedId));
    setEdges((prev) =>
      prev.filter((e) => e.fromNodeId !== selectedId && e.toNodeId !== selectedId)
    );
    setSelectedId(null);
  };

  const toGraph = () => ({
    id: workflowId || "00000000-0000-0000-0000-000000000000",
    name,
    description: "Diseñado en Next.js",
    version: 1,
    nodes: nodes.map((n) => ({
      id: n.id,
      stepId: n.stepId || n.id,
      name: n.name,
      type: n.type,
      configurationJson: n.configurationJson || "{}",
    })),
    edges: edges.map((e) => ({
      id: e.id,
      fromNodeId: e.fromNodeId,
      toNodeId: e.toNodeId,
    })),
  });

  const validate = async () => {
    setBusy(true);
    setStatus(null);
    try {
      const result = await apiClient.post<{ isValid?: boolean; IsValid?: boolean }>(
        "/api/workflows/designer/validate",
        toGraph()
      );
      const ok = result?.isValid ?? result?.IsValid ?? false;
      setStatus({
        ok,
        message: ok ? "Estructura válida." : "La validación encontró errores.",
      });
    } catch (err: unknown) {
      setStatus({
        ok: false,
        message: err instanceof Error ? err.message : "Error al validar.",
      });
    } finally {
      setBusy(false);
    }
  };

  const save = async () => {
    setBusy(true);
    setStatus(null);
    try {
      const saved = await apiClient.post<{ id?: string }>("/api/workflows/designer/save", toGraph());
      if (saved?.id) setWorkflowId(saved.id);
      setStatus({ ok: true, message: `Workflow guardado${saved?.id ? ` (${saved.id})` : ""}.` });
    } catch (err: unknown) {
      setStatus({
        ok: false,
        message: err instanceof Error ? err.message : "Error al guardar.",
      });
    } finally {
      setBusy(false);
    }
  };

  const execute = async () => {
    if (!workflowId) {
      setStatus({ ok: false, message: "Guarda el workflow antes de ejecutarlo." });
      return;
    }
    setBusy(true);
    setStatus(null);
    try {
      const exec = await apiClient.post<{ id?: string }>(`/api/workflows/${workflowId}/execute`);
      setStatus({
        ok: true,
        message: `Ejecución iniciada${exec?.id ? `: ${exec.id}` : ""}.`,
      });
    } catch (err: unknown) {
      setStatus({
        ok: false,
        message: err instanceof Error ? err.message : "Error al ejecutar.",
      });
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-2 text-sm font-semibold focus:outline-none focus:ring-1 focus:ring-neutral-950"
        />
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" size="sm" onClick={() => void validate()} loading={busy}>
            <CheckSquare className="h-3.5 w-3.5" />
            Validar
          </Button>
          <Button variant="secondary" size="sm" onClick={() => void save()} loading={busy}>
            <Save className="h-3.5 w-3.5" />
            Guardar
          </Button>
          <Button size="sm" onClick={() => void execute()} loading={busy} disabled={!workflowId}>
            <Play className="h-3.5 w-3.5" />
            Ejecutar
          </Button>
        </div>
      </div>

      {status && (
        <div
          className={`flex items-center gap-2 rounded-md border px-3 py-2 text-xs ${
            status.ok ? "border-neutral-300 bg-neutral-50" : "border-neutral-950 bg-white"
          }`}
        >
          {status.ok ? (
            <CheckCircle2 className="h-4 w-4 shrink-0" />
          ) : (
            <AlertCircle className="h-4 w-4 shrink-0" />
          )}
          {status.message}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
        <Surface padding="md" className="space-y-2">
          <h3 className="text-xs font-semibold text-neutral-950">Toolbox</h3>
          {PALETTE.map((item) => (
            <button
              key={item.type}
              type="button"
              onClick={() => addNode(item.type, item.label)}
              className="w-full flex items-center gap-2 rounded-lg border border-neutral-200 bg-white px-3 py-2 text-xs font-medium hover:border-neutral-400"
            >
              <Plus className="h-3.5 w-3.5" />
              {item.label}
            </button>
          ))}
          <button
            type="button"
            onClick={removeSelected}
            disabled={!selectedId}
            className="w-full flex items-center gap-2 rounded-lg border border-neutral-200 px-3 py-2 text-xs text-neutral-600 disabled:opacity-40"
          >
            <Trash2 className="h-3.5 w-3.5" />
            Eliminar seleccionado
          </button>
        </Surface>

        <Surface className="lg:col-span-3 min-h-[360px]">
          {nodes.length === 0 ? (
            <div className="flex h-full min-h-[320px] items-center justify-center text-xs text-neutral-500">
              Añade un nodo Start desde el toolbox para comenzar.
            </div>
          ) : (
            <div className="flex flex-col md:flex-row md:items-center gap-3 p-4 overflow-x-auto">
              {nodes.map((node, index) => (
                <React.Fragment key={node.id}>
                  <button
                    type="button"
                    onClick={() => setSelectedId(node.id)}
                    className={`min-w-[140px] rounded-lg border p-3 text-left space-y-1 ${
                      selectedId === node.id
                        ? "border-neutral-950 bg-neutral-100"
                        : "border-neutral-200 bg-white"
                    }`}
                  >
                    <div className="text-[10px] font-mono uppercase text-neutral-500">{node.type}</div>
                    <div className="text-xs font-semibold text-neutral-950">{node.name}</div>
                  </button>
                  {index < nodes.length - 1 && (
                    <ArrowRight className="h-4 w-4 shrink-0 text-neutral-400 hidden md:block" />
                  )}
                </React.Fragment>
              ))}
            </div>
          )}
        </Surface>
      </div>
    </div>
  );
}

export default function WorkflowDesignerPage() {
  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Diseñador de workflows"
        description="Crea y guarda grafos visuales en el frontend Next.js (API designer/validate|save)."
        icon={<GitFork className="h-5 w-5 text-neutral-700" />}
        actions={
          <Link href="/workflows" className="text-xs font-medium text-neutral-600 hover:text-neutral-950">
            Volver al listado
          </Link>
        }
      />
      <WorkflowDesigner />
    </div>
  );
}
