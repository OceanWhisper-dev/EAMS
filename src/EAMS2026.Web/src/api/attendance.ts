import request from '@/utils/request'

/* ---- 考勤模块类型定义 ---- */

export interface DayType {
  id: number
  name: string
  [key: string]: unknown
}

export interface SchemeClass {
  id: number
  className: string
  [key: string]: unknown
}

export interface PlanTime {
  id: number
  planName: string
  description?: string
  bTime: string
  eTime: string
  dayTypeId: number
  [key: string]: unknown
}

export interface PlanRefClass {
  id: number
  planId: number
  classId: number
  [key: string]: unknown
}

export interface EventRecord {
  id: number
  recordId: number
  isBeginTime: boolean
  time: string
  [key: string]: unknown
}

export interface Holiday {
  id: number
  date: string
  name: string
  [key: string]: unknown
}

export interface EmployeeRefClass {
  id: number
  employeeId: number
  employeeName: string
  classId: number
  className: string
  periodNo: number
  effDate: string
  expDate: string
  [key: string]: unknown
}

export interface FeeCalculator {
  id: number
  dayTypeId: number
  rangeA: number
  rangeB: number
  rangePrice: number
  [key: string]: unknown
}

export interface AttendanceRecord {
  id: number
  employeeId: number
  employeeName: string
  [key: string]: unknown
}

export interface CardRecord {
  id: number
  employeeName: string
  [key: string]: unknown
}

export interface AttendanceEmployee {
  id: number
  name: string
  [key: string]: unknown
}

export type AttendanceCreateData = Record<string, unknown>
export type AttendanceUpdateData = Record<string, unknown>

export const attendanceApi = {
  searchReport: (data: { employeeId?: number; startDate: string; endDate: string }) =>
    request.post('/attendance/report/search', data),

  getEmployees: () => request.get<AttendanceEmployee[]>('/attendance/employees'),
  updateEmployee: (id: number, data: Partial<AttendanceEmployee>) => request.put(`/attendance/employees/${id}`, data),
  updateEmployeeMapping: (id: number, systemEmployeeId: number | null) =>
    request.put(`/attendance/employees/${id}/mapping`, { systemEmployeeId }),
  deleteEmployee: (id: number) => request.delete(`/attendance/employees/${id}`),
  exportEmployees: () => request.get('/attendance/employees/export'),

  searchRecords: (data: { employeeId?: number; startDate?: string; endDate?: string; page: number; pageSize: number }) =>
    request.post('/attendance/records/search', data),

  getRecord: (id: number) => request.get<AttendanceRecord>(`/attendance/records/${id}`),
  createRecord: (data: AttendanceCreateData) => request.post('/attendance/records', data),
  updateRecord: (id: number, data: AttendanceUpdateData) => request.put(`/attendance/records/${id}`, data),
  deleteRecord: (id: number) => request.delete(`/attendance/records/${id}`),

  getDayTypes: () => request.get<DayType[]>('/attendance/day-types'),
  createDayType: (data: Partial<DayType>) => request.post('/attendance/day-types', data),
  updateDayType: (id: number, data: Partial<DayType>) => request.put(`/attendance/day-types/${id}`, data),
  deleteDayType: (id: number) => request.delete(`/attendance/day-types/${id}`),

  getSchemeClasses: () => request.get<SchemeClass[]>('/attendance/scheme-classes'),
  createSchemeClass: (data: Partial<SchemeClass>) => request.post('/attendance/scheme-classes', data),
  updateSchemeClass: (id: number, data: Partial<SchemeClass>) => request.put(`/attendance/scheme-classes/${id}`, data),
  deleteSchemeClass: (id: number) => request.delete(`/attendance/scheme-classes/${id}`),

  getPlanTimes: () => request.get<PlanTime[]>('/attendance/plan-times'),
  createPlanTime: (data: Partial<PlanTime>) => request.post('/attendance/plan-times', data),
  updatePlanTime: (id: number, data: Partial<PlanTime>) => request.put(`/attendance/plan-times/${id}`, data),
  deletePlanTime: (id: number) => request.delete(`/attendance/plan-times/${id}`),

  getPlanRefClasses: (planId: number) => request.get<PlanRefClass[]>(`/attendance/plan-times/${planId}/ref-classes`),
  addPlanRefClass: (data: Partial<PlanRefClass>) => request.post('/attendance/plan-ref-classes', data),
  deletePlanRefClass: (id: number) => request.delete(`/attendance/plan-ref-classes/${id}`),

  getEvents: (recordId: number) => request.get<EventRecord[]>(`/attendance/records/${recordId}/events`),
  createEvent: (data: Partial<EventRecord>) => request.post('/attendance/events', data),
  updateEvent: (id: number, data: Partial<EventRecord>) => request.put(`/attendance/events/${id}`, data),
  deleteEvent: (id: number) => request.delete(`/attendance/events/${id}`),

  getHolidays: (year?: number) => request.get<Holiday[]>('/attendance/holidays', { params: { year } }),
  createHoliday: (data: Partial<Holiday>) => request.post('/attendance/holidays', data),
  updateHoliday: (id: number, data: Partial<Holiday>) => request.put(`/attendance/holidays/${id}`, data),
  deleteHoliday: (id: number) => request.delete(`/attendance/holidays/${id}`),

  getEmployeeRefClasses: (employeeId?: number) =>
    request.get<EmployeeRefClass[]>('/attendance/employee-ref-classes', { params: { employeeId } }),
  addEmployeeRefClass: (data: Partial<EmployeeRefClass>) => request.post('/attendance/employee-ref-classes', data),
  updateEmployeeRefClass: (id: number, data: Partial<EmployeeRefClass>) => request.put(`/attendance/employee-ref-classes/${id}`, data),
  deleteEmployeeRefClass: (id: number) => request.delete(`/attendance/employee-ref-classes/${id}`),

  getFeeCalculators: (dayTypeId?: number) =>
    request.get<FeeCalculator[]>('/attendance/fee-calculators', { params: { dayTypeId } }),
  createFeeCalculator: (data: Partial<FeeCalculator>) => request.post('/attendance/fee-calculators', data),
  updateFeeCalculator: (id: number, data: Partial<FeeCalculator>) => request.put(`/attendance/fee-calculators/${id}`, data),
  deleteFeeCalculator: (id: number) => request.delete(`/attendance/fee-calculators/${id}`),

  syncEmployees: () => request.post('/attendance/hwatt/sync-employees'),

  syncCardRecords: (data: { startDate: string; endDate: string }) =>
    request.post('/attendance/hwatt/sync-card-records', data),

  importDevice: (data: { employeeId?: number; beginDate: string; endDate: string }) =>
    request.post('/attendance/hwatt/import-records', data),

  importAllStart: (data: { doDate: string; endDate?: string }) =>
    request.post('/attendance/hwatt/import-all-records', data),

  getImportStatus: (taskId: string) =>
    request.get(`/attendance/hwatt/import-tasks/${taskId}`),

  // 钉钉数据导入
  dingtalkSyncEmployees: () => request.post('/attendance/dingtalk/sync-employees'),

  dingtalkSyncCardRecords: (data: { startDate: string; endDate: string }) =>
    request.post('/attendance/dingtalk/sync-card-records', data),

  // 查询原始打卡记录
  getCardRecords: (data: { employeeName?: string; startDate?: string; endDate?: string; page: number; pageSize: number }) =>
    request.post('/attendance/card-records', data),

  // 数据权限配置
  getDataPermissionRoles: () => request.get('/data-permission/roles'),
  getDataPermissionRules: (module: string) => request.get(`/data-permission/rules/${module}`),
  saveDataPermissionRule: (module: string, data: { roleId: number; dataScope: string }) =>
    request.post(`/data-permission/rules/${module}/save`, data),
  deleteDataPermissionRule: (module: string, roleId: number) =>
    request.delete(`/data-permission/rules/${module}/${roleId}`)
}