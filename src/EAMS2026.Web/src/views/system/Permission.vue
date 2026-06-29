<template>
  <div>
    <div class="page-header">
      <h3>权限管理</h3>
      <div>
        <el-button @click="handleDownloadTemplate">下载模板</el-button>
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
        <el-button type="primary" @click="handleAdd">新增权限</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="treeData" row-key="id" default-expand-all border :tree-props="{ children: 'children' }" v-loading="loading">
        <el-table-column prop="name" label="权限名称" min-width="180" />
        <el-table-column prop="code" label="权限编码" width="150" />
        <el-table-column prop="type" label="类型" width="100">
          <template #default="{ row }">
            <el-tag>{{ row.type === 'menu' ? '菜单' : row.type === 'button' ? '按钮' : '接口' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑权限' : '新增权限'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="权限名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="权限编码" prop="code">
          <el-input v-model="form.code" />
        </el-form-item>
        <el-form-item label="类型" prop="type">
          <el-select v-model="form.type">
            <el-option label="菜单" value="menu" />
            <el-option label="按钮" value="button" />
            <el-option label="接口" value="api" />
          </el-select>
        </el-form-item>
        <el-form-item label="上级权限" prop="parentId">
          <el-tree-select v-model="form.parentId" :data="treeData" :props="{ label: 'name', value: 'id' }" placeholder="请选择上级权限" clearable check-strictly />
        </el-form-item>
        <el-form-item label="路由路径" prop="path">
          <el-input v-model="form.path" placeholder="例: /system/user" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="form.sortOrder" :min="0" />
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
import { permissionApi, importExportApi, downloadBlob } from '@/api/system'
import { openPrintWindow, flattenTree } from '@/utils/print'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const treeData = ref<any[]>([])
const formRef = ref<FormInstance>()

const initForm = () => ({ id: 0, name: '', code: '', type: 'menu', parentId: null, path: '', sortOrder: 0 })
const form = ref<any>(initForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入权限名称' }],
  code: [{ required: true, message: '请输入权限编码' }]
}

async function fetchTree() {
  loading.value = true
  try {
    const res: any = await permissionApi.getTree()
    treeData.value = res.data || []
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
    await ElMessageBox.confirm(`确定删除权限 "${row.name}" 吗？`, '提示')
    await permissionApi.delete(row.id)
    ElMessage.success('删除成功')
    fetchTree()
  } catch { /* cancelled */ }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    if (isEdit.value) {
      await permissionApi.update(form.value)
      ElMessage.success('更新成功')
    } else {
      await permissionApi.create(form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    fetchTree()
  } finally { submitting.value = false }
}

async function handleDownloadTemplate() {
  try {
    const res = await importExportApi.downloadTemplate('permission')
    downloadBlob(res as unknown as Blob, `权限导入模板_${new Date().toISOString().slice(0, 10)}.xlsx`)
  } catch { ElMessage.error('下载模板失败') }
}

async function handleExport() {
  try {
    const res = await importExportApi.exportData('permission')
    downloadBlob(res as unknown as Blob, `权限数据_${new Date().toISOString().slice(0, 10)}.xlsx`)
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
      const res: any = await importExportApi.importData('permission', file, overwrite)
      showImportResult(res)
      fetchTree()
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
  if (!treeData.value.length) {
    ElMessage.warning('暂无数据可打印')
    return
  }
  const flatData = flattenTree(treeData.value)
  openPrintWindow('权限列表', [
    { label: '权限名称', value: (r: any) => r.name },
    { label: '权限编码', value: (r: any) => r.code },
    { label: '类型', value: (r: any) => r.type === 'menu' ? '菜单' : r.type === 'button' ? '按钮' : '接口', align: 'center' },
    { label: '排序', value: (r: any) => r.sortOrder, align: 'center' },
    { label: '路由路径', value: (r: any) => r.path ?? '' }
  ], flatData)
}

onMounted(fetchTree)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
</style>