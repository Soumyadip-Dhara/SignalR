import { InjectionToken } from '@angular/core';

/** Configuration for {@link SignalRHubService}. */
export interface SignalRHubConfig {
  /**
   * Full URL of the hub endpoint.
   * @example 'https://your-hub-host/hubs/notifications'
   */
  hubUrl: string;

  /**
   * Optional. When `true`, the service automatically starts the connection
   * when it is first instantiated (i.e. the first consumer injects it).
   * Defaults to `true`.
   */
  autoConnect?: boolean;

  /**
   * Optional. Log level forwarded to the SignalR client.
   * Defaults to `LogLevel.Warning` (1).
   * Pass `LogLevel.None` (6) to silence the client entirely.
   */
  logLevel?: number;
}

/** DI token for the {@link SignalRHubConfig}. */
export const SIGNALR_HUB_CONFIG = new InjectionToken<SignalRHubConfig>(
  'SIGNALR_HUB_CONFIG'
);
