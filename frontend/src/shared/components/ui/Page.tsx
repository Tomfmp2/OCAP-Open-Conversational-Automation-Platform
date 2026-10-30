import React from "react";
import { cn } from "@/shared/utils/cn";

/** Ancho de lectura y márgenes de todas las pantallas del panel: listas a 5xl, formularios a 2xl. */
export function Page({ children, width = "default" }: { children: React.ReactNode; width?: "default" | "narrow" }) {
  return (
    <div className={cn("mx-auto w-full space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-10", width === "narrow" ? "max-w-2xl" : "max-w-5xl")}>
      {children}
    </div>
  );
}
