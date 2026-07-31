import React from "react";
import { AlertCircle } from "lucide-react";
import { Surface } from "./Surface";
import { Button } from "./Button";

interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
}

export function ErrorState({
  title = "No se pudo cargar la información",
  message = "Revisa la conexión con la API e inténtalo de nuevo.",
  onRetry,
}: ErrorStateProps) {
  return (
    <Surface
      role="alert"
      className="border-neutral-950 px-6 py-12 text-center"
      padding="none"
    >
      <AlertCircle className="mx-auto mb-3 h-6 w-6 text-neutral-950" />
      <h3 className="text-sm font-semibold text-neutral-950">{title}</h3>
      <p className="mx-auto mt-1 max-w-md text-xs text-neutral-500">{message}</p>
      {onRetry && (
        <div className="mt-4 flex justify-center">
          <Button type="button" size="sm" onClick={onRetry}>
            Reintentar
          </Button>
        </div>
      )}
    </Surface>
  );
}
