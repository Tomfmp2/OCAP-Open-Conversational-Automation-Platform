"use client";

import React from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { isPublicRoute, requiredPermissionFor } from "@/shared/config/navigation";


interface AuthGuardProps {
  children: React.ReactNode;
  requiredPermission?: string;
  requiredRole?: string;
}

export function AuthGuard({
  children,
  requiredPermission,
  requiredRole,
}: AuthGuardProps) {
  const { isAuthenticated, isLoading, hasPermission, hasRole } = useAuth();
  const pathname = usePathname();
  const router = useRouter();

  const isPublic = isPublicRoute(pathname);

  React.useEffect(() => {
    if (isLoading) return;
    if (!isAuthenticated && !isPublic) {
      router.replace("/login");
    }
    if (isAuthenticated && pathname === "/login") {
      router.replace("/");
    }
  }, [isAuthenticated, isLoading, isPublic, pathname, router]);

  if (isPublic) {
    return <>{children}</>;
  }

  if (isLoading) {
    return (
      <div className="flex h-full w-full items-center justify-center bg-neutral-50">
        <div className="text-sm text-neutral-500">Cargando sesión…</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  // Sin permiso explícito, el de la ruta según el menú (p. ej. un Operador no entra a Canales escribiendo la URL).
  const permission = requiredPermission ?? requiredPermissionFor(pathname);
  const lacksPermission = !!permission && !hasPermission(permission);
  const lacksRole = !!requiredRole && !hasRole(requiredRole);

  if (isAuthenticated && (lacksPermission || lacksRole)) {
    return (
      <div className="flex h-full w-full items-center justify-center bg-neutral-50 p-6">
        <div className="text-center">
          <h1 className="text-lg font-semibold text-neutral-950">Acceso denegado</h1>
          <p className="mt-2 text-sm text-neutral-500">
            No tienes los permisos necesarios para ver esta sección.
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
