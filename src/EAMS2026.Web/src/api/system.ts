import request from '@/utils/request'

/* ---- 系统管理模块类型定义 ---- */

export interface MessageSendData {
  title: string
  content: string
  receiverId?: number | null
  receiverIds?: number[]
  priority?: string
  [key: string]: unknown
}

export interface DepartmentData {
  id?: number
  name: string
  parentId?: number | null
  [key: string]: unknown
}

export interface EmployeeData {
  id?: number
  name: string
  departmentId?: number
  [key: string]: unknown
}

export interface UserData {
  id?: number
  username: string
  employeeId?: number
  [key: string]: unknown
}

export interface RoleData {
  id?: number
  name: string
  code?: string
  description?: string
  [key: string]: unknown
}

export interface PermissionData {
  id?: number
  name: string
  code?: string
  parentId?: number | null
  [key: string]: unknown
}

export interface DictTypeData {
  id?: number
  name: string
  code: string
  [key: string]: unknown
}

export interface DictItemData {
  id?: number
  typeId: number
  label: string
  value: string
  [key: string]: unknown
}

export function downloadBlob(data: Blob, fileName: string) {
  const url = window.URL.createObjectURL(data)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  window.URL.revokeObjectURL(url)
}

export const messageApi = {
  getReceived: (page = 1, pageSize = 20) => request.get('/message/received', { params: { page, pageSize } }),
  getSent: (page = 1, pageSize = 20) => request.get('/message/sent', { params: { page, pageSize } }),
  getUnreadCount: () => request.get('/message/unread-count'),
  getById: (id: number) => request.get(`/message/${id}`),
  getConversation: (id: number) => request.get(`/message/${id}/conversation`),
  send: (data: MessageSendData) => request.post('/message', data),
  reply: (id: number, content: string) => request.post(`/message/${id}/reply`, { content }),
  markAsRead: (id: number) => request.put(`/message/${id}/read`),
  markAllAsRead: () => request.put('/message/read-all'),
  delete: (id: number) => request.delete(`/message/${id}`)
}

export const importExportApi = {
  exportData: (module: string) => request.get(`/import-export/export/${module}`, { responseType: 'blob' }),
  downloadTemplate: (module: string) => request.get(`/import-export/template/${module}`, { responseType: 'blob' }),
  importData: (module: string, file: File, overwrite = false) => {
    const formData = new FormData()
    formData.append('file', file)
    return request.post(`/import-export/import/${module}${overwrite ? '?overwrite=true' : ''}`, formData)
  }
}

export const printApi = {
  print: (module: string) => request.get(`/print/${module}`, { responseType: 'blob' })
}

export const departmentApi = {
  getTree: () => request.get('/department/tree'),
  getById: (id: number) => request.get(`/department/${id}`),
  create: (data: DepartmentData) => request.post('/department', data),
  update: (data: DepartmentData) => request.put('/department', data),
  delete: (id: number) => request.delete(`/department/${id}`)
}

export const employeeApi = {
  getAll: () => request.get('/employee'),
  getById: (id: number) => request.get(`/employee/${id}`),
  getByDepartmentId: (deptId: number) => request.get(`/employee/by-department/${deptId}`),
  create: (data: EmployeeData) => request.post('/employee', data),
  update: (data: EmployeeData) => request.put('/employee', data),
  delete: (id: number) => request.delete(`/employee/${id}`),
  getDeleted: () => request.get('/employee/deleted'),
  hardDelete: (id: number) => request.delete(`/employee/${id}/hard`)
}

export const userApi = {
  getAll: () => request.get('/user'),
  getById: (id: number) => request.get(`/user/${id}`),
  create: (data: any) => request.post('/user', data),
  update: (data: any) => request.put('/user', data),
  delete: (id: number) => request.delete(`/user/${id}`),
  assignRoles: (userId: number, roleIds: number[]) => request.post(`/user/${userId}/roles`, roleIds),
  resetPassword: (userId: number) => request.post(`/user/${userId}/reset-password`),
  getDeleted: () => request.get('/user/deleted'),
  hardDelete: (id: number) => request.delete(`/user/${id}/hard`)
}

export const roleApi = {
  getAll: () => request.get('/role'),
  getById: (id: number) => request.get(`/role/${id}`),
  create: (data: RoleData) => request.post('/role', data),
  update: (data: RoleData) => request.put('/role', data),
  delete: (id: number) => request.delete(`/role/${id}`),
  getPermissions: (roleId: number) => request.get(`/role/${roleId}/permissions`),
  assignPermissions: (roleId: number, permissionIds: number[]) => request.post(`/role/${roleId}/permissions`, permissionIds),
  getUsers: (roleId: number) => request.get(`/role/${roleId}/users`),
  assignUsers: (roleId: number, userIds: number[]) => request.post(`/role/${roleId}/users`, userIds),
  getDeleted: () => request.get('/role/deleted'),
  hardDelete: (id: number) => request.delete(`/role/${id}/hard`)
}

export const permissionApi = {
  getTree: () => request.get('/permission/tree'),
  getById: (id: number) => request.get(`/permission/${id}`),
  create: (data: any) => request.post('/permission', data),
  update: (data: any) => request.put('/permission', data),
  delete: (id: number) => request.delete(`/permission/${id}`)
}

export const dictApi = {
  getTypes: () => request.get('/dict/types'),
  getTypeById: (id: number) => request.get(`/dict/types/${id}`),
  createType: (data: DictTypeData) => request.post('/dict/types', data),
  updateType: (data: DictTypeData) => request.put('/dict/types', data),
  deleteType: (id: number) => request.delete(`/dict/types/${id}`),
  getItems: (typeId: number) => request.get(`/dict/types/${typeId}/items`),
  getItemsByCode: (code: string) => request.get(`/dict/items/${code}`),
  createItem: (data: DictItemData) => request.post('/dict/items', data),
  updateItem: (data: DictItemData) => request.put('/dict/items', data),
  deleteItem: (id: number) => request.delete(`/dict/items/${id}`)
}

export const authApi = {
  getProfile: () => request.get('/auth/profile'),
  updateProfile: (data: { phone?: string; email?: string; position?: string }) => request.put('/auth/profile', data)
}

export const operationLogApi = {
  getPaged: (params: { page: number; pageSize: number; module?: string; startDate?: string; endDate?: string }) =>
    request.get('/operation-log', { params }),
  getMine: (params: { page: number; pageSize: number }) =>
    request.get('/operation-log/mine', { params })
}