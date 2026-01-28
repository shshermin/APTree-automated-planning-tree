import { useCallback, useMemo, useState } from "react";
import "./AptreeValidateModal.css";

import type { components } from "../../generated/api-types";

type AptreeValidateRequest = components["schemas"]["AptreeValidateRequest"];

type AptreeValidateResult = {
  ok?: boolean;
  errors?: string[];
  findings?: string[];
  toolLogs?: string;
  stderr?: string;
  stdout?: string;
};

function safeJsonParse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function normalizeResult(payload: unknown): AptreeValidateResult {
  if (!payload || typeof payload !== "object") {
    return { ok: false, errors: ["Invalid response from backend"] };
  }

  const asAny = payload as Record<string, unknown>;
  return {
    ok: typeof asAny.ok === "boolean" ? asAny.ok : undefined,
    errors: Array.isArray(asAny.errors) ? (asAny.errors as string[]) : undefined,
    findings: Array.isArray(asAny.findings)
      ? (asAny.findings as string[])
      : undefined,
    toolLogs: typeof asAny.toolLogs === "string" ? asAny.toolLogs : undefined,
    stderr: typeof asAny.stderr === "string" ? asAny.stderr : undefined,
    stdout: typeof asAny.stdout === "string" ? asAny.stdout : undefined,
  };
}

export default function AptreeValidateModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const [modelText, setModelText] = useState<string>("");
  const [instancesText, setInstancesText] = useState<string>("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [rawResponse, setRawResponse] = useState<unknown>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const parsed = useMemo(() => normalizeResult(rawResponse), [rawResponse]);

  const handleSubmit = useCallback(async () => {
    setIsSubmitting(true);
    setErrorMessage(null);
    setRawResponse(null);

    const body: AptreeValidateRequest = {
      modelText,
      instancesText: instancesText.trim() ? instancesText : null,
      jarPath: null,
    };

    try {
      const response = await fetch("/api/aptree/validate", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      const responseText = await response.text();
      const json = safeJsonParse(responseText);

      if (!response.ok) {
        const fallback =
          typeof responseText === "string" && responseText.trim().length
            ? responseText
            : `HTTP ${response.status}`;
        setRawResponse(json ?? { ok: false, errors: [fallback] });
        return;
      }

      setRawResponse(json ?? { ok: false, errors: ["Backend returned non-JSON"] });
    } catch (e) {
      setErrorMessage(e instanceof Error ? e.message : String(e));
    } finally {
      setIsSubmitting(false);
    }
  }, [modelText, instancesText]);

  const handleClear = useCallback(() => {
    setModelText("");
    setInstancesText("");
    setRawResponse(null);
    setErrorMessage(null);
  }, []);

  if (!isOpen) return null;

  const badgeClass =
    parsed.ok === true
      ? "aptree-validate-badge is-ok"
      : parsed.ok === false
      ? "aptree-validate-badge is-fail"
      : "aptree-validate-badge";

  return (
    <div
      className="aptree-validate-overlay"
      role="dialog"
      aria-modal="true"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="aptree-validate-modal">
        <div className="aptree-validate-modal-header">
          <div className="aptree-validate-title">
            <h2>Validate APTree (MontiCore)</h2>
            <p>Uses backend endpoint /api/aptree/validate</p>
          </div>
          <button className="aptree-validate-close" type="button" onClick={onClose}>
            Close
          </button>
        </div>

        <div className="aptree-validate-body">
          <div className="aptree-validate-grid">
            <div className="aptree-validate-field">
              <label>Model text (.bt)</label>
              <textarea
                value={modelText}
                onChange={(e) => setModelText(e.target.value)}
                placeholder="Paste your APTree model text here..."
              />
            </div>

            <div className="aptree-validate-field">
              <label>Instances text (optional)</label>
              <textarea
                value={instancesText}
                onChange={(e) => setInstancesText(e.target.value)}
                placeholder="Optional instances text..."
              />
            </div>
          </div>

          <div className="aptree-validate-actions">
            <button
              className="aptree-validate-primary"
              type="button"
              onClick={handleSubmit}
              disabled={isSubmitting || !modelText.trim()}
            >
              {isSubmitting ? "Validating..." : "Validate"}
            </button>
            <button
              className="aptree-validate-secondary"
              type="button"
              onClick={handleClear}
              disabled={isSubmitting}
            >
              Clear
            </button>
            <span className="aptree-validate-status">
              Tip: run the backend on http://localhost:5254
            </span>
          </div>

          {(errorMessage !== null || rawResponse !== null) && (
            <div className="aptree-validate-result">
              <h3>
                Result{" "}
                <span className={badgeClass}>
                  {parsed.ok === true
                    ? "OK"
                    : parsed.ok === false
                    ? "FAILED"
                    : "UNKNOWN"}
                </span>
              </h3>

              {errorMessage && (
                <pre className="aptree-validate-pre">{errorMessage}</pre>
              )}

              {rawResponse !== null && (
                <pre className="aptree-validate-pre">
                  {JSON.stringify(rawResponse, null, 2)}
                </pre>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
