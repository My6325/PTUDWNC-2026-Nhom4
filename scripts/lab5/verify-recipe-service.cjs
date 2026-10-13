// Uses the existing TypeScript/Axios dependencies; makes no network requests.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const Module = require("node:module");
const frontend = path.resolve(__dirname, "../../frontend");
const localRequire = Module.createRequire(path.join(frontend, "package.json"));
const ts = localRequire("typescript");

function compile(relative, overrides = {}) {
  const filename = path.join(frontend, relative);
  const compiled = ts.transpileModule(fs.readFileSync(filename, "utf8"), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
  }).outputText;
  const target = new Module(filename);
  target.filename = filename;
  target.paths = Module._nodeModulePaths(path.dirname(filename));
  const originalRequire = target.require.bind(target);
  target.require = (name) => Object.hasOwn(overrides, name) ? overrides[name] : originalRequire(name);
  target._compile(compiled, filename);
  return target.exports;
}

async function main() {
  // A synthetic token only; never load real credentials.
  global.window = {};
  global.localStorage = { getItem: (key) => key === "accessToken" ? "lab5-synthetic-token" : null };
  const api = compile("src/services/apiClient.ts");
  const { recipeService } = compile("src/services/recipeService.ts", { "./apiClient": api });
  const requests = [];
  const responseDto = { id: "response-id" };
  api.apiClient.defaults.adapter = async (config) => {
    requests.push(config);
    return { data: responseDto, status: config.method === "delete" ? 204 : 200,
      statusText: "OK", headers: {}, config };
  };
  const payload = { title: "Test recipe", rowVersion: "AAAAAA==", categoryId: null };
  const cases = [
    ["post", "/recipes", () => recipeService.createDraft(payload)],
    ["put", "/recipes/recipe", () => recipeService.update("recipe", payload)],
    ["delete", "/recipes/recipe", () => recipeService.delete("recipe")],
    ["post", "/recipes/recipe/steps", () => recipeService.addStep("recipe", payload)],
    ["put", "/recipes/recipe/steps/step", () => recipeService.updateStep("recipe", "step", payload)],
    ["delete", "/recipes/recipe/steps/step", () => recipeService.deleteStep("recipe", "step")],
    ["post", "/recipes/recipe/ingredients", () => recipeService.addIngredient("recipe", payload)],
    ["put", "/recipes/recipe/ingredients/ingredient", () => recipeService.updateIngredient("recipe", "ingredient", payload)],
    ["delete", "/recipes/recipe/ingredients/ingredient", () => recipeService.deleteIngredient("recipe", "ingredient")],
    ["post", "/recipes/recipe/images", () => recipeService.uploadImage("recipe", new File(["test"], "test.png", { type: "image/png" }))],
    ["delete", "/recipes/recipe/images/image", () => recipeService.deleteImage("recipe", "image")],
  ];
  for (const [method, url, operation] of cases) {
    const result = await operation();
    const request = requests.at(-1);
    assert.equal(request.method, method);
    assert.equal(request.url, url);
    assert.equal(request.headers.get("Authorization"), "Bearer lab5-synthetic-token");
    assert.equal(result, method === "delete" ? undefined : responseDto);
    if (url.endsWith("/images") && method === "post") {
      assert(request.data instanceof FormData);
      assert.equal(request.data.get("file").name, "test.png");
      assert.notEqual(request.headers.get("Content-Type"), "application/json");
    } else if (method !== "delete") {
      assert.deepEqual(JSON.parse(request.data), payload);
    }
  }
  await recipeService.deleteStep("id/with slash", "step?#");
  assert.equal(requests.at(-1).url, "/recipes/id%2Fwith%20slash/steps/step%3F%23");
  console.log("PASS: 11 Recipe service operations, JSON preservation, synthetic Bearer header, FormData upload, void deletes and escaped IDs.");
}
main().catch((error) => { console.error(error); process.exitCode = 1; });
