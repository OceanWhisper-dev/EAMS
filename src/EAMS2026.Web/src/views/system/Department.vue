<template>
  <div>
    <div class="page-header">
      <h3>部门管理</h3>
      <div class="header-actions">
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
        <el-button type="primary" @click="handleAdd">新增部门</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="treeData" row-key="id" default-expand-all border :tree-props="{ children: 'children' }" v-loading="loading">
        <el-table-column prop="name" label="部门名称" min-width="180" />
        <el-table-column prop="code" label="部门编码" width="120" />
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status ? 'success' : 'danger'">{{ row.status ? '启用' : '禁用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑部门' : '新增部门'" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="部门名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="部门编码" prop="code">
          <el-input v-model="form.code" />
        </el-form-item>
        <el-form-item label="上级部门" prop="parentId">
          <el-tree-select v-model="form.parentId" :data="treeData" :props="{ label: 'name', value: 'id' }" placeholder="请选择上级部门" clearable allow-create check-strictly />
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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { departmentApi, importExportApi, printApi, downloadBlob } from '@/api/system'
import { openPrintWindow, flattenTree } from '@/utils/print'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const treeData = ref<any[]>([])
const formRef = ref<FormInstance>()

const initForm = () => ({ id: 0, name: '', code: '', parentId: null, sortOrder: 0, status: true })
const form = ref<any>(initForm())
const rules: FormRules = {
  name: [{ required: true, message: '请输入部门名称' }],
  code: [{ required: true, message: '请输入部门编码' }]
}

async function fetchTree() {
  loading.value = true
  try {
    const res: any = await departmentApi.getTree()
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
    await ElMessageBox.confirm(`确定删除部门 "${row.name}" 吗？`, '提示')
    await departmentApi.delete(row.id)
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
      await departmentApi.update(form.value)
      ElMessage.success('更新成功')
    } else {
      await departmentApi.create(form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    fetchTree()
  } finally { submitting.value = false }
}

async function handleExport() {
  try {
    const res: any = await importExportApi.exportData('department')
    downloadBlob(res as unknown as Blob, `部门数据_${new Date().toISOString().slice(0, 10)}.xlsx`)
    ElMessage.success('导出成功')
  } catch { ElMessage.error('导出失败') }
}

async function handlePrint() {
  if (!treeData.value.length) {
    ElMessage.warning('暂无数据可打印')
    return
  }
  const flatData = flattenTree(treeData.value)
  openPrintWindow('部门列表', [
    { label: '部门名称', value: (r: any) => r.name },
    { label: '部门编码', value: (r: any) => r.code },
    { label: '排序', value: (r: any) => r.sortOrder, align: 'center' },
    { label: '状态', value: (r: any) => r.status ? '启用' : '禁用', align: 'center' }
  ], flatData)
}

async function handleImport() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = '.xlsx,.xls'
  input.onchange = async (e: any) => {
    const file = e.target.files[0]
    if (!file) return
    try {
      const res: any = await importExportApi.importData('department', file)
      if (res.data?.duplicateCount > 0) {
        try {
          await ElMessageBox.confirm(
            `发现 ${res.data.duplicateCount} 条重复数据，是否覆盖更新？`,
            '导入确认',
            { confirmButtonText: '确定更新', cancelButtonText: '跳过重复' }
          )
          const res2: any = await importExportApi.importData('department', file, true)
          ElMessage.success(res2.message || '覆盖更新完成')
        } catch { /* user cancelled */ }
      } else {
        ElMessage.success(res.message || '导入完成')
      }
      fetchTree()
    } catch { ElMessage.error('导入失败') }
  }
  input.click()
}

onMounted(fetchTree)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>