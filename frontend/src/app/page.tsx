"use client";

import { PrincipalAgentChat } from "@/features/assistant/components/PrincipalAgentChat";

export default function HomePage() {
  return (
    <div className="flex h-full w-full flex-col px-4 py-4 sm:px-6">
      <PrincipalAgentChat className="h-full" hero />
    </div>
  );
}
