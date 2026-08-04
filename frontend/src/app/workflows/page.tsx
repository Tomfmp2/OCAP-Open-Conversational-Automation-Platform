import { redirect } from "next/navigation";

/** Workflows fuera del alcance de v1 — redirige al resumen. */
export default function WorkflowsRemovedPage() {
  redirect("/");
}
