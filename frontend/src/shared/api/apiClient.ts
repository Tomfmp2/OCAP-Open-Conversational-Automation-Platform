import { ApiError, type ApiRequestOptions } from "./types";
import {
  clearAuthSession,
  getAccessToken,
  getRefreshToken,
  getTenantId,
  updateAccessToken,
} from "./tokenStorage";

const DEFAULT_TIMEOUT_MS = 30_000;
const MAX_RETRIES = 2;
const RETRYABLE_STATUSES = new Set([429, 500, 502, 503, 504]);

function generateId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function getBaseUrl(): string {
  return process.env.NEXT_PUBLIC_API_URL || "";
}

function buildUrl(path: string): string {
  const base = getBaseUrl();
  if (path.startsWith("http")) return path;
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${base}${normalized}`;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

type RefreshHandler = () => Promise<string | null>;

class ApiClient {
  private refreshHandler: RefreshHandler | null = null;
  private refreshPromise: Promise<string | null> | null = null;

  setRefreshHandler(handler: RefreshHandler): void {
    this.refreshHandler = handler;
  }

  private async attemptRefresh(): Promise<string | null> {
    if (!this.refreshHandler) return null;
    if (!this.refreshPromise) {
      this.refreshPromise = this.refreshHandler().finally(() => {
        this.refreshPromise = null;
      });
    }
    return this.refreshPromise;
  }

  private handleUnauthorized(): void {
    clearAuthSession();
    if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
      window.location.href = "/login";
    }
  }

  private buildHeaders(
    initHeaders?: Record<string, string>,
    body?: unknown,
    skipAuth?: boolean
  ): Headers {
    const headers = new Headers(initHeaders);

    if (body !== undefined && !(body instanceof FormData) && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }

    headers.set("X-Correlation-Id", generateId());
    headers.set("X-Request-Id", generateId());

    if (!skipAuth) {
      const accessToken = getAccessToken();
      const tenantId = getTenantId();
      if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
      if (tenantId) headers.set("X-Tenant-Id", tenantId);
    }

    return headers;
  }

  async request<T>(
    method: string,
    path: string,
    body?: unknown,
    options: ApiRequestOptions = {}
  ): Promise<T> {
    const { signal, timeout = DEFAULT_TIMEOUT_MS, skipAuth, skipRetry, headers: customHeaders } = options;
    const url = buildUrl(path);
    let attempt = 0;
    let triedRefresh = false;

    while (true) {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), timeout);

      const onAbort = () => controller.abort();
      if (signal) {
        if (signal.aborted) {
          controller.abort();
        } else {
          signal.addEventListener("abort", onAbort);
        }
      }

      try {
        const init: RequestInit = {
          method,
          headers: this.buildHeaders(customHeaders, body, skipAuth),
          signal: controller.signal,
        };

        if (body !== undefined) {
          init.body = body instanceof FormData ? body : JSON.stringify(body);
        }

        const response = await fetch(url, init);

        if (response.status === 401 && !skipAuth && !triedRefresh) {
          triedRefresh = true;
          const newToken = await this.attemptRefresh();
          if (newToken) {
            continue;
          }
          this.handleUnauthorized();
          throw new ApiError("Sesión expirada", 401);
        }

        if (response.status === 403) {
          const errorBody = await this.parseBody(response);
          throw new ApiError("Acceso denegado", 403, errorBody);
        }

        if (!response.ok) {
          const errorBody = await this.parseBody(response);

          if (
            !skipRetry &&
            RETRYABLE_STATUSES.has(response.status) &&
            attempt < MAX_RETRIES
          ) {
            attempt += 1;
            await sleep(300 * 2 ** attempt);
            continue;
          }

          const message =
            typeof errorBody === "object" &&
            errorBody !== null &&
            "message" in errorBody &&
            typeof (errorBody as { message: unknown }).message === "string"
              ? (errorBody as { message: string }).message
              : `Error HTTP ${response.status}`;

          throw new ApiError(message, response.status, errorBody);
        }

        if (response.status === 204) {
          return undefined as T;
        }

        const contentType = response.headers.get("content-type") || "";
        if (contentType.includes("application/json")) {
          return (await response.json()) as T;
        }

        const text = await response.text();
        return text as T;
      } catch (error) {
        if (error instanceof ApiError) throw error;
        if (error instanceof DOMException && error.name === "AbortError") {
          throw new ApiError("La solicitud excedió el tiempo de espera", 408);
        }
        throw error;
      } finally {
        clearTimeout(timeoutId);
        if (signal) {
          signal.removeEventListener("abort", onAbort);
        }
      }
    }
  }

  private async parseBody(response: Response): Promise<unknown> {
    const contentType = response.headers.get("content-type") || "";
    if (contentType.includes("application/json")) {
      try {
        return await response.json();
      } catch {
        return null;
      }
    }
    try {
      return await response.text();
    } catch {
      return null;
    }
  }

  get<T>(path: string, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>("GET", path, undefined, options);
  }

  post<T>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>("POST", path, body, options);
  }

  put<T>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>("PUT", path, body, options);
  }

  patch<T>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>("PATCH", path, body, options);
  }

  delete<T>(path: string, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>("DELETE", path, undefined, options);
  }

  upload<T>(path: string, formData: FormData, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>("POST", path, formData, options);
  }
}

export const apiClient = new ApiClient();

export async function defaultRefreshHandler(): Promise<string | null> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return null;

  try {
    const response = await fetch(buildUrl("/api/auth/refresh"), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Correlation-Id": generateId(),
        "X-Request-Id": generateId(),
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) return null;

    const data = (await response.json()) as {
      accessToken: string;
      refreshToken?: string;
    };

    updateAccessToken(data.accessToken);
    return data.accessToken;
  } catch {
    return null;
  }
}

apiClient.setRefreshHandler(defaultRefreshHandler);

export { ApiError };
export type { ApiRequestOptions };
