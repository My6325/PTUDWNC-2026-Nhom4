import { Suspense } from "react";
import { RecipeAccess } from "@/components/recipes/RecipeAccess";
import { ManagedRecipes } from "@/components/recipes/ManagedRecipes";
export default function Page() { return <RecipeAccess><Suspense fallback={<p>Đang tải...</p>}><ManagedRecipes /></Suspense></RecipeAccess>; }
