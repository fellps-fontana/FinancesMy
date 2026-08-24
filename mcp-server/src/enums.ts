// Helpers de conversao entre os valores "amigaveis" que os schemas zod aceitam
// (minusculo, com underscore) e os dois formatos que a API do MyFinances usa
// na escrita: PascalCase (para campos de enum "cru") ou ALL_CAPS com underscore
// (para campos "string" preenchidos via ToStorageValue()). Ver mapeamento em
// docs/*.md e no relatorio de exploracao dos controllers.

export function paraStorageValue(valor: string): string {
  return valor.toUpperCase();
}

export function paraPascalCase(valor: string): string {
  return valor
    .split("_")
    .filter(Boolean)
    .map((parte) => parte.charAt(0).toUpperCase() + parte.slice(1).toLowerCase())
    .join("");
}
