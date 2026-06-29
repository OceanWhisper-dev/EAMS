<template>
  <div>
    <div class="page-header">
      <h3>用户管理</h3>
      <div>
        <el-switch v-model="showDeleted" active-text="显示已删除" style="margin-right:12px" @change="fetchData" />
        <el-button @click="handleDownloadTemplate">下载模板</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
        <el-button type="primary" @click="handleAdd">新增用户</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="list" border v-loading="loading" :row-class-name="tableRowClassName">
        <el-table-column prop="username" label="用户名" width="130">
          <template #default="{ row }">
            <span :class="row.isDeleted ? 'text-deleted' : ''">{{ row.username }}</span>
          </template>
        </el-table-column>
        <el-table-column label="姓名" width="120">
          <template #default="{ row }">
            <span :class="row.isDeleted ? 'text-deleted' : ''">{{ row.employeeName || row.username }}</span>
          </template>
        </el-table-column>
        <el-table-column label="角色" min-width="200">
          <template #default="{ row }">
            <el-tag v-for="r in (row.roles || [])" :key="r.id" style="margin-right: 4px;">{{ r.name }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isDeleted" type="danger">已删除</el-tag>
            <el-tag v-else :type="row.status ? 'success' : 'danger'">{{ row.status ? '启用' : '禁用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="最后登录" width="170">
          <template #default="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="320" fixed="right">
          <template #default="{ row }">
            <template v-if="row.isDeleted">
              <el-button size="small" type="danger" @click="handleHardDelete(row)">永久删除</el-button>
            </template>
            <template v-else>
              <el-button size="small" @click="handleEdit(row)">编辑</el-button>
              <el-button size="small" v-if="authStore.isAdmin" @click="handleResetPassword(row)">重置密码</el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑用户' : '新增用户'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" />
        </el-form-item>
        <el-form-item label="密码" prop="passwordHash" v-if="!isEdit">
          <el-input v-model="form.passwordHash" type="password" show-password />
        </el-form-item>
        <el-form-item label="姓名" prop="employeeId">
          <el-select v-model="form.employeeId" filterable clearable placeholder="请选择员工" style="width: 100%">
            <el-option v-for="e in allEmployees" :key="e.id" :label="e.name" :value="e.id">
              <span>{{ e.name }}</span>
              <span style="color: #999; margin-left: 8px;">{{ e.employeeNo }}</span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="角色" prop="roleIds">
          <el-checkbox-group v-model="form.roleIds">
            <el-checkbox v-for="r in allRoles" :key="r.id" :label="r.id" :value="r.id">{{ r.name }}</el-checkbox>
          </el-checkbox-group>
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-switch v-model="form.status" />
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
import { userApi, roleApi, employeeApi, importExportApi, downloadBlob } from '@/api/system'
import { openPrintWindow } from '@/utils/print'
import { useAuthStore } from '@/stores/auth'
import { formatDateTime } from '@/utils/date'

const loading = ref(false)
const submitting = ref(false)
const authStore = useAuthStore()
const dialogVisible = ref(false)
const isEdit = ref(false)
const showDeleted = ref(false)
const list = ref<any[]>([])
const allRoles = ref<any[]>([])
const allEmployees = ref<any[]>([])
const formRef = ref<FormInstance>()

const initForm = () => ({ id: 0, username: '', passwordHash: '', employeeId: null, roleIds: [] as number[], status: true })
const form = ref<any>(initForm())
const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名' }],
  passwordHash: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

function tableRowClassName({ row }: { row: any }) {
  return row.isDeleted ? 'deleted-row' : ''
}

async function fetchData() {
  loading.value = true
  try {
    const [userRes, roleRes, empRes]: any = await Promise.all([userApi.getAll(), roleApi.getAll(), employeeApi.getAll()])
    let activeUsers = userRes.data?.items || userRes.data || []
    if (showDeleted.value) {
      const deletedRes: any = await userApi.getDeleted()
      const deletedUsers = (deletedRes.data || []).map((u: any) => ({ ...u, isDeleted: true }))
      list.value = [...activeUsers, ...deletedUsers]
    } else {
      list.value = activeUsers
    }
    allRoles.value = roleRes.data || []
    allEmployees.value = empRes.data || []
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
    username: row.username,
    employeeId: row.employeeId ?? null,
    roleIds: (row.roles || []).map((r: any) => r.id),
    status: row.status
  }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除用户 "${row.username}" 吗？`, '提示')
    await userApi.delete(row.id)
    ElMessage.success('删除成功')
    fetchData()
  } catch { /* cancelled */ }
}

async function handleHardDelete(row: any) {
  try {
    await ElMessageBox.confirm(
      `确定永久删除用户 "${row.username}" 吗？\n此操作不可恢复，将从数据库中彻底移除！`,
      '警告',
      { confirmButtonText: '确定永久删除', cancelButtonText: '取消', type: 'warning' }
    )
    const res: any = await userApi.hardDelete(row.id)
    if (res.success) {
      ElMessage.success('已永久删除')
      fetchData()
    } else {
      ElMessage.error(res.message || '删除失败')
    }
  } catch { /* cancelled */ }
}

function handleResetPassword(row: any) {
  ElMessageBox.confirm(
    `确定重置用户 "${row.username}" 的密码吗？重置后密码为空，用户下次登录需修改密码。`,
    '提示',
    { confirmButtonText: '确定重置', cancelButtonText: '取消', type: 'warning' }
  ).then(async () => {
    await userApi.resetPassword(row.id)
    ElMessage.success('密码已重置为空')
  }).catch(() => {})
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (isEdit.value) {
      await userApi.update({ id: form.value.id, username: form.value.username, employeeId: form.value.employeeId, status: form.value.status })
      if (form.value.roleIds.length > 0) {
        await userApi.assignRoles(form.value.id, form.value.roleIds)
      }
      ElMessage.success('更新成功')
    } else {
      const res: any = await userApi.create({ username: form.value.username, passwordHash: form.value.passwordHash, employeeId: form.value.employeeId, status: form.value.status })
      const userId = res.data?.id || res.data
      if (userId && form.value.roleIds.length > 0) {
        await userApi.assignRoles(userId, form.value.roleIds)
      }
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    fetchData()
  } finally { submitting.value = false }
}

async function handleDownloadTemplate() {
  try {
    const res = await importExportApi.downloadTemplate('user')
    downloadBlob(res as unknown as Blob, `用户导入模板_${new Date().toISOString().slice(0, 10)}.xlsx`)
  } catch { ElMessage.error('下载模板失败') }
}

async function handleExport() {
  try {
    const res = await importExportApi.exportData('user')
    downloadBlob(res as unknown as Blob, `用户数据_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
          confirmButtonText: '覆盖更新（推荐）',
          cancelButtonText: '跳过重复',
          distinguishCancelAndClose: true
        }
      ).then(() => 'overwrite').catch((action) => {
        if (action === 'cancel') return 'skip'
        throw new Error('cancelled')
      })

      const overwrite = mode === 'overwrite'
      const res: any = await importExportApi.importData('user', file, overwrite)
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

function handlePrint() {
  if (!list.value.length) {
    ElMessage.warning('暂无数据可打印')
    return
  }
  openPrintWindow('用户列表', [
    { label: '用户名', value: (r: any) => r.username },
    { label: '姓名', value: (r: any) => r.employeeName || r.username },
    { label: '角色', value: (r: any) => (r.roles || []).map((x: any) => x.name).join(', ') },
    { label: '状态', value: (r: any) => r.status ? '启用' : '禁用', align: 'center' },
    { label: '最后登录', value: (r: any) => formatDateTime(r.lastLoginAt) }
  ], list.value)
}

onMounted(fetchData)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.text-deleted { text-decoration: line-through; color: #999; }
:deep(.deleted-row) { background-color: #fef0f0; }
</style>