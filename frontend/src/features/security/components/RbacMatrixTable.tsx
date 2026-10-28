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
      <div className="flex items-center justify-between border-b border-neutral-200 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-neutral-100 p-2 text-neutral-700">
            <ShieldCheck className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-neutral-950">Roles y permisos</h2>
            <p className="text-xs text-neutral-600">Matriz RBAC del tenant activo.</p>
          </div>
        </div>
        <Badge tone="info">{roles.length} roles</Badge>
      </div>

      <div className="space-y-3">
        {roles.map((role) => (
          <div
            key={role.id}
            className="rounded-xl border border-neutral-200 bg-neutral-50 p-4 text-xs"
          >
            <p className="font-semibold text-neutral-950">{role.name}</p>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {role.permissions.length === 0 ? (
                <span className="text-xs text-neutral-600">Sin permisos asignados</span>
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
