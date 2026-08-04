"use client";

import { PrincipalAgentChat } from "@/features/assistant/components/PrincipalAgentChat";

export default function HomePage() {
  return (
    <div className="mx-auto flex h-[calc(100vh-7.5rem)] w-full max-w-4xl flex-col">
      <PrincipalAgentChat className="h-full" hero />
    </div>
  );
}
