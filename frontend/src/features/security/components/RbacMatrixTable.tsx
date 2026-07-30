import React from "react";
import { ShieldCheck } from "lucide-react";
import { RbacRole } from "../api/useSecurityData";
import { Badge, Surface } from "@/shared/components/ui";

interface RbacMatrixTableProps {
  roles: RbacRole[];
}

export function RbacMatrixTable({ roles }: RbacMatrixTableProps) {
  return (
    <Surface variant="glass" className="space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-800/80 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-blue-500/10 p-2 text-blue-400">
            <ShieldCheck className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-100">Roles y permisos</h2>
            <p className="text-[11px] text-zinc-500">Matriz RBAC del tenant activo.</p>
          </div>
        </div>
        <Badge tone="info">{roles.length} roles</Badge>
      </div>

      <div className="space-y-3">
        {roles.map((role) => (
          <div
            key={role.id}
            className="rounded-xl border border-zinc-800 bg-zinc-950/70 p-4 text-xs"
          >
            <p className="font-semibold text-zinc-100">{role.name}</p>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {role.permissions.length === 0 ? (
                <span className="text-[11px] text-zinc-500">Sin permisos asignados</span>
              ) : (
                role.permissions.map((permission) => (
                  <Badge key={permission} tone="info" className="font-mono normal-case">
                    {permission}
                  </Badge>
                ))
              )}
              </div>
          </div>
        ))}
      </div>
    </Surface>
  );
}
