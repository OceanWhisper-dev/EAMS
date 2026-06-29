import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/stores/auth'

let connection: signalR.HubConnection | null = null

export function getImportConnection(taskId: string): signalR.HubConnection {
  if (connection) return connection

  const authStore = useAuthStore()

  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/import', {
      accessTokenFactory: () => authStore.token
    })
    .withAutomaticReconnect()
    .build()

  return connection
}

export async function connectImportTask(taskId: string): Promise<signalR.HubConnection> {
  const conn = getImportConnection(taskId)
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start()
    await conn.invoke('JoinTaskGroup', taskId)
  }
  return conn
}

export async function disconnectImportTask(taskId?: string) {
  if (connection) {
    try {
      if (taskId && connection.state === signalR.HubConnectionState.Connected) {
        await connection.invoke('LeaveTaskGroup', taskId)
      }
      await connection.stop()
    } catch (e) { console.warn('[signalr] disconnectImportTask failed', e) }
    connection = null
  }
}