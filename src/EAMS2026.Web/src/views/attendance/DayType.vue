<template>
  <div>
    <div class="page-header">
      <h3>考勤类型</h3>
      <div class="header-actions">
        <el-button type="primary" @click="handleAdd">新增类型</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="list" border v-loading="loading">
        <el-table-column prop="dayTypeName" label="类型名称" min-width="150" />
        <el-table-column prop="dayTypeCaption" label="说明" min-width="200" />
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑类型' : '新增类型'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="类型名称" prop="dayTypeName">
          <el-input v-model="form.dayTypeName" />
        </el-form-item>
        <el-form-item label="说明" prop="dayTypeCaption">
          <el-input v-model="form.dayTypeCaption" />
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
const formRef = ref<FormInstance>()
const initForm = () => ({ id: 0, dayTypeName: '', dayTypeCaption: '' })
const form = ref<any>(initForm())
const rules: FormRules = {
  dayTypeName: [{ required: true, message: '请输入类型名称' }]
}

onMounted(() => { fetchData() })

async function fetchData() {
  loading.value = true
  try {
    const res: any = await attendanceApi.getDayTypes()
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
  form.value = { ...row }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除类型 "${row.dayTypeName}" 吗？`, '提示')
    await attendanceApi.deleteDayType(row.id)
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
      await attendanceApi.updateDayType(form.value.id, form.value)
    } else {
      await attendanceApi.createDayType(form.value)
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    await fetchData()
  } finally { submitting.value = false }
}

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('day-type')
    downloadBlob(res as unknown as Blob, `考勤类型_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
      const res: any = await importExportApi.importData('day-type', file, overwrite)
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
  openPrintWindow('考勤类型列表', [
    { label: '类型名称', value: (r: any) => r.dayTypeName },
    { label: '说明', value: (r: any) => r.dayTypeCaption || '' }
  ], list.value)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>