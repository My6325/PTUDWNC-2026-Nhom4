import type { RecipeDetail } from "@/types/recipe";

export interface RecipeJsonLd {
  "@context": "https://schema.org";
  "@type": "Recipe";
  name: string;
  description?: string;
  datePublished: string;
  author: { "@type": "Person"; name: string };
  image?: string[];
  recipeCategory?: string;
  recipeYield: string;
  prepTime: string;
  cookTime: string;
  totalTime: string;
  recipeIngredient: string[];
  recipeInstructions: Array<{
    "@type": "HowToStep";
    position: number;
    name: string;
    text: string;
  }>;
  nutrition?: {
    "@type": "NutritionInformation";
    calories?: string;
    proteinContent?: string;
    carbohydrateContent?: string;
    fatContent?: string;
  };
}

const toDuration = (minutes: number) => `PT${minutes}M`;

export function toRecipeJsonLd(recipe: RecipeDetail): RecipeJsonLd {
  const primaryImage = recipe.images.find((image) => image.isPrimary) ?? recipe.images[0];
  const nutrition = recipe.nutrition;

  return {
    "@context": "https://schema.org",
    "@type": "Recipe",
    name: recipe.title,
    ...(recipe.description ? { description: recipe.description } : {}),
    datePublished: recipe.datePublished,
    author: { "@type": "Person", name: recipe.author.name },
    ...(primaryImage ? { image: [primaryImage.mediumUrl ?? primaryImage.originalUrl] } : {}),
    ...(recipe.category ? { recipeCategory: recipe.category.name } : {}),
    recipeYield: `${recipe.servings} khẩu phần`,
    prepTime: toDuration(recipe.prepTimeMinutes),
    cookTime: toDuration(recipe.cookTimeMinutes),
    totalTime: toDuration(recipe.totalTimeMinutes),
    recipeIngredient: recipe.ingredients.map((ingredient) =>
      [ingredient.quantity, ingredient.unit, ingredient.name, ingredient.notes]
        .filter((part) => part !== null && part !== undefined && part !== "")
        .join(" "),
    ),
    recipeInstructions: recipe.steps.map((step) => ({
      "@type": "HowToStep",
      position: step.stepNumber,
      name: step.title,
      text: step.description,
    })),
    ...(nutrition.calories !== null || nutrition.protein !== null || nutrition.carbohydrates !== null || nutrition.fat !== null
      ? {
          nutrition: {
            "@type": "NutritionInformation" as const,
            ...(nutrition.calories !== null ? { calories: `${nutrition.calories} calories` } : {}),
            ...(nutrition.protein !== null ? { proteinContent: `${nutrition.protein} g` } : {}),
            ...(nutrition.carbohydrates !== null ? { carbohydrateContent: `${nutrition.carbohydrates} g` } : {}),
            ...(nutrition.fat !== null ? { fatContent: `${nutrition.fat} g` } : {}),
          },
        }
      : {}),
  };
}
