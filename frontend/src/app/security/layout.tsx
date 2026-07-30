"use client";

import React from "react";
import { AuthGuard } from "@/features/auth/components/AuthGuard";

export default function SecurityLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthGuard requiredPermission="Security.Manage" requiredRole="Admin">
      {children}
    </AuthGuard>
  );
}
