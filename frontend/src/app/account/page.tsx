"use client";

import { useAuth } from "@/features/auth/context/AuthProvider";
import { ChangePasswordCard } from "@/features/account/components/ChangePasswordCard";
import { MfaCard } from "@/features/account/components/MfaCard";
import { Page, PageHeader } from "@/shared/components/ui";

export default function AccountPage() {
  const { user } = useAuth();

  return (
    <Page>
      <PageHeader title="Mi cuenta" description={user?.email} />
      <ChangePasswordCard />
      <MfaCard />
    </Page>
  );
}
