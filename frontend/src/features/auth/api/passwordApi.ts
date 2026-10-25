import { apiClient } from "@/shared/api/apiClient";

export const passwordApi = {
  /** Define la contraseña con el enlace de un solo uso (invitación o recuperación). */
  setup(token: string, newPassword: string): Promise<void> {
    return apiClient.post<void>("/api/auth/password/setup", { token, newPassword }, { skipAuth: true, skipRetry: true });
  },

  forgot(email: string): Promise<{ emailConfigured: boolean }> {
    return apiClient.post<{ emailConfigured: boolean }>("/api/auth/password/forgot", { email }, { skipAuth: true, skipRetry: true });
  },

  options(): Promise<{ emailConfigured: boolean; minLength: number }> {
    return apiClient.get<{ emailConfigured: boolean; minLength: number }>("/api/auth/password/options", { skipAuth: true });
  },
};

/** La misma regla que la API: al menos 12 caracteres con letras y números. */
export function passwordProblem(password: string, minLength = 12): string | null {
  if (password.length < minLength) return `Usa al menos ${minLength} caracteres.`;
  if (!/\p{L}/u.test(password) || !/\d/.test(password)) return "Combina letras y números.";
  return null;
}
