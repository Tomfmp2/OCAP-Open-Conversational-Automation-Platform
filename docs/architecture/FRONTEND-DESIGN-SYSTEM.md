# OCAP Enterprise Dashboard — Frontend Architecture & Design System Specification

## 1. Análisis de Inspiración Visual (Reference Architecture)

A partir de la inspección del layout de referencia, extraemos los siguientes principios fundamentales adaptados para la plataforma **OCAP**:

- **Navegación en Capas (Dual-Sidebar System)**:
  - **Primary Navigation Rail (Slim)**: Barra vertical fija a la izquierda (ancho ~64px), superficie neutra oscura (`bg-zinc-950`), dedicada a la conmutación entre módulos principales (Dashboard, Canales, Agentes, Workflows, Seguridad, Ajustes).
  - **Secondary Navigation Sidebar (Expandible/Colapsable)**: Panel secundario de contexto (~240px) que presenta submenús, árboles jerárquicos de proyectos/documentos, estado activo y accesos directos por Tenant.
- **Densidad de Información Equilibrada**:
  - Encabezados claros sin saturación visual.
  - Tarjetas analíticas principales con tipografía numérica prominente (`text-3xl / font-semibold`) e indicadores de tendencia (`+204%` badge).
  - Pestañas horizontales limpias (`Tabs`) para cambiar de vista sin recargar el contexto.
- **Estética Enterprise Minimalista**:
  - Bordes finos y limpios (`border-zinc-200 / border-zinc-800`), radio de curvatura sutil (`rounded-lg` de 6px a 8px).
  - Paleta monocromática de grises neutros (`zinc`/`slate`) con acentos de estado hiper-específicos (Azul OCAP para selección, Verde para salud/éxito, Amarillo para alertas, Rojo para fallas).

---

## 2. Design System Tokens & Configuration

### 2.1 CSS Variables (`globals.css`)

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    /* Brand & Neutrals (Light Mode) */
    --background: 0 0% 98%;           /* #FAFAFA */
    --foreground: 240 10% 3.9%;       /* #09090B */
    
    --card: 0 0% 100%;                /* #FFFFFF */
    --card-foreground: 240 10% 3.9%;
    
    --popover: 0 0% 100%;
    --popover-foreground: 240 10% 3.9%;

    --primary: 221.2 83.2% 53.3%;     /* #2563EB - OCAP Enterprise Blue */
    --primary-foreground: 210 40% 98%;
    
    --secondary: 240 4.8% 95.9%;      /* #F4F4F5 */
    --secondary-foreground: 240 5.9% 10%;

    --muted: 240 4.8% 95.9%;
    --muted-foreground: 240 3.8% 46.1%; /* #71717A */

    --accent: 240 4.8% 95.9%;
    --accent-foreground: 240 5.9% 10%;

    /* Status Colors */
    --success: 142.1 76.2% 36.3%;     /* #16A34A */
    --warning: 37.7 92.1% 50.2%;     /* #EAB308 */
    --destructive: 346.8 77.2% 49.8%;/* #DC2626 */
    --destructive-foreground: 210 40% 98%;

    --border: 240 5.9% 90%;          /* #E4E4E7 */
    --input: 240 5.9% 90%;
    --ring: 221.2 83.2% 53.3%;
    --radius: 0.375rem;              /* 6px rounded */

    /* Sidebar & Navigation Rail */
    --rail-background: 240 10% 3.9%;  /* #09090B Dark Rail */
    --rail-foreground: 240 5% 64.9%;
    --rail-active: 0 0% 100%;
  }

  .dark {
    --background: 240 10% 3.9%;       /* #09090B */
    --foreground: 0 0% 98%;           /* #FAFAFA */
    
    --card: 240 10% 5.9%;             /* #0F0F12 */
    --card-foreground: 0 0% 98%;
    
    --popover: 240 10% 5.9%;
    --popover-foreground: 0 0% 98%;

    --primary: 217.2 91.2% 59.8%;     /* #3B82F6 */
    --primary-foreground: 222.2 47.4% 11.2%;

    --secondary: 240 3.7% 15.9%;
    --secondary-foreground: 0 0% 98%;

    --muted: 240 3.7% 15.9%;
    --muted-foreground: 240 5% 64.9%;

    --accent: 240 3.7% 15.9%;
    --accent-foreground: 0 0% 98%;

    --success: 142.1 70.6% 45.3%;
    --warning: 48 96% 53%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 0 0% 98%;

    --border: 240 3.7% 15.9%;        /* #27272A */
    --input: 240 3.7% 15.9%;
    --ring: 217.2 91.2% 59.8%;

    --rail-background: 240 10% 2.5%;
    --rail-foreground: 240 5% 64.9%;
    --rail-active: 0 0% 100%;
  }
}
```

### 2.2 Configuración Tailwind CSS (`tailwind.config.ts`)

```typescript
import type { Config } from "tailwindcss";
import { fontFamily } from "tailwindcss/defaultTheme";

const config: Config = {
  darkMode: ["class"],
  content: [
    "./src/app/**/*.{ts,tsx}",
    "./src/features/**/*.{ts,tsx}",
    "./src/shared/**/*.{ts,tsx}",
    "./src/components/**/*.{ts,tsx}",
  ],
  theme: {
    container: {
      center: true,
      padding: "1.5rem",
      screens: {
        "2xl": "1440px",
      },
    },
    extend: {
      fontFamily: {
        sans: ["var(--font-inter)", ...fontFamily.sans],
        mono: ["var(--font-jetbrains-mono)", ...fontFamily.mono],
      },
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        success: {
          DEFAULT: "hsl(var(--success))",
        },
        warning: {
          DEFAULT: "hsl(var(--warning))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        rail: {
          background: "hsl(var(--rail-background))",
          foreground: "hsl(var(--rail-foreground))",
          active: "hsl(var(--rail-active))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      spacing: {
        "4.5": "1.125rem",
        "13": "3.25rem",
        "15": "3.75rem",
        "18": "4.5rem",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
};

export default config;
```

---

## 3. Arquitectura Frontend Basada en Features (`Feature-Based`)

El proyecto frontend estará estructurado bajo la arquitectura basada en características (`Feature-Based Architecture`) para mantener máxima modularidad y aislamiento:

```
frontend/
├── src/
│   ├── app/                          # Next.js App Router Pages & Layouts
│   │   ├── [locale]/
│   │   │   ├── (auth)/
│   │   │   │   ├── login/page.tsx
│   │   │   │   └── layout.tsx
│   │   │   ├── (dashboard)/
│   │   │   │   ├── overview/page.tsx
│   │   │   │   ├── channels/page.tsx
│   │   │   │   ├── intelligence/page.tsx
│   │   │   │   ├── agents/page.tsx
│   │   │   │   ├── workflows/page.tsx
│   │   │   │   ├── security/page.tsx
│   │   │   │   ├── settings/page.tsx
│   │   │   │   └── layout.tsx
│   │   │   └── layout.tsx
│   │   └── api/                      # Next.js API Routes (Proxy / Auth)
│   ├── features/                     # Módulos Funcionales Independientes
│   │   ├── overview/                 # Dashboard Analytics & Widgets
│   │   │   ├── components/           # Widgets, Summary Cards, Metric Grids
│   │   │   ├── hooks/                # useOverviewMetrics, useSystemHealth
│   │   │   ├── services/             # overviewService.ts
│   │   │   └── types/                # overview.types.ts
│   │   ├── channels/                 # Gestión Dinámica de Canales (CAP-01 / CAP-02)
│   │   │   ├── components/           # ChannelCard, ChannelConfigModal, TelegramConnectQR
│   │   │   ├── hooks/                # useChannels, useChannelStatus
│   │   │   └── services/             # channelService.ts
│   │   ├── intelligence/             # AI Provider Runtime & Vault (CAP-04)
│   │   │   ├── components/           # ProviderConfigTable, ModelSelector, TokenUsageChart
│   │   │   └── hooks/                # useAiProviders, useProviderHealth
│   │   ├── agents/                   # Enterprise Assistant & Sub-Agents (CAP-03)
│   │   │   ├── components/           # AgentRuntimeCard, CapabilityMatrix, AgentLogs
│   │   │   └── hooks/                # useAgents, useAgentExecution
│   │   ├── workflows/                # Visual Workflow Builder & Monitoring
│   │   ├── security/                 # Multi-Tenant, Users, Roles, Audit Logs
│   │   └── settings/                 # System Preferences & Localization
│   ├── shared/                       # Componentes y Utilidades Reutilizables
│   │   ├── components/               # Primitivas UI (Buttons, Cards, Modals, Tables)
│   │   │   ├── ui/                   # Componentes Base Shadcn Personalizados
│   │   │   ├── navigation/           # PrimaryRail, SecondarySidebar, Topbar
│   │   │   ├── data-display/         # DataTable, VirtualList, Badge, Skeleton
│   │   │   └── feedback/             # Toast, EmptyState, CommandPalette
│   │   ├── hooks/                    # useTenant, useTheme, useDebounce, useShortcut
│   │   ├── stores/                   # Zustand Stores (useTenantStore, useAuthStore)
│   │   ├── providers/                # QueryProvider, ThemeProvider, NextIntlProvider
│   │   ├── services/                 # apiClient.ts (Axios/Fetch Wrapper)
│   │   ├── types/                    # Common DTOs & API Contracts
│   │   └── utils/                    # cn(), formatters, validators
│   ├── i18n/                         # Archivos de Traducción Multi-idioma
│   │   ├── messages/
│   │   │   ├── es.json
│   │   │   ├── en.json
│   │   │   └── de.json
│   │   └── navigation.ts
│   └── styles/                       # CSS Globales & Design Tokens
```

---

## 4. Definición de Componentes Base de Navegación & Layout

### 4.1 Primary Navigation Rail (`PrimaryRail.tsx`)
- Ancho fijo `64px`, superficie `bg-rail-background`, iconos Lucide neutros con indicador activo en azul de marca (`primary`).
- Conmutación directa entre las grandes áreas de la plataforma:
  - **Overview** (`LayoutDashboard`)
  - **Canales** (`MessageSquare`)
  - **IA & Modelos** (`Cpu` / `Sparkles`)
  - **Agentes** (`Bot`)
  - **Workflows** (`GitFork`)
  - **Seguridad** (`ShieldCheck`)
  - **Configuración** (`Settings`)

### 4.2 Secondary Navigation Sidebar (`SecondarySidebar.tsx`)
- Panel colapsable de 240px con transición Framer Motion.
- Selector contextual de **Tenant activo** con menú desplegable.
- Menús organizados por grupos funcionales con número de elementos activos o estado (Badges de salud verde/rojo).
- Buscador local integrado con filtrado dinámico.

### 4.3 Enterprise Topbar (`Topbar.tsx`)
- **Command Palette (`Ctrl+K`)**: Acceso rápido global a acciones, agentes, canales y navegación.
- **System Health Pill**: Indicador en vivo de estado del núcleo OCAP (`Online / Operational`).
- **Selector de Idioma**: Dropdown sutil (`Español`, `English`, `Deutsch`).
- **Selector de Tema**: Toggle Claro / Oscuro.
- **Notificaciones & Perfil**: Avatar con fallback de iniciales y badge de conteo de alertas.

---

## 5. Estrategia de Internacionalización (i18n)

Se utilizará `next-intl` con soporte nativo de Server Components y Client Components.

Ejemplo de estructura de diccionario (`src/i18n/messages/es.json`):

```json
{
  "Navigation": {
    "overview": "Resumen General",
    "channels": "Canales de Comunicación",
    "intelligence": "Proveedores de IA",
    "agents": "Agentes Empresariales",
    "workflows": "Workflows Autónomos",
    "security": "Seguridad & Accesos",
    "settings": "Configuración del Sistema"
  },
  "Overview": {
    "title": "Panel Empresarial OCAP",
    "subtitle": "Gestión inteligente de agentes, procesos y canales en tiempo real.",
    "executionsMetric": "Ejecuciones Totales",
    "activeChannelsMetric": "Canales Activos",
    "aiCostMetric": "Costo IA Consumido",
    "systemHealth": "Salud del Sistema"
  },
  "Channels": {
    "title": "Conectores de Canales",
    "connectTelegram": "Conectar Bot de Telegram",
    "readQrInstruction": "Escanea el código QR desde tu aplicación personal de Telegram para vincular el bot."
  }
}
```

---

## 6. Estado Global & Gestión de Datos (Zustand + TanStack Query)

- **Zustand (`useTenantStore`)**:
  Almacena el Tenant ID activo, la lista de Tenants disponibles para el usuario y el estado colapsado/expandido de la barra lateral.
- **Zustand (`useAuthStore`)**:
  Gestiona la sesión del usuario, JWT Bearer Token, roles y permisos evaluados.
- **TanStack Query (`useQuery` / `useMutation`)**:
  - `useChannelsQuery(tenantId)`
  - `useAiProvidersQuery(tenantId)`
  - `useAgentsQuery(tenantId)`
  - Invalidación automática tras mutaciones con mutación optimistic UI.

---

## 7. Declaración de Cumplimiento Accesibilidad (WCAG 2.2 AA)
- Todos los elementos interactivos cuentan con `aria-label` descriptivos.
- Contraste de texto mínimo de 4.5:1 en modo claro y oscuro.
- Enfoque de teclado completamente visible (`ring-2 ring-primary ring-offset-2`).
- Soporte para atajos de teclado globales mediante `CommandPalette` (`Cmd+K` / `Ctrl+K`).
