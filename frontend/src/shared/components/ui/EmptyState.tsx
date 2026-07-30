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
      <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-xl bg-zinc-100 text-zinc-400 dark:bg-zinc-800">
        {icon ?? <Inbox className="h-5 w-5" />}
      </div>
      <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">{title}</h3>
      {description && (
        <p className="mx-auto mt-1 max-w-md text-xs text-zinc-500">{description}</p>
      )}
      {action && <div className="mt-4 flex justify-center">{action}</div>}
    </Surface>
  );
}
