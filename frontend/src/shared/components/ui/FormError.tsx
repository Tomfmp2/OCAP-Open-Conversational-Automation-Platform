import { AlertCircle } from "lucide-react";

/** Error de una acción, junto al botón que la lanzó. El mensaje viene de la API (dice qué pasó). */
export function FormError({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p role="alert" className="flex items-start gap-2 text-sm text-danger">
      <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
      {error instanceof Error ? error.message : "No se pudo completar la acción."}
    </p>
  );
}
