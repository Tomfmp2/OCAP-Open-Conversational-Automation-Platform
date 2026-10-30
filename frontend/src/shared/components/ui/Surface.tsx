import React from "react";
import { cn } from "@/shared/utils/cn";

interface SurfaceProps extends React.HTMLAttributes<HTMLDivElement> {
  padding?: "none" | "sm" | "md";
}

const paddingMap = { none: "", sm: "p-4", md: "p-5" };

/** Contenedor blanco con borde: el único nivel de tarjeta del panel. */
export function Surface({ className, padding = "md", children, ...props }: SurfaceProps) {
  return (
    <div className={cn("rounded-lg border border-neutral-200 bg-white", paddingMap[padding], className)} {...props}>
      {children}
    </div>
  );
}
