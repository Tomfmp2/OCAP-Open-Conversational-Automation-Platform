import { create } from "zustand";

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  isDefault?: boolean;
}

interface TenantState {
  activeTenant: Tenant;
  tenants: Tenant[];
  sidebarCollapsed: boolean;
  setActiveTenant: (tenant: Tenant) => void;
  toggleSidebar: () => void;
}

const DEFAULT_TENANTS: Tenant[] = [
  { id: "e8392929-1111-4444-8888-999999999999", name: "OCAP Enterprise HQ", slug: "ocap-hq", isDefault: true },
  { id: "a1112222-3333-4444-5555-666666666666", name: "Acme Operations Corp", slug: "acme-corp" },
];

export const useTenantStore = create<TenantState>((set) => ({
  activeTenant: DEFAULT_TENANTS[0],
  tenants: DEFAULT_TENANTS,
  sidebarCollapsed: false,
  setActiveTenant: (tenant) => set({ activeTenant: tenant }),
  toggleSidebar: () => set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),
}));
