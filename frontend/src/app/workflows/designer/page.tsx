import { redirect } from "next/navigation";

/** Diseñador de workflows fuera del alcance de v1 — redirige al resumen. */
export default function WorkflowDesignerRemovedPage() {
  redirect("/");
}
