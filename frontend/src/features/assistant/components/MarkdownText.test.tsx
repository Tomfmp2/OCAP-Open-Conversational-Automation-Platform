import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { MarkdownText } from "./MarkdownText";

describe("MarkdownText", () => {
  it("dibuja negritas, código y listas en lugar de mostrar los símbolos", () => {
    const { container } = render(
      <MarkdownText text={"Usas **Gemini** (modelo `gemini-3.5-flash`).\n\n- **Gemini**\n- **Gemini-tests**"} />
    );

    expect(container.querySelectorAll("strong")).toHaveLength(3);
    expect(container.querySelector("code")?.textContent).toBe("gemini-3.5-flash");
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
    expect(container.textContent).not.toContain("**");
  });

  it("no interpreta HTML, enlaces peligrosos ni carga imágenes que mande el modelo", () => {
    const { container } = render(
      <MarkdownText text={'<img src=x onerror="alert(1)"> <script>alert(1)</script> [clic](javascript:alert(1)) ![foto](https://ajeno.example/x.png)'} />
    );

    expect(container.querySelector("script")).toBeNull();
    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("a")?.getAttribute("href") ?? "").not.toContain("javascript:");
  });

  it("los enlaces se abren aparte y sin dar acceso a la pestaña del panel", () => {
    render(<MarkdownText text="[docs](https://ocap.example/docs)" />);

    const link = screen.getByRole("link", { name: "docs" });
    expect(link.getAttribute("target")).toBe("_blank");
    expect(link.getAttribute("rel")).toContain("noopener");
  });
});
