export default function Loading() {
  return (
    <div className="flex h-full w-full items-center justify-center bg-neutral-50">
      <div
        className="h-8 w-8 animate-spin rounded-full border-2 border-neutral-300 border-t-neutral-950"
        role="status"
        aria-label="Cargando"
      />
    </div>
  );
}
