import React from "react";
import { AlertCircle } from "lucide-react";
import { Button } from "./Button";

interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
}

export function ErrorState({
  title = "No se pudo cargar",
  message = "El servidor no respondió. Comprueba que OCAP esté en marcha e inténtalo de nuevo.",
  onRetry,
}: ErrorStateProps) {
  return (
    <div role="alert" className="rounded-lg border border-danger/30 bg-danger-subtle px-5 py-4">
      <div className="flex items-start gap-3">
        <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-danger" aria-hidden />
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium text-danger">{title}</p>
          <p className="mt-0.5 text-sm text-neutral-700">{message}</p>
        </div>
        {onRetry && (
          <Button variant="secondary" size="sm" onClick={onRetry}>
            Reintentar
          </Button>
        )}
      </div>
    </div>
  );
}
