"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import {
  googleRedirectUri,
  useEmailSettings,
  useEmailSettingsActions,
  useGoogleActions,
  useGoogleConnection,
  useGoogleSettings,
  useOrganization,
  usePreferences,
  useRenameOrganization,
  useSavePreferences,
  type EmailSettings,
  type Preferences,
} from "@/features/settings/api/useOrganization";
import { ApiError } from "@/shared/api/types";
import { Badge, Button, CopyButton, ErrorState, FormError, Input, Page, PageHeader, Section, Select, Textarea } from "@/shared/components/ui";

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
      <PreferencesSection />
      <OutgoingEmail />
      <GoogleSection />
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

const noSubscribe = () => () => {};

const MEMORY_OPTIONS = [
  { value: "", label: "90 días (recomendado)" },
  { value: "30", label: "30 días" },
  { value: "180", label: "6 meses" },
  { value: "365", label: "1 año" },
  { value: "0", label: "No recordar" },
];

const RETENTION_OPTIONS = [
  { value: "", label: "Siempre" },
  { value: "30", label: "30 días" },
  { value: "90", label: "90 días" },
  { value: "180", label: "6 meses" },
  { value: "365", label: "1 año" },
];

function PreferencesSection() {
  const preferences = usePreferences();
  return (
    <Section title="Preferencias" description="Idioma y hora del asistente, dirección del panel y cuánto se guardan las conversaciones con clientes.">
      {preferences.isError ? (
        <ErrorState onRetry={() => void preferences.refetch()} />
      ) : preferences.data ? (
        <PreferencesForm key={JSON.stringify(preferences.data)} current={preferences.data} />
      ) : (
        <div className="h-40 animate-pulse rounded-lg bg-neutral-100 motion-reduce:animate-none" role="status" aria-label="Cargando" />
      )}
    </Section>
  );
}

function PreferencesForm({ current }: { current: Preferences }) {
  const save = useSavePreferences();
  const [language, setLanguage] = React.useState(current.language ?? "");
  const [timeZone, setTimeZone] = React.useState(current.timeZone ?? "");
  const [panelUrl, setPanelUrl] = React.useState(current.panelUrl ?? "");
  const [retention, setRetention] = React.useState(current.conversationRetentionDays?.toString() ?? "");
  const [memory, setMemory] = React.useState(current.memoryDays?.toString() ?? "");
  const [notice, setNotice] = React.useState(current.privacyNotice ?? "");
  const zonesId = React.useId();
  const zones = React.useMemo(() => (typeof Intl.supportedValuesOf === "function" ? Intl.supportedValuesOf("timeZone") : []), []);

  return (
    <form
      className="max-w-md space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate({
          language: (language || null) as Preferences["language"],
          timeZone: timeZone.trim() || null,
          panelUrl: panelUrl.trim() || null,
          conversationRetentionDays: retention ? Number(retention) : null,
          memoryDays: memory ? Number(memory) : null,
          privacyNotice: notice.trim() || null,
        });
      }}
    >
      <Select label="Idioma del asistente" value={language} onChange={(e) => setLanguage(e.target.value)}>
        <option value="">El del servidor</option>
        <option value="es">Español</option>
        <option value="en">Inglés</option>
      </Select>
      <Input
        label="Zona horaria"
        list={zonesId}
        placeholder={Intl.DateTimeFormat().resolvedOptions().timeZone}
        hint="El asistente la usa para entender «mañana a las 3» y crear eventos."
        value={timeZone}
        onChange={(e) => setTimeZone(e.target.value)}
      />
      <datalist id={zonesId}>
        {zones.map((zone) => (
          <option key={zone} value={zone} />
        ))}
      </datalist>
      <Input
        label="Dirección del panel"
        type="url"
        placeholder="https://panel.tuempresa.com"
        hint="Se usa en los enlaces de invitación. Vacía, la del servidor."
        value={panelUrl}
        onChange={(e) => setPanelUrl(e.target.value)}
      />
      <Select
        label="Guardar conversaciones con clientes"
        hint="Los mensajes más antiguos se borran cada día."
        value={retention}
        onChange={(e) => setRetention(e.target.value)}
      >
        {RETENTION_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </Select>
      <Select
        label="Los agentes recuerdan a cada persona"
        hint="Nombre, preferencias y conversaciones anteriores. Cuenta desde la última vez que la persona lo mencionó."
        value={memory}
        onChange={(e) => setMemory(e.target.value)}
      >
        {MEMORY_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </Select>
      <Textarea
        label="Aviso de tratamiento de datos"
        hint="Opcional. Se envía con la primera respuesta a cada contacto nuevo. Los clientes pueden escribir «olvídame» para borrar lo que se recuerda de ellos."
        rows={3}
        maxLength={600}
        placeholder="Usamos tus datos solo para atenderte. Escribe «olvídame» para que los borremos."
        value={notice}
        onChange={(e) => setNotice(e.target.value)}
      />
      <FormError error={save.error} />
      {save.isSuccess && (
        <p role="status" className="flex items-center gap-2 text-sm text-success">
          <CheckCircle2 className="h-4 w-4" aria-hidden /> Preferencias guardadas.
        </p>
      )}
      <Button type="submit" loading={save.isPending}>
        Guardar preferencias
      </Button>
    </form>
  );
}

/** Solo para quien gestiona integraciones: el resto recibe 403 y la sección no aparece. */
function GoogleSection() {
  const settings = useGoogleSettings();
  const configured = !!settings.data?.clientId || !!settings.data?.usesServer;
  const connection = useGoogleConnection(configured);
  if (settings.error instanceof ApiError && settings.error.status === 403) return null;

  return (
    <Section
      title="Google Workspace"
      description="Para que el asistente envíe correos con Gmail, cree eventos en Calendar y escriba en Sheets."
    >
      {settings.isError ? (
        <ErrorState onRetry={() => void settings.refetch()} />
      ) : settings.data ? (
        <div className="max-w-md space-y-6">
          <GoogleClientForm key={settings.data.clientId ?? "none"} clientId={settings.data.clientId} usesServer={settings.data.usesServer} />
          {configured && <GoogleConnection connected={!!connection.data?.isConnected} loading={connection.isLoading} />}
        </div>
      ) : (
        <div className="h-40 animate-pulse rounded-lg bg-neutral-100 motion-reduce:animate-none" role="status" aria-label="Cargando" />
      )}
    </Section>
  );
}

function GoogleClientForm({ clientId: current, usesServer }: { clientId: string | null; usesServer: boolean }) {
  const { save, remove } = useGoogleActions();
  const [clientId, setClientId] = React.useState(current ?? "");
  const [clientSecret, setClientSecret] = React.useState("");
  // Depende de la dirección del navegador: vacía al renderizar en el servidor.
  const redirectUri = React.useSyncExternalStore(noSubscribe, googleRedirectUri, () => "");

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate({ clientId: clientId.trim(), clientSecret: clientSecret.trim() || null }, { onSuccess: () => setClientSecret("") });
      }}
    >
      <p className="text-sm text-neutral-600">
        {current
          ? "Cliente OAuth propio configurado."
          : usesServer
            ? "Ahora se usa el cliente OAuth del servidor. Completa esto para usar el de tu organización."
            : "Crea un cliente OAuth «Aplicación web» en Google Cloud Console y registra esta URI de redirección:"}
      </p>
      {redirectUri && (
        <div className="flex items-center gap-2">
          <code className="min-w-0 flex-1 truncate rounded-md bg-neutral-100 px-3 py-2 text-xs">{redirectUri}</code>
          <CopyButton value={redirectUri} />
        </div>
      )}
      <Input label="Client ID" required placeholder="….apps.googleusercontent.com" value={clientId} onChange={(e) => setClientId(e.target.value)} />
      <Input
        label="Client secret"
        type="password"
        autoComplete="off"
        required={!current}
        placeholder={current ? "Sin cambios" : undefined}
        hint="Se guarda cifrado."
        value={clientSecret}
        onChange={(e) => setClientSecret(e.target.value)}
      />
      <FormError error={save.error ?? remove.error} />
      <div className="flex flex-wrap gap-2">
        <Button type="submit" loading={save.isPending}>
          Guardar cliente
        </Button>
        {current && (
          <Button variant="ghost" className="text-danger hover:bg-danger-subtle hover:text-danger" loading={remove.isPending} onClick={() => remove.mutate()}>
            Quitar
          </Button>
        )}
      </div>
    </form>
  );
}

function GoogleConnection({ connected, loading }: { connected: boolean; loading: boolean }) {
  const { connect, disconnect } = useGoogleActions();
  return (
    <div className="flex flex-wrap items-center gap-3 border-t border-neutral-200 pt-4">
      <p className="min-w-0 flex-1 text-sm text-neutral-800">
        Cuenta de Google {loading ? "" : connected ? <Badge tone="success">Conectada</Badge> : <Badge>Sin conectar</Badge>}
      </p>
      {connected ? (
        <Button variant="secondary" loading={disconnect.isPending} onClick={() => disconnect.mutate()}>
          Desconectar
        </Button>
      ) : (
        <Button loading={connect.isPending} onClick={() => connect.mutate()}>
          Conectar con Google
        </Button>
      )}
      <FormError error={connect.error ?? disconnect.error} />
    </div>
  );
}
