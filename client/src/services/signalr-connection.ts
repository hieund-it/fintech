import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

import { useAuthStore } from '../stores/auth-store';

let connection: HubConnection | null = null;
let startPromise: Promise<void> | null = null;

/** Returns the singleton SignalR HubConnection, creating it if needed. */
export function getConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl('/hubs/market', {
        accessTokenFactory: () =>
          useAuthStore.getState().user?.accessToken ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return connection;
}

/**
 * Ensure the connection is started. Safe to call multiple times —
 * concurrent callers share the same in-flight start promise.
 */
export async function ensureConnected(): Promise<void> {
  const conn = getConnection();
  if (conn.state === 'Connected') return;
  if (!startPromise) {
    startPromise = conn.start().finally(() => {
      startPromise = null;
    });
  }
  return startPromise;
}
