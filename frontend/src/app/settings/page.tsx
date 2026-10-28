"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import { useOrganization, useRenameOrganization } from "@/features/settings/api/useOrganization";
import { Button, ErrorState, FormError, Input, Page, PageHeader, Section } from "@/shared/components/ui";

export default function OrganizationPage() {
  const organization = useOrganization();

  return (
    <Page>
      <PageHeader title="Organización" />
      {organization.isError ? (
        <ErrorState onRetry={() => void organization.refetch()} />
      ) : organization.data ? (
        <NameForm key={organization.data.name} current={organization.data.name} />
      ) : (
        <div className="h-24 animate-pulse rounded-lg bg-neutral-100 motion-reduce:animate-none" role="status" aria-label="Cargando" />
      )}
    </Page>
  );
}

function NameForm({ current }: { current: string }) {
  const rename = useRenameOrganization();
  const [name, setName] = React.useState(current);
  const unchanged = name.trim() === current;

  return (
    <Section title="Nombre" description="Se ve en el panel y en las invitaciones al equipo.">
      <form
        className="max-w-sm space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          rename.mutate(name.trim());
        }}
      >
        <Input label="Nombre de la organización" required maxLength={256} value={name} onChange={(e) => setName(e.target.value)} />
        <FormError error={rename.error} />
        {rename.isSuccess && unchanged && (
          <p role="status" className="flex items-center gap-2 text-sm text-success">
            <CheckCircle2 className="h-4 w-4" aria-hidden /> Nombre guardado.
          </p>
        )}
        <Button type="submit" loading={rename.isPending} disabled={unchanged || !name.trim()}>
          Guardar nombre
        </Button>
      </form>
    </Section>
  );
}
