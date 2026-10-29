"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import {
  useEmailSettings,
  useEmailSettingsActions,
  useOrganization,
  useRenameOrganization,
  type EmailSettings,
} from "@/features/settings/api/useOrganization";
import { ApiError } from "@/shared/api/types";
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
      <OutgoingEmail />
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

/** Solo para quien administra la organización: el resto recibe 403 y la sección no aparece. */
function OutgoingEmail() {
  const email = useEmailSettings();
  if (email.error instanceof ApiError && email.error.status === 403) return null;

  return (
    <Section
      title="Correo saliente"
      description="Para enviar invitaciones al equipo y enlaces de «Olvidé mi contraseña». Sin él, los enlaces se copian a mano."
    >
      {email.isError ? (
        <ErrorState onRetry={() => void email.refetch()} />
      ) : email.data ? (
        <EmailForm key={JSON.stringify(email.data.settings)} current={email.data.settings} usesServer={email.data.usesServer} />
      ) : (
        <div className="h-40 animate-pulse rounded-lg bg-neutral-100 motion-reduce:animate-none" role="status" aria-label="Cargando" />
      )}
    </Section>
  );
}

function EmailForm({ current, usesServer }: { current: EmailSettings | null; usesServer: boolean }) {
  const { save, remove, test } = useEmailSettingsActions();
  const [host, setHost] = React.useState(current?.host ?? "");
  const [port, setPort] = React.useState(String(current?.port ?? 587));
  const [username, setUsername] = React.useState(current?.username ?? "");
  const [password, setPassword] = React.useState("");
  const [from, setFrom] = React.useState(current?.from ?? "");
  const [useSsl, setUseSsl] = React.useState(current?.useSsl ?? true);
  const sslId = React.useId();

  return (
    <form
      className="max-w-md space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate({ host: host.trim(), port: Number(port), username: username.trim(), password: password ? password : null, from: from.trim(), useSsl });
      }}
    >
      {!current && (
        <p className="text-sm text-neutral-600">
          {usesServer ? "Ahora se usa el correo configurado en el servidor. Completa esto para usar uno propio." : "Todavía no hay correo configurado."}
        </p>
      )}
      <div className="grid gap-4 sm:grid-cols-[1fr_7rem]">
        <Input label="Servidor SMTP" required placeholder="smtp.tudominio.com" value={host} onChange={(e) => setHost(e.target.value)} />
        <Input label="Puerto" required type="number" hint="25, 465, 587 o 2525" value={port} onChange={(e) => setPort(e.target.value)} />
      </div>
      <Input label="Usuario" autoComplete="off" value={username} onChange={(e) => setUsername(e.target.value)} />
      <Input
        label="Contraseña"
        type="password"
        autoComplete="new-password"
        placeholder={current?.hasPassword ? "Sin cambios" : undefined}
        hint={current?.hasPassword ? "Se guarda cifrada. Déjala vacía para conservarla." : "Se guarda cifrada."}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      <Input label="Enviar como" type="email" required placeholder="equipo@tudominio.com" value={from} onChange={(e) => setFrom(e.target.value)} />
      <label htmlFor={sslId} className="flex items-center gap-2 text-sm text-neutral-800">
        <input id={sslId} type="checkbox" className="h-4 w-4 accent-neutral-950" checked={useSsl} onChange={(e) => setUseSsl(e.target.checked)} />
        Conexión cifrada (TLS)
      </label>

      <FormError error={save.error ?? remove.error ?? test.error} />
      {save.isSuccess && (
        <p role="status" className="flex items-center gap-2 text-sm text-success">
          <CheckCircle2 className="h-4 w-4" aria-hidden /> Correo guardado.
        </p>
      )}
      {test.isSuccess && (
        <p role="status" className="flex items-center gap-2 text-sm text-success">
          <CheckCircle2 className="h-4 w-4" aria-hidden /> Enviado a {test.data.sentTo}. Revisa tu bandeja.
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        <Button type="submit" loading={save.isPending}>
          Guardar correo
        </Button>
        {(current || usesServer) && (
          <Button variant="secondary" loading={test.isPending} onClick={() => test.mutate()}>
            Enviar correo de prueba
          </Button>
        )}
        {current && (
          <Button variant="ghost" className="text-danger hover:bg-danger-subtle hover:text-danger" loading={remove.isPending} onClick={() => remove.mutate()}>
            Quitar
          </Button>
        )}
      </div>
    </form>
  );
}
