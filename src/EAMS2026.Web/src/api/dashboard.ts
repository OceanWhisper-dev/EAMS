import request from '@/utils/request'

/* ---- 仪表盘模块类型定义 ---- */

export interface WidgetConfig {
  id?: number
  type: string
  title: string
  [key: string]: unknown
}

export interface WidgetData {
  widgetKey?: string
  widgetName?: string
  widgetType?: string
  type?: string
  title?: string
  description?: string
  icon?: string
  dataSourceType?: string
  dataSourceConfig?: string
  layoutConfig?: string
  defaultConfig?: string
  refreshInterval?: number
  sortOrder?: number
  [key: string]: unknown
}

export const dashboardApi = {
  getStats() {
    return request.get('/dashboard/stats')
  },
  getWidgets() {
    return request.get('/dashboard/widgets')
  },
  getRoleConfig(roleId: number) {
    return request.get('/dashboard/config', { params: { roleId } })
  },
  saveRoleConfig(roleId: number, configs: WidgetConfig[]) {
    return request.post('/dashboard/config', { roleId, configs })
  },
  getMyDashboard() {
    return request.get('/dashboard/my-dashboard')
  },
  addWidget(data: WidgetData) {
    return request.post('/dashboard/widget', data)
  },
  updateWidget(id: number, data: Partial<WidgetData>) {
    return request.put(`/dashboard/widget/${id}`, data)
  },
  deleteWidget(id: number) {
    return request.delete(`/dashboard/widget/${id}`)
  },
  previewWidgetData(data: WidgetData) {
    return request.post('/dashboard/widget/preview', data)
  }
}
