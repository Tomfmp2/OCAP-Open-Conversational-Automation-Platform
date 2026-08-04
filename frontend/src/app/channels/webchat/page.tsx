"use client";

import { PrincipalAgentChat } from "@/features/assistant/components/PrincipalAgentChat";

export default function WebChatPage() {
  return (
    <div className="mx-auto flex h-[calc(100vh-7.5rem)] max-w-3xl flex-col">
      <PrincipalAgentChat
        className="h-full"
        hero={false}
        title="WebChat"
        description="Canal embebible · mismo agente principal"
      />
    </div>
  );
}
