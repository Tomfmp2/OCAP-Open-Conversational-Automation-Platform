"use client";

import React from "react";
import { Code2, RefreshCw } from "lucide-react";
import { useDeveloperData } from "@/features/developer/api/useDeveloperData";
import { ApiKeyManager } from "@/features/developer/components/ApiKeyManager";
import { WebhookManager } from "@/features/developer/components/WebhookManager";
import { DeveloperSkeleton } from "@/features/developer/components/DeveloperSkeleton";
import { Button, ErrorState, PageHeader } from "@/shared/components/ui";

export default function DeveloperPage() {
  const {
    data,
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
    createApiKeyMutation,
    revokeApiKeyMutation,
    createWebhookMutation,
    deleteWebhookMutation,
  } = useDeveloperData();

  if (isLoading) {
    return <DeveloperSkeleton />;
  }

  if (isError) {
    return (
      <div className="mx-auto max-w-7xl">
        <ErrorState
          title="No se pudo cargar el espacio de desarrollo"
          message={error instanceof Error ? error.message : undefined}
          onRetry={() => void refetch()}
        />
      </div>
    );
  }

  const { apiKeys, webhooks } = data ?? { apiKeys: [], webhooks: [] };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Espacio de desarrollo"
        description="Credenciales y entregas de eventos para integrar servicios con OCAP."
        icon={<Code2 className="h-5 w-5 text-blue-400" />}
        actions={
          <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
            <RefreshCw className="h-3.5 w-3.5" />
            Actualizar
          </Button>
        }
      />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <ApiKeyManager
          keys={apiKeys}
          onCreate={(name) => createApiKeyMutation.mutateAsync(name)}
          onRevoke={(id) => revokeApiKeyMutation.mutateAsync(id)}
          isCreating={createApiKeyMutation.isPending}
          isRevoking={revokeApiKeyMutation.isPending}
        />
        <WebhookManager
          webhooks={webhooks}
          onCreate={(input) => createWebhookMutation.mutateAsync(input)}
          onDelete={(id) => deleteWebhookMutation.mutateAsync(id)}
          isCreating={createWebhookMutation.isPending}
          isDeleting={deleteWebhookMutation.isPending}
        />
      </div>
    </div>
  );
}
