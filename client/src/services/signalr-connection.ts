import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

let connection: HubConnection | null = null;

/** Returns the singleton SignalR HubConnection, creating it if needed. */
export function getConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl('/hubs/market', {
        accessTokenFactory: () => localStorage.getItem('accessToken') ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return connection;
}

/**
 * Ensure the connection is started. Safe to call multiple times —
 * does nothing if already Connected or Connecting.
 */
export async function ensureConnected(): Promise<void> {
  const conn = getConnection();
  if (conn.state === 'Disconnected') {
    await conn.start();
  }
}
