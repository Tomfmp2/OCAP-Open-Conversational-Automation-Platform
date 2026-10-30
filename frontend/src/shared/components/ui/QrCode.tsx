import qrcode from "qrcode-generator";

/** QR como un solo <path> SVG (sin innerHTML): nítido a cualquier tamaño y legible para la cámara del teléfono. */
export function QrCode({ value, label, size = 192 }: { value: string; label: string; size?: number }) {
  const qr = qrcode(0, "M");
  qr.addData(value);
  qr.make();

  const count = qr.getModuleCount();
  const quiet = 4; // margen blanco que exigen los lectores de QR
  let path = "";
  for (let row = 0; row < count; row++) {
    for (let col = 0; col < count; col++) {
      if (qr.isDark(row, col)) path += `M${col + quiet} ${row + quiet}h1v1h-1z`;
    }
  }

  const box = count + quiet * 2;
  return (
    <svg role="img" aria-label={label} width={size} height={size} viewBox={`0 0 ${box} ${box}`} shapeRendering="crispEdges">
      <rect width={box} height={box} fill="#fff" />
      <path d={path} fill="#000" />
    </svg>
  );
}
