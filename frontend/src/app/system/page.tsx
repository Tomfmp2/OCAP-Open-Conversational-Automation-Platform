"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import {
  useSaveSystemSettings,
  useSystemSettings,
  type SystemDefaults,
  type SystemSettings,
} from "@/features/system/api/useSystemSettings";
import { Button, ErrorState, FormError, Input, Page, PageHeader, Section, Select, Textarea } from "@/shared/components/ui";

export default function SystemPage() {
  const system = useSystemSettings();

  return (
    <Page>
      <PageHeader title="Sistema" description="Ajustes de toda la instalación. Se aplican al guardar, sin reiniciar el servidor." />
      {system.isError ? (
        <ErrorState onRetry={() => void system.refetch()} />
      ) : system.data ? (
        <SystemForm key={JSON.stringify(system.data.settings)} current={system.data.settings} defaults={system.data.defaults} />
      ) : (
        <div className="h-64 animate-pulse rounded-lg bg-neutral-100 motion-reduce:animate-none" role="status" aria-label="Cargando" />
      )}
    </Page>
  );
}

const text = (value: number | null) => value?.toString() ?? "";
const number = (value: string) => (value.trim() ? Number(value) : null);

function SystemForm({ current, defaults }: { current: SystemSettings; defaults: SystemDefaults }) {
  const save = useSaveSystemSettings();
  const [limiting, setLimiting] = React.useState(current.rateLimitingEnabled === null ? "" : String(current.rateLimitingEnabled));
  const [requests, setRequests] = React.useState(text(current.requestsPerMinute));
  const [auth, setAuth] = React.useState(text(current.authAttemptsPerMinute));
  const [widgetMessages, setWidgetMessages] = React.useState(text(current.widgetMessagesPerMinute));
  const [widgetReads, setWidgetReads] = React.useState(text(current.widgetReadsPerMinute));
  const [session, setSession] = React.useState(text(current.sessionMinutes));
  const [origins, setOrigins] = React.useState(current.allowedOrigins.join("\n"));

  return (
    <form
      className="space-y-8"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate({
          rateLimitingEnabled: limiting === "" ? null : limiting === "true",
          requestsPerMinute: number(requests),
          authAttemptsPerMinute: number(auth),
          widgetMessagesPerMinute: number(widgetMessages),
          widgetReadsPerMinute: number(widgetReads),
          sessionMinutes: number(session),
          allowedOrigins: origins.split("\n").map((o) => o.trim()).filter(Boolean),
        });
      }}
    >
      <Section title="Límites de uso" description="Protegen la instalación de abusos y cuidan la cuota de IA. Un campo vacío usa el valor del servidor.">
        <div className="max-w-md space-y-4">
          <Select
            label="Límite general de peticiones"
            hint="Los intentos de inicio de sesión y WebChat se limitan siempre."
            value={limiting}
            onChange={(e) => setLimiting(e.target.value)}
          >
            <option value="">El del servidor ({defaults.rateLimitingEnabled ? "activado" : "desactivado"})</option>
            <option value="true">Activado</option>
            <option value="false">Desactivado</option>
          </Select>
          <Input
            label="Peticiones por usuario y minuto"
            type="number"
            min={60}
            max={10000}
            placeholder={String(defaults.requestsPerMinute)}
            value={requests}
            onChange={(e) => setRequests(e.target.value)}
          />
          <Input
            label="Intentos de inicio de sesión por minuto"
            type="number"
            min={3}
            max={100}
            placeholder={String(defaults.authAttemptsPerMinute)}
            hint="Por dirección IP. Frena a quien prueba contraseñas."
            value={auth}
            onChange={(e) => setAuth(e.target.value)}
          />
          <Input
            label="Mensajes de WebChat por minuto"
            type="number"
            min={1}
            max={600}
            placeholder={String(defaults.widgetMessagesPerMinute)}
            hint="Por visitante. Cada mensaje llama al modelo de IA."
            value={widgetMessages}
            onChange={(e) => setWidgetMessages(e.target.value)}
          />
          <Input
            label="Consultas de WebChat por minuto"
            type="number"
            min={10}
            max={6000}
            placeholder={String(defaults.widgetReadsPerMinute)}
            hint="Por visitante. Es lo que el widget pregunta para mostrar respuestas nuevas."
            value={widgetReads}
            onChange={(e) => setWidgetReads(e.target.value)}
          />
        </div>
      </Section>

      <Section title="Sesiones" description="Cada cuánto se vuelve a comprobar la cuenta de quien usa el panel.">
        <div className="max-w-md">
          <Input
            label="Minutos entre comprobaciones"
            type="number"
            min={5}
            max={1440}
            placeholder={String(defaults.sessionMinutes)}
            hint="Con un valor bajo, quien pierde un permiso deja de tenerlo antes. Aplica desde el próximo inicio o renovación de sesión."
            value={session}
            onChange={(e) => setSession(e.target.value)}
          />
        </div>
      </Section>

      <Section
        title="Dominios que pueden usar la API"
        description="Solo hace falta si el panel u otra aplicación web llaman a la API desde un dominio distinto. WebChat tiene sus propios dominios en cada canal."
      >
        <div className="max-w-md space-y-2">
          <Textarea
            label="Un dominio por línea"
            rows={4}
            placeholder="https://panel.tuempresa.com"
            hint="Con https y sin ruta."
            value={origins}
            onChange={(e) => setOrigins(e.target.value)}
          />
          {defaults.allowedOrigins.length > 0 && (
            <p className="text-sm text-neutral-600">
              El servidor permite siempre: <span className="break-all">{defaults.allowedOrigins.join(", ")}</span>
            </p>
          )}
        </div>
      </Section>

      <div className="space-y-3">
        <FormError error={save.error} />
        {save.isSuccess && (
          <p role="status" className="flex items-center gap-2 text-sm text-success">
            <CheckCircle2 className="h-4 w-4" aria-hidden /> Ajustes guardados.
          </p>
        )}
        <Button type="submit" loading={save.isPending}>
          Guardar ajustes
        </Button>
      </div>
    </form>
  );
}
