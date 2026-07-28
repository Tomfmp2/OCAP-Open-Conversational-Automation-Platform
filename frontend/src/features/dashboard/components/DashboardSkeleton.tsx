import React from "react";

export function DashboardSkeleton() {
  return (
    <div className="max-w-7xl mx-auto space-y-6 animate-pulse">
      {/* Header Skeleton */}
      <div className="flex justify-between items-center pb-4 border-b border-zinc-200 dark:border-zinc-800">
        <div className="space-y-2">
          <div className="h-7 w-64 bg-zinc-200 dark:bg-zinc-800 rounded-md" />
          <div className="h-4 w-96 bg-zinc-200 dark:bg-zinc-800 rounded-md" />
        </div>
        <div className="flex gap-2">
          <div className="h-9 w-24 bg-zinc-200 dark:bg-zinc-800 rounded-lg" />
          <div className="h-9 w-32 bg-zinc-200 dark:bg-zinc-800 rounded-lg" />
        </div>
      </div>

      {/* KPI Skeleton Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="h-28 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-4 space-y-3">
            <div className="flex justify-between">
              <div className="h-4 w-24 bg-zinc-200 dark:bg-zinc-800 rounded" />
              <div className="h-6 w-6 bg-zinc-200 dark:bg-zinc-800 rounded-lg" />
            </div>
            <div className="h-8 w-32 bg-zinc-200 dark:bg-zinc-800 rounded" />
          </div>
        ))}
      </div>

      {/* Main Charts & Widgets Skeleton Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <div className="h-72 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-4" />
          <div className="h-60 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-4" />
        </div>
        <div className="space-y-6">
          <div className="h-64 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-4" />
          <div className="h-68 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-4" />
        </div>
      </div>
    </div>
  );
}
