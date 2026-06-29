<template>
  <div>
    <div class="page-header">
      <h3>角色管理</h3>
      <div>
        <el-switch v-model="showDeleted" active-text="显示已删除" style="margin-right:12px" @change="fetchData" />
        <el-button @click="handleDownloadTemplate">下载模板</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
        <el-button type="primary" @click="handleAdd">新增角色</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="list" border v-loading="loading" :row-class-name="tableRowClassName">
        <el-table-column prop="name" label="角色名称" width="160">
          <template #default="{ row }">
            <span :class="row.isDeleted ? 'text-deleted' : ''">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="角色编码" width="160" />
        <el-table-column prop="description" label="描述" min-width="200" />
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isDeleted" type="danger">已删除</el-tag>
            <el-tag v-else :type="row.status ? 'success' : 'danger'">{{ row.status ? '启用' : '禁用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="360" fixed="right">
          <template #default="{ row }">
            <template v-if="row.isDeleted">
              <el-button size="small" type="danger" @click="handleHardDelete(row)">永久删除</el-button>
            </template>
            <template v-else>
              <el-button size="small" @click="handleEdit(row)">编辑</el-button>
              <el-button size="small" @click="handleAssignPermissions(row)">分配权限</el-button>
              <el-button size="small" @click="handleAssignUsers(row)">分配用户</el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑角色' : '新增角色'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="角色名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="角色编码" prop="code">
          <el-input v-model="form.code" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="form.sortOrder" :min="0" />
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

    <el-dialog v-model="permDialogVisible" title="分配权限" width="500px">
      <el-tree
        ref="permTreeRef"
        :data="permTree"
        show-checkbox
        node-key="id"
        :props="{ label: 'name', children: 'children' }"
        default-expand-all
        check-strictly
      />
      <template #footer>
        <el-button @click="permDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitPermissions" :loading="submittingPerms">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="userDialogVisible" title="分配用户" width="600px">
      <el-transfer
        v-model="selectedUserIds"
        :data="allUsers"
        :props="{ key: 'id', label: 'label' }"
        filterable
        filter-placeholder="搜索用户"
        :titles="['可选用户', '已选用户']"
      />
      <template #footer>
        <el-button @click="userDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitUsers" :loading="submittingUsers">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { roleApi, permissionApi, userApi, importExportApi, downloadBlob } from '@/api/system'
import { openPrintWindow } from '@/utils/print'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const permDialogVisible = ref(false)
const isEdit = ref(false)
const showDeleted = ref(false)
const list = ref<any[]>([])
const permTree = ref<any[]>([])
const currentRoleId = ref(0)
const permTreeRef = ref<any>()
const submittingPerms = ref(false)
const formRef = ref<FormInstance>()

const userDialogVisible = ref(false)
const submittingUsers = ref(false)
const allUsers = ref<any[]>([])
const selectedUserIds = ref<number[]>([])

const initForm = () => ({ id: 0, name: '', code: '', description: '', sortOrder: 0, status: true })
const form = ref<any>(initForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入角色名称' }],
  code: [{ required: true, message: '请输入角色编码' }]
}

async function fetchData() {
  loading.value = true
  try {
    const res: any = await roleApi.getAll()
    let activeRoles = res.data || []
    if (showDeleted.value) {
      const deletedRes: any = await roleApi.getDeleted()
      const deletedRoles = (deletedRes.data || []).map((r: any) => ({ ...r, isDeleted: true }))
      list.value = [...activeRoles, ...deletedRoles]
    } else {
      list.value = activeRoles
    }
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
  form.value = { ...row }
  dialogVisible.value = true
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定删除角色 "${row.name}" 吗？`, '提示')
    await roleApi.delete(row.id)
    ElMessage.success('删除成功')
    fetchData()
  } catch { /* cancelled */ }
}

async function handleHardDelete(row: any) {
  try {
    await ElMessageBox.confirm(
      `确定永久删除角色 "${row.name}" 吗？\n此操作不可恢复，将从数据库中彻底移除！`,
      '警告',
      { confirmButtonText: '确定永久删除', cancelButtonText: '取消', type: 'warning' }
    )
    await roleApi.hardDelete(row.id)
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
      await roleApi.update(form.value)
      ElMessage.success('更新成功')
    } else {
      await roleApi.create(form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    fetchData()
  } finally { submitting.value = false }
}

async function handleAssignPermissions(row: any) {
  currentRoleId.value = row.id
  permDialogVisible.value = true
  const permRes: any = await permissionApi.getTree()
  permTree.value = permRes.data || []
  const rolePermRes: any = await roleApi.getPermissions(row.id)
  permTreeRef.value?.setCheckedNodes(rolePermRes.data || [])
}

async function handleSubmitPermissions() {
  submittingPerms.value = true
  try {
    const checkedIds = permTreeRef.value?.getCheckedKeys() || []
    await roleApi.assignPermissions(currentRoleId.value, checkedIds)
    ElMessage.success('权限分配成功')
    permDialogVisible.value = false
  } finally { submittingPerms.value = false }
}

async function handleAssignUsers(row: any) {
  currentRoleId.value = row.id
  userDialogVisible.value = true
  selectedUserIds.value = []
  const [userRes, roleUserRes] = await Promise.all([
    userApi.getAll(),
    roleApi.getUsers(row.id)
  ])
  const all = (userRes.data || []).map((u: any) => ({
    id: u.id,
    label: `${u.username}${u.employeeName ? ' - ' + u.employeeName : ''}`
  }))
  allUsers.value = all
  selectedUserIds.value = (roleUserRes.data || []).map((u: any) => u.id)
}

async function handleSubmitUsers() {
  submittingUsers.value = true
  try {
    await roleApi.assignUsers(currentRoleId.value, selectedUserIds.value)
    ElMessage.success('用户分配成功')
    userDialogVisible.value = false
  } finally { submittingUsers.value = false }
}

async function handleDownloadTemplate() {
  try {
    const res = await importExportApi.downloadTemplate('role')
    downloadBlob(res as unknown as Blob, `角色导入模板_${new Date().toISOString().slice(0, 10)}.xlsx`)
  } catch { ElMessage.error('下载模板失败') }
}

async function handleExport() {
  try {
    const res = await importExportApi.exportData('role')
    downloadBlob(res as unknown as Blob, `角色数据_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
      const res: any = await importExportApi.importData('role', file, overwrite)
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
  openPrintWindow('角色列表', [
    { label: '角色名称', value: (r: any) => r.name },
    { label: '角色编码', value: (r: any) => r.code },
    { label: '描述', value: (r: any) => r.description ?? '' },
    { label: '排序', value: (r: any) => r.sortOrder, align: 'center' },
    { label: '状态', value: (r: any) => r.status ? '启用' : '禁用', align: 'center' }
  ], list.value)
}

onMounted(fetchData)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.text-deleted { text-decoration: line-through; color: #999; }
:deep(.deleted-row) { background-color: #fef0f0; }
</style>