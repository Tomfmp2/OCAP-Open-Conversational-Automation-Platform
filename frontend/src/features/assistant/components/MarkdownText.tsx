import ReactMarkdown, { type Components } from "react-markdown";
import remarkGfm from "remark-gfm";

// El modelo responde en Markdown. Se dibuja con los estilos del panel; el HTML que venga en la respuesta no se
// interpreta (react-markdown lo deja como texto) y los enlaces con esquemas peligrosos se descartan.
const components: Components = {
  p: ({ children }) => <p className="mb-3 last:mb-0">{children}</p>,
  strong: ({ children }) => <strong className="font-semibold text-neutral-950">{children}</strong>,
  ul: ({ children }) => <ul className="mb-3 list-disc space-y-1 pl-5 last:mb-0">{children}</ul>,
  ol: ({ children }) => <ol className="mb-3 list-decimal space-y-1 pl-5 last:mb-0">{children}</ol>,
  h1: ({ children }) => <h3 className="mb-2 mt-4 text-base font-semibold text-neutral-950 first:mt-0">{children}</h3>,
  h2: ({ children }) => <h3 className="mb-2 mt-4 text-base font-semibold text-neutral-950 first:mt-0">{children}</h3>,
  h3: ({ children }) => <h4 className="mb-2 mt-3 font-semibold text-neutral-950 first:mt-0">{children}</h4>,
  a: ({ children, href }) => (
    <a href={href} target="_blank" rel="noopener noreferrer" className="break-words font-medium underline underline-offset-2">
      {children}
    </a>
  ),
  code: ({ children }) => (
    <code className="break-words rounded bg-neutral-100 px-1 py-0.5 font-mono text-[0.9em] text-neutral-900">{children}</code>
  ),
  pre: ({ children }) => (
    <pre className="mb-3 overflow-x-auto rounded-lg bg-neutral-100 p-3 text-sm last:mb-0 [&_code]:bg-transparent [&_code]:p-0">
      {children}
    </pre>
  ),
  blockquote: ({ children }) => <blockquote className="mb-3 border-l-2 border-neutral-300 pl-3 text-neutral-700 last:mb-0">{children}</blockquote>,
  table: ({ children }) => (
    <div className="mb-3 overflow-x-auto last:mb-0">
      <table className="w-full border-collapse text-left text-sm">{children}</table>
    </div>
  ),
  th: ({ children }) => <th className="border-b border-neutral-300 px-2 py-1.5 font-semibold">{children}</th>,
  td: ({ children }) => <td className="border-b border-neutral-200 px-2 py-1.5 align-top">{children}</td>,
  hr: () => <hr className="my-4 border-neutral-200" />,
  // Una imagen cargaría una dirección elegida por el modelo: se muestra solo su texto.
  img: ({ alt }) => <span>{alt}</span>,
};

export function MarkdownText({ text }: { text: string }) {
  return (
    <ReactMarkdown remarkPlugins={[remarkGfm]} components={components}>
      {text}
    </ReactMarkdown>
  );
}
