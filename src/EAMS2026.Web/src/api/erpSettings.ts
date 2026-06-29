import request from '@/utils/request'

/**
 * ERP 设置 API
 * 提供数据源配置、业务员映射等共享设置
 * 报表模块与单据修改模块共用
 */
export const erpSettingsApi = {
  // ===== 数据源 =====
  getDataSources() {
    return request.get('/erp/settings/datasources')
  },
  getDataSource(id: number) {
    return request.get(`/erp/settings/datasources/${id}`)
  },
  addDataSource(data: {
    name: string
    displayName?: string
    dbType: string
    connectionString: string
    description?: string
    sortOrder?: number
    isEnabled?: boolean
  }) {
    return request.post('/erp/settings/datasources', data)
  },
  updateDataSource(id: number, data: Record<string, any>) {
    return request.put(`/erp/settings/datasources/${id}`, data)
  },
  deleteDataSource(id: number) {
    return request.delete(`/erp/settings/datasources/${id}`)
  },
  testDataSource(data: { dbType: string; connectionString: string }) {
    return request.post('/erp/settings/datasources/test', data)
  },

  // ===== 业务员映射 =====
  getSalespersonMappings() {
    return request.get('/erp/settings/salespersons/mappings')
  },
  saveSalespersonMapping(data: {
    employeeId: number
    salespersonCode: string
    salespersonName: string
    type?: string
  }) {
    return request.post('/erp/settings/salespersons/mappings', data)
  },
  deleteSalespersonMapping(employeeId: number) {
    return request.delete(`/erp/settings/salespersons/mappings/${employeeId}`)
  },
  getCurrentSalesperson() {
    return request.get('/erp/settings/salespersons/current')
  },

  // ===== 业务员列表（从映射表读取，供下拉框使用）=====
  getSalespersons() {
    return request.get('/erp/settings/salespersons')
  }
}