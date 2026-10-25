"use client";

import React from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { MailCheck } from "lucide-react";
import { AuthScreen } from "@/features/auth/components/AuthScreen";
import { passwordApi } from "@/features/auth/api/passwordApi";
import { Button, Input } from "@/shared/components/ui";

export default function ForgotPasswordPage() {
  const options = useQuery({ queryKey: ["passwordOptions"], queryFn: passwordApi.options });
  const [email, setEmail] = React.useState("");
  const [sent, setSent] = React.useState(false);
  const [submitting, setSubmitting] = React.useState(false);

  const back = (
    <Link href="/login" className="focus-ring mt-6 self-start text-sm underline underline-offset-2">
      Volver a iniciar sesión
    </Link>
  );

  if (options.data && !options.data.emailConfigured) {
    return (
      <AuthScreen
        title="Recuperar contraseña"
        description="Este servidor no envía correos. Pide a un administrador de tu organización un enlace para restablecerla: lo genera desde Equipo."
      >
        {back}
      </AuthScreen>
    );
  }

  if (sent) {
    return (
      <AuthScreen title="Revisa tu correo" description="Si el correo corresponde a una cuenta, te enviamos un enlace para definir una nueva contraseña. Vence en 2 horas.">
        <p className="mt-6 flex items-center gap-2 text-sm text-neutral-700">
          <MailCheck className="h-4 w-4" aria-hidden /> Revisa también la carpeta de spam.
        </p>
        {back}
      </AuthScreen>
    );
  }

  return (
    <AuthScreen title="Recuperar contraseña" description="Te enviaremos un enlace para definir una nueva.">
      <form
        className="mt-8 space-y-4"
        onSubmit={async (e) => {
          e.preventDefault();
          setSubmitting(true);
          try {
            await passwordApi.forgot(email.trim());
          } finally {
            // La respuesta es la misma exista o no la cuenta: siempre se muestra el mismo mensaje.
            setSubmitting(false);
            setSent(true);
          }
        }}
      >
        <Input id="email" label="Email" type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <Button type="submit" size="lg" loading={submitting || options.isLoading} className="mt-2 w-full">
          Enviar enlace
        </Button>
      </form>
      {back}
    </AuthScreen>
  );
}
