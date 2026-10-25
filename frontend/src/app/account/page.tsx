"use client";

import { UserRound } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { ChangePasswordCard } from "@/features/account/components/ChangePasswordCard";
import { MfaCard } from "@/features/account/components/MfaCard";
import { PageHeader } from "@/shared/components/ui/PageHeader";

export default function AccountPage() {
  const { user } = useAuth();

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <PageHeader
        icon={<UserRound className="h-5 w-5 text-neutral-500" aria-hidden />}
        title="Mi cuenta"
        description={user ? `${user.fullName} · ${user.email}` : undefined}
      />
      <ChangePasswordCard />
      <MfaCard />
    </div>
  );
}
