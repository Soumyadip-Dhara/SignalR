/**
 * Notification payload received from the hub.
 * Mirrors the server-side NotificationMessage model.
 */
export interface NotificationMessage {
  /** Logical channel / topic (e.g. "document-upload", "order-status"). */
  channel: string;

  /** Short machine-readable event type (e.g. "upload-success"). */
  eventType: string;

  /** Human-readable message shown in the UI. */
  message: string;

  /** Arbitrary JSON payload attached to the event. */
  payload?: unknown;

  /** User ID of the intended recipient, when targeted delivery was used. */
  targetUserId?: string;

  /** UTC timestamp set by the hub before forwarding the message. */
  timestamp: string;
}
