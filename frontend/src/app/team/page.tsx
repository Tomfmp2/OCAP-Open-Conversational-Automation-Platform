"use client";

import React from "react";
import { UserPlus } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { useTeam } from "@/features/team/api/useTeam";
import { InviteMemberModal } from "@/features/team/components/InviteMemberModal";
import { TeamMemberRow } from "@/features/team/components/TeamMemberRow";
import { Button, ErrorState, ListSkeleton, Page, PageHeader } from "@/shared/components/ui";

export default function TeamPage() {
  const { user } = useAuth();
  const team = useTeam();
  const [inviting, setInviting] = React.useState(false);

  return (
    <Page>
      <PageHeader
        title="Equipo"
        description="Los operadores atienden conversaciones; los administradores, además, configuran todo."
        actions={
          <Button onClick={() => setInviting(true)}>
            <UserPlus className="h-4 w-4" aria-hidden /> Invitar
          </Button>
        }
      />

      {team.isLoading ? (
        <ListSkeleton label="Cargando equipo" />
      ) : team.isError ? (
        <ErrorState message={team.error instanceof Error ? team.error.message : undefined} onRetry={() => void team.refetch()} />
      ) : (
        <>
          <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
            {team.data?.members.map((member) => (
              <TeamMemberRow key={member.id} member={member} isSelf={member.id === user?.id} />
            ))}
          </ul>
          {team.data && !team.data.emailConfigured && (
            <p className="text-sm text-neutral-600">
              El servidor no tiene correo configurado: al invitar te damos un enlace para que lo compartas tú. Para enviarlo
              por email, define SMTP_HOST y SMTP_FROM en el .env.
            </p>
          )}
        </>
      )}

      <InviteMemberModal open={inviting} onClose={() => setInviting(false)} />
    </Page>
  );
}
