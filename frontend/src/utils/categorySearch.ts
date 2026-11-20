export function normalizeCategoryName(value: string): string {
  return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[đĐ]/g, "d").toLocaleLowerCase("vi").trim().replace(/\s+/g, " ");
}

export function matchesCategoryName(name: string, query: string): boolean {
  return normalizeCategoryName(name).includes(normalizeCategoryName(query));
}
