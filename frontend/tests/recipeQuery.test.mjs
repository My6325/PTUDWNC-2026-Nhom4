import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import ts from "typescript";

const source = readFileSync(new URL("../src/utils/recipeQuery.ts", import.meta.url), "utf8");
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
}).outputText;
const exports = {};
new Function("exports", compiled)(exports);
const { normalizeRecipePagination, buildRecipeQuery } = exports;

test("pagination rejects malformed, fractional and overflowing pages", () => {
  for (const page of ["abc", "2abc", -1, 0, 1.5, Infinity, 2147483647]) {
    assert.deepEqual(normalizeRecipePagination(page, 12), { pageIndex: 1, pageSize: 12 });
  }
  assert.equal(normalizeRecipePagination("2", 12).pageIndex, 2);
  assert.equal(normalizeRecipePagination(42949673, 50).pageIndex, 42949673);
  assert.equal(normalizeRecipePagination(42949674, 50).pageIndex, 1);
});

test("page size is capped at the SRS limit and invalid sizes use a safe default", () => {
  assert.equal(normalizeRecipePagination(1, 100).pageSize, 50);
  for (const size of [0, -1, NaN, Infinity, 1.5]) {
    assert.equal(normalizeRecipePagination(1, size).pageSize, 12);
  }
});

test("query preserves zero cook time, all facets, accents and relevance", () => {
  assert.deepEqual(buildRecipeQuery({
    searchTerm: "  Bún Bò  ", categoryId: "category", difficulty: "Easy",
    minCookTimeMinutes: 0, maxCookTimeMinutes: 30, minServings: 1, maxServings: 4,
    sortBy: "relevance", pageIndex: 2, pageSize: 12,
  }), {
    SearchTerm: "Bún Bò", CategoryId: "category", Difficulty: "Easy",
    MinCookTimeMinutes: 0, MaxCookTimeMinutes: 30, MinServings: 1, MaxServings: 4,
    SortBy: "relevance", PageIndex: 2, PageSize: 12,
  });
});

test("PascalCase callers remain supported and relevance requires a keyword", () => {
  assert.equal(buildRecipeQuery({ SearchTerm: "bun bo", SortBy: "relevance" }).SortBy, "relevance");
  assert.equal(buildRecipeQuery({ SearchTerm: " ", SortBy: "relevance" }).SortBy, "newest");
  assert.equal(buildRecipeQuery({ SortBy: "unknown" }).SortBy, "newest");
  assert.equal(buildRecipeQuery({ SortBy: "cookTime" }).SortBy, "cookTime");
});

test("non-finite filters are not serialized to the API", () => {
  const query = buildRecipeQuery({ minCookTimeMinutes: NaN, maxServings: Infinity });
  assert.equal("MinCookTimeMinutes" in query, false);
  assert.equal("MaxServings" in query, false);
});
