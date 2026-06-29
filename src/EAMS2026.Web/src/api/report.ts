import request from '@/utils/request'

/* ---- 类型定义 ---- */

export interface ReportCategory {
  id: number
  name: string
  parentId: number | null
  sortOrder: number
  children?: ReportCategory[]
}

export interface ReportField {
  id?: number
  reportId?: number
  fieldName: string
  fieldTitle: string
  fieldType: string
  sortOrder: number
  width: number
  align: string
  isDisplay: boolean
  isSortable: boolean
  isFilterable: boolean
  isGroupable: boolean
  isSummary: boolean
  summaryType: string
  formatPattern: string
}

export interface ReportFilter {
  id?: number
  reportId?: number
  fieldName: string
  label: string
  operator: string
  defaultValue: string
  controlType: string
  dataUrl: string
  sortOrder: number
}

export interface ReportSort {
  id?: number
  reportId?: number
  fieldName: string
  direction: string
  sortOrder: number
}

export interface ReportChart {
  id?: number
  reportId?: number
  title: string
  chartType: string
  dataField: string
  categoryField: string
  width: number
  height: number
  options: string
}

export interface ReportDto {
  id: number
  name: string
  title: string
  description: string | null
  categoryId: number | null
  categoryName: string | null
  queryType: string
  queryDatasource: string
  isSystem: boolean
  status: string
  defaultTab?: string
  isBookmarked?: boolean
  canManage?: boolean
  createdAt: string
  updatedAt: string
}

export interface ReportDetailDto extends ReportDto {
  queryText: string
  fields: ReportField[]
  filters: ReportFilter[]
  sorts: ReportSort[]
  charts: ReportChart[]
}

export interface ReportColumnDto {
  field: string
  title: string
  type: string
  width?: number
  align?: string
  isSortable: boolean
}

export interface ReportPagination {
  page: number
  pageSize: number
}

export interface ReportExecuteRequest {
  params?: Record<string, unknown>
  pagination?: ReportPagination
  sort?: { field: string; direction: string }[]
}

export interface ReportExecuteResult {
  columns: ReportColumnDto[]
  rows: Record<string, unknown>[]
  summary?: Record<string, unknown>
  pagination?: {
    page: number
    pageSize: number
    total: number
  }
  executionInfo?: {
    durationMs: number
    rowCount: number
  }
}

export interface ReportPreviewRequest {
  queryText: string
  queryDatasource: string
  queryType?: string
  params?: Record<string, unknown>
  filters?: Array<{
    fieldName: string
    label: string
    operator: string
    defaultValue?: string
    controlType: string
  }>
  fields?: Array<{
    fieldName: string
    fieldTitle: string
    width: number
    align: string
    isSortable: boolean
    summaryType?: string
    formatPattern?: string
  }>
  page?: number
  pageSize?: number
}

export interface ReportExportRequest {
  format: string
  params?: Record<string, unknown>
}

/* ---- API 方法 ---- */

export const reportApi = {
  // ===== 分类 =====
  getCategories() {
    return request.get('/reports/categories')
  },
  addCategory(data: { name: string; parentId?: number | null; sortOrder: number }) {
    return request.post('/reports/categories', data)
  },
  updateCategory(id: number, data: { name: string; parentId?: number | null; sortOrder: number }) {
    return request.put(`/reports/categories/${id}`, data)
  },
  deleteCategory(id: number) {
    return request.delete(`/reports/categories/${id}`)
  },

  // ===== 报表CRUD =====
  getReports(categoryId?: number) {
    return request.get('/reports', { params: { categoryId } })
  },
  getReportDetail(id: number) {
    return request.get(`/reports/${id}`)
  },
  addReport(data: ReportDetailDto) {
    return request.post('/reports', data)
  },
  updateReport(id: number, data: ReportDetailDto) {
    return request.put(`/reports/${id}`, data)
  },
  deleteReport(id: number) {
    return request.delete(`/reports/${id}`)
  },
  updateStatus(id: number, status: string) {
    return request.patch(`/reports/${id}/status`, { status })
  },

  // ===== 执行 =====
  executeReport(id: number, data: ReportExecuteRequest) {
    return request.post(`/reports/${id}/execute`, data)
  },
  previewReport(data: ReportPreviewRequest) {
    return request.post('/reports/preview', data)
  },

  // ===== 导出 =====
  exportReport(id: number, format: string, params?: Record<string, unknown>) {
    return request.post(`/reports/${id}/export`, { format, params } satisfies ReportExportRequest, {
      responseType: 'blob'
    })
  },

  // ===== 收藏 =====
  getBookmarks() {
    return request.get('/reports/bookmarks')
  },
  toggleBookmark(reportId: number) {
    return request.post(`/reports/bookmarks/${reportId}`)
  },

  // ===== 数据源配置 =====
  getDataSources() {
    return request.get('/reports/datasources')
  },
  getDataSource(id: number) {
    return request.get(`/reports/datasources/${id}`)
  },
  addDataSource(data: {
    name: string
    displayName: string
    dbType: string
    connectionString: string
    description?: string
    sortOrder: number
    isEnabled: boolean
  }) {
    return request.post('/reports/datasources', data)
  },
  updateDataSource(id: number, data: {
    displayName: string
    dbType: string
    connectionString: string
    description?: string
    sortOrder: number
    isEnabled: boolean
  }) {
    return request.put(`/reports/datasources/${id}`, data)
  },
  deleteDataSource(id: number) {
    return request.delete(`/reports/datasources/${id}`)
  },
  testDataSource(data: { dbType: string; connectionString: string }) {
    return request.post('/reports/datasources/test', data)
  },

  // ===== 业务员 =====
  getSalespersons(datasource?: string) {
    return request.get('/reports/salespersons', { params: { datasource } })
  },
  getCurrentSalesperson() {
    return request.get('/reports/salespersons/current')
  },

  // ===== 业务员映射管理 =====
  getSalespersonMappings() {
    return request.get('/reports/salespersons/mappings')
  },
  saveSalespersonMapping(data: { employeeId: number; salespersonCode: string; salespersonName: string; type?: string }) {
    return request.post('/reports/salespersons/mappings', data)
  },
  deleteSalespersonMapping(employeeId: number) {
    return request.delete(`/reports/salespersons/mappings/${employeeId}`)
  },

  // ===== 报表权限管理 =====
  /** 获取报表权限列表 */
  getReportPermissions(reportId: number) {
    return request.get(`/reports/${reportId}/permissions`)
  },
  /** 检查当前用户是否有指定权限 */
  checkPermission(reportId: number, accessType = 'manage') {
    return request.get(`/reports/${reportId}/check-permission`, { params: { accessType } })
  },
  /** 获取可选用户/角色列表 */
  getPrincipalOptions() {
    return request.get('/reports/permissions/principals')
  },
  /** 添加报表权限 */
  setReportPermission(reportId: number, data: { principalType: string; principalId: number; accessType: string }) {
    return request.post(`/reports/${reportId}/permissions`, data)
  },
  /** 删除报表权限 */
  deleteReportPermission(permissionId: number) {
    return request.delete(`/reports/permissions/${permissionId}`)
  },

  // ===== 透视表配置 =====
  /** 获取透视表配置列表 */
  getPivotViews(reportId: number) {
    return request.get(`/reports/${reportId}/pivots`)
  },
  /** 保存透视表配置 */
  savePivotView(data: SavePivotViewRequest) {
    return request.post('/reports/pivots', data)
  },
  /** 删除透视表配置 */
  deletePivotView(id: number) {
    return request.delete(`/reports/pivots/${id}`)
  },

  // ===== 透视表共享管理 =====
  /** 获取共享目标列表 */
  getShares(pivotViewId: number) {
    return request.get(`/reports/pivots/${pivotViewId}/shares`)
  },
  /** 添加共享目标（指定用户或角色） */
  addShare(pivotViewId: number, data: PivotViewShareTargetRequest) {
    return request.post(`/reports/pivots/${pivotViewId}/shares`, data)
  },
  /** 移除共享目标 */
  removeShare(pivotViewId: number, shareId: number) {
    return request.delete(`/reports/pivots/${pivotViewId}/shares/${shareId}`)
  },

  // ===== 报表配置导入导出（仅管理员） =====
  /** 导出全部报表配置为 JSON 文件 */
  exportReportsConfig() {
    return request.get('/reports/export-config', { responseType: 'blob' })
  },
  /** 导入报表配置 JSON 文件 */
  importReportsConfig(file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return request.post('/reports/import-config', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },

  // ===== 数据表视图格式配置（类似透视表样式保存/分享） =====
  /** 获取数据表视图列表 */
  getTableViews(reportId: number) {
    return request.get(`/reports/${reportId}/table-views`)
  },
  /** 保存数据表视图 */
  saveTableView(data: SaveTableViewRequest) {
    return request.post('/reports/table-views', data)
  },
  /** 删除数据表视图 */
  deleteTableView(id: number) {
    return request.delete(`/reports/table-views/${id}`)
  },
  /** 获取数据表视图共享列表 */
  getTableViewShares(viewId: number) {
    return request.get(`/reports/table-views/${viewId}/shares`)
  },
  /** 添加共享目标 */
  addTableViewShare(viewId: number, data: PivotViewShareTargetRequest) {
    return request.post(`/reports/table-views/${viewId}/shares`, data)
  },
  /** 移除共享目标 */
  removeTableViewShare(viewId: number, shareId: number) {
    return request.delete(`/reports/table-views/${viewId}/shares/${shareId}`)
  }
}

export interface ReportPivotDto {
  id: number
  reportId: number
  userId: number
  pivotName: string
  pivotParams: string
  isLast: boolean
  isDefault: boolean
  isShared: boolean
  creatorName?: string
  createdAt: string
  updatedAt: string
}

export interface SavePivotViewRequest {
  id?: number
  reportId: number
  pivotName: string
  pivotParams: string
  isDefault: boolean
  isLast: boolean
  creatorName?: string
}

export interface PivotViewShareDto {
  id: number
  pivotViewId: number
  targetType: string
  targetId: number
  targetName: string
  createdAt: string
}

export interface PivotViewShareTargetRequest {
  targetType: string
  targetId: number
}

// ===== 数据表视图配置 =====

export interface ReportTableViewDto {
  id: number
  reportId: number
  userId: number
  viewName: string
  viewParams: string
  isLast: boolean
  isDefault: boolean
  isShared: boolean
  creatorName?: string
  createdAt: string
  updatedAt: string
}

export interface SaveTableViewRequest {
  id?: number
  reportId: number
  viewName: string
  viewParams: string
  isDefault: boolean
  isLast: boolean
  creatorName?: string
}

export interface TableViewShareDto {
  id: number
  viewId: number
  targetType: string
  targetId: number
  targetName: string
  createdAt: string
}