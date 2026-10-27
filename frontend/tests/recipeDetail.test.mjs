import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import ts from "typescript";

function loadUtility(name) {
  const source = readFileSync(new URL(`../src/utils/${name}.ts`, import.meta.url), "utf8");
  const code = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
  }).outputText;
  const exports = {};
  new Function("exports", code)(exports);
  return exports;
}
const { hasNutritionValues } = loadUtility("recipeNutrition");
const { toRecipeJsonLd } = loadUtility("recipeSeo");
const recipe = {
  title: "Bún bò", datePublished: "2026-10-27T00:00:00Z", author: { name: "Đoan" },
  images: [], ingredients: [], steps: [], prepTimeMinutes: 10, cookTimeMinutes: 20,
  totalTimeMinutes: 30, servings: 2,
};

test("zero is nutrition data, unlike missing or non-finite values", () => {
  assert.equal(hasNutritionValues({ calories: 0, protein: 0, fat: 0 }), true);
  assert.equal(hasNutritionValues({ fiber: 0 }), true);
  for (const nutrition of [null, undefined, {}, { calories: null }, { calories: NaN }]) {
    assert.equal(hasNutritionValues(nutrition), false);
  }
});

test("JSON-LD preserves zero and omits missing nutrition fields", () => {
  const result = toRecipeJsonLd({ ...recipe, nutrition: { calories: 0 } });
  assert.deepEqual(result.nutrition, { "@type": "NutritionInformation", calories: "0 calories" });
  assert.equal(result.totalTime, "PT30M");
  for (const nutrition of [null, undefined, {}, { calories: null }]) {
    assert.equal("nutrition" in toRecipeJsonLd({ ...recipe, nutrition }), false);
  }
});

test("JSON-LD includes recipe ingredients steps and the primary image", () => {
  const result = toRecipeJsonLd({ ...recipe, nutrition: {},
    images: [{ originalUrl: "other.jpg", isPrimary: false }, { originalUrl: "main.jpg", isPrimary: true }],
    ingredients: [{ name: "Muối", quantity: 0, unit: "g" }],
    steps: [{ stepNumber: 1, title: "Nấu", description: "Đun sôi" }],
  });
  assert.deepEqual(result.image, ["main.jpg"]);
  assert.deepEqual(result.recipeIngredient, ["0 g Muối"]);
  assert.equal(result.recipeInstructions[0]["@type"], "HowToStep");
});
