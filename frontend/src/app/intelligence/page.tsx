"use client";

import React from "react";
import { Cpu, Plus, RefreshCw, Activity } from "lucide-react";
import { useIntelligenceData, AiProviderConfigDto } from "@/features/intelligence/api/useIntelligenceData";
import { ProviderCard } from "@/features/intelligence/components/ProviderCard";
import { AddProviderModal } from "@/features/intelligence/components/AddProviderModal";
import { IntelligenceSkeleton } from "@/features/intelligence/components/IntelligenceSkeleton";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { MetricCard } from "@/shared/components/ui/MetricCard";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";

export default function IntelligencePage() {
 const {
 data: providers,
 isLoading,
 isError,
 error,
 refetch,
 isFetching,
 testProviderMutation,
 } = useIntelligenceData();
 const [modalOpen, setModalOpen] = React.useState(false);
 const [testingId, setTestingId] = React.useState<string | null>(null);

 if (isLoading) {
 return <IntelligenceSkeleton />;
 }

 if (isError) {
 return <div className="mx-auto max-w-7xl"><ErrorState message={error instanceof Error ? error.message : undefined} onRetry={() => void refetch()} /></div>;
 }

 const handleTest = (id: string) => {
 setTestingId(id);
 testProviderMutation.mutate(id, {
 onSettled: () => setTestingId(null),
 });
 };

 const providerList = providers || [];
 const activeCount = providerList.filter((p) => p.isActive).length;

 return (
 <div className="mx-auto max-w-7xl space-y-6">
 <PageHeader
 title="IA y modelos"
 description="Proveedores y modelos disponibles para el runtime."
 icon={<Cpu className="h-5 w-5 text-neutral-700" />}
 actions={<>
 <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}><RefreshCw className="h-3.5 w-3.5" /> Actualizar</Button>
 <Button size="sm" onClick={() => setModalOpen(true)}><Plus className="h-3.5 w-3.5" /> Registrar proveedor</Button>
 </>}
 />
 <div className="grid gap-4 sm:grid-cols-2">
 <MetricCard title="Proveedores configurados" value={providerList.length} icon={Cpu} tone="accent" />
 <MetricCard title="Proveedores activos" value={activeCount} icon={Activity} tone="success" />
 </div>
 {providerList.length === 0 ? (
 <EmptyState title="No hay proveedores configurados" description="Registra un proveedor cuando tengas un endpoint o credenciales válidas." action={<Button size="sm" onClick={() => setModalOpen(true)}><Plus className="h-4 w-4" /> Registrar proveedor</Button>} />
 ) : (
 <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
 {providerList.map((provider: AiProviderConfigDto) => (
 <ProviderCard
 key={provider.id}
 provider={provider}
 onTest={handleTest}
 isTesting={testingId === provider.providerType}
 />
 ))}
 </div>
 )}

 <AddProviderModal open={modalOpen} onClose={() => setModalOpen(false)} />
 </div>
 );
}
