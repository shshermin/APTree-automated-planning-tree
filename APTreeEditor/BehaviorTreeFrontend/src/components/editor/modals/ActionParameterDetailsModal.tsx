import type { ActionParameterDetail } from "../types";
import "./ActionParameterDetailsModal.css";

interface ActionParameterDetailsModalProps {
  detail: ActionParameterDetail | null;
  onClose: () => void;
}

export default function ActionParameterDetailsModal({
  detail,
  onClose,
}: ActionParameterDetailsModalProps) {
  if (!detail) {
    return null;
  }

  const resolvedValue = detail.parameterValue?.trim() ?? "";
  const fallbackValue = "No value provided";
  const valueText = resolvedValue.length > 0 ? resolvedValue : fallbackValue;
  const isPlaceholderValue = valueText === fallbackValue;

  return (
    <div className="modal-overlay param-detail-overlay" role="dialog" aria-modal="true">
      <div className="modal-content param-detail-modal">
        <div className="modal-header">
          <h3>Parameter Details</h3>
          <button
            type="button"
            className="modal-close-btn"
            onClick={onClose}
            aria-label="Close dialog"
          >
            ×
          </button>
        </div>
        <div className="modal-form param-detail-body">
          <div className="param-detail-section">
            <span className="param-detail-label">Action</span>
            <div className="param-detail-value">{detail.nodeName}</div>
            <span className="param-detail-meta">{detail.nodeTypeLabel}</span>
          </div>

          <div className="param-detail-section">
            <span className="param-detail-label">Parameter</span>
            <div className="param-detail-value">{detail.parameterName}</div>
          </div>

          <div className="param-detail-section param-detail-inline">
            <div className="param-detail-column">
              <span className="param-detail-label">Type</span>
              <div className="param-detail-value">{detail.parameterType || "Unknown"}</div>
            </div>
            <div className="param-detail-column">
              <span className="param-detail-label">Current Value</span>
              <div
                className={`param-detail-value${isPlaceholderValue ? " is-placeholder" : ""}`}
              >
                {valueText}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
