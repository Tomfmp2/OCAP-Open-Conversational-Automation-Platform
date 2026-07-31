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
        className={cn(
          "w-full rounded-md border bg-white px-3.5 py-2.5 text-sm text-neutral-950 placeholder:text-neutral-400 focus-ring",
          error ? "border-neutral-950" : "border-neutral-300",
          className
        )}
        {...props}
      />
      {error ? (
        <p className="text-[11px] font-medium text-neutral-950">{error}</p>
      ) : hint ? (
        <p className="text-[11px] text-neutral-500">{hint}</p>
      ) : null}
    </div>
  );
}
