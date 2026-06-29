export function formatDateTime(date: string | Date | null | undefined): string {
  if (!date) return '-'
  const d = new Date(date)
  if (isNaN(d.getTime())) return '-'
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hours = String(d.getHours()).padStart(2, '0')
  const minutes = String(d.getMinutes()).padStart(2, '0')
  const seconds = String(d.getSeconds()).padStart(2, '0')
  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
}

export function formatDate(date: string | Date | null | undefined): string {
  if (!date) return '-'
  const d = new Date(date)
  if (isNaN(d.getTime())) return '-'
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/**
 * 安全的日期时间格式化函数（适用于表格单元格显示）
 * - 接受 unknown 类型输入
 * - null/undefined/空字符串返回空字符串（不在单元格显示 "-"）
 * - 非日期字符串返回原值兜底
 */
export function formatDateTimeSafe(val: unknown): string {
  if (val === null || val === undefined || val === '') return ''
  if (val instanceof Date || typeof val === 'string' || typeof val === 'number') {
    const d = new Date(val)
    if (!isNaN(d.getTime())) {
      const year = d.getFullYear()
      const month = String(d.getMonth() + 1).padStart(2, '0')
      const day = String(d.getDate()).padStart(2, '0')
      const hours = String(d.getHours()).padStart(2, '0')
      const minutes = String(d.getMinutes()).padStart(2, '0')
      const seconds = String(d.getSeconds()).padStart(2, '0')
      return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
    }
  }
  return String(val)
}

/**
 * 金额格式化，保留两位小数千分位
 */
export function formatMoney(val: unknown): string {
  if (val === null || val === undefined) return ''
  const num = Number(val)
  if (isNaN(num)) return String(val)
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

/**
 * 格式化汇总值
 * - 整数值（distinct count）显示为千位分隔整数
 * - 小数金额值（sum）显示为金额格式
 * - 其他值转为字符串
 */
export function formatSummaryValue(val: unknown): string {
  if (val === null || val === undefined) return ''
  if (typeof val === 'number' && Number.isInteger(val)) return val.toLocaleString()
  if (typeof val === 'number') return formatMoney(val)
  return String(val)
}
