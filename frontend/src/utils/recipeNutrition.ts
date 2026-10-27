import type { RecipeNutritionDto } from "../types/recipe";

export function hasNutritionValues(nutrition: RecipeNutritionDto | null | undefined): boolean {
  return nutrition != null && [
    nutrition.calories, nutrition.protein, nutrition.carbohydrates,
    nutrition.fat, nutrition.fiber, nutrition.sodium,
  ].some(value => typeof value === "number" && Number.isFinite(value));
}
