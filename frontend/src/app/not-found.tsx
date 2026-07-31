import Link from "next/link";

export default function NotFound() {
 return (
 <div className="flex min-h-[60vh] w-full flex-col items-center justify-center gap-3 bg-neutral-50 px-6 text-center">
 <p className="text-sm font-medium uppercase tracking-wide text-neutral-500">404</p>
 <h1 className="text-2xl font-semibold text-neutral-950">
 Página no encontrada
 </h1>
 <p className="max-w-md text-sm text-neutral-500">
 La ruta solicitada no existe o fue movida. Vuelve al panel principal.
 </p>
 <Link
 href="/"
 className="mt-2 rounded-lg border border-neutral-200 bg-white px-4 py-2 text-sm text-neutral-800 hover:bg-neutral-100"
 >
 Ir al inicio
 </Link>
 </div>
 );
}
