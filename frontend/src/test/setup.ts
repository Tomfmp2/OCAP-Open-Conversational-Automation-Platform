import "@testing-library/jest-dom/vitest";

// jsdom no implementa showModal/close de <dialog>: lo mínimo para que Modal abra y cierre en las pruebas.
if (typeof HTMLDialogElement !== "undefined" && !HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.open = true;
  };
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.open = false;
  };
}
