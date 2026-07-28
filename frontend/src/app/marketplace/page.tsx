"use client";

import React from "react";
import { Store, Search, Filter, RefreshCw } from "lucide-react";
import { useMarketplaceData } from "@/features/marketplace/api/useMarketplaceData";
import { MarketplaceItemCard } from "@/features/marketplace/components/MarketplaceItemCard";
import { MarketplaceSkeleton } from "@/features/marketplace/components/MarketplaceSkeleton";

export default function MarketplacePage() {
  const { data, isLoading, refetch, isFetching } = useMarketplaceData();
  const [searchQuery, setSearchQuery] = React.useState("");
  const [selectedCategory, setSelectedCategory] = React.useState<string>("all");
  const [items, setItems] = React.useState(data || []);

  React.useEffect(() => {
    if (data) setItems(data);
  }, [data]);

  if (isLoading) {
    return <MarketplaceSkeleton />;
  }

  const handleInstallToggle = (id: string) => {
    setItems((prev) =>
      prev.map((item) => (item.id === id ? { ...item, installed: !item.installed } : item))
    );
  };

  const filteredItems = items.filter((item) => {
    const matchesQuery =
      item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.description.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCategory = selectedCategory === "all" ? true : item.category === selectedCategory;
    return matchesQuery && matchesCategory;
  });

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Store className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Marketplace de Módulos, Agentes & Conectores
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Descubre e instala extensiones auditadas para potenciar el ecosistema OCAP de tu organización.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualizar Catálogo</span>
          </button>
        </div>
      </div>

      {/* Search & Category Filter */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-white dark:bg-zinc-900 p-4 border border-zinc-200 dark:border-zinc-800/80 rounded-xl shadow-sm">
        <div className="relative w-full sm:w-96">
          <Search className="w-4 h-4 absolute left-3 top-2.5 text-zinc-400" />
          <input
            type="text"
            placeholder="Buscar por nombre, descripción o autor..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full bg-zinc-100 dark:bg-zinc-950 border border-zinc-200 dark:border-zinc-800 rounded-lg pl-9 pr-3 py-2 text-xs text-zinc-900 dark:text-zinc-100 placeholder:text-zinc-400 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>

        <div className="flex items-center gap-1.5 text-xs w-full sm:w-auto overflow-x-auto">
          <Filter className="w-3.5 h-3.5 text-zinc-400 mr-1" />
          {(["all", "Agentes", "Conectores", "Herramientas", "Modelos"] as const).map((cat) => (
            <button
              key={cat}
              onClick={() => setSelectedCategory(cat)}
              className={`px-3 py-1 rounded-lg text-xs font-medium transition-colors ${
                selectedCategory === cat
                  ? "bg-blue-600 text-white font-bold"
                  : "bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-200"
              }`}
            >
              {cat === "all" ? "Todos" : cat}
            </button>
          ))}
        </div>
      </div>

      {/* Marketplace Items Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filteredItems.map((item) => (
          <MarketplaceItemCard
            key={item.id}
            item={item}
            onInstallToggle={handleInstallToggle}
          />
        ))}
      </div>
    </div>
  );
}
