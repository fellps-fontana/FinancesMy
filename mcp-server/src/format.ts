export function formatarMoeda(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return "-";
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function formatarData(data: string | null | undefined): string {
  if (!data) return "-";
  const iso = data.length >= 10 ? data.slice(0, 10) : data;
  const partes = iso.split("-");
  if (partes.length !== 3) return data;
  const [ano, mes, dia] = partes;
  return `${dia}/${mes}/${ano}`;
}

// Normaliza tanto PascalCase ("DinheiroFisico") quanto storage snake ("DINHEIRO_FISICO")
// para um rotulo legivel em portugues, sem precisar de tabela por enum.
export function formatarEnum(valor: string | null | undefined): string {
  if (!valor) return "-";
  const comEspacosPascal = valor.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
  const semUnderscore = comEspacosPascal.replace(/_/g, " ");
  return semUnderscore
    .toLowerCase()
    .replace(/(^|\s)\S/g, (c) => c.toUpperCase());
}

export function diasEntre(dataIso: string, referencia: Date = new Date()): number {
  const alvo = new Date(`${dataIso.slice(0, 10)}T00:00:00`);
  const hoje = new Date(referencia.getFullYear(), referencia.getMonth(), referencia.getDate());
  const msPorDia = 24 * 60 * 60 * 1000;
  return Math.round((alvo.getTime() - hoje.getTime()) / msPorDia);
}
