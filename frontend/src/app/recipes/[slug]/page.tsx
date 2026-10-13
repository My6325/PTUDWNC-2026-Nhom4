import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { isAxiosError } from "axios";
import { getRecipeBySlug } from "@/services/recipeService";
import { toRecipeJsonLd } from "@/utils/recipeSeo";

interface RecipePageProps {
  params: Promise<{ slug: string }>;
}

async function loadRecipe(slug: string) {
  try {
    return await getRecipeBySlug(slug);
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 404) {
      notFound();
    }
    throw error;
  }
}

export async function generateMetadata({ params }: RecipePageProps): Promise<Metadata> {
  const recipe = await loadRecipe((await params).slug);
  return {
    title: `${recipe.title} | Culinary Blog`,
    description: recipe.description ?? `Công thức ${recipe.title}`,
  };
}

export default async function RecipePage({ params }: RecipePageProps) {
  const recipe = await loadRecipe((await params).slug);
  const jsonLd = toRecipeJsonLd(recipe);

  return (
    <main className="mx-auto w-full max-w-4xl px-6 py-10">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }}
      />
      <article>
        <header className="mb-8">
          <p className="text-sm text-emerald-700">{recipe.category?.name ?? "Công thức"}</p>
          <h1 className="mt-2 text-4xl font-bold tracking-tight">{recipe.title}</h1>
          {recipe.description && <p className="mt-4 text-lg text-zinc-600">{recipe.description}</p>}
          <p className="mt-3 text-sm text-zinc-500">
            {recipe.prepTimeMinutes + recipe.cookTimeMinutes} phút · {recipe.servings} khẩu phần · {recipe.author.name}
          </p>
        </header>

        <section className="grid gap-8 md:grid-cols-[minmax(0,1fr)_minmax(0,2fr)]">
          <div>
            <h2 className="text-xl font-semibold">Nguyên liệu</h2>
            <ul className="mt-3 list-disc space-y-2 pl-5 text-zinc-700">
              {recipe.ingredients.map((ingredient) => (
                <li key={`${ingredient.orderIndex}-${ingredient.name}`}>
                  {[ingredient.quantity, ingredient.unit, ingredient.name].filter(Boolean).join(" ")}
                  {ingredient.notes ? ` (${ingredient.notes})` : ""}
                </li>
              ))}
            </ul>
          </div>
          <div>
            <h2 className="text-xl font-semibold">Các bước thực hiện</h2>
            <ol className="mt-3 space-y-5">
              {recipe.steps.map((step) => (
                <li key={step.stepNumber}>
                  <h3 className="font-semibold">{step.stepNumber}. {step.title}</h3>
                  <p className="mt-1 text-zinc-700">{step.description}</p>
                </li>
              ))}
            </ol>
          </div>
        </section>
      </article>
    </main>
  );
}
