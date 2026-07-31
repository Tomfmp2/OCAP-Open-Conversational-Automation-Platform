import React from "react";
import { Inbox } from "lucide-react";
import { Surface } from "./Surface";

interface EmptyStateProps {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
}

export function EmptyState({
  title,
  description,
  action,
  icon,
}: EmptyStateProps) {
  return (
    <Surface className="px-6 py-12 text-center" padding="none">
      <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-500">
        {icon ?? <Inbox className="h-5 w-5" />}
      </div>
      <h3 className="text-sm font-semibold text-neutral-950">{title}</h3>
      {description && (
        <p className="mx-auto mt-1 max-w-md text-xs text-neutral-500">{description}</p>
      )}
      {action && <div className="mt-4 flex justify-center">{action}</div>}
    </Surface>
  );
}
