<template>
  <div>
    <div class="page-header">
      <h3>报表管理</h3>
      <div>
        <el-button type="primary" @click="handleAddReport" v-if="hasPermission('erp-report:create')">新增报表</el-button>
        <el-button v-if="authStore.isAdmin" @click="handleExportConfig">导出配置</el-button>
        <el-button v-if="authStore.isAdmin" type="success" @click="handleImportConfig">导入配置</el-button>
      </div>
    </div>
    <el-row :gutter="16">
      <!-- 左侧分类树 -->
      <el-col :span="5">
        <el-card>
          <template #header>
            <div class="flex-between">
              <span>分类</span>
              <el-button size="small" @click="handleAddCategory">新增</el-button>
            </div>
          </template>
          <el-tree
            ref="categoryTreeRef"
            :data="categories"
            node-key="id"
            :props="{ label: 'name', children: 'children' }"
            highlight-current
            @node-click="handleCategoryClick"
            @node-contextmenu="handleCategoryContextMenu"
          />
          <!-- 分类右键菜单 -->
          <div
            v-show="contextMenu.visible"
            class="category-context-menu"
            :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
            @click.stop
          >
            <div class="context-item" @click="handleContextEdit">编辑</div>
            <div class="context-item context-item-danger" @click="handleContextDelete">删除</div>
          </div>
          <div v-if="contextMenu.visible" class="context-overlay" @click="closeContextMenu" />
        </el-card>
      </el-col>
      <!-- 右侧报表列表 -->
      <el-col :span="19">
        <el-card>
          <el-table :data="paginatedReports" border v-loading="loading" @row-dblclick="handleViewReport" @sort-change="handleSortChange">
            <el-table-column prop="title" label="报表名称" min-width="160" sortable="custom" />
            <el-table-column prop="categoryName" label="分类" width="120" sortable="custom" />
            <el-table-column prop="queryType" label="类型" width="80" align="center" sortable="custom">
              <template #default="{ row }">
                <el-tag size="small">{{ row.queryType }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="queryDatasourceName" label="数据源" width="120" sortable="custom">
              <template #default="{ row }">
                <el-tag size="small" type="warning" effect="plain">{{ row.queryDatasourceName || row.queryDatasource || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="80" align="center" sortable="custom">
              <template #default="{ row }">
                <el-tag :type="row.status === 'published' ? 'success' : 'info'" size="small">
                  {{ row.status === 'published' ? '已发布' : row.status === 'draft' ? '草稿' : '已禁用' }}
                </el-tag>
              </template>
            </el-table-column>
            
            <el-table-column prop="updatedAt" label="更新时间" width="170" sortable="custom">
              <template #default="{ row }">
                {{ formatDateTimeSafe(row.updatedAt) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" fixed="right">
              <template #default="{ row }">
                <div class="action-btns">
                  <el-button size="small" @click="handleViewReport(row)">查看</el-button>
                  <el-button v-if="row.canManage" size="small" @click="handleEditReport(row)" :disabled="row.isSystem">编辑</el-button>
                  <el-button v-if="row.canManage" size="small" @click="handleManagePermission(row)">权限</el-button>
                  <el-popconfirm v-if="row.canManage" title="确定删除？" @confirm="handleDeleteReport(row)">
                    <template #reference>
                      <el-button size="small" type="danger" :disabled="row.isSystem">删</el-button>
                    </template>
                  </el-popconfirm>
                </div>
              </template>
            </el-table-column>
          </el-table>
          <div class="pagination-wrapper" v-if="total > 0">
            <el-pagination
              v-model:current-page="currentPage"
              v-model:page-size="pageSize"
              :total="total"
              :page-sizes="[10, 20, 50, 100]"
              layout="total, sizes, prev, pager, next"
            />
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 分类对话框 -->
    <el-dialog v-model="categoryDialogVisible" :title="categoryIsEdit ? '编辑分类' : '新增分类'" width="400px">
      <el-form :model="categoryForm" label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="categoryForm.name" />
        </el-form-item>
        <el-form-item label="父分类">
          <el-tree-select
            v-model="categoryForm.parentId"
            :data="categories"
            :props="{ label: 'name', value: 'id' }"
            placeholder="不选则为顶级"
            clearable
            check-strictly
          />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="categoryForm.sortOrder" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="categoryDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleCategorySubmit" :loading="categorySubmitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 权限管理对话框 -->
    <el-dialog v-model="permDialogVisible" title="报表权限" width="600px">
      <template v-if="permReport">
        <p style="margin-bottom:12px;color:#666">
          报表：<strong>{{ permReport.title }}</strong>
        </p>
        <el-table :data="permList" border stripe max-height="300" size="small">
          <el-table-column label="类型" width="80" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="row.principalType === 'role' ? 'warning' : 'primary'">
                {{ row.principalType === 'role' ? '角色' : '用户' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="principalName" label="主体" min-width="140" />
          <el-table-column prop="accessType" label="访问类型" width="100" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="accessTypeTag(row.accessType)">
                {{ accessTypeLabel(row.accessType) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ row }">
              <el-button size="small" type="danger" link @click="handleDeletePermission(row)">移除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-divider />
        <div style="display:flex;gap:8px;align-items:flex-end">
          <el-form label-width="60px" size="small" style="flex:1">
            <div style="display:flex;gap:8px">
              <el-form-item label="类型">
                <el-select v-model="permForm.principalType" placeholder="类型" style="width:100px">
                  <el-option label="角色" value="role" />
                  <el-option label="用户" value="user" />
                </el-select>
              </el-form-item>
              <el-form-item label="主体" style="flex:1">
                <el-select v-model="permForm.principalId" placeholder="选择用户或角色" filterable style="width:100%">
                  <el-option
                    v-for="opt in filteredPrincipalOptions"
                    :key="opt.type + '-' + opt.id"
                    :label="opt.name"
                    :value="opt.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="权限">
                <el-select v-model="permForm.accessType" style="width:110px">
                  <el-option label="查看" value="view" />
                  <el-option label="导出" value="export" />
                  <el-option label="管理" value="manage" />
                </el-select>
              </el-form-item>
            </div>
          </el-form>
          <el-button type="primary" size="small" @click="handleAddPermission" :loading="permSubmitting">添加</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { reportApi, type ReportCategory, type ReportDto } from '@/api/report'
import { ElMessage, ElMessageBox } from 'element-plus'
import { formatDateTimeSafe } from '@/utils/date'

/** 后端统一响应格式 { success, data, message } */
interface ApiResult<T = any> {
  success: boolean
  data?: T
  message?: string
}

const router = useRouter()
const authStore = useAuthStore()
const loading = ref(false)
const categories = ref<ReportCategory[]>([])
const reports = ref<ReportDto[]>([])
const selectedCategoryId = ref<number | undefined>()

function hasPermission(code: string): boolean {
  return authStore.isAdmin || authStore.hasPermission(code)
}

// 分页状态
const currentPage = ref(1)
const pageSize = ref(20)
const total = computed(() => reports.value.length)
const paginatedReports = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return reports.value.slice(start, start + pageSize.value)
})

// 分类对话框
const categoryTreeRef = ref<any>(null)
const categoryDialogVisible = ref(false)
const categoryIsEdit = ref(false)
const categorySubmitting = ref(false)
const categoryForm = ref({ name: '', parentId: null as number | null, sortOrder: 0 })
const editingCategoryId = ref<number | null>(null)

// 分类右键菜单
const contextMenu = reactive({ visible: false, x: 0, y: 0, node: null as ReportCategory | null })
function handleCategoryContextMenu(evt: MouseEvent, data: ReportCategory) {
  evt.preventDefault()
  contextMenu.visible = true
  contextMenu.x = evt.pageX
  contextMenu.y = evt.pageY
  contextMenu.node = data
}
function closeContextMenu() {
  contextMenu.visible = false
  contextMenu.node = null
}
function handleContextEdit() {
  const node = contextMenu.node
  closeContextMenu()
  if (!node) return
  categoryIsEdit.value = true
  editingCategoryId.value = node.id
  categoryForm.value = { name: node.name, parentId: node.parentId, sortOrder: node.sortOrder ?? 0 }
  categoryDialogVisible.value = true
}
async function handleContextDelete() {
  const node = contextMenu.node
  closeContextMenu()
  if (!node) return
  try {
    await ElMessageBox.confirm(`确定删除分类「${node.name}」吗？`, '确认删除', { type: 'warning' })
    await reportApi.deleteCategory(node.id)
    ElMessage.success('删除成功')
    loadCategories()
  } catch {
    // 取消删除
  }
}

onMounted(() => {
  loadCategories()
  loadReports()
})

async function loadCategories() {
  const res = await reportApi.getCategories()
  categories.value = res.data || []
}

async function loadReports() {
  loading.value = true
  try {
    const res = await reportApi.getReports(selectedCategoryId.value)
    reports.value = res.data || []
  } finally {
    loading.value = false
  }
}

let sortState: { prop: string; order: 'ascending' | 'descending' | null } | null = null

function handleSortChange(sort: { prop: string | null; order: string | null }) {
  currentPage.value = 1  // 排序后回到第一页
  if (!sort.prop || !sort.order) {
    sortState = null
    loadReports()
    return
  }
  sortState = { prop: sort.prop, order: sort.order as 'ascending' | 'descending' }
  const key = sort.prop as keyof ReportDto
  const asc = sort.order === 'ascending'
  reports.value = [...reports.value].sort((a: ReportDto, b: ReportDto) => {
    let va = a[key], vb = b[key]
    // 日期字符串比较
    if (key === 'updatedAt') {
      va = va ? new Date(va as string).getTime() : 0
      vb = vb ? new Date(vb as string).getTime() : 0
    }
    // 字符串比较
    if (typeof va === 'string' && typeof vb === 'string') {
      return asc ? va.localeCompare(vb, 'zh-CN') : vb.localeCompare(va, 'zh-CN')
    }
    // 数值比较
    if (va == null) return asc ? -1 : 1
    if (vb == null) return asc ? 1 : -1
    return asc ? (va > vb ? 1 : -1) : (va < vb ? 1 : -1)
  })
}

function handleCategoryClick(data: ReportCategory) {
  selectedCategoryId.value = data.id
  currentPage.value = 1
  loadReports()
}

// 分类操作
function handleAddCategory() {
  categoryIsEdit.value = false
  editingCategoryId.value = null
  categoryForm.value = { name: '', parentId: null, sortOrder: 0 }
  categoryDialogVisible.value = true
}

async function handleCategorySubmit() {
  categorySubmitting.value = true
  try {
    if (categoryIsEdit.value && editingCategoryId.value) {
      await reportApi.updateCategory(editingCategoryId.value, categoryForm.value)
      ElMessage.success('更新成功')
    } else {
      await reportApi.addCategory(categoryForm.value)
      ElMessage.success('创建成功')
    }
    categoryDialogVisible.value = false
    loadCategories()
  } finally {
    categorySubmitting.value = false
  }
}

// 报表操作
function handleAddReport() {
  router.push({ name: 'ReportDesigner' })
}

function handleViewReport(row: ReportDto) {
  router.push({ name: 'ReportViewer', params: { id: row.id } })
}

function handleEditReport(row: ReportDto) {
  router.push({ name: 'ReportDesigner', params: { id: row.id } })
}

async function handleDeleteReport(row: ReportDto) {
  await reportApi.deleteReport(row.id)
  ElMessage.success('删除成功')
  loadReports()
}

async function handleExportConfig() {
  try {
    const blob = await reportApi.exportReportsConfig() as unknown as Blob
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'report-config.json'
    a.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch {
    ElMessage.error('导出失败')
  }
}

function handleImportConfig() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = '.json'
  input.onchange = async () => {
    const file = input.files?.[0]
    if (!file) return
    try {
      const res = await reportApi.importReportsConfig(file) as unknown as ApiResult
      ElMessage.success(res.message || '导入成功')
    } catch (e: unknown) {
      const errMsg = (e as { message?: string })?.message || '导入失败'
      ElMessage.error(errMsg)
    }
  }
  input.click()
}

async function handleToggleBookmark(row: ReportDto) {
  const res = await reportApi.toggleBookmark(row.id) as unknown as ApiResult<{ bookmarked: boolean }>
  row.isBookmarked = res.data?.bookmarked ?? !row.isBookmarked
}

// ===== 权限管理 =====
interface PrincipalOption {
  id: number
  name: string
  type: string
  typeLabel?: string
}

interface ReportPermission {
  id: number
  reportId: number
  principalType: string
  principalId: number
  principalName: string
  accessType: string
  createdAt: string
}

const permDialogVisible = ref(false)
const permReport = ref<ReportDto | null>(null)
const permList = ref<ReportPermission[]>([])
const principalOptions = ref<PrincipalOption[]>([])
const permSubmitting = ref(false)
const permForm = ref({ principalType: 'role', principalId: null as number | null, accessType: 'view' })

const filteredPrincipalOptions = computed(() =>
  principalOptions.value.filter(o => o.type === permForm.value.principalType)
)

watch(() => permForm.value.principalType, () => {
  permForm.value.principalId = null
})

function accessTypeLabel(type: string): string {
  return type === 'view' ? '查看' : type === 'export' ? '导出' : '管理'
}

function accessTypeTag(type: string): string {
  return type === 'view' ? 'success' : type === 'export' ? 'primary' : 'warning'
}

async function handleManagePermission(row: ReportDto) {
  permReport.value = row
  permDialogVisible.value = true
  permForm.value = { principalType: 'role', principalId: null, accessType: 'view' }
  try {
    const [permRes, optRes] = await Promise.all([
      reportApi.getReportPermissions(row.id),
      reportApi.getPrincipalOptions()
    ])
    permList.value = (permRes as unknown as ApiResult<ReportPermission[]>).data || []
    const opts: PrincipalOption[] = ((optRes as unknown as ApiResult<PrincipalOption[]>).data || []).map((o) => ({
      ...o,
      typeLabel: o.type === 'role' ? '角色' : '用户'
    }))
    principalOptions.value = opts
  } catch {
    ElMessage.error('加载权限数据失败')
  }
}

async function handleAddPermission() {
  if (!permReport.value || !permForm.value.principalId) return
  permSubmitting.value = true
  try {
    const res = await reportApi.setReportPermission(permReport.value.id, {
      principalType: permForm.value.principalType,
      principalId: permForm.value.principalId,
      accessType: permForm.value.accessType
    }) as unknown as ApiResult
    if (res.success) {
      ElMessage.success('授权成功')
      // 刷新权限列表
      const permRes = await reportApi.getReportPermissions(permReport.value.id) as unknown as ApiResult<ReportPermission[]>
      permList.value = permRes.data || []
      permForm.value.principalId = null
    } else {
      ElMessage.error(res.message || '授权失败')
    }
  } catch {
    ElMessage.error('授权失败')
  } finally {
    permSubmitting.value = false
  }
}

async function handleDeletePermission(row: ReportPermission) {
  try {
    const res = await reportApi.deleteReportPermission(row.id) as unknown as ApiResult
    if (res.success) {
      ElMessage.success('已移除')
      permList.value = permList.value.filter(p => p.id !== row.id)
    } else {
      ElMessage.error(res.message || '移除失败')
    }
  } catch {
    ElMessage.error('移除失败')
  }
}
</script>

<style scoped>
.flex-between {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.action-btns {
  display: flex;
  gap: 2px;
  justify-content: flex-end;
}
.action-btns .el-button {
  margin-left: 0 !important;
  padding: 5px 6px;
}
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
.category-context-menu {
  position: fixed;
  z-index: 9999;
  background: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.12);
  padding: 4px 0;
  min-width: 100px;
}
.context-item {
  padding: 6px 16px;
  cursor: pointer;
  font-size: 13px;
  color: #333;
  white-space: nowrap;
}
.context-item:hover {
  background-color: #f5f7fa;
}
.context-item-danger {
  color: #f56c6c;
}
.context-item-danger:hover {
  background-color: #fef0f0;
}
.context-overlay {
  position: fixed;
  inset: 0;
  z-index: 9998;
}
</style>