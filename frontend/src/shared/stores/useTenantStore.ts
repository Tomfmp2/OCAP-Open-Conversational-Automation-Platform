import { create } from "zustand";
import { apiClient } from "@/shared/api/apiClient";
import { getTenantId } from "@/shared/api/tokenStorage";

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  isDefault?: boolean;
}

interface TenantState {
  activeTenant: Tenant | null;
  tenants: Tenant[];
  sidebarCollapsed: boolean;
  isLoadingTenants: boolean;
  setActiveTenant: (tenant: Tenant) => void;
  toggleSidebar: () => void;
  syncFromAuth: (tenantId: string, fallbackName?: string) => void;
  fetchTenants: () => Promise<void>;
}

export const useTenantStore = create<TenantState>((set, get) => ({
  activeTenant: null,
  tenants: [],
  sidebarCollapsed: false,
  isLoadingTenants: false,

  setActiveTenant: (tenant) => {
    set({ activeTenant: tenant });
    if (typeof window !== "undefined") {
      localStorage.setItem("ocap.tenantId", tenant.id);
    }
  },

  toggleSidebar: () => set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),

  syncFromAuth: (tenantId, fallbackName) => {
    const { tenants } = get();
    const existing = tenants.find((t) => t.id === tenantId);
    if (existing) {
      set({ activeTenant: existing });
      return;
    }

    const authTenant: Tenant = {
      id: tenantId,
      name: fallbackName || "Mi Organización",
      slug: tenantId.slice(0, 8),
      isDefault: true,
    };

    set((state) => ({
      activeTenant: authTenant,
      tenants: state.tenants.length > 0 ? state.tenants : [authTenant],
    }));
  },

  fetchTenants: async () => {
    set({ isLoadingTenants: true });
    try {
      const data = await apiClient.get<
        Array<{ id: string; name: string; slug: string; isActive: boolean }>
      >("/api/tenants");

      const tenants: Tenant[] = data.map((t) => ({
        id: t.id,
        name: t.name,
        slug: t.slug,
        isDefault: t.id === getTenantId(),
      }));

      const activeId = getTenantId();
      const activeTenant = tenants.find((t) => t.id === activeId) ?? tenants[0] ?? null;

      set({ tenants, activeTenant, isLoadingTenants: false });
    } catch {
      const tenantId = getTenantId();
      if (tenantId) {
        get().syncFromAuth(tenantId);
      }
      set({ isLoadingTenants: false });
    }
  },
}));
