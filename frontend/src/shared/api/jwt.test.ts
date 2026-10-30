import { describe, expect, it } from "vitest";
import { permissionsFromToken } from "./jwt";

const token = (payload: object) =>
  `x.${btoa(JSON.stringify(payload)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "")}.y`;

describe("permissionsFromToken", () => {
  it("lee uno o varios permisos del claim", () => {
    expect(permissionsFromToken(token({ permission: ["Conversation.Read", "AI.Execute"] }))).toEqual([
      "Conversation.Read",
      "AI.Execute",
    ]);
    expect(permissionsFromToken(token({ permission: "Security.Manage" }))).toEqual(["Security.Manage"]);
  });

  it("sin token válido no concede nada", () => {
    expect(permissionsFromToken(null)).toEqual([]);
    expect(permissionsFromToken("no-es-un-jwt")).toEqual([]);
  });
});
