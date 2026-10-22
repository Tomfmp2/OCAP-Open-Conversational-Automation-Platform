/** Mismas reglas que SetupRequestValidator en la API: el cliente avisa antes, el servidor decide. */
export const MIN_PASSWORD_LENGTH = 12;

export interface AdminStepForm {
  setupCode: string;
  organizationName: string;
  adminName: string;
  adminEmail: string;
  adminPassword: string;
  confirmPassword: string;
}

export type AdminStepErrors = Partial<Record<keyof AdminStepForm, string>>;

export const emptyAdminStep: AdminStepForm = {
  setupCode: "",
  organizationName: "",
  adminName: "",
  adminEmail: "",
  adminPassword: "",
  confirmPassword: "",
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
/** Código impreso por la API: 12 caracteres, con o sin guiones. */
const CODE_PATTERN = /^[A-Z2-9]{12}$/;

export function normalizeSetupCode(code: string): string {
  return code.toUpperCase().replace(/[^A-Z0-9]/g, "");
}

export function validateAdminStep(form: AdminStepForm): AdminStepErrors {
  const errors: AdminStepErrors = {};

  if (!form.setupCode.trim()) {
    errors.setupCode = "Ingresa el código que muestra la API al arrancar.";
  } else if (!CODE_PATTERN.test(normalizeSetupCode(form.setupCode))) {
    errors.setupCode = "El código tiene 12 caracteres, por ejemplo ABCD-EFGH-JKLM.";
  }

  if (!form.organizationName.trim()) {
    errors.organizationName = "Escribe el nombre de tu organización.";
  } else if (!/[\p{L}\p{N}]/u.test(form.organizationName)) {
    errors.organizationName = "El nombre debe tener al menos una letra o un número.";
  }

  if (!form.adminName.trim()) errors.adminName = "Escribe tu nombre.";

  if (!form.adminEmail.trim()) {
    errors.adminEmail = "Escribe tu correo.";
  } else if (!EMAIL_PATTERN.test(form.adminEmail.trim())) {
    errors.adminEmail = "Revisa el correo: por ejemplo, nombre@empresa.com.";
  }

  const password = form.adminPassword;
  if (password.length < MIN_PASSWORD_LENGTH) {
    errors.adminPassword = `Usa al menos ${MIN_PASSWORD_LENGTH} caracteres.`;
  } else if (!/\p{L}/u.test(password) || !/\d/.test(password)) {
    errors.adminPassword = "Combina letras y números.";
  }

  if (form.confirmPassword !== password) {
    errors.confirmPassword = "Las contraseñas no coinciden.";
  }

  return errors;
}

export function toSetupPayload(form: AdminStepForm) {
  return {
    setupCode: normalizeSetupCode(form.setupCode),
    organizationName: form.organizationName.trim(),
    adminName: form.adminName.trim(),
    adminEmail: form.adminEmail.trim(),
    adminPassword: form.adminPassword,
  };
}

/** La API responde errores de validación como ValidationProblemDetails: { errors: { AdminPassword: [...] } }. */
export function fieldErrorsFromProblem(body: unknown): AdminStepErrors {
  const errors: AdminStepErrors = {};
  const raw = (body as { errors?: Record<string, string[]> } | null)?.errors;
  if (!raw || typeof raw !== "object") return errors;

  for (const [key, messages] of Object.entries(raw)) {
    const field = (key.charAt(0).toLowerCase() + key.slice(1)) as keyof AdminStepForm;
    if (field in emptyAdminStep && Array.isArray(messages) && messages[0]) {
      errors[field] = messages[0];
    }
  }
  return errors;
}
