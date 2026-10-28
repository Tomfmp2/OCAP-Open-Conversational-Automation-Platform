import React from "react";
import { cn } from "@/shared/utils/cn";

interface FieldProps {
  label?: string;
  hint?: string;
  error?: string;
}

const control =
  "focus-ring w-full rounded-md border bg-white px-3 text-sm text-neutral-950 placeholder:text-neutral-500 disabled:bg-neutral-50 disabled:text-neutral-600";

/**
 * Etiqueta, control y mensaje de un campo. La etiqueta siempre queda asociada (useId si no hay id ni name) y la ayuda
 * o el error se enlazan con aria-describedby para que el lector de pantalla los anuncie.
 */
function useField(id: string | undefined, name: string | undefined, { hint, error }: FieldProps) {
  const generated = React.useId();
  const controlId = id || name || generated;
  const messageId = error || hint ? `${controlId}-message` : undefined;
  return { controlId, messageId };
}

function FieldFrame({ label, hint, error, controlId, messageId, children }: FieldProps & { controlId: string; messageId?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      {label && (
        <label htmlFor={controlId} className="block text-sm font-medium text-neutral-800">
          {label}
        </label>
      )}
      {children}
      {error ? (
        <p id={messageId} className="text-xs font-medium text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="text-xs text-neutral-600">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

export function Input({ className, label, hint, error, id, ...props }: FieldProps & React.InputHTMLAttributes<HTMLInputElement>) {
  const { controlId, messageId } = useField(id, props.name, { hint, error });
  return (
    <FieldFrame label={label} hint={hint} error={error} controlId={controlId} messageId={messageId}>
      <input
        id={controlId}
        aria-invalid={error ? true : undefined}
        aria-describedby={messageId}
        className={cn(control, "h-9", error ? "border-danger" : "border-neutral-300", className)}
        {...props}
      />
    </FieldFrame>
  );
}

export function Textarea({ className, label, hint, error, id, ...props }: FieldProps & React.TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const { controlId, messageId } = useField(id, props.name, { hint, error });
  return (
    <FieldFrame label={label} hint={hint} error={error} controlId={controlId} messageId={messageId}>
      <textarea
        id={controlId}
        aria-invalid={error ? true : undefined}
        aria-describedby={messageId}
        className={cn(control, "py-2 leading-relaxed", error ? "border-danger" : "border-neutral-300", className)}
        {...props}
      />
    </FieldFrame>
  );
}

export function Select({ className, label, hint, error, id, children, ...props }: FieldProps & React.SelectHTMLAttributes<HTMLSelectElement>) {
  const { controlId, messageId } = useField(id, props.name, { hint, error });
  return (
    <FieldFrame label={label} hint={hint} error={error} controlId={controlId} messageId={messageId}>
      <select
        id={controlId}
        aria-invalid={error ? true : undefined}
        aria-describedby={messageId}
        className={cn(control, "h-9 pr-8", error ? "border-danger" : "border-neutral-300", className)}
        {...props}
      >
        {children}
      </select>
    </FieldFrame>
  );
}
