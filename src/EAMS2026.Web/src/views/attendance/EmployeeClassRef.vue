<template>
  <div>
    <div class="page-header">
      <h3>员工关联班次</h3>
      <div class="header-actions">
        <el-button type="primary" @click="handleAdd">新增关联</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
      </div>
    </div>
    <el-card>
      <el-form :inline="true" style="margin-bottom:16px">
        <el-form-item label="班次">
          <el-select v-model="searchClassId" placeholder="全部班次" clearable style="width:180px" @change="fetchData">
            <el-option v-for="c in schemeClasses" :key="c.id" :label="c.className" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="员工名">
          <el-input v-model="searchEmployeeName" placeholder="搜索员工名" clearable style="width:180px" @clear="fetchData" @keyup.enter="fetchData" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchData">查询</el-button>
        </el-form-item>
      </el-form>
      <el-table :data="filteredList" border v-loading="loading">
        <el-table-column prop="employeeName" label="员工名" min-width="120" />
        <el-table-column prop="className" label="班次名" min-width="120" />
        <el-table-column label="生效日期" width="120" align="center">
          <template #default="{ row }">{{ formatDate(row.effDate) }}</template>
        </el-table-column>
        <el-table-column prop="periodNo" label="周期序号" width="100" align="center" />
        <el-table-column label="失效日期" width="120" align="center">
          <template #default="{ row }">{{ row.expDate ? formatDate(row.expDate) : '--' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑关联' : '新增关联'" width="520px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="110px">
        <el-form-item label="班次" prop="classId">
          <el-select v-model="form.classId" placeholder="请选择班次" style="width:100%">
            <el-option v-for="c in schemeClasses" :key="c.id" :label="c.className" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="员工ID(考勤机)" prop="employeeId">
          <el-input-number v-model="form.employeeId" :min="0" style="width:100%" />
        </el-form-item>
        <el-form-item label="生效日期" prop="effDate">
          <el-date-picker v-model="form.effDate" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" style="width:100%" />
        </el-form-item>
        <el-form-item label="周期序号" prop="periodNo">
          <el-input-number v-model="form.periodNo" :min="1" :max="99" style="width:100%" />
        </el-form-item>
        <el-form-item label="失效日期" prop="expDate">
          <el-date-picker v-model="form.expDate" type="date" value-format="YYYY-MM-DD" placeholder="永久有效" clearable style="width:100%" />
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
const schemeClasses = ref<any[]>([])
const searchClassId = ref<number | undefined>()
const searchEmployeeName = ref('')
const formRef = ref<FormInstance>()
const form = ref<any>({})
const rules: FormRules = {
  classId: [{ required: true, message: '请选择班次' }],
  employeeId: [{ required: true, message: '请输入员工ID' }],
  effDate: [{ required: true, message: '请选择生效日期' }]
}

const filteredList = computed(() => {
  const keyword = searchEmployeeName.value.trim().toLowerCase()
  if (!keyword) return list.value
  return list.value.filter(item =>
    (item.employeeName || '').toLowerCase().includes(keyword)
  )
})

function formatDate(dateStr: string): string {
  if (!dateStr) return '--'
  return dateStr.substring(0, 10)
}

function initForm() {
  return { id: 0, employeeId: undefined, classId: undefined, periodNo: 1, effDate: '', expDate: null }
}

onMounted(async () => {
  await loadSchemeClasses()
  await fetchData()
})

async function loadSchemeClasses() {
  try {
    const res: any = await attendanceApi.getSchemeClasses()
    schemeClasses.value = res.data || []
  } catch {
    ElMessage.error('加载班次列表失败')
  }
}

async function fetchData() {
  loading.value = true
  try {
    const res: any = await attendanceApi.getEmployeeRefClasses()
    let data = res.data || []
    if (searchClassId.value) {
      data = data.filter((item: any) => item.classId === searchClassId.value)
    }
    list.value = data
  } finally { loading.value = false }
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
    employeeId: row.employeeId,
    classId: row.classId,
    periodNo: row.periodNo ?? 1,
    effDate: row.effDate ? row.effDate.substring(0, 10) : '',
    expDate: row.expDate ? row.expDate.substring(0, 10) : null
  }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(
      `确定删除 员工"${row.employeeName || row.employeeId}" 关联班次"${row.className}" 的记录吗？`,
      '提示'
    )
    await attendanceApi.deleteEmployeeRefClass(row.id)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* cancelled */ }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (isEdit.value) {
      await attendanceApi.updateEmployeeRefClass(form.value.id, form.value)
    } else {
      await attendanceApi.addEmployeeRefClass(form.value)
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    await fetchData()
  } finally { submitting.value = false }
}

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('employee-ref-class')
    downloadBlob(res as unknown as Blob, `员工关联班次_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
      const res: any = await importExportApi.importData('employee-ref-class', file, overwrite)
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
  openPrintWindow('员工关联班次列表', [
    { label: '员工名', value: (r: any) => r.employeeName || '' },
    { label: '班次名', value: (r: any) => r.className || '' },
    { label: '周期序号', value: (r: any) => r.periodNo, align: 'center' },
    { label: '生效日期', value: (r: any) => r.effDate ? r.effDate.substring(0, 10) : '' },
    { label: '失效日期', value: (r: any) => r.expDate ? r.expDate.substring(0, 10) : '--' }
  ], list.value)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>