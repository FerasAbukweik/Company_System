import { Injectable, Injector, inject, signal } from '@angular/core';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { firstValueFrom, Subject } from 'rxjs';
import { Urls } from '../../constants/urls';
import { AuthService } from '../api-services/auth-service';
import { HubState } from './hub-state.model';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly injector = inject(Injector);

  readonly connection = new HubConnectionBuilder()
    .withUrl(Urls.appHub, { withCredentials: true })
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000, 60_000])
    .configureLogging(LogLevel.Warning)
    .build();

  private readonly currentState = signal<HubState>('idle');
  readonly state = this.currentState.asReadonly();
  readonly onReconnected$ = new Subject<void>();
  readonly onConnectionLost$ = new Subject<void>();

  private starting: Promise<void> | null = null;
  private stopping: Promise<void> | null = null;
  private wanted = false;
  private version = 0;
  private missedEvents = false;

  constructor() {
    this.connection.onreconnecting(() => this.currentState.set('reconnecting'));
    this.connection.onreconnected(() => {
      if (!this.wanted) return;
      this.currentState.set('connected');
      this.onReconnected$.next();
    });
    this.connection.onclose(() => {
      if (!this.wanted) return;
      this.currentState.set('failed');
      this.missedEvents = true;
      this.onConnectionLost$.next();
    });
  }

  start(): Promise<void> {
    if (this.stopping) return this.stopping.then(() => this.start());
    if (this.starting) return this.starting;
    if (
      this.connection.state === HubConnectionState.Connected ||
      this.connection.state === HubConnectionState.Reconnecting
    )
      return Promise.resolve();

    this.wanted = true;
    this.currentState.set('connecting');
    const version = this.version;
    this.starting = this.connect(version).finally(() => (this.starting = null));
    return this.starting;
  }

  stop(): Promise<void> {
    this.wanted = false;
    this.version++;
    this.missedEvents = false;
    this.currentState.set('idle');
    if (this.stopping) return this.stopping;

    const starting = this.starting;
    this.stopping = (async () => {
      try {
        await this.connection.stop();
      } catch {
        /* The session is already closing. */
      }
      await starting;
    })().finally(() => (this.stopping = null));
    return this.stopping;
  }

  private async connect(version: number) {
    try {
      try {
        await this.connection.start();
      } catch {
        if (!this.wanted || version !== this.version) return;
        await firstValueFrom(this.injector.get(AuthService).updateTokens());
        if (!this.wanted || version !== this.version) return;
        await this.connection.start();
      }
      if (!this.wanted || version !== this.version) return;
      this.currentState.set('connected');
      if (this.missedEvents) {
        this.missedEvents = false;
        this.onReconnected$.next();
      }
    } catch {
      if (!this.wanted || version !== this.version) return;
      this.currentState.set('failed');
      this.missedEvents = true;
      this.onConnectionLost$.next();
    }
  }
}
