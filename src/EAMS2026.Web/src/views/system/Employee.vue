<template>
  <div>
    <div class="page-header">
      <h3>员工管理</h3>
      <div class="header-actions">
        <el-switch v-model="showDeleted" active-text="显示已删除" style="margin-right:12px" @change="fetchData" />
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
        <el-button type="primary" @click="handleAdd">新增员工</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="list" border v-loading="loading" :row-class-name="tableRowClassName">
        <el-table-column prop="employeeNo" label="工号" width="100" />
        <el-table-column prop="name" label="姓名" width="120">
          <template #default="{ row }">
            <span :class="row.isDeleted ? 'text-deleted' : ''">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="gender" label="性别" width="60" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column label="所属部门" width="150">
          <template #default="{ row }">{{ row.departmentName }}</template>
        </el-table-column>
        <el-table-column prop="position" label="岗位" width="120" />
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isDeleted" type="danger">已删除</el-tag>
            <el-tag v-else :type="row.status ? 'success' : 'danger'">{{ row.status ? '在职' : '离职' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="离职日期" width="110" align="center">
          <template #default="{ row }">{{ row.resignationDate || '--' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <template v-if="row.isDeleted">
              <el-button size="small" type="danger" @click="handleHardDelete(row)">永久删除</el-button>
            </template>
            <template v-else>
              <el-button size="small" @click="handleEdit(row)">编辑</el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑员工' : '新增员工'" width="600px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="工号" prop="employeeNo">
          <el-input v-model="form.employeeNo" />
        </el-form-item>
        <el-form-item label="姓名" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="性别" prop="gender">
          <el-select v-model="form.gender">
            <el-option label="男" value="男" />
            <el-option label="女" value="女" />
          </el-select>
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="所属部门" prop="departmentId">
          <el-tree-select v-model="form.departmentId" :data="deptTree" :props="{ label: 'name', value: 'id' }" placeholder="请选择部门" check-strictly />
        </el-form-item>
        <el-form-item label="岗位" prop="position">
          <el-input v-model="form.position" />
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-switch v-model="form.status" />
        </el-form-item>
        <el-form-item label="离职日期" prop="resignationDate">
          <el-date-picker v-model="form.resignationDate" type="date" placeholder="选择离职日期" value-format="YYYY-MM-DD" style="width:100%" />
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
import { employeeApi, departmentApi, importExportApi, printApi, downloadBlob } from '@/api/system'
import { openPrintWindow } from '@/utils/print'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const showDeleted = ref(false)
const list = ref<any[]>([])
const deptTree = ref<any[]>([])
const formRef = ref<FormInstance>()

const initForm = () => ({ id: 0, employeeNo: '', name: '', gender: '男', phone: '', email: '', departmentId: null, position: '', status: true, resignationDate: '' })
const form = ref<any>(initForm())
const rules: FormRules = {
  employeeNo: [{ required: true, message: '请输入工号' }],
  name: [{ required: true, message: '请输入姓名' }]
}

async function fetchData() {
  loading.value = true
  try {
    const [empRes, deptRes]: any = await Promise.all([employeeApi.getAll(), departmentApi.getTree()])
    let activeEmployees = empRes.data || []
    if (showDeleted.value) {
      const deletedRes: any = await employeeApi.getDeleted()
      const deleted = (deletedRes.data || []).map((r: any) => ({ ...r, isDeleted: true }))
      list.value = [...activeEmployees, ...deleted]
    } else {
      list.value = activeEmployees
    }
    deptTree.value = deptRes.data || []
  } finally { loading.value = false }
}

function tableRowClassName({ row }: { row: any }) {
  return row.isDeleted ? 'deleted-row' : ''
}

function handleAdd() {
  isEdit.value = false
  form.value = initForm()
  dialogVisible.value = true
}

function handleEdit(row: any) {
  isEdit.value = true
  form.value = { ...row, status: !!row.status }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除员工 "${row.name}" 吗？`, '提示')
    await employeeApi.delete(row.id)
    ElMessage.success('删除成功')
    fetchData()
  } catch { /* cancelled */ }
}

async function handleHardDelete(row: any) {
  try {
    await ElMessageBox.confirm(
      `确定永久删除员工 "${row.name}" 吗？\n此操作不可恢复，将从数据库中彻底移除！`,
      '警告',
      { confirmButtonText: '确定永久删除', cancelButtonText: '取消', type: 'warning' }
    )
    await employeeApi.hardDelete(row.id)
    ElMessage.success('已永久删除')
    fetchData()
  } catch { /* cancelled */ }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (isEdit.value) {
      await employeeApi.update(form.value)
      ElMessage.success('更新成功')
    } else {
      await employeeApi.create(form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    fetchData()
  } finally { submitting.value = false }
}

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('employee')
    downloadBlob(res as unknown as Blob, `员工数据_${new Date().toISOString().slice(0, 10)}.xlsx`)
    ElMessage.success('导出成功')
  } catch { ElMessage.error('导出失败') }
}

async function handlePrint() {
  if (!list.value.length) {
    ElMessage.warning('暂无数据可打印')
    return
  }
  openPrintWindow('员工列表', [
    { label: '工号', value: (r: any) => r.employeeNo },
    { label: '姓名', value: (r: any) => r.name },
    { label: '性别', value: (r: any) => r.gender, align: 'center' },
    { label: '手机号', value: (r: any) => r.phone },
    { label: '邮箱', value: (r: any) => r.email },
    { label: '所属部门', value: (r: any) => r.departmentName ?? '' },
    { label: '岗位', value: (r: any) => r.position },
    { label: '状态', value: (r: any) => r.status ? '在职' : '离职', align: 'center' }
  ], list.value)
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
          confirmButtonText: '覆盖更新（推荐）',
          cancelButtonText: '跳过重复',
          distinguishCancelAndClose: true
        }
      ).then(() => 'overwrite').catch((action) => {
        if (action === 'cancel') return 'skip'
        throw new Error('cancelled')
      })

      const overwrite = mode === 'overwrite'
      const res: any = await importExportApi.importData('employee', file, overwrite)
      showImportResult(res)
      fetchData()
    } catch (err: any) {
      if (err !== 'cancelled') ElMessage.error(err.message || '导入失败')
    }
  }
  input.click()
}

function showImportResult(res: any) {
  const data = res.data || {}
  const { successCount = 0, failCount = 0, duplicateCount = 0, errors = [] } = data
  
  if (failCount > 0 && errors.length > 0) {
    ElMessageBox.alert(
      errors.join('\n'),
      `导入完成：成功${successCount}条，失败${failCount}条`,
      { type: 'warning', confirmButtonText: '知道了' }
    )
  } else {
    ElMessage.success(res.message || `导入完成：成功${successCount}条`)
  }
}

onMounted(fetchData)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
.text-deleted { text-decoration: line-through; color: #999; }
:deep(.deleted-row) { background-color: #fef0f0; }
</style>