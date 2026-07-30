"use client";

import React from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";

const PUBLIC_ROUTES = ["/login", "/installer"];

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

  const isPublic = PUBLIC_ROUTES.some(
    (route) => pathname === route || pathname.startsWith(`${route}/`)
  );

  React.useEffect(() => {
    if (isLoading) return;
    if (!isAuthenticated && !isPublic) {
      router.replace("/login");
    }
    if (isAuthenticated && pathname === "/login") {
      router.replace("/");
    }
  }, [isAuthenticated, isLoading, isPublic, pathname, router]);

  if (isLoading) {
    return (
      <div className="flex h-full w-full items-center justify-center bg-zinc-50 dark:bg-zinc-950">
        <div className="text-sm text-zinc-500">Cargando sesión...</div>
      </div>
    );
  }

  if (!isAuthenticated && !isPublic) {
    return null;
  }

  const lacksPermission =
    !!requiredPermission && !hasPermission(requiredPermission);
  const lacksRole = !!requiredRole && !hasRole(requiredRole);

  if (isAuthenticated && (lacksPermission || lacksRole)) {
    return (
      <div className="flex h-full w-full items-center justify-center bg-zinc-50 p-6 dark:bg-zinc-950">
        <div className="text-center">
          <h1 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
            Acceso denegado
          </h1>
          <p className="mt-2 text-sm text-zinc-500">
            No tienes los permisos necesarios para ver esta sección.
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
