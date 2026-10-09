#!/usr/bin/env node
// Checks that the committed src/generated/api-types.ts matches the backend's
// live Swagger spec: builds and boots the backend, regenerates the types and
// diffs them. Run from APTreeEditor/BehaviorTreeFrontend:
//
//   node scripts/check-api-types-fresh.mjs
//
// Exit codes: 0 fresh, 1 stale (prints a diff), 2 backend failed to build/start.

import { spawn, execFileSync } from "node:child_process";
import { readFileSync, writeFileSync, mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const FRONTEND_DIR = fileURLToPath(new URL("..", import.meta.url));
const BACKEND_DIR = join(FRONTEND_DIR, "..", "..", "APTreeExecutionEngine");
const COMMITTED_TYPES = join(FRONTEND_DIR, "src", "generated", "api-types.ts");
const PORT = process.env.API_TYPES_CHECK_PORT ?? "5254";
const BASE_URL = `http://localhost:${PORT}`;

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitForHealth(timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const res = await fetch(`${BASE_URL}/health`);
      if (res.ok) return;
    } catch {
      // backend not listening yet
    }
    await sleep(500);
  }
  throw new Error(`Backend did not become healthy within ${timeoutMs}ms`);
}

async function main() {
  console.log(`Building backend in ${BACKEND_DIR} ...`);
  execFileSync("dotnet", ["build", "BehaviorTreeMainProject.csproj", "-c", "Debug"], {
    cwd: BACKEND_DIR,
    stdio: "inherit",
  });

  console.log(`Starting backend on ${BASE_URL} ...`);
  const server = spawn(
    "dotnet",
    ["run", "--no-build", "-c", "Debug", "--urls", BASE_URL],
    { cwd: BACKEND_DIR, stdio: "ignore" },
  );

  let exitCode = 2;
  try {
    await waitForHealth(60_000);

    const tmpDir = mkdtempSync(join(tmpdir(), "api-types-fresh-"));
    const freshTypesPath = join(tmpDir, "api-types.fresh.ts");
    try {
      execFileSync(
        "npx",
        ["openapi-typescript", `${BASE_URL}/swagger/v1/swagger.json`, "--output", freshTypesPath],
        { cwd: FRONTEND_DIR, stdio: "inherit" },
      );

      const committed = readFileSync(COMMITTED_TYPES, "utf8");
      const fresh = readFileSync(freshTypesPath, "utf8");

      if (committed === fresh) {
        console.log("api-types.ts is up to date with the backend's Swagger spec.");
        exitCode = 0;
      } else {
        console.error(
          "STALE: src/generated/api-types.ts does not match the backend's live Swagger spec.\n" +
            "Regenerate it with: npm run gen:api (backend must be running on " +
            BASE_URL +
            ")",
        );
        writeFileSync(join(tmpDir, "committed-for-diff.ts"), committed);
        try {
          execFileSync("diff", ["-u", join(tmpDir, "committed-for-diff.ts"), freshTypesPath], {
            stdio: "inherit",
          });
        } catch {
          // `diff` exits 1 when files differ - that's expected, not a script failure.
        }
        exitCode = 1;
      }
    } finally {
      rmSync(tmpDir, { recursive: true, force: true });
    }
  } catch (err) {
    console.error("Could not complete the freshness check:", err.message);
    exitCode = 2;
  } finally {
    server.kill();
  }

  process.exit(exitCode);
}

main();
