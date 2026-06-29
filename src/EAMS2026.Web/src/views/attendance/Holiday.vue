<template>
  <div>
    <div class="page-header">
      <h3>节假日管理</h3>
      <div class="header-actions">
        <el-button type="primary" @click="handleAdd">新增假日</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
      </div>
    </div>
    <el-card>
      <el-form :inline="true" style="margin-bottom:16px">
        <el-form-item label="年份">
          <el-input-number v-model="searchYear" :min="2020" :max="2099" @change="fetchData" />
        </el-form-item>
      </el-form>
      <el-table :data="list" border v-loading="loading">
        <el-table-column prop="iYear" label="年份" width="60" align="center" />
        <el-table-column label="日期" width="120" align="center">
          <template #default="{ row }">{{ row.sDate?.slice(0, 10) }}</template>
        </el-table-column>
        <el-table-column prop="sName" label="假日名称" min-width="150" />
        <el-table-column label="开始时间" width="100" align="center">
          <template #default="{ row }">{{ row.bTime || '--' }}</template>
        </el-table-column>
        <el-table-column label="结束时间" width="100" align="center">
          <template #default="{ row }">{{ row.eTime || '--' }}</template>
        </el-table-column>
        <el-table-column prop="sDescription" label="描述" min-width="200" />
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑假日' : '新增假日'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="年份" prop="iYear">
          <el-input-number v-model="form.iYear" :min="2020" :max="2099" />
        </el-form-item>
        <el-form-item label="日期" prop="sDate">
          <el-date-picker v-model="form.sDate" type="date" placeholder="选择日期" value-format="YYYY-MM-DD" style="width:100%" />
        </el-form-item>
        <el-form-item label="假日名称" prop="sName">
          <el-input v-model="form.sName" />
        </el-form-item>
        <el-form-item label="开始时间">
          <el-time-picker v-model="form.bTime" format="HH:mm" value-format="HH:mm:ss" />
        </el-form-item>
        <el-form-item label="结束时间">
          <el-time-picker v-model="form.eTime" format="HH:mm" value-format="HH:mm:ss" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.sDescription" type="textarea" />
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
import { ref, onMounted } from 'vue'
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
const searchYear = ref<number | undefined>(new Date().getFullYear())
const formRef = ref<FormInstance>()
const initForm = () => ({ id: 0, iYear: new Date().getFullYear(), sDate: '', sName: '', bTime: '', eTime: '', sDescription: '' })
const form = ref<any>(initForm())
const rules: FormRules = {
  iYear: [{ required: true, message: '请输入年份' }],
  sDate: [{ required: true, message: '请选择日期' }],
  sName: [{ required: true, message: '请输入假日名称' }]
}

onMounted(() => { fetchData() })

async function fetchData() {
  loading.value = true
  try {
    const res: any = await attendanceApi.getHolidays(searchYear.value)
    list.value = res.data || []
  } finally { loading.value = false }
}

function handleAdd() {
  isEdit.value = false
  form.value = initForm()
  dialogVisible.value = true
}

function handleEdit(row: any) {
  isEdit.value = true
  form.value = { ...row, sDate: row.sDate?.slice(0, 10), bTime: row.bTime || '', eTime: row.eTime || '' }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除 "${row.sName}" 吗？`, '提示')
    await attendanceApi.deleteHoliday(row.id)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* cancelled */ }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    // 将空字符串转为 null，避免后端 TimeSpan? 反序列化失败
    const payload = {
      ...form.value,
      bTime: form.value.bTime || null,
      eTime: form.value.eTime || null
    }
    if (isEdit.value) {
      await attendanceApi.updateHoliday(form.value.id, payload)
    } else {
      await attendanceApi.createHoliday(payload)
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    await fetchData()
  } finally { submitting.value = false }
}

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('holiday')
    downloadBlob(res as unknown as Blob, `节假日_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
      const res: any = await importExportApi.importData('holiday', file, overwrite)
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
  openPrintWindow('节假日列表', [
    { label: '年份', value: (r: any) => r.iYear, align: 'center' },
    { label: '日期', value: (r: any) => r.sDate?.slice(0, 10) || '' },
    { label: '假日名称', value: (r: any) => r.sName },
    { label: '开始时间', value: (r: any) => r.bTime || '--', align: 'center' },
    { label: '结束时间', value: (r: any) => r.eTime || '--', align: 'center' },
    { label: '描述', value: (r: any) => r.sDescription || '' }
  ], list.value)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>