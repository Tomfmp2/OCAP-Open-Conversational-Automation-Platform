/** Filas grises mientras carga una lista; el lector de pantalla oye la etiqueta. */
export function ListSkeleton({ label, rows = 3 }: { label: string; rows?: number }) {
  return (
    <div role="status" aria-label={label} className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
      {Array.from({ length: rows }, (_, i) => (
        <div key={i} className="flex items-center gap-4 px-5 py-4">
          <div className="h-4 flex-1 animate-pulse rounded bg-neutral-100 motion-reduce:animate-none" />
          <div className="h-4 w-20 animate-pulse rounded bg-neutral-100 motion-reduce:animate-none" />
        </div>
      ))}
    </div>
  );
}
