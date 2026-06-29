<template>
  <div>
    <div class="page-header">
      <h3>计划标准时间</h3>
      <div class="header-actions">
        <el-button type="primary" @click="handleAdd">新增计划</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
      </div>
    </div>
    <el-card>
      <el-form :inline="true" style="margin-bottom:16px">
        <el-form-item label="计划名称">
          <el-input v-model="searchName" placeholder="搜索计划名称" clearable style="width:200px" @clear="fetchData" @keyup.enter="fetchData" />
        </el-form-item>
        <el-form-item label="日期类型">
          <el-select v-model="searchDayTypeId" placeholder="全部类型" clearable style="width:180px" @change="fetchData">
            <el-option v-for="d in dayTypes" :key="d.id" :label="d.dayTypeCaption || d.dayTypeName" :value="d.id" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchData">查询</el-button>
        </el-form-item>
      </el-form>
      <el-table :data="filteredList" border v-loading="loading">
        <el-table-column prop="planName" label="计划名称" min-width="150" />
        <el-table-column label="日期类型" width="120">
          <template #default="{ row }">{{ getDayTypeName(row.dayTypeId) }}</template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="150" />
        <el-table-column label="上班时间" width="120" align="center">
          <template #default="{ row }">{{ row.bTime ? row.bTime.slice(0, 5) : '--' }}</template>
        </el-table-column>
        <el-table-column label="下班时间" width="120" align="center">
          <template #default="{ row }">{{ row.eTime ? row.eTime.slice(0, 5) : '--' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑计划' : '新增计划'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="计划名称" prop="planName">
          <el-input v-model="form.planName" />
        </el-form-item>
        <el-form-item label="日期类型" prop="dayTypeId">
          <el-select v-model="form.dayTypeId" placeholder="请选择类型" style="width:100%">
            <el-option v-for="d in dayTypes" :key="d.id" :label="d.dayTypeCaption || d.dayTypeName" :value="d.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" />
        </el-form-item>
        <el-form-item label="上班时间" prop="bTime">
          <el-time-picker v-model="form.bTime" format="HH:mm" value-format="HH:mm:ss" style="width:100%" />
        </el-form-item>
        <el-form-item label="下班时间" prop="eTime">
          <el-time-picker v-model="form.eTime" format="HH:mm" value-format="HH:mm:ss" style="width:100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { attendanceApi } from '@/api/attendance'
import { downloadBlob, importExportApi } from '@/api/system'
import { openPrintWindow } from '@/utils/print'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const list = ref<any[]>([])
const dayTypes = ref<any[]>([])
const searchName = ref('')
const searchDayTypeId = ref<number | undefined>()
const formRef = ref<FormInstance>()
const initForm = () => ({ id: 0, planName: '', dayTypeId: null, description: '', bTime: '', eTime: '' })
const form = ref<any>(initForm())
const rules: FormRules = {
  planName: [{ required: true, message: '请输入计划名称' }],
  dayTypeId: [{ required: true, message: '请选择日期类型' }]
}

const filteredList = computed(() => {
  let data = list.value
  if (searchName.value) {
    const q = searchName.value.toLowerCase()
    data = data.filter((x: any) => x.planName?.toLowerCase().includes(q))
  }
  if (searchDayTypeId.value) {
    data = data.filter((x: any) => x.dayTypeId === searchDayTypeId.value)
  }
  return data
})

onMounted(async () => {
  loading.value = true
  try {
    const [res, dayRes]: any = await Promise.all([attendanceApi.getPlanTimes(), attendanceApi.getDayTypes()])
    list.value = res.data || []
    dayTypes.value = dayRes.data || []
  } finally { loading.value = false }
})

function getDayTypeName(dayTypeId: number): string {
  const d = dayTypes.value.find(x => x.id === dayTypeId)
  return d ? (d.dayTypeCaption || d.dayTypeName) : `类型${dayTypeId}`
}

function handleAdd() {
  isEdit.value = false
  form.value = initForm()
  dialogVisible.value = true
}

function handleEdit(row: any) {
  isEdit.value = true
  form.value = {
    id: row.id,
    planName: row.planName,
    dayTypeId: row.dayTypeId,
    description: row.description,
    bTime: row.bTime,
    eTime: row.eTime
  }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除 "${row.planName}" 吗？`, '提示')
    await attendanceApi.deletePlanTime(row.id)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* cancelled */ }
}

async function fetchData() {
  loading.value = true
  try {
    const res: any = await attendanceApi.getPlanTimes()
    list.value = res.data || []
  } finally { loading.value = false }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (isEdit.value) {
      await attendanceApi.updatePlanTime(form.value.id, form.value)
    } else {
      await attendanceApi.createPlanTime(form.value)
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    await fetchData()
  } finally { submitting.value = false }
}

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('plan-time')
    downloadBlob(res as unknown as Blob, `计划标准时间_${new Date().toISOString().slice(0, 10)}.xlsx`)
  } catch { ElMessage.error('导出失败') }
}

async function handleImport() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = '.xlsx,.xls'
  input.onchange = async (e: any) => {
    const file = e.target.files[0]
    if (!file) return
    try {
      const mode = await ElMessageBox.confirm(
        '检测到导入文件，请选择导入模式：',
        '导入模式',
        {
          confirmButtonText: '覆盖更新',
          cancelButtonText: '跳过重复',
          distinguishCancelAndClose: true
        }
      ).then(() => 'overwrite').catch((action: string) => {
        if (action === 'cancel') return 'skip'
        throw new Error('cancelled')
      })

      const overwrite = mode === 'overwrite'
      const res: any = await importExportApi.importData('plan-time', file, overwrite)
      const data = res.data || {}
      const { successCount = 0, failCount = 0, errors = [] } = data
      if (failCount > 0 && errors.length > 0) {
        ElMessageBox.alert(errors.join('\n'), `导入完成：成功${successCount}条，失败${failCount}条`, { type: 'warning' })
      } else {
        ElMessage.success(res.message || `导入完成：成功${successCount}条`)
      }
      await fetchData()
    } catch (err: any) {
      if (err !== 'cancelled') ElMessage.error(err.message || '导入失败')
    }
  }
  input.click()
}

async function handlePrint() {
  if (!list.value.length) {
    ElMessage.warning('暂无数据可打印')
    return
  }
  openPrintWindow('计划标准时间列表', [
    { label: '计划名称', value: (r: any) => r.planName },
    { label: '描述', value: (r: any) => r.description || '' },
    { label: '上班时间', value: (r: any) => r.bTime ? r.bTime.slice(0, 5) : '--', align: 'center' },
    { label: '下班时间', value: (r: any) => r.eTime ? r.eTime.slice(0, 5) : '--', align: 'center' }
  ], list.value)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>