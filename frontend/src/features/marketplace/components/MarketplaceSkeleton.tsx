import React from "react";

export function MarketplaceSkeleton() {
  return (
    <div className="max-w-7xl mx-auto space-y-6 animate-pulse">
      <div className="flex justify-between items-center pb-4 border-b border-zinc-200 dark:border-zinc-800">
        <div className="space-y-2">
          <div className="h-7 w-64 bg-zinc-200 dark:bg-zinc-800 rounded-md" />
          <div className="h-4 w-96 bg-zinc-200 dark:bg-zinc-800 rounded-md" />
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-44 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-5 space-y-4" />
        ))}
      </div>
    </div>
  );
}
