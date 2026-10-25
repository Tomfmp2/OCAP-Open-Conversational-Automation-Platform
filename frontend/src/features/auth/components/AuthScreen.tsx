import React from "react";

/** Marco de las pantallas sin sesión (login, aceptar invitación, recuperar contraseña). */
export function AuthScreen({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <div className="relative min-h-dvh w-full bg-neutral-50 text-neutral-950">
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.35]"
        style={{
          backgroundImage:
            "linear-gradient(#e5e5e5 1px, transparent 1px), linear-gradient(90deg, #e5e5e5 1px, transparent 1px)",
          backgroundSize: "48px 48px",
        }}
        aria-hidden
      />

      <div className="relative mx-auto flex min-h-dvh w-full max-w-md flex-col justify-center px-5 py-12">
        <div className="mb-10">
          <p className="text-3xl font-semibold tracking-tight">OCAP</p>
          <p className="mt-1 text-sm text-neutral-500">Open Conversational Automation Platform</p>
        </div>

        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        {description && <p className="mt-2 text-sm text-neutral-500">{description}</p>}
        {children}
      </div>
    </div>
  );
}

export function AuthAlert({ children }: { children: React.ReactNode }) {
  return (
    <div role="alert" className="flex items-start gap-2 rounded-md border-2 border-neutral-950 bg-white px-3.5 py-3 text-sm text-neutral-950">
      {children}
    </div>
  );
}
