import { RecipeAccess } from "@/components/recipes/RecipeAccess";
import { RecipeEditor } from "@/components/recipes/RecipeEditor";
export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <RecipeAccess><RecipeEditor id={id} /></RecipeAccess>;
}
