import { ModuleWithProviders, NgModule } from '@angular/core';

import { SIGNALR_HUB_CONFIG, SignalRHubConfig } from './signalrhub.config';
import { SignalRHubService } from './signalrhub.service';

/**
 * Angular module for the Common SignalR Hub.
 *
 * Import once in the root `AppModule` (or `bootstrapApplication` providers)
 * using `forRoot()`:
 *
 * ```ts
 * // app.module.ts
 * @NgModule({
 *   imports: [
 *     SignalRHubModule.forRoot({
 *       hubUrl: 'https://your-hub-host/hubs/notifications'
 *     })
 *   ]
 * })
 * export class AppModule {}
 *
 * // ── or with standalone bootstrap ──────────────────────────────────
 * // main.ts
 * bootstrapApplication(AppComponent, {
 *   providers: [
 *     ...SignalRHubModule.forRoot({
 *       hubUrl: 'https://your-hub-host/hubs/notifications'
 *     }).providers!
 *   ]
 * });
 * ```
 */
@NgModule()
export class SignalRHubModule {
  /**
   * Call once in the root module with your hub configuration.
   * Returns providers that register {@link SignalRHubService} as a singleton.
   */
  static forRoot(config: SignalRHubConfig): ModuleWithProviders<SignalRHubModule> {
    return {
      ngModule: SignalRHubModule,
      providers: [
        { provide: SIGNALR_HUB_CONFIG, useValue: config },
        SignalRHubService,
      ],
    };
  }
}
