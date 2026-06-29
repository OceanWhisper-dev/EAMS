import request from '@/utils/request'

// ==================== 类型定义 ====================

export interface VouchQueryParam {
  cusCode?: string
  cusName?: string
  cusAbbName?: string
  cusContact?: string
  cusPPerson?: string
  cusPhone?: string
  cusMobile?: string
  cusAddr?: string
  vouchCode?: string
  vouchDateFrom?: string
  vouchDateTo?: string
  verifierStatus?: string
  page: number
  pageSize: number
}

export interface OrderDto {
  soid: number
  csoCode: string
  soDate: string | null
  soCusCode: string
  soCusName: string
  cPersonName: string | null
  orderDetails: OrderDetailDto[]
  dispatches: DispatchDto[]
}

export interface OrderDetailDto {
  soInvCode: string | null
  soInvName: string | null
  soInvStd: string | null
  soQuantity: number
}

export interface DispatchDto {
  dlid: number
  cdlCode: string
  dlDate: string | null
  dlCusCode: string
  dlCusName: string
  cVerifier: string | null
  dispatchDetails: DispatchDetailDto[]
}

export interface DispatchDetailDto {
  dlInvCode: string | null
  dlInvName: string | null
  dlInvStd: string | null
  dlQuantity: number
}

export interface UnverifiedDispatchRow {
  dlid: number
  cdlCode: string
  cusCode: string
  cusName: string
  dDate: string | null
  cVerifier: string | null
  cPersonName: string | null
}

export interface UpdateCustomerRequest {
  soid?: number
  dlid?: number
  newCusCode: string
  newCusName: string
  oldCusCode?: string
  oldCusName?: string
  syncDispatches: boolean
}

export interface UpdateDateRequest {
  dlCode?: string
  dlid?: number
  newDate: string
}

export interface BatchUpdateDateRequest {
  dlids: number[]
  newDate: string
  autoCalculate: boolean
}

export interface BatchUpdateResult {
  totalCount: number
  successCount: number
  failCount: number
  failures: BatchFailItem[]
}

export interface BatchFailItem {
  dlid: number
  dlCode: string
  errorMessage: string
}

export interface CustomerRef {
  cusCode: string
  cusName: string
}

export interface VouchModifyLog {
  id: number
  vouchType: string
  vouchId: number
  vouchCode: string
  fieldName: string
  oldValue: string | null
  newValue: string | null
  operatorId: number
  operatorName: string
  operateAt: string
  status: string
  errorMsg: string | null
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// ==================== API 方法 ====================

export const vouchModifyApi = {
  // 查询
  queryOrders(params: VouchQueryParam): Promise<PagedResult<OrderDto>> {
    return request.get('/erp/vouch-modify/orders', { params })
  },
  queryDispatches(params: VouchQueryParam): Promise<PagedResult<DispatchDto>> {
    return request.get('/erp/vouch-modify/dispatches', { params })
  },
  queryUnverifiedDispatches(params: VouchQueryParam): Promise<PagedResult<UnverifiedDispatchRow>> {
    return request.get('/erp/vouch-modify/unverified-dispatches', { params })
  },
  getDispatch(dlcode: string): Promise<DispatchDto> {
    return request.get(`/erp/vouch-modify/dispatch/${dlcode}`)
  },
  hasVerifiedDispatches(soid: number): Promise<{ hasVerified: boolean }> {
    return request.get(`/erp/vouch-modify/orders/${soid}/has-verified-dispatches`)
  },

  // 修改
  updateOrderCustomer(data: UpdateCustomerRequest): Promise<{ message: string }> {
    return request.put('/erp/vouch-modify/order/customer', data)
  },
  updateDispatchCustomer(data: UpdateCustomerRequest): Promise<{ message: string }> {
    return request.put('/erp/vouch-modify/dispatch/customer', data)
  },
  updateDispatchDate(data: UpdateDateRequest): Promise<{ message: string }> {
    return request.put('/erp/vouch-modify/dispatch/date', data)
  },
  batchUpdateDispatchDate(data: BatchUpdateDateRequest): Promise<BatchUpdateResult> {
    return request.put('/erp/vouch-modify/dispatch/date/batch', data)
  },

  // 客户参照
  getCustomerRef(code: string): Promise<CustomerRef> {
    return request.get(`/erp/vouch-modify/customer-ref/${code}`)
  },

  // 日志查询
  queryLogs(params: {
    vouchType?: string
    operatorId?: number
    from?: string
    to?: string
    page?: number
    pageSize?: number
  }): Promise<PagedResult<VouchModifyLog>> {
    return request.get('/erp/vouch-modify/logs', { params })
  },

  // 业务员
  getSalespersons(): Promise<{ code: string; name: string }[]> {
    return request.get('/erp/vouch-modify/salespersons')
  }
}