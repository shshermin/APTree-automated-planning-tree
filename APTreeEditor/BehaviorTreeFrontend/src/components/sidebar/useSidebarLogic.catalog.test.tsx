import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useSidebarManager } from "./useSidebarLogic";
import {
  FALLBACK_DECORATOR_NODE_OPTIONS,
  FALLBACK_SERVICE_NODE_OPTIONS,
  FALLBACK_FLOW_NODE_OPTIONS,
} from "./utils/constants";

// The sidebar's node catalogs populate from the
// backend endpoints, and fall back to the built-in defaults when the
// backend is unavailable - without ever leaving a category empty.

function jsonResponse(body: unknown, ok = true, status = 200) {
  return { ok, status, json: () => Promise.resolve(body) } as Response;
}

const decoratorPayload = [
  { id: "d1", label: "Backend Decorator", typeLabel: "Decorator", description: "from backend" },
];
const servicePayload = [
  { id: "s1", label: "Backend Service", typeLabel: "Service", description: "from backend" },
];
const flowPayload = [{ id: "f1", label: "Backend Flow", typeLabel: "Flow", description: "from backend" }];

function mockFetchByRoute(routes: Record<string, Response | (() => Response) | "reject">) {
  return vi.fn((url: string | URL | Request) => {
    const path = typeof url === "string" ? url : url instanceof URL ? url.pathname : url.url;
    const entry = routes[path];
    if (entry === "reject" || entry === undefined) return Promise.reject(new Error(`no route for ${path}`));
    return Promise.resolve(typeof entry === "function" ? entry() : entry);
  }) as unknown as typeof fetch;
}

describe("useSidebarManager catalog loading", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", mockFetchByRoute({}));
  });
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("populates decorator, service and flow options from the backend catalogs", async () => {
    vi.stubGlobal(
      "fetch",
      mockFetchByRoute({
        "/api/catalog/decorators": jsonResponse(decoratorPayload),
        "/api/catalog/services": jsonResponse(servicePayload),
        "/api/catalog/flows": jsonResponse(flowPayload),
      })
    );

    const { result } = renderHook(() => useSidebarManager());

    await waitFor(() => expect(result.current.decoratorNodeOptions).toHaveLength(1));
    await waitFor(() => expect(result.current.flowNodeOptions).toHaveLength(1));

    expect(result.current.decoratorNodeOptions[0]).toMatchObject({
      id: "d1",
      label: "Backend Decorator",
      typeLabel: "Decorator",
      kind: "decorator",
    });
    expect(result.current.serviceNodeOptions[0]).toMatchObject({ id: "s1", label: "Backend Service" });
    expect(result.current.flowNodeOptions[0]).toMatchObject({ id: "f1", label: "Backend Flow", kind: "flow" });
  });

  it("falls back to the built-in options when the catalog endpoints reject", async () => {
    vi.stubGlobal(
      "fetch",
      mockFetchByRoute({
        "/api/catalog/decorators": "reject",
        "/api/catalog/services": "reject",
        "/api/catalog/flows": "reject",
      })
    );
    const warn = vi.spyOn(console, "warn").mockImplementation(() => {});

    const { result } = renderHook(() => useSidebarManager());

    await waitFor(() =>
      expect(result.current.decoratorNodeOptions.map((o) => o.id)).toEqual(
        FALLBACK_DECORATOR_NODE_OPTIONS.map((o) => o.id)
      )
    );
    expect(result.current.serviceNodeOptions.map((o) => o.id)).toEqual(
      FALLBACK_SERVICE_NODE_OPTIONS.map((o) => o.id)
    );
    await waitFor(() =>
      expect(result.current.flowNodeOptions.map((o) => o.id)).toEqual(FALLBACK_FLOW_NODE_OPTIONS.map((o) => o.id))
    );

    warn.mockRestore();
  });

  it("falls back when the endpoint responds with a non-OK status, not just on network failure", async () => {
    vi.stubGlobal(
      "fetch",
      mockFetchByRoute({
        "/api/catalog/decorators": jsonResponse({ error: "boom" }, false, 500),
        "/api/catalog/services": jsonResponse({ error: "boom" }, false, 500),
        "/api/catalog/flows": jsonResponse({ error: "boom" }, false, 500),
      })
    );
    vi.spyOn(console, "warn").mockImplementation(() => {});

    const { result } = renderHook(() => useSidebarManager());

    await waitFor(() =>
      expect(result.current.decoratorNodeOptions.map((o) => o.id)).toEqual(
        FALLBACK_DECORATOR_NODE_OPTIONS.map((o) => o.id)
      )
    );
  });

  it("defaults a missing typeLabel/description to the category label and undefined, never crashing", async () => {
    vi.stubGlobal(
      "fetch",
      mockFetchByRoute({
        "/api/catalog/decorators": jsonResponse([{ id: "bare", label: "Bare Decorator" }]),
        "/api/catalog/services": jsonResponse(servicePayload),
        "/api/catalog/flows": jsonResponse(flowPayload),
      })
    );

    const { result } = renderHook(() => useSidebarManager());

    await waitFor(() => expect(result.current.decoratorNodeOptions).toHaveLength(1));
    expect(result.current.decoratorNodeOptions[0]).toMatchObject({
      id: "bare",
      label: "Bare Decorator",
      typeLabel: "Decorator",
    });
    expect(result.current.decoratorNodeOptions[0].description).toBeUndefined();
  });

  it("every fallback option surfaces with a stable, non-empty id (used as its React key)", () => {
    // Not backend-dependent: a regression guard on the static fallback data
    // itself, since useSidebarManager trusts these ids are unique.
    for (const list of [FALLBACK_DECORATOR_NODE_OPTIONS, FALLBACK_SERVICE_NODE_OPTIONS, FALLBACK_FLOW_NODE_OPTIONS]) {
      const ids = list.map((o) => o.id);
      expect(new Set(ids).size).toBe(ids.length);
      expect(ids.every((id) => id.trim().length > 0)).toBe(true);
    }
  });
});
