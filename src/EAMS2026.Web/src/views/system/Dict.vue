<template>
  <div>
    <div class="page-header">
      <h3>字典管理</h3>
      <el-button type="primary" @click="handleAddType">新增字典类型</el-button>
    </div>
    <el-row :gutter="16">
      <el-col :span="10">
        <el-card>
          <template #header>字典类型</template>
          <el-table :data="types" border v-loading="loading" highlight-current-row @current-change="handleTypeChange">
            <el-table-column prop="name" label="名称" min-width="120" />
            <el-table-column prop="code" label="编码" width="120" />
            <el-table-column label="操作" width="160" fixed="right">
              <template #default="{ row }">
                <el-button size="small" @click="handleEditType(row)">编辑</el-button>
                <el-button size="small" type="danger" @click="handleDeleteType(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
      <el-col :span="14">
        <el-card>
          <template #header>
            <span>{{ currentType ? currentType.name : '' }} - 字典项</span>
            <el-button size="small" type="primary" style="float:right" @click="handleAddItem" :disabled="!currentType">新增项</el-button>
          </template>
          <el-table :data="items" border v-loading="itemsLoading">
            <el-table-column prop="label" label="标签" width="120" />
            <el-table-column prop="value" label="值" width="120" />
            <el-table-column prop="sortOrder" label="排序" width="60" align="center" />
            <el-table-column prop="status" label="状态" width="60" align="center">
              <template #default="{ row }">
                <el-tag :type="row.status ? 'success' : 'danger'">{{ row.status ? '是' : '否' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" fixed="right">
              <template #default="{ row }">
                <el-button size="small" @click="handleEditItem(row)">编辑</el-button>
                <el-button size="small" type="danger" @click="handleDeleteItem(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <el-dialog v-model="typeDialogVisible" :title="isEditType ? '编辑字典类型' : '新增字典类型'" width="500px">
      <el-form ref="typeFormRef" :model="typeForm" :rules="typeRules" label-width="80px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="typeForm.name" />
        </el-form-item>
        <el-form-item label="编码" prop="code">
          <el-input v-model="typeForm.code" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="typeForm.description" type="textarea" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="typeForm.sortOrder" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="typeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitType" :loading="submittingType">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="itemDialogVisible" :title="isEditItem ? '编辑字典项' : '新增字典项'" width="500px">
      <el-form ref="itemFormRef" :model="itemForm" :rules="itemRules" label-width="80px">
        <el-form-item label="标签" prop="label">
          <el-input v-model="itemForm.label" />
        </el-form-item>
        <el-form-item label="值" prop="value">
          <el-input v-model="itemForm.value" />
        </el-form-item>
        <el-form-item label="颜色" prop="color">
          <el-input v-model="itemForm.color" placeholder="如: success / warning / danger" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="itemForm.sortOrder" :min="0" />
        </el-form-item>
        <el-form-item label="默认" prop="isDefault">
          <el-switch v-model="itemForm.isDefault" />
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-switch v-model="itemForm.status" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="itemDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitItem" :loading="submittingItem">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { dictApi } from '@/api/system'

const loading = ref(false)
const itemsLoading = ref(false)
const submittingType = ref(false)
const submittingItem = ref(false)
const types = ref<any[]>([])
const items = ref<any[]>([])
const currentType = ref<any>(null)
const typeDialogVisible = ref(false)
const itemDialogVisible = ref(false)
const isEditType = ref(false)
const isEditItem = ref(false)
const typeFormRef = ref<FormInstance>()
const itemFormRef = ref<FormInstance>()

const initTypeForm = () => ({ id: 0, name: '', code: '', description: '', sortOrder: 0 })
const typeForm = ref<any>(initTypeForm())
const typeRules: FormRules = {
  name: [{ required: true, message: '请输入名称' }],
  code: [{ required: true, message: '请输入编码' }]
}

const initItemForm = () => ({ id: 0, dictTypeId: 0, label: '', value: '', color: '', sortOrder: 0, isDefault: false, status: true })
const itemForm = ref<any>(initItemForm())
const itemRules: FormRules = {
  label: [{ required: true, message: '请输入标签' }],
  value: [{ required: true, message: '请输入值' }]
}

async function fetchTypes() {
  loading.value = true
  try {
    const res: any = await dictApi.getTypes()
    types.value = res.data || []
  } finally { loading.value = false }
}

async function handleTypeChange(row: any) {
  currentType.value = row
  if (!row) return
  itemsLoading.value = true
  try {
    const res: any = await dictApi.getItems(row.id)
    items.value = res.data || []
  } finally { itemsLoading.value = false }
}

function handleAddType() {
  isEditType.value = false
  typeForm.value = initTypeForm()
  typeDialogVisible.value = true
}

function handleEditType(row: any) {
  isEditType.value = true
  typeForm.value = { ...row }
  typeDialogVisible.value = true
}

async function handleDeleteType(row: any) {
  try {
    await ElMessageBox.confirm('确定删除该字典类型吗？', '提示')
    await dictApi.deleteType(row.id)
    ElMessage.success('删除成功')
    if (currentType.value?.id === row.id) {
      currentType.value = null
      items.value = []
    }
    fetchTypes()
  } catch { /* cancelled */ }
}

async function handleSubmitType() {
  const valid = await typeFormRef.value?.validate().catch(() => false)
  if (!valid) return
  submittingType.value = true
  try {
    if (isEditType.value) {
      await dictApi.updateType(typeForm.value)
      ElMessage.success('更新成功')
    } else {
      await dictApi.createType(typeForm.value)
      ElMessage.success('创建成功')
    }
    typeDialogVisible.value = false
    fetchTypes()
  } finally { submittingType.value = false }
}

function handleAddItem() {
  isEditItem.value = false
  itemForm.value = { ...initItemForm(), dictTypeId: currentType.value!.id }
  itemDialogVisible.value = true
}

function handleEditItem(row: any) {
  isEditItem.value = true
  itemForm.value = { ...row, dictTypeId: currentType.value!.id }
  itemDialogVisible.value = true
}

async function handleDeleteItem(row: any) {
  try {
    await ElMessageBox.confirm('确定删除该字典项吗？', '提示')
    await dictApi.deleteItem(row.id)
    ElMessage.success('删除成功')
    handleTypeChange(currentType.value)
  } catch { /* cancelled */ }
}

async function handleSubmitItem() {
  const valid = await itemFormRef.value?.validate().catch(() => false)
  if (!valid) return
  submittingItem.value = true
  try {
    if (isEditItem.value) {
      await dictApi.updateItem(itemForm.value)
      ElMessage.success('更新成功')
    } else {
      await dictApi.createItem(itemForm.value)
      ElMessage.success('创建成功')
    }
    itemDialogVisible.value = false
    handleTypeChange(currentType.value)
  } finally { submittingItem.value = false }
}

onMounted(fetchTypes)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
</style>