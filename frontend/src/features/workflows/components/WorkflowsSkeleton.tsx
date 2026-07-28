import React from "react";

export function WorkflowsSkeleton() {
  return (
    <div className="max-w-7xl mx-auto space-y-6 animate-pulse">
      <div className="flex justify-between items-center pb-4 border-b border-zinc-200 dark:border-zinc-800">
        <div className="space-y-2">
          <div className="h-7 w-64 bg-zinc-200 dark:bg-zinc-800 rounded-md" />
          <div className="h-4 w-96 bg-zinc-200 dark:bg-zinc-800 rounded-md" />
        </div>
        <div className="h-9 w-36 bg-zinc-200 dark:bg-zinc-800 rounded-lg" />
      </div>

      <div className="space-y-4">
        {[1, 2].map((i) => (
          <div key={i} className="h-40 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-5" />
        ))}
      </div>
    </div>
  );
}
