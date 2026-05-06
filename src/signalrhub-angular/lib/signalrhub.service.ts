import { Inject, Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { filter, map } from 'rxjs/operators';

import { NotificationMessage } from './notification-message.model';
import { SIGNALR_HUB_CONFIG, SignalRHubConfig } from './signalrhub.config';

/** Possible connection states exposed to consumers. */
export type HubConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting';

/**
 * Core service for the Common SignalR Hub.
 *
 * Provided at root level via {@link SignalRHubModule.forRoot}.
 *
 * @example
 * ```ts
 * export class AppComponent implements OnInit {
 *   constructor(private hub: SignalRHubService) {}
 *
 *   ngOnInit() {
 *     this.hub.joinChannel('document-upload');
 *     this.hub.onNotification('document-upload').subscribe(n =>
 *       console.log(n.eventType, n.payload)
 *     );
 *   }
 * }
 * ```
 */
@Injectable()
export class SignalRHubService implements OnDestroy {
  // ── Connection ──────────────────────────────────────────────────────────────

  private readonly _connection: signalR.HubConnection;

  /** Holds the in-flight start() promise so callers can await it. */
  private _startPromise: Promise<void> | null = null;

  private readonly _state$ = new BehaviorSubject<HubConnectionState>('Disconnected');

  /** Emits the current connection state whenever it changes. */
  readonly state$: Observable<HubConnectionState> = this._state$.asObservable();

  /** Snapshot of the current connection state. */
  get state(): HubConnectionState {
    return this._state$.value;
  }

  // ── Notification stream ──────────────────────────────────────────────────────

  private readonly _notification$ = new Subject<NotificationMessage>();
  private readonly _joinedChannel$ = new Subject<string>();
  private readonly _leftChannel$ = new Subject<string>();

  constructor(@Inject(SIGNALR_HUB_CONFIG) private readonly _config: SignalRHubConfig) {
    const logLevel = _config.logLevel ?? signalR.LogLevel.Warning;

    this._connection = new signalR.HubConnectionBuilder()
      .withUrl(_config.hubUrl)
      .withAutomaticReconnect()
      .configureLogging(logLevel)
      .build();

    this._connection.on('ReceiveNotification', (notification: NotificationMessage) => {
      this._notification$.next(notification);
    });

    this._connection.on('JoinedChannel', (channel: string) => {
      this._joinedChannel$.next(channel);
    });

    this._connection.on('LeftChannel', (channel: string) => {
      this._leftChannel$.next(channel);
    });

    this._connection.onreconnecting(() => this._state$.next('Reconnecting'));
    this._connection.onreconnected(() => this._state$.next('Connected'));
    this._connection.onclose(() => this._state$.next('Disconnected'));

    if (_config.autoConnect !== false) {
      this.connect();
    }
  }

  // ── Connection management ────────────────────────────────────────────────────

  /**
   * Start the SignalR connection.  Safe to call multiple times; returns the
   * same in-flight promise if a connection attempt is already underway, and
   * resolves immediately when already connected.
   */
  connect(): Promise<void> {
    if (this._connection.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }
    if (this._startPromise) {
      return this._startPromise;
    }
    this._state$.next('Connecting');
    this._startPromise = this._connection
      .start()
      .then(() => {
        this._state$.next('Connected');
      })
      .catch(err => {
        this._state$.next('Disconnected');
        console.error('[SignalRHubService] Connection failed:', err);
        throw err;
      })
      .finally(() => {
        this._startPromise = null;
      });
    return this._startPromise;
  }

  /** Stop the SignalR connection. */
  async disconnect(): Promise<void> {
    await this._connection.stop();
    this._state$.next('Disconnected');
  }

  // ── Channel management ───────────────────────────────────────────────────────

  /**
   * Subscribe the current connection to a channel so that it receives all
   * notifications published on that channel.
   * @param channel Channel name (e.g. `'document-upload'`).
   */
  async joinChannel(channel: string): Promise<void> {
    await this.connect();
    await this._connection.invoke('JoinChannel', channel);
  }

  /**
   * Unsubscribe the current connection from a channel.
   * @param channel Channel name.
   */
  async leaveChannel(channel: string): Promise<void> {
    await this.connect();
    await this._connection.invoke('LeaveChannel', channel);
  }

  // ── Observables ──────────────────────────────────────────────────────────────

  /**
   * Returns an `Observable` that emits every notification received from the hub.
   *
   * Pass a `channel` to filter to a specific topic; omit it to receive all
   * notifications (useful for global listeners / interceptors).
   *
   * @param channel Optional channel filter.
   */
  onNotification(channel?: string): Observable<NotificationMessage> {
    if (!channel) return this._notification$.asObservable();
    return this._notification$.pipe(
      filter(n => n.channel === channel)
    );
  }

  /**
   * Emits the channel name whenever the hub confirms that this connection has
   * successfully joined a channel.
   */
  onJoinedChannel(): Observable<string> {
    return this._joinedChannel$.asObservable();
  }

  /**
   * Emits the channel name whenever the hub confirms that this connection has
   * left a channel.
   */
  onLeftChannel(): Observable<string> {
    return this._leftChannel$.asObservable();
  }

  // ── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnDestroy(): void {
    this._connection.stop().catch(() => {
      // Suppress errors during cleanup — the process/tab may already be closing.
    });
    this._notification$.complete();
    this._joinedChannel$.complete();
    this._leftChannel$.complete();
    this._state$.complete();
  }
}
