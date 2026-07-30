import Link from "next/link";

export default function NotFound() {
  return (
    <div className="flex min-h-[60vh] w-full flex-col items-center justify-center gap-3 bg-zinc-50 px-6 text-center dark:bg-zinc-950">
      <p className="text-sm font-medium uppercase tracking-wide text-zinc-500">404</p>
      <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100">
        Página no encontrada
      </h1>
      <p className="max-w-md text-sm text-zinc-500">
        La ruta solicitada no existe o fue movida. Vuelve al panel principal.
      </p>
      <Link
        href="/"
        className="mt-2 rounded-lg border border-zinc-200 bg-white px-4 py-2 text-sm text-zinc-800 hover:bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-100 dark:hover:bg-zinc-800"
      >
        Ir al inicio
      </Link>
    </div>
  );
}
