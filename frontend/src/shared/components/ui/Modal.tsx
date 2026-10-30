"use client";

import React from "react";
import { X } from "lucide-react";
import { cn } from "@/shared/utils/cn";

interface ModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: React.ReactNode;
  className?: string;
}

/**
 * <dialog> nativo con showModal: el navegador atrapa el foco, cierra con Escape, vuelve el foco al botón que lo abrió
 * y deja el resto de la página inerte. En móvil se abre como hoja desde abajo; el contenido largo hace scroll dentro.
 */
export function Modal({ open, onClose, title, description, children, className }: ModalProps) {
  const ref = React.useRef<HTMLDialogElement>(null);
  const titleId = React.useId();

  React.useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onClose={onClose}
      onCancel={(e) => {
        e.preventDefault();
        onClose();
      }}
      // Clic en el fondo (el propio <dialog>, fuera del panel) cierra.
      onClick={(e) => e.target === ref.current && onClose()}
      className={cn(
        "m-0 mt-auto max-h-[92dvh] w-full max-w-none overflow-hidden rounded-t-xl border border-neutral-200 bg-white p-0 text-neutral-950 shadow-xl backdrop:bg-neutral-950/40",
        "sm:m-auto sm:max-h-[85dvh] sm:max-w-lg sm:rounded-xl",
        className
      )}
    >
      {open && (
        <div className="flex max-h-[inherit] flex-col">
          <div className="flex items-start justify-between gap-4 border-b border-neutral-200 px-5 py-4">
            <div className="min-w-0">
              <h2 id={titleId} className="text-base font-semibold">
                {title}
              </h2>
              {description && <p className="mt-0.5 text-sm text-neutral-600">{description}</p>}
            </div>
            <button
              type="button"
              onClick={onClose}
              className="focus-ring -mr-1.5 rounded-md p-1.5 text-neutral-600 hover:bg-neutral-100 hover:text-neutral-950"
              aria-label="Cerrar"
            >
              <X className="h-4 w-4" aria-hidden />
            </button>
          </div>
          <div className="overflow-y-auto px-5 py-5">{children}</div>
        </div>
      )}
    </dialog>
  );
}
