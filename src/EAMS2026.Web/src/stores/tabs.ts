import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { RouteLocationNormalized } from 'vue-router'

export interface TabItem {
  path: string
  title: string
  name: string
  closable: boolean
}

const FIXED_TABS: TabItem[] = [
  { path: '/dashboard', title: '仪表盘', name: 'Dashboard', closable: false }
]

export const useTabsStore = defineStore('tabs', () => {
  const tabs = ref<TabItem[]>([...FIXED_TABS])
  const activeTab = ref('/dashboard')

  const tabPaths = computed(() => tabs.value.map(t => t.path))

  function addTab(route: RouteLocationNormalized) {
    const path = route.path
    if (tabPaths.value.includes(path)) {
      activeTab.value = path
      return
    }
    const title = (route.meta?.title as string) || route.name?.toString() || '未知'
    tabs.value.push({
      path,
      title,
      name: route.name as string,
      closable: path !== '/dashboard'
    })
    activeTab.value = path
  }

  function closeTab(path: string): string {
    if (path === '/dashboard') return '/dashboard'
    const idx = tabs.value.findIndex(t => t.path === path)
    if (idx === -1) return activeTab.value

    tabs.value.splice(idx, 1)

    if (activeTab.value === path) {
      const newIdx = Math.min(idx, tabs.value.length - 1)
      activeTab.value = tabs.value[newIdx].path
    }
    return activeTab.value
  }

  function closeOthers(path: string) {
    tabs.value = [tabs.value[0], ...tabs.value.filter(t => t.path === path)]
    activeTab.value = path
  }

  function closeAll(): string {
    tabs.value = [...FIXED_TABS]
    activeTab.value = '/dashboard'
    return '/dashboard'
  }

  function closeRight(path: string) {
    const idx = tabs.value.findIndex(t => t.path === path)
    if (idx === -1) return
    tabs.value = tabs.value.slice(0, idx + 1)

    if (tabs.value.findIndex(t => t.path === activeTab.value) === -1) {
      activeTab.value = path
    }
  }

  function setActiveTab(path: string) {
    activeTab.value = path
  }

  function reset() {
    tabs.value = [...FIXED_TABS]
    activeTab.value = '/dashboard'
  }

  return {
    tabs,
    activeTab,
    tabPaths,
    addTab,
    closeTab,
    closeOthers,
    closeAll,
    closeRight,
    setActiveTab,
    reset
  }
})