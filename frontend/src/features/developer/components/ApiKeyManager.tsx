import React from "react";
import { Key, Plus, Trash2 } from "lucide-react";
import { ApiKeyItem } from "../api/useDeveloperData";
import {
  Badge,
  Button,
  EmptyState,
  Input,
  Modal,
  Surface,
} from "@/shared/components/ui";

interface ApiKeyManagerProps {
  keys: ApiKeyItem[];
  onCreate?: (name: string) => void | Promise<unknown>;
  onRevoke?: (id: string) => void | Promise<unknown>;
  isCreating?: boolean;
  isRevoking?: boolean;
}

export function ApiKeyManager({
  keys,
  onCreate,
  onRevoke,
  isCreating = false,
  isRevoking = false,
}: ApiKeyManagerProps) {
  const [isOpen, setIsOpen] = React.useState(false);
  const [name, setName] = React.useState("");

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!onCreate || !name.trim()) return;
    await onCreate(name.trim());
    setName("");
    setIsOpen(false);
  };

  return (
    <Surface variant="glass" glow className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800/80 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-blue-500/10 p-2 text-blue-400">
            <Key className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-100">Claves de API</h2>
            <p className="text-[11px] text-zinc-500">Credenciales de acceso programático.</p>
          </div>
        </div>
        <Button size="sm" onClick={() => setIsOpen(true)} disabled={!onCreate}>
          <Plus className="h-3.5 w-3.5" />
          Generar clave
        </Button>
      </div>

      {keys.length === 0 ? (
        <EmptyState
          title="No hay claves de API"
          description="Todavía no se han creado credenciales para este tenant."
        />
      ) : (
        <div className="space-y-3">
          {keys.map((key) => (
          <div
            key={key.id}
            className="flex flex-col justify-between gap-3 rounded-xl border border-zinc-800 bg-zinc-950/70 p-4 sm:flex-row sm:items-center"
          >
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <p className="truncate text-sm font-semibold text-zinc-100">{key.name}</p>
                <Badge tone={key.status === "active" ? "success" : "neutral"}>
                  {key.status === "active" ? "Activa" : "Revocada"}
                </Badge>
              </div>
              <p className="mt-1 font-mono text-xs text-zinc-500">{key.keyPrefix}</p>
              <p className="mt-1 text-[11px] text-zinc-500">Último uso: {key.lastUsed}</p>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="text-red-400 hover:text-red-300"
              disabled={!onRevoke || key.status === "revoked" || isRevoking}
              onClick={() => void onRevoke?.(key.id)}
              aria-label={`Revocar ${key.name}`}
            >
              <Trash2 className="h-4 w-4" />
              Revocar
            </Button>
          </div>
          ))}
        </div>
      )}

      <Modal
        open={isOpen}
        onClose={() => setIsOpen(false)}
        title="Generar clave de API"
        description="La clave se creará mediante la API del tenant activo."
      >
        <form className="space-y-4" onSubmit={handleCreate}>
          <Input
            autoFocus
            label="Nombre de la clave"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Ej. Integración de producción"
          />
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={() => setIsOpen(false)}>
              Cancelar
            </Button>
            <Button type="submit" loading={isCreating} disabled={!name.trim()}>
              Generar
            </Button>
          </div>
        </form>
      </Modal>
    </Surface>
  );
}
