"use client";

import { PrincipalAgentChat } from "@/features/assistant/components/PrincipalAgentChat";

export default function HomePage() {
  return (
    <div className="mx-auto flex h-full w-full max-w-4xl flex-col px-4 py-4 sm:px-6">
      <PrincipalAgentChat className="h-full" hero />
    </div>
  );
}
