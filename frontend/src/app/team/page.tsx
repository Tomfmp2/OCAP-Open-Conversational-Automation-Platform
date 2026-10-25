"use client";

import React from "react";
import { Mail, UserPlus, Users } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { useTeam } from "@/features/team/api/useTeam";
import { InviteMemberModal } from "@/features/team/components/InviteMemberModal";
import { TeamMemberRow } from "@/features/team/components/TeamMemberRow";
import { Button } from "@/shared/components/ui/Button";
import { ErrorState } from "@/shared/components/ui/ErrorState";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Surface } from "@/shared/components/ui/Surface";

export default function TeamPage() {
  const { user } = useAuth();
  const team = useTeam();
  const [inviting, setInviting] = React.useState(false);

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <PageHeader
        icon={<Users className="h-5 w-5 text-neutral-500" aria-hidden />}
        title="Equipo"
        description="Quién entra al panel y qué puede hacer. Los operadores atienden la bandeja; los administradores configuran todo."
        actions={
          <Button type="button" onClick={() => setInviting(true)}>
            <UserPlus className="h-4 w-4" aria-hidden /> Invitar
          </Button>
        }
      />

      {team.data && !team.data.emailConfigured && (
        <p className="flex items-start gap-2 rounded-md border border-neutral-200 bg-white px-3 py-2 text-xs text-neutral-600">
          <Mail className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden />
          Sin correo configurado: al invitar o restablecer una contraseña te mostramos el enlace para que lo compartas.
          Para enviarlo por email, define SMTP_HOST y SMTP_FROM en el .env.
        </p>
      )}

      {team.isLoading ? (
        <Surface>
          <div className="space-y-3" aria-busy="true" aria-label="Cargando equipo">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-12 animate-pulse rounded-md bg-neutral-100 motion-reduce:animate-none" />
            ))}
          </div>
        </Surface>
      ) : team.isError ? (
        <ErrorState message={team.error instanceof Error ? team.error.message : undefined} onRetry={() => void team.refetch()} />
      ) : (
        <Surface>
          <ul className="divide-y divide-neutral-100">
            {team.data?.members.map((member) => (
              <TeamMemberRow key={member.id} member={member} isSelf={member.id === user?.id} />
            ))}
          </ul>
        </Surface>
      )}

      <InviteMemberModal open={inviting} onClose={() => setInviting(false)} />
    </div>
  );
}
