/**
 * localStorage 封装 composable
 * 提供类型安全的读写操作，统一管理存储键名
 */

const STORAGE_KEYS = {
  REPORT_DESIGNER_DRAFT: 'report_designer_draft',
  REPORT_VIEWER_SETTINGS: 'report_viewer_settings',
  // 以后在此扩展
} as const

type StorageKey = keyof typeof STORAGE_KEYS

export function useStorage() {
  function getItem<T = string>(key: StorageKey): T | null {
    const raw = localStorage.getItem(STORAGE_KEYS[key])
    if (raw === null) return null
    try { return JSON.parse(raw) as T } catch (e) { console.warn('[useStorage] JSON parse failed for', key, e); return raw as T }
  }

  function setItem(key: StorageKey, value: unknown): void {
    localStorage.setItem(STORAGE_KEYS[key], JSON.stringify(value))
  }

  function removeItem(key: StorageKey): void {
    localStorage.removeItem(STORAGE_KEYS[key])
  }

  return { getItem, setItem, removeItem }
}