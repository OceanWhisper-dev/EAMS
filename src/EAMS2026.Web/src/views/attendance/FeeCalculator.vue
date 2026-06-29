<template>
  <div>
    <div class="page-header">
      <h3>费用计算规则</h3>
      <div class="header-actions">
        <el-button type="primary" @click="handleAdd">新增规则</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
      </div>
    </div>
    <el-card>
      <el-form :inline="true" style="margin-bottom:16px">
        <el-form-item label="日期类型">
          <el-select v-model="searchDayTypeId" placeholder="全部类型" clearable style="width:200px" @change="fetchData">
            <el-option v-for="d in dayTypes" :key="d.id" :label="d.dayTypeCaption || d.dayTypeName" :value="d.id" />
          </el-select>
        </el-form-item>
      </el-form>
      <el-table :data="list" border v-loading="loading">
        <el-table-column label="日期类型" width="120">
          <template #default="{ row }">{{ getDayTypeName(row.dayTypeId) }}</template>
        </el-table-column>
        <el-table-column label="时长范围A" width="130" align="center">
          <template #default="{ row }">{{ row.rangeA }} 分钟</template>
        </el-table-column>
        <el-table-column label="时长范围B" width="130" align="center">
          <template #default="{ row }">{{ row.rangeB }} 分钟</template>
        </el-table-column>
        <el-table-column label="金额" width="120" align="center">
          <template #default="{ row }">{{ row.rangePrice }} 元</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑规则' : '新增规则'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="110px">
        <el-form-item label="日期类型" prop="dayTypeId">
          <el-select v-model="form.dayTypeId" placeholder="请选择" style="width:100%">
            <el-option v-for="d in dayTypes" :key="d.id" :label="d.dayTypeCaption || d.dayTypeName" :value="d.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="时长范围A(分钟)" prop="rangeA">
          <el-input-number v-model="form.rangeA" :min="-1440" :max="1440" style="width:100%" />
        </el-form-item>
        <el-form-item label="时长范围B(分钟)" prop="rangeB">
          <el-input-number v-model="form.rangeB" :min="-1440" :max="1440" style="width:100%" />
        </el-form-item>
        <el-form-item label="金额(元)" prop="rangePrice">
          <el-input-number v-model="form.rangePrice" :min="0" :precision="2" :step="1" style="width:100%" />
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
const dayTypes = ref<any[]>([])
const searchDayTypeId = ref<number | undefined>()
const formRef = ref<FormInstance>()
const initForm = () => ({ id: 0, dayTypeId: undefined, rangeA: 0, rangeB: 0, rangePrice: 0 })
const form = ref<any>(initForm())
const rules: FormRules = {
  dayTypeId: [{ required: true, message: '请选择日期类型' }],
  rangeA: [{ required: true, message: '请输入时长范围A' }],
  rangeB: [{ required: true, message: '请输入时长范围B' }],
  rangePrice: [{ required: true, message: '请输入金额' }]
}

async function loadDayTypes() {
  try {
    const res: any = await attendanceApi.getDayTypes()
    dayTypes.value = res.data || []
  } catch {
    ElMessage.error('加载日期类型失败')
  }
}

function getDayTypeName(dayTypeId: number): string {
  const d = dayTypes.value.find(x => x.id === dayTypeId)
  return d ? (d.dayTypeCaption || d.dayTypeName) : `类型${dayTypeId}`
}

async function fetchData() {
  loading.value = true
  try {
    const res: any = await attendanceApi.getFeeCalculators(searchDayTypeId.value)
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
  form.value = { ...row, dayTypeId: row.dayTypeId }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(
      `确定删除 类型"${getDayTypeName(row.dayTypeId)}" 范围[${row.rangeA}~${row.rangeB}] 的规则吗？`,
      '提示'
    )
    await attendanceApi.deleteFeeCalculator(row.id)
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
      await attendanceApi.updateFeeCalculator(form.value.id, form.value)
    } else {
      await attendanceApi.createFeeCalculator(form.value)
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    await fetchData()
  } finally { submitting.value = false }
}

onMounted(async () => {
  await loadDayTypes()
  await fetchData()
})

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('fee-calculator')
    downloadBlob(res as unknown as Blob, `费用计算规则_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
      const res: any = await importExportApi.importData('fee-calculator', file, overwrite)
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
  openPrintWindow('费用计算规则列表', [
    { label: '日期类型ID', value: (r: any) => r.dayTypeId, align: 'center' },
    { label: '范围A(分钟)', value: (r: any) => r.rangeA, align: 'center' },
    { label: '范围B(分钟)', value: (r: any) => r.rangeB, align: 'center' },
    { label: '金额(元)', value: (r: any) => r.rangePrice, align: 'center' }
  ], list.value)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>