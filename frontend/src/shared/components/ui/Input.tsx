import React from "react";
import { cn } from "@/shared/utils/cn";

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
}

export function Input({
  className,
  label,
  hint,
  error,
  id,
  ...props
}: InputProps) {
  const inputId = id || props.name;
  // El error y la ayuda se asocian al campo para que los lectores de pantalla los anuncien.
  const messageId = inputId && (error || hint) ? `${inputId}-message` : undefined;

  return (
    <div className="space-y-1.5">
      {label && (
        <label
          htmlFor={inputId}
          className="block text-xs font-medium tracking-wide text-neutral-700"
        >
          {label}
        </label>
      )}
      <input
        id={inputId}
        aria-invalid={error ? true : undefined}
        aria-describedby={messageId}
        className={cn(
          "w-full rounded-md border bg-white px-3.5 py-2.5 text-sm text-neutral-950 placeholder:text-neutral-500 focus-ring",
          error ? "border-neutral-950" : "border-neutral-300",
          className
        )}
        {...props}
      />
      {error ? (
        <p id={messageId} className="text-xs font-medium text-neutral-950">{error}</p>
      ) : hint ? (
        <p id={messageId} className="text-xs text-neutral-500">{hint}</p>
      ) : null}
    </div>
  );
}
