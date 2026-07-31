import React from "react";

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  icon?: React.ReactNode;
}

export function PageHeader({ title, description, actions, icon }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-4 border-b border-neutral-200 pb-4 sm:flex-row sm:items-end sm:justify-between">
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          {icon}
          <h1 className="truncate text-2xl font-semibold tracking-tight text-neutral-950">
            {title}
          </h1>
        </div>
        {description && (
          <p className="mt-1 max-w-2xl text-sm text-neutral-500">{description}</p>
        )}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  );
}
