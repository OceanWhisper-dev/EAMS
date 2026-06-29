<template>
  <div>
    <div class="page-header">
      <h3>考勤报表</h3>
    </div>

    <el-card class="search-card">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="员工">
          <el-select v-model="searchForm.employeeId" placeholder="全部员工" clearable filterable style="width:200px">
            <el-option v-for="e in employees" :key="e.employeeId" :label="e.employeeName" :value="e.employeeId" />
          </el-select>
        </el-form-item>
        <el-form-item label="日期范围">
          <el-date-picker
            v-model="searchForm.dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width:260px"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="doSearch">查询</el-button>
          <el-button @click="doSearch">刷新</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card style="margin-top:10px">
      <div class="toolbar">
        <el-divider content-position="left" style="margin:8px 0">
          <span style="font-size:13px;font-weight:500">考勤操作</span>
        </el-divider>
        <el-button v-permission="'attendance:import-all'" type="success" @click="handleImportAll">
          导入所有员工考勤
        </el-button>
        <el-button v-permission="'attendance:import'" type="primary" @click="handleImport">
          导入考勤
        </el-button>
        <el-button v-permission="'attendance:print'" type="primary" @click="handlePrint">
          打印
        </el-button>
        <div class="progress-area" :class="{ 'no-display': !showProgress }">
          <el-progress :percentage="progressValue" :show-text="true" :precision="2" style="width:200px;margin-left:16px;" />
        </div>
      </div>

      <el-table :data="reportData" border stripe v-loading="loading" style="margin-top:10px">
        <el-table-column label="日" width="60" align="center">
          <template #default="{ row }">{{ formatDay(row.sDate) }}</template>
        </el-table-column>
        <el-table-column prop="employeeName" label="员工" width="80" />
        <el-table-column label="周" width="90" align="center">
          <template #default="{ row }">{{ row.description || getDayOfWeek(row.sDate) }}</template>
        </el-table-column>
        <el-table-column label="上班" width="65" align="center">
          <template #default="{ row }">{{ formatTime(row.bAttTime) }}</template>
        </el-table-column>
        <el-table-column label="上班时长" width="70" align="center">
          <template #default="{ row }">
            <el-link
              :type="(row.bOffset ?? 0) < 0 ? 'danger' : 'success'"
              :underline="'never'"
              @click="handleEditDuration(row, true)"
            >
              {{ row.bOffset }}
            </el-link>
          </template>
        </el-table-column>
        <el-table-column label="下班" width="65" align="center">
          <template #default="{ row }">{{ formatTime(row.eAttTime) }}</template>
        </el-table-column>
        <el-table-column label="下班时长" width="70" align="center">
          <template #default="{ row }">
            <el-link
              :type="(row.eOffset ?? 0) < 0 ? 'danger' : 'success'"
              :underline="'never'"
              @click="handleEditDuration(row, false)"
            >
              {{ row.eOffset }}
            </el-link>
          </template>
        </el-table-column>
        <el-table-column prop="event" label="事件" min-width="200" show-overflow-tooltip />
        <el-table-column prop="fee" label="费用" width="55" align="center" />
      </el-table>
      <div style="margin-top:20px;text-align:right;color:#909399">
        无分页
      </div>
    </el-card>

    <!-- 事件编辑弹窗 -->
    <el-dialog v-model="eventDialogVisible" :title="'事件编辑 - ' + eventDialogTitle" width="560px">
      <el-table :data="events" border size="small" style="margin-bottom:12px">
        <el-table-column prop="eventDescription" label="事件描述" min-width="180" />
        <el-table-column prop="fee" label="扣款" width="80" align="center" />
        <el-table-column prop="checkMan" label="审批人" width="90" />
        <el-table-column label="操作" width="100" align="center">
          <template #default="{ row, $index }">
            <el-button link type="primary" size="small" @click="handleEditEvent(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDeleteEvent(row, $index)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-button type="primary" size="small" @click="handleAddEvent">+ 添加事件</el-button>
      <template #footer>
        <el-button @click="eventDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 事件表单弹窗（新增/编辑） -->
    <el-dialog v-model="eventFormVisible" :title="eventFormTitle" width="420px" :close-on-click-modal="false">
      <el-form ref="eventFormRef" :model="eventForm" label-width="80px">
        <el-form-item label="事件描述" prop="eventDescription">
          <el-input v-model="eventForm.eventDescription" />
        </el-form-item>
        <el-form-item label="扣款" prop="fee">
          <el-input-number v-model="eventForm.fee" :min="0" :precision="2" controls-position="right" style="width:160px" :disabled="eventForm.id === 0" />
        </el-form-item>
        <el-form-item label="审批人" prop="checkMan">
          <el-input v-model="eventForm.checkMan" :disabled="eventForm.id === 0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="eventFormVisible = false">取消</el-button>
        <el-button type="primary" :loading="eventSaving" @click="handleSaveEvent">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { attendanceApi, type AttendanceEmployee, type EventRecord } from '@/api/attendance'
import { authApi } from '@/api/system'
import { useAuthStore } from '@/stores/auth'
import { openPrintWindow } from '@/utils/print'
import { formatDate } from '@/utils/date'
import { connectImportTask, disconnectImportTask } from '@/utils/signalr'

/** 考勤报表行记录 */
interface AttendanceReportRow {
  sDate: string
  description?: string
  bAttTime?: string
  bOffset?: number | null
  eAttTime?: string
  eOffset?: number | null
  event?: string
  fee?: number | null
  recordId: number
  employeeId?: number
  employeeName?: string
  [key: string]: unknown
}

/** 通用 API 响应体 */
interface ApiResponse<T = unknown> {
  success: boolean
  data: T
  message?: string
}

/** 当前登录用户简档 */
interface ProfileData {
  employee?: {
    name: string
  }
  [key: string]: unknown
}

/** 导入任务启动响应 */
interface ImportTaskResponse {
  taskId: string
}

/** 单员工导入响应 */
interface ImportDeviceResponse {
  count: number
}

/** 事件记录（含业务字段） */
interface AttendanceEvent extends EventRecord {
  eventDescription?: string
  fee?: number
  checkMan?: string
}

const loading = ref(false)
const showProgress = ref(false)
const progressValue = ref(0)
const reportData = ref<AttendanceReportRow[]>([])
const employees = ref<AttendanceEmployee[]>([])
const events = ref<AttendanceEvent[]>([])
const eventDialogVisible = ref(false)
const eventDialogTitle = ref('')
const eventFormVisible = ref(false)
const eventFormTitle = ref('')
const eventSaving = ref(false)
const eventFormRef = ref()
const eventForm = ref({
  id: 0,
  eventDescription: '',
  fee: 0,
  checkMan: '',
  recordId: 0,
  isBeginTime: true
})

const searchForm = ref({
  employeeId: null as number | null,
  dateRange: null as [string, string] | null
})

const authStore = useAuthStore()

const WEEK_NAMES = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

function getDayOfWeek(dateStr: string) {
  if (!dateStr) return ''
  return WEEK_NAMES[new Date(dateStr).getDay()]
}

onMounted(async () => {
  const today = new Date()
  const y = today.getFullYear()
  const m = today.getMonth()
  let attDateStart: Date, attDateEnd: Date
  if (today.getDate() < 10) {
    attDateStart = new Date(y, m - 1, 1)
    attDateEnd = new Date(y, m, 0)
  } else {
    attDateStart = new Date(y, m, 1)
    attDateEnd = new Date(y, m, today.getDate())
  }
  searchForm.value.dateRange = [
    formatDate(attDateStart),
    formatDate(attDateEnd)
  ]

  try {
    const [empRes, profileRes] = await Promise.all([
      attendanceApi.getEmployees(),
      authApi.getProfile()
    ])
    employees.value = (empRes.data || []).filter(
      (e: AttendanceEmployee, i: number, arr: AttendanceEmployee[]) => arr.findIndex((x: AttendanceEmployee) => x.employeeName === e.employeeName) === i
    )
    const profile = (profileRes as unknown as ApiResponse<ProfileData>).data
    if (profile?.employee?.name) {
      const match = employees.value.find((e: AttendanceEmployee) => e.employeeName === profile.employee!.name)
      if (match) {
        searchForm.value.employeeId = (match.employeeId as number) || null
      }
    }
  } catch { /* ignore */ }

  doSearch()
})

function formatDay(dateStr: string) {
  if (!dateStr) return ''
  return new Date(dateStr).getDate().toString()
}

function formatTime(timeStr: string) {
  if (!timeStr) return ''
  if (timeStr.includes(':')) return timeStr.slice(0, 5)
  return timeStr
}

async function doSearch() {
  if (!searchForm.value.dateRange) {
    ElMessage.warning('请选择日期范围')
    return
  }
  loading.value = true
  try {
    const res = await attendanceApi.searchReport({
      employeeId: searchForm.value.employeeId ?? undefined,
      startDate: searchForm.value.dateRange[0],
      endDate: searchForm.value.dateRange[1]
    }) as unknown as ApiResponse<AttendanceReportRow[]>
    reportData.value = res.data || []
  } finally {
    loading.value = false
  }
}

/* ---- 事件编辑 ---- */

const currentRecordId = ref(0)
const currentIsBegin = ref(true)

async function handleEditDuration(row: AttendanceReportRow, isBegin: boolean) {
  currentRecordId.value = row.recordId
  currentIsBegin.value = isBegin
  eventDialogTitle.value = (row.sDate || '') + ' - ' + (isBegin ? '上班' : '下班')
  await loadEvents(row.recordId, isBegin)
  eventDialogVisible.value = true
}

async function loadEvents(recordId: number, isBegin: boolean) {
  try {
    const res = await attendanceApi.getEvents(recordId) as unknown as ApiResponse<AttendanceEvent[]>
    const allEvents = res.data || []
    events.value = allEvents.filter((e: AttendanceEvent) => e.isBeginTime === isBegin)
  } catch {
    ElMessage.error('获取事件失败')
  }
}

function handleAddEvent() {
  eventFormTitle.value = '添加事件'
  eventForm.value = {
    id: 0,
    eventDescription: '',
    fee: 0,
    checkMan: '',
    recordId: currentRecordId.value,
    isBeginTime: currentIsBegin.value
  }
  eventFormVisible.value = true
}

function handleEditEvent(row: AttendanceEvent) {
  eventFormTitle.value = '编辑事件'
  eventForm.value = {
    id: row.id,
    eventDescription: row.eventDescription || '',
    fee: row.fee ?? 0,
    checkMan: row.checkMan || '',
    recordId: currentRecordId.value,
    isBeginTime: currentIsBegin.value
  }
  eventFormVisible.value = true
}

async function handleDeleteEvent(row: AttendanceEvent, _index: number) {
  try {
    await attendanceApi.deleteEvent(row.id)
    ElMessage.success('删除成功')
    await loadEvents(currentRecordId.value, currentIsBegin.value)
    await doSearch()
  } catch {
    ElMessage.error('删除失败')
  }
}

async function handleSaveEvent() {
  eventSaving.value = true
  try {
    const payload = {
      eventDescription: eventForm.value.eventDescription,
      fee: eventForm.value.fee,
      checkMan: eventForm.value.checkMan,
      recordId: eventForm.value.recordId,
      isBeginTime: eventForm.value.isBeginTime
    }
    if (eventForm.value.id) {
      await attendanceApi.updateEvent(eventForm.value.id, payload)
    } else {
      await attendanceApi.createEvent(payload)
    }
    ElMessage.success('保存成功')
    eventFormVisible.value = false
    await loadEvents(currentRecordId.value, currentIsBegin.value)
    await doSearch()
  } catch {
    ElMessage.error('保存失败')
  } finally {
    eventSaving.value = false
  }
}

/* ---- 导入考勤 ---- */

async function handleImportAll() {
  if (!searchForm.value.dateRange) {
    ElMessage.warning('请先选择日期范围')
    return
  }
  try {
    await ElMessageBox.confirm(
      `确认导入 ${searchForm.value.dateRange[0]} ~ ${searchForm.value.dateRange[1]} 所有员工考勤数据？`,
      '确认操作',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch {
    return
  }

  showProgress.value = true
  progressValue.value = 0

  try {
    const res = await attendanceApi.importAllStart({
      doDate: searchForm.value.dateRange[0],
      endDate: searchForm.value.dateRange[1]
    }) as unknown as ApiResponse<ImportTaskResponse>
    const taskId = res.data?.taskId
    if (!taskId) {
      ElMessage.error('启动导入任务失败')
      showProgress.value = false
      return
    }

    const conn = await connectImportTask(taskId)

    await new Promise<void>((resolve, reject) => {
      conn.on('ProgressUpdate', (data: { taskId: string; progress: number; message: string }) => {
        progressValue.value = data.progress
      })

      conn.on('Completed', async (data: { taskId: string; message: string }) => {
        await disconnectImportTask(taskId)
        ElMessage.success(data.message || '导入完成')
        showProgress.value = false
        progressValue.value = 0
        await doSearch()
        resolve()
      })

      conn.on('Failed', async (data: { taskId: string; message: string }) => {
        await disconnectImportTask(taskId)
        ElMessage.error(data.message || '导入失败')
        showProgress.value = false
        progressValue.value = 0
        reject(new Error(data.message))
      })
    })
  } catch (err: unknown) {
    if (!showProgress.value) return
    ElMessage.error((err as Error).message || '导入失败')
    showProgress.value = false
    progressValue.value = 0
  }
}

async function handleImport() {
  // 发送过滤条件中的员工ID，后端根据数据权限范围（ALL / DEPARTMENT / 无）自动判断：
  //   - ALL 范围（超级管理员/考勤管理员）：导入指定员工
  //   - DEPARTMENT 范围（部门主管）：导入指定员工
  //   - 无范围（普通员工）：强制导入本人
  const importEmployeeId = searchForm.value.employeeId
  if (!searchForm.value.dateRange || !importEmployeeId) {
    ElMessage.warning('请先选择员工和日期范围')
    return
  }
  showProgress.value = true
  loading.value = true
  try {
    const res = await attendanceApi.importDevice({
      employeeId: importEmployeeId,
      beginDate: searchForm.value.dateRange[0],
      endDate: searchForm.value.dateRange[1]
    }) as unknown as ApiResponse<ImportDeviceResponse>
    ElMessage.success(`导入完成，共处理 ${res.data?.count || 0} 天`)
    await doSearch()
  } catch {
    ElMessage.error('导入失败')
  } finally {
    loading.value = false
    showProgress.value = false
  }
}

/* ---- 打印 / 导出 ---- */

async function handlePrint() {
  const empName = searchForm.value.employeeId
    ? (employees.value.find(e => e.employeeId === searchForm.value.employeeId)?.employeeName || '全部员工')
    : '全部员工'
  const dateRange = searchForm.value.dateRange
  const startDate = dateRange ? new Date(dateRange[0]) : new Date()
  const monthLabel = startDate.getFullYear() + '年' + String(startDate.getMonth() + 1).padStart(2, '0') + '月'

  let html = `<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>考勤报表</title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Microsoft YaHei', 'SimSun', Arial, sans-serif; padding: 0; color: #333; }
@page { margin: 10mm; }
.header-row { display: flex; justify-content: space-between; align-items: flex-start; }
.header-title { font-size: 28px; font-weight: bold; line-height: 2; }
.header-right { text-align: right; font-size: 14px; line-height: 1.8; margin-top:10px;}
table { width: 100%; border-collapse: collapse; font-size: 32px; }
th, td { border: 1px solid #999; padding: 4px 6px; }
th { background-color: #e8e8e8; font-weight: bold; text-align: center; }
td { text-align: center; white-space: nowrap; }
td.left { text-align: left; white-space: normal; word-break: break-all; }
.col-day { width: 36px; }
.col-week { width: 70px; }
.col-time { width: 58px; }
.col-dur { width: 52px; }
.col-appr { width: 65px; }
.col-fee { width: 40px; }
.bottom-row { display: flex; justify-content: space-between; align-items: center; margin-top: 12px; font-size: 13px; }
.sign-line { display: flex; gap: 40px; white-space: nowrap; }
@media print {
  body { margin: 0; }
  .header-title { font-size: 28pt; }
  table { font-size: 12pt; margin-top: -8px; }
  th, td { padding: 2pt 4pt; }
}
</style>
</head>
<body>
<div class="header-row">
  <div class="header-title">考勤</div>
  <div class="header-right">
    <div>当前员工:${empName}</div>
    <div>考勤年月:${monthLabel}</div>
  </div>
</div>
<table>
<colgroup>
  <col class="col-day">
  <col class="col-week">
  <col class="col-time">
  <col class="col-dur">
  <col class="col-appr">
  <col class="col-time">
  <col class="col-dur">
  <col class="col-appr">
  <col>
  <col class="col-fee">
</colgroup>
<thead><tr>
<th>日期</th>
<th>周</th>
<th>上班时间</th>
<th>上班时长</th>
<th>上班审批</th>
<th>下班时间</th>
<th>下班时长</th>
<th>下班审批</th>
<th>事由</th>
<th>费用</th>
</tr></thead>
<tbody>`

  for (const row of reportData.value) {
    const day = row.sDate ? new Date(row.sDate).getDate().toString() : ''
    const week = row.description || getDayOfWeek(row.sDate)
    const bTime = row.bAttTime?.slice(0, 5) ?? ''
    const bOffset = row.bOffset ?? ''
    const eTime = row.eAttTime?.slice(0, 5) ?? ''
    const eOffset = row.eOffset ?? ''
    const event = row.event || ''
    const fee = row.fee != null && row.fee !== 0 ? row.fee : ''

    html += `<tr>
<td>${day}</td>
<td>${week}</td>
<td>${bTime}</td>
<td>${bOffset}</td>
<td></td>
<td>${eTime}</td>
<td>${eOffset}</td>
<td></td>
<td class="left">${event}</td>
<td>${fee}</td>
</tr>`
  }

  const overtimeTotal = reportData.value.reduce((sum: number, row: AttendanceReportRow) => {
    const f = Number(row.fee ?? 0)
    return sum + (isNaN(f) ? 0 : (f > 0 ? f : 0))
  }, 0)

  html += `</tbody></table>
<div class="bottom-row">
  <div>加班:${overtimeTotal.toFixed(2)}元</div>
  <div class="sign-line">
    <span>主管审核:______________</span>
    <span>总经理审批:______________</span>
  </div>
</div>
<script>
  window.onload = function () {
    window.print();
    window.close();
  }
<\/script>
</body>
</html>`

  const printWindow = window.open('', '_blank')
  if (!printWindow) return
  printWindow.document.write(html)
  printWindow.document.close()
}
</script>

<style scoped>
.search-card {
  margin-bottom: 0;
}
.toolbar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}
.progress-area.no-display {
  display: none;
}
</style>