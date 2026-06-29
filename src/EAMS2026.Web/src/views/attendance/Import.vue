<template>
  <div>
    <div class="page-header">
      <h3>数据导入</h3>
      <div>
        <el-button @click="handlePrint">打印</el-button>
        <el-button @click="handleExport">导出</el-button>
      </div>
    </div>

    <!-- 操作按钮栏 - 去掉标签文本，按钮放大 -->
    <el-card style="margin-top:10px">
      <div class="toolbar" style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">
        <el-button v-permission="'attendance:sync-employees'" type="warning" @click="handleSyncHwatt">
          同步HWATT员工
        </el-button>
        <el-button v-permission="'attendance:sync-employees'" type="warning" @click="handleDingtalkSyncEmployees">
          同步钉钉员工
        </el-button>
        <el-button v-permission="'attendance:sync-card-records'" type="warning" plain @click="handleSyncHwattCardRecords">
          同步HWATT打卡记录
        </el-button>
        <el-button v-permission="'attendance:sync-card-records'" type="warning" plain @click="handleDingtalkSyncCardRecords">
          同步钉钉打卡记录
        </el-button>
        <el-button v-permission="'attendance:import-all'" type="success" @click="handleImportAll">
          导入所有员工考勤
        </el-button>
        <el-button v-permission="'attendance:import'" type="primary" @click="handleImport">
          导入考勤
        </el-button>
        <div class="progress-area" :class="{ 'no-display': !showProgress }" style="margin-left:auto;">
          <el-progress :percentage="progressValue" :show-text="true" :precision="2" style="width:200px;margin-left:16px;" />
        </div>
      </div>
    </el-card>

    <el-card style="margin-top:10px">
      <!-- 过滤条件 - 去掉卡片容器，直接放在标签切换之前 -->
      <div style="margin-bottom:10px;padding-bottom:10px;border-bottom:1px solid #ebeef5;">
        <el-form :inline="true">
          <el-form-item label="日期范围">
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width:260px"
            />
          </el-form-item>
          <el-form-item label="员工">
            <el-select
              v-model="selectedEmployeeId"
              placeholder="全部员工"
              clearable
              filterable
              style="width:200px"
              @change="handleEmployeeChange"
            >
              <el-option v-for="e in employeeList" :key="e.employeeId" :label="e.employeeName" :value="e.employeeId" />
            </el-select>
          </el-form-item>
        </el-form>
      </div>
      
      <!-- 手动标签切换 -->
      <div style="margin-bottom:10px;">
        <el-button 
          :type="activeTab === 'employees' ? 'primary' : 'default'" 
          @click="activeTab = 'employees'; fetchEmployees()"
        >已导入员工信息</el-button>
        <el-button 
          :type="activeTab === 'cardRecords' ? 'primary' : 'default'" 
          @click="activeTab = 'cardRecords'; fetchCardRecords()"
        >已导入的原始考勤信息</el-button>
      </div>
      
      <!-- 员工信息内容 -->
      <div v-if="activeTab === 'employees'">
          <el-tag size="small" style="margin-bottom:8px">共 {{ employeeList.length }} 名员工</el-tag>
          <el-table :data="employeeList" border v-loading="loadingEmployees" @sort-change="() => {}">
            <el-table-column prop="employeeId" label="考勤ID" width="90" align="center" sortable />
            <el-table-column prop="employeeName" label="员工姓名" min-width="150" sortable />
            <el-table-column prop="source" label="数据来源" width="110" align="center" sortable>
              <template #default="{ row }">
                <el-tag :type="row.source === 'dingtalk' ? 'warning' : 'success'" size="small">
                  {{ row.source === 'dingtalk' ? '钉钉' : 'HWATT' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="hwattEmployeeId" label="HWATT员工ID" width="130" align="center" sortable>
              <template #default="{ row }">
                {{ row.hwattEmployeeId ?? '--' }}
              </template>
            </el-table-column>
            <el-table-column prop="createdAt" label="导入时间" width="170" align="center" sortable>
              <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
            </el-table-column>
            <el-table-column label="系统员工映射" min-width="150" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.systemEmployeeName" type="info" size="small">{{ row.systemEmployeeName }}</el-tag>
                <span v-else style="color:#999">未映射</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="200" fixed="right">
              <template #default="{ row }">
                <el-button size="small" @click="handleMapping(row)">映射</el-button>
                <el-button size="small" @click="handleEdit(row)">编辑</el-button>
                <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-empty v-if="!employeeList.length && !loadingEmployees" description="暂无考勤员工，请先点击同步按钮进行导入" />
      </div>
      
      <!-- 打卡记录内容 -->
      <div v-else>
          <div class="card-records-toolbar">
            <el-tag size="small">共 {{ totalCardRecords }} 条打卡记录</el-tag>
          </div>
          <el-table :data="cardRecords" border v-loading="loadingCardRecords" @sort-change="() => {}">
            <el-table-column prop="employeeName" label="员工姓名" width="120" sortable>
              <template #default="{ row }">{{ row.employeeName || '--' }}</template>
            </el-table-column>
            <el-table-column prop="cardTime" label="打卡时间" width="170" align="center" sortable>
              <template #default="{ row }">{{ formatDateTime(row.cardTime) }}</template>
            </el-table-column>
            <el-table-column prop="source" label="来源" width="90" align="center" sortable>
              <template #default="{ row }">
                <el-tag :type="row.source === 'dingtalk' ? 'warning' : 'success'" size="small">
                  {{ row.source === 'dingtalk' ? '钉钉' : 'HWATT' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="cardType" label="打卡类型" width="130" align="center" sortable>
              <template #default="{ row }">{{ row.cardType || '--' }}</template>
            </el-table-column>
            <el-table-column prop="deviceInfo" label="设备/基准时间" min-width="170">
              <template #default="{ row }">{{ row.deviceInfo || '--' }}</template>
            </el-table-column>
            <el-table-column prop="createdAt" label="导入时间" width="170" align="center" sortable>
              <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
            </el-table-column>
          </el-table>
          <div v-if="totalCardRecords > 0" class="pagination-wrapper">
            <el-pagination
              v-model:current-page="cardPage"
              :page-size="cardPageSize"
              :total="totalCardRecords"
              layout="total, prev, pager, next"
              @current-change="fetchCardRecords"
            />
          </div>
          <el-empty v-if="!cardRecords.length && !loadingCardRecords" description="暂无原始打卡记录，请先点击同步按钮进行导入" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" title="编辑考勤员工" width="420px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="考勤ID">
          <el-input v-model="form.employeeId" disabled />
        </el-form-item>
        <el-form-item label="数据来源">
          <el-tag :type="form.source === 'dingtalk' ? 'warning' : 'success'" size="small">
            {{ form.source === 'dingtalk' ? '钉钉' : 'HWATT' }}
          </el-tag>
        </el-form-item>
        <el-form-item label="员工姓名" prop="employeeName">
          <el-input v-model="form.employeeName" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="mappingDialogVisible" title="员工映射" width="480px">
      <el-form ref="mappingFormRef" :model="mappingForm" label-width="120px">
        <el-form-item label="考勤员工">
          <div>
            <el-tag :type="mappingForm.source === 'dingtalk' ? 'warning' : 'success'" size="small">
              {{ mappingForm.source === 'dingtalk' ? '钉钉' : 'HWATT' }}
            </el-tag>
            <span style="margin-left:8px">{{ mappingForm.employeeName }}</span>
          </div>
        </el-form-item>
        <el-form-item label="当前映射">
          <el-tag v-if="mappingForm.systemEmployeeName" type="info" size="small">{{ mappingForm.systemEmployeeName }}</el-tag>
          <span v-else style="color:#999">未映射</span>
        </el-form-item>
        <el-form-item label="映射系统员工">
          <el-select
            v-model="mappingForm.systemEmployeeId"
            placeholder="请选择系统员工"
            clearable
            filterable
            style="width:100%"
          >
            <el-option
              v-for="e in systemEmployeeList"
              :key="e.id"
              :label="`${e.name} (${e.employeeNo})`"
              :value="e.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="mappingDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="mappingSubmitting" @click="handleMappingSubmit">保存映射</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { attendanceApi } from '@/api/attendance'
import { employeeApi } from '@/api/system'
import { formatDateTime, formatDate } from '@/utils/date'
import { openPrintWindow } from '@/utils/print'
import { connectImportTask, disconnectImportTask } from '@/utils/signalr'

const loadingEmployees = ref(false)
const loadingCardRecords = ref(false)
const showProgress = ref(false)
const progressValue = ref(0)
const employeeList = ref<any[]>([])
const cardRecords = ref<any[]>([])
const totalCardRecords = ref(0)
const cardPage = ref(1)
const cardPageSize = ref(20)
const dialogVisible = ref(false)
const submitting = ref(false)
const formRef = ref()
const form = ref<any>({})
const rules = {
  employeeName: [{ required: true, message: '请输入员工姓名', trigger: 'blur' }]
}
const dateRange = ref<[string, string] | null>(null)
const selectedEmployeeId = ref<number | null>(null)
const activeTab = ref('employees')

// 映射对话框
const mappingDialogVisible = ref(false)
const mappingSubmitting = ref(false)
const mappingFormRef = ref()
const mappingForm = ref<any>({})
const systemEmployeeList = ref<any[]>([])

onMounted(() => {
  console.log('AttendanceEmployeeList: onMounted')
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
  dateRange.value = [
    formatDate(attDateStart),
    formatDate(attDateEnd)
  ]
  fetchEmployees()
})

watch(activeTab, (newVal) => {
  console.log('watch activeTab:', newVal, 'cardRecords.length:', cardRecords.value.length)
  if (newVal === 'cardRecords') {
    fetchCardRecords()
  }
})

function handleEmployeeChange() {
  if (activeTab.value === 'cardRecords') {
    fetchCardRecords()
  }
}

async function fetchEmployees() {
  loadingEmployees.value = true
  try {
    const res: any = await attendanceApi.getEmployees()
    let list = res.data || []
    if (selectedEmployeeId.value) {
      list = list.filter((e: any) => e.employeeId === selectedEmployeeId.value)
    }
    employeeList.value = list
  } finally {
    loadingEmployees.value = false
  }
}

async function fetchCardRecords() {
  console.log('fetchCardRecords called, loadingCardRecords before:', loadingCardRecords.value)
  loadingCardRecords.value = true
  console.log('loadingCardRecords set to true')
  // 将选中的 employeeId 转为员工姓名用于查询
  let employeeName: string | undefined
  if (selectedEmployeeId.value) {
    const match = employeeList.value.find((e: any) => e.employeeId === selectedEmployeeId.value)
    employeeName = match?.employeeName || undefined
  }
  try {
    const res: any = await attendanceApi.getCardRecords({
      employeeName,
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1],
      page: cardPage.value,
      pageSize: cardPageSize.value
    })
    const data = res.data || {}
    cardRecords.value = data.items || []
    totalCardRecords.value = data.totalCount || 0
    console.log('fetchCardRecords response items count:', cardRecords.value.length, 'totalCount:', totalCardRecords.value)
    await nextTick()
    console.log('after nextTick - cardRecords.length:', cardRecords.value.length)
    if (totalCardRecords.value === 0) {
      console.log('考勤打卡记录查询无数据，请求参数:', { employeeName, startDate: dateRange.value?.[0], endDate: dateRange.value?.[1] })
    }
  } catch (e: any) {
    console.error('获取打卡记录失败:', e)
    ElMessage.error('获取打卡记录失败: ' + (e.message || '未知错误'))
  } finally {
    loadingCardRecords.value = false
  }
}

/* ---- 员工映射 ---- */

async function loadSystemEmployees() {
  try {
    const res: any = await employeeApi.getAll()
    systemEmployeeList.value = (res.data || []).filter((e: any) => e.status)
  } catch {
    systemEmployeeList.value = []
  }
}

function handleMapping(row: any) {
  mappingForm.value = {
    id: row.id,
    employeeName: row.employeeName,
    employeeId: row.employeeId,
    source: row.source,
    systemEmployeeId: row.systemEmployeeId ?? null,
    systemEmployeeName: row.systemEmployeeName || null
  }
  loadSystemEmployees()
  mappingDialogVisible.value = true
}

async function handleMappingSubmit() {
  mappingSubmitting.value = true
  try {
    await attendanceApi.updateEmployeeMapping(mappingForm.value.id, mappingForm.value.systemEmployeeId)
    ElMessage.success('映射更新成功')
    mappingDialogVisible.value = false
    await fetchEmployees()
  } catch {
    ElMessage.error('映射更新失败')
  } finally {
    mappingSubmitting.value = false
  }
}

/* ---- 编辑 / 删除 ---- */

function handleEdit(row: any) {
  form.value = { ...row, systemEmployeeId: row.systemEmployeeId ?? null }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除考勤员工 "${row.employeeName}" 吗？`, '提示', { type: 'warning' })
    await attendanceApi.deleteEmployee(row.id)
    ElMessage.success('删除成功')
    await fetchEmployees()
  } catch { /* cancelled */ }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    await attendanceApi.updateEmployee(form.value.id, { employeeName: form.value.employeeName })
    ElMessage.success('更新成功')
    dialogVisible.value = false
    await fetchEmployees()
  } catch {
    ElMessage.error('更新失败')
  } finally {
    submitting.value = false
  }
}

/* ---- HWATT 同步 ---- */

async function handleSyncHwatt() {
  try {
    await ElMessageBox.confirm(
      '确认从HWATT同步所有员工信息？已存在的员工将更新姓名。',
      '确认操作',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch { return }
  showProgress.value = true
  try {
    const res: any = await attendanceApi.syncEmployees()
    const data = res.data || {}
    ElMessage.success(`同步完成：共 ${data.totalCount || 0} 名，新增 ${data.syncedCount || 0} 名，更新 ${data.updatedCount || 0} 名`)
    await fetchEmployees()
  } catch {
    ElMessage.error('同步员工失败')
  } finally {
    showProgress.value = false
  }
}

async function handleSyncHwattCardRecords() {
  if (!dateRange.value) {
    ElMessage.warning('请先选择日期范围')
    return
  }
  try {
    await ElMessageBox.confirm(
      `确认同步 ${dateRange.value[0]} ~ ${dateRange.value[1]} 的HWATT打卡记录？`,
      '确认操作',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch { return }
  showProgress.value = true
  try {
    const res: any = await attendanceApi.syncCardRecords({
      startDate: dateRange.value[0],
      endDate: dateRange.value[1]
    })
    const count = res.data?.syncedCount ?? 0
    ElMessage.success(`同步完成：共同步 ${count} 条打卡记录`)
    await fetchCardRecords()
  } catch {
    ElMessage.error('同步打卡记录失败')
  } finally {
    showProgress.value = false
  }
}

/* ---- 钉钉同步 ---- */

async function handleDingtalkSyncEmployees() {
  try {
    await ElMessageBox.confirm(
      '确认从钉钉同步所有考勤员工信息？',
      '确认操作',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch { return }
  showProgress.value = true
  try {
    const res: any = await attendanceApi.dingtalkSyncEmployees()
    const data = res.data || {}
    ElMessage.success(`同步完成：钉钉共 ${data.totalCount || 0} 名员工，新增 ${data.syncedCount || 0} 名`)
    await fetchEmployees()
  } catch {
    ElMessage.error('同步钉钉员工失败')
  } finally {
    showProgress.value = false
  }
}

async function handleDingtalkSyncCardRecords() {
  if (!dateRange.value) {
    ElMessage.warning('请先选择日期范围')
    return
  }
  try {
    await ElMessageBox.confirm(
      `确认同步 ${dateRange.value[0]} ~ ${dateRange.value[1]} 的钉钉打卡记录？`,
      '确认操作',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch { return }
  showProgress.value = true
  try {
    const res: any = await attendanceApi.dingtalkSyncCardRecords({
      startDate: dateRange.value[0],
      endDate: dateRange.value[1]
    })
    const count = res.data?.syncedCount ?? 0
    ElMessage.success(`同步完成：共同步 ${count} 条钉钉打卡记录`)
    await fetchCardRecords()
  } catch {
    ElMessage.error('同步钉钉打卡记录失败')
  } finally {
    showProgress.value = false
  }
}

/* ---- 考勤操作 ---- */

async function handleImportAll() {
  if (!dateRange.value) {
    ElMessage.warning('请先选择日期范围')
    return
  }
  try {
    await ElMessageBox.confirm(
      `确认导入 ${dateRange.value[0]} ~ ${dateRange.value[1]} 所有员工考勤数据？`,
      '确认操作',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )
  } catch { return }

  showProgress.value = true
  progressValue.value = 0

  try {
    const res: any = await attendanceApi.importAllStart({
      doDate: dateRange.value[0],
      endDate: dateRange.value[1]
    })
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
  } catch (err: any) {
    if (!showProgress.value) return
    ElMessage.error(err.message || '导入失败')
    showProgress.value = false
    progressValue.value = 0
  }
}

async function handleImport() {
  if (!dateRange.value) {
    ElMessage.warning('请先选择日期范围')
    return
  }
  if (!selectedEmployeeId.value) {
    ElMessage.warning('请先选择员工')
    return
  }
  showProgress.value = true
  try {
    const res: any = await attendanceApi.importDevice({
      employeeId: selectedEmployeeId.value,
      beginDate: dateRange.value[0],
      endDate: dateRange.value[1]
    })
    ElMessage.success(`导入完成，共处理 ${res.data?.count || 0} 天`)
  } catch {
    ElMessage.error('导入失败')
  } finally {
    showProgress.value = false
  }
}

/* ---- 打印 / 导出 ---- */

function handlePrint() {
  if (activeTab.value === 'employees') {
    if (!employeeList.value.length) {
      ElMessage.warning('暂无数据可打印')
      return
    }
    openPrintWindow('考勤员工列表', [
      { label: '考勤ID', value: (r: any) => r.employeeId, align: 'center' },
      { label: '员工姓名', value: (r: any) => r.employeeName },
      { label: '数据来源', value: (r: any) => r.source === 'dingtalk' ? '钉钉' : 'HWATT', align: 'center' },
      { label: 'HWATT员工ID', value: (r: any) => r.hwattEmployeeId ?? '', align: 'center' },
      { label: '系统员工映射', value: (r: any) => r.systemEmployeeName ?? '未映射', align: 'center' },
      { label: '导入时间', value: (r: any) => formatDateTime(r.createdAt) ?? '', align: 'center' }
    ], employeeList.value)
  } else {
    if (!cardRecords.value.length) {
      ElMessage.warning('暂无数据可打印')
      return
    }
    openPrintWindow('原始打卡记录', [
      { label: '员工姓名', value: (r: any) => r.employeeName, align: 'center' },
      { label: '打卡时间', value: (r: any) => formatDateTime(r.cardTime) ?? '' },
      { label: '来源', value: (r: any) => r.source === 'dingtalk' ? '钉钉' : 'HWATT', align: 'center' },
      { label: '设备/基准时间', value: (r: any) => r.deviceInfo ?? '' }
    ], cardRecords.value)
  }
}

function handleExport() {
  ElMessage.info('导出功能开发中')
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.search-card { margin-bottom: 0; }
.toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.progress-area.no-display { display: none; }
.card-records-toolbar { margin-bottom: 8px; }
.pagination-wrapper { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>