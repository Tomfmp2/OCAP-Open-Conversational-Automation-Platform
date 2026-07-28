import React from "react";
import { ShieldCheck, Users, Lock, Key } from "lucide-react";
import { RbacRole } from "../api/useSecurityData";

interface RbacMatrixTableProps {
  roles: RbacRole[];
}

export function RbacMatrixTable({ roles }: RbacMatrixTableProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <ShieldCheck className="w-4 h-4 text-blue-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Matriz de Roles & Permisos (RBAC)</h2>
        </div>
        <span className="text-xs text-zinc-400 font-mono">Multi-Tenant Isolation</span>
      </div>

      <div className="space-y-3">
        {roles.map((role) => (
          <div
            key={role.id}
            className="p-3.5 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-center justify-between text-xs"
          >
            <div>
              <div className="flex items-center gap-2">
                <p className="font-semibold text-zinc-900 dark:text-zinc-100">{role.name}</p>
                <span className="text-[10px] px-1.5 py-0.2 rounded bg-zinc-200 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 font-mono">
                  {role.usersCount} usuarios
                </span>
              </div>
              <div className="flex flex-wrap gap-1 mt-1.5">
                {role.permissions.map((p, idx) => (
                  <span key={idx} className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-blue-500/10 text-blue-500 border border-blue-500/20">
                    {p}
                  </span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
