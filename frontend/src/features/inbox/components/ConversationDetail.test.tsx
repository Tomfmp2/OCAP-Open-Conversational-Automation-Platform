import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { InboxConversation } from "../api/useInbox";
import { ConversationDetail } from "./ConversationDetail";

const mutation = () => ({ mutate: vi.fn(), mutateAsync: vi.fn().mockResolvedValue(undefined), isPending: false, isError: false, error: null });
const actions = { takeOver: mutation(), release: mutation(), reply: mutation() };

vi.mock("../api/useInbox", () => ({
  useInboxMessages: () => ({
    isLoading: false,
    isError: false,
    data: [
      { id: "1", sender: "User", text: "¿Tienen pan integral?", createdAt: "2026-10-23T10:00:00Z" },
      { id: "2", sender: "Agent", text: "Sí, todos los días.", createdAt: "2026-10-23T10:00:05Z" },
    ],
  }),
  useInboxActions: () => actions,
}));

const conversation = (overrides: Partial<InboxConversation> = {}): InboxConversation => ({
  id: "c1",
  channel: "Telegram",
  contactName: "Ana",
  externalContactId: "9901",
  status: "Active",
  handledByHuman: false,
  canReply: true,
  lastActivityAt: "2026-10-23T10:00:05Z",
  lastMessage: "Sí, todos los días.",
  lastSender: "Agent",
  ...overrides,
});

describe("ConversationDetail", () => {
  afterEach(cleanup);
  beforeEach(() => {
    Element.prototype.scrollIntoView = vi.fn();
    Object.values(actions).forEach((a) => {
      a.mutate.mockReset();
      a.mutateAsync.mockReset().mockResolvedValue(undefined);
    });
  });

  it("muestra los mensajes y permite tomar la conversación", async () => {
    const user = userEvent.setup();
    render(<ConversationDetail conversation={conversation()} />);

    expect(screen.getByText("¿Tienen pan integral?")).toBeInTheDocument();
    expect(screen.getByText(/^Agente, /)).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /tomar conversación/i }));
    expect(actions.takeOver.mutate).toHaveBeenCalledOnce();
  });

  it("cuando atiende el equipo ofrece devolverla al agente", async () => {
    const user = userEvent.setup();
    render(<ConversationDetail conversation={conversation({ handledByHuman: true })} />);

    await user.click(screen.getByRole("button", { name: /devolver al agente/i }));
    expect(actions.release.mutate).toHaveBeenCalledOnce();
  });

  it("envía la respuesta del equipo", async () => {
    const user = userEvent.setup();
    render(<ConversationDetail conversation={conversation()} />);

    await user.type(screen.getByLabelText("Respuesta"), "Recién horneado");
    await user.click(screen.getByRole("button", { name: "Enviar" }));
    expect(actions.reply.mutate).toHaveBeenCalledWith("Recién horneado", expect.anything());
  });

  it("sin canal de respuesta no muestra el cuadro de texto", () => {
    render(<ConversationDetail conversation={conversation({ canReply: false })} />);

    expect(screen.queryByLabelText("Respuesta")).not.toBeInTheDocument();
    expect(screen.getByText(/no permite responder/i)).toBeInTheDocument();
  });
});
