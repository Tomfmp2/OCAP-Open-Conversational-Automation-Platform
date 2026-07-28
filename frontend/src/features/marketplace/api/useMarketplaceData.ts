import { useQuery } from "@tanstack/react-query";

export interface MarketplaceItem {
  id: string;
  name: string;
  category: "Agentes" | "Conectores" | "Herramientas" | "Modelos";
  author: string;
  description: string;
  downloads: number;
  rating: number;
  installed: boolean;
  version: string;
}

const MOCK_MARKETPLACE: MarketplaceItem[] = [
  {
    id: "m-1",
    name: "HubSpot & Salesforce CRM Sync Agent",
    category: "Conectores",
    author: "OCAP Official",
    description: "Sincronización bidireccional automatizada de leads y oportunidades comerciales.",
    downloads: 14200,
    rating: 4.9,
    installed: true,
    version: "v1.2.0",
  },
  {
    id: "m-2",
    name: "PostgreSQL & Qdrant RAG Memory Tool",
    category: "Herramientas",
    author: "Enterprise Labs",
    description: "Indexación de base de conocimientos vectorial en tiempo real para agentes.",
    downloads: 8900,
    rating: 4.8,
    installed: false,
    version: "v2.0.4",
  },
  {
    id: "m-3",
    name: "Financial Invoice Extractor Agent",
    category: "Agentes",
    author: "Fintech Tools",
    description: "Agente especializado en análisis de facturas electrónicas e impuestos.",
    downloads: 11200,
    rating: 5.0,
    installed: true,
    version: "v3.1.0",
  },
];

export function useMarketplaceData() {
  return useQuery<MarketplaceItem[]>({
    queryKey: ["marketplaceData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_MARKETPLACE;
    },
    staleTime: 30000,
  });
}
