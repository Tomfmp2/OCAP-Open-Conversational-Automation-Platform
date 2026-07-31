import React from "react";

export function SettingsSkeleton() {
 return (
 <div className="max-w-7xl mx-auto space-y-6 animate-pulse">
 <div className="flex justify-between items-center pb-4 border-b border-neutral-200">
 <div className="space-y-2">
 <div className="h-7 w-64 bg-neutral-200 rounded-md" />
 <div className="h-4 w-96 bg-neutral-200 rounded-md" />
 </div>
 </div>
 <div className="h-64 bg-white border border-neutral-200 rounded-xl p-5" />
 </div>
 );
}
