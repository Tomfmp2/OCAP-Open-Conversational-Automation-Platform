"use client";

import React from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "@/features/auth/context/AuthProvider";
import { AuthGuard } from "@/features/auth/components/AuthGuard";
import { AppShell } from "@/shared/components/AppShell";

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = React.useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            refetchOnWindowFocus: false,
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <AuthGuard>
          <AppShell>{children}</AppShell>
        </AuthGuard>
      </AuthProvider>
    </QueryClientProvider>
  );
}
