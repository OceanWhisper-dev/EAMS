<template>
  <div>
    <div class="page-header">
      <h3>{{ report?.title || '报表查看' }}</h3>
      <div>
        <el-button @click="handleBack">返回</el-button>
        <el-button v-if="canManage" @click="handleEdit">编辑</el-button>
        <el-dropdown v-if="canExport && report && activeTab === 'table'" @command="handleExport" split-button type="success">
          导出
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="xlsx">Excel (.xlsx)</el-dropdown-item>
              <el-dropdown-item command="csv">CSV (.csv)</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <!-- 过滤条件面板 -->
    <el-card v-if="report?.filters?.length" class="filter-card">
      <el-form :model="filterValues" inline>
        <el-form-item v-for="f in report.filters" :key="f.fieldName" :label="f.label || f.fieldName">
          <el-input v-if="f.controlType === 'input'" v-model="filterValues[f.fieldName]" :placeholder="f.fieldName" />
          <el-select v-else-if="f.controlType === 'select'" v-model="filterValues[f.fieldName]" :placeholder="f.fieldName" clearable style="min-width:160px">
            <el-option v-for="opt in selectOptions[f.fieldName] || []" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
          <el-date-picker v-else-if="f.controlType === 'date'" v-model="filterValues[f.fieldName]" type="daterange" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="dateShortcuts" />
          <el-date-picker v-else-if="f.controlType === 'daterange'" v-model="filterValues[f.fieldName]" type="daterange" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="dateShortcuts" />
          <el-select v-else-if="f.controlType === 'salesperson'" v-model="filterValues[f.fieldName]" placeholder="选择业务员" clearable filterable :disabled="isRestrictedSalesperson">
            <el-option v-for="sp in filteredSalespersonList" :key="sp.code" :label="sp.name" :value="sp.code" />
          </el-select>
          <el-input v-else v-model="filterValues[f.fieldName]" :placeholder="f.fieldName" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleExecute">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 执行信息：显示后端查询耗时 + 前端总耗时 -->
    <div v-if="executionInfo" class="execution-bar">
      后端查询: {{ executionInfo.durationMs }}ms | 数据行: {{ executionInfo.rowCount }}
      <span v-if="totalLoadTime > 0"> | 总加载: {{ totalLoadTime }}ms</span>
    </div>

    <!-- 加载中提示（查询中始终显示） -->
    <el-card v-if="loading" v-loading="loading" element-loading-text="正在查询数据..." style="min-height:120px">
      <el-empty v-if="!result.rows.length && !pivotRows.length" description="正在查询数据..." />
    </el-card>

    <!-- 数据/透视表 Tabs -->
    <el-tabs v-if="result.rows.length || pivotRows.length" v-model="activeTab">
      <el-tab-pane label="数据表" name="table">
        <el-card ref="tableContainerRef" v-loading="loading">
          <!-- 数据表格式工具栏 -->
          <div class="table-view-toolbar">
            <el-dropdown @command="handleTableViewCommand" size="small">
              <el-button size="small">
                格式 <el-icon><arrow-down /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="save">保存当前格式</el-dropdown-item>
                  <el-dropdown-item command="load">加载格式</el-dropdown-item>
                  <el-dropdown-item v-if="selectedViewId" command="share" divided>共享格式</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <el-button size="small" @click="showColumnSettings">
              <el-icon><setting /></el-icon> 列设置
            </el-button>
            <span v-if="activeViewName" class="active-view-name">
              当前格式: {{ activeViewName }}
            </span>
          </div>

          <ListTable
            :options="vTableOptions"
            :records="vTableRecords"
            :height="vTableHeight"
            :on-error="handleVTableError"
            @on-sort-click="handleVTableSortChange"
            style="width:100%;"
          />

          <!-- 汇总行 -->
          <div v-if="result.summary && Object.keys(result.summary).length" class="summary-row">
            <strong>汇总: </strong>
            <span v-for="(val, key) in result.summary" :key="key" class="summary-item">
              {{ key }}: {{ formatSummaryValue(val) }}
            </span>
          </div>

          <!-- 分页：仅SQL报表显示，存储过程报表不显示 -->
          <div v-if="!isStoredProcedure && result.rows.length > 0" class="pagination-row">
            <el-pagination
              v-model:current-page="currentPage"
              v-model:page-size="pageSize"
              :total="result.pagination?.total || result.rows.length"
              :page-sizes="[20, 50, 100, 200]"
              layout="total, sizes, prev, pager, next"
              @current-change="handlePageChange"
              @size-change="handleSizeChange"
            />
          </div>
        </el-card>
      </el-tab-pane>
      <el-tab-pane label="透视表" name="pivot">
        <PivotTable
          v-loading="pivotLoading"
          :report-id="report!.id"
          :columns="result.columns"
          :rows="pivotRows.length > 0 ? pivotRows : result.rows"
        />
      </el-tab-pane>
    </el-tabs>

    <!-- 无数据时 -->
    <el-card v-else v-loading="loading">
      <el-empty description="暂无数据" />
    </el-card>

    <!-- 图表 -->
    <el-row v-if="report?.charts?.length" :gutter="16" class="chart-area">
      <el-col v-for="chart in report.charts" :key="chart.title" :span="12">
        <el-card>
          <template #header>{{ chart.title }}</template>
          <div :id="'chart-' + chart.title" :style="{ width: chart.width + 'px', height: chart.height + 'px' }"></div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 保存格式对话框 -->
    <el-dialog v-model="tableViewDialogVisible" title="保存数据表格式" width="420px">
      <el-form :model="editingView" label-width="80px">
        <el-form-item label="名称">
          <el-input v-model="editingView.viewName" placeholder="输入格式名称，如: 默认视图" />
        </el-form-item>
        <el-form-item label="说明">
          <span class="form-desc">将保存当前列顺序、宽度和对齐方式</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="tableViewDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="tableViewSaveLoading" @click="handleSaveTableView">保存</el-button>
      </template>
    </el-dialog>

    <!-- 加载格式对话框 -->
    <el-dialog v-model="tableViewLoadVisible" title="加载格式" width="560px">
      <div v-if="tableViews.length === 0" class="empty-hint">暂无已保存的格式</div>
      <el-radio-group v-else v-model="selectedViewId" class="table-view-list">
        <el-radio v-for="v in tableViews" :key="v.id" :value="v.id" class="table-view-item">
          <div class="view-item-content">
            <span class="view-name">{{ v.viewName }}</span>
            <span class="view-meta">
              {{ v.isDefault ? '系统缺省' : (v.creatorName || '我') }}
              · {{ new Date(v.updatedAt).toLocaleDateString() }}
              <el-tag v-if="v.isShared" size="small" type="warning">已共享</el-tag>
            </span>
          </div>
          <el-button v-if="!v.isDefault" size="small" type="danger" text @click.stop="handleDeleteTableView(v.id)">
            删除
          </el-button>
        </el-radio>
      </el-radio-group>
      <template #footer>
        <el-button @click="tableViewLoadVisible = false">取消</el-button>
        <el-button type="primary" :disabled="!selectedViewId" @click="applySelectedView">应用</el-button>
      </template>
    </el-dialog>

    <!-- 共享格式对话框 -->
    <el-dialog v-model="tableViewShareVisible" title="共享格式" width="500px">
      <div class="share-section">
        <div class="share-form-row">
          <el-select v-model="shareTargetType" style="width:100px">
            <el-option label="用户" value="user" />
            <el-option label="角色" value="role" />
          </el-select>
          <el-input v-model.number="shareTargetId" :placeholder="shareTargetType === 'user' ? '用户ID' : '角色ID'" type="number" min="1" style="flex:1" />
          <el-button type="primary" :loading="shareLoading" @click="handleAddShare">添加</el-button>
        </div>
        <el-divider />
        <div v-if="currentShares.length === 0" class="empty-hint">暂无共享目标</div>
        <el-table v-else :data="currentShares" size="small" max-height="300">
          <el-table-column prop="targetName" label="目标" min-width="120" />
          <el-table-column label="类型" width="80">
            <template #default="{ row }">
              <el-tag :type="row.targetType === 'user' ? '' : 'success'" size="small">
                {{ row.targetType === 'user' ? '用户' : '角色' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80">
            <template #default="{ row }">
              <el-button size="small" type="danger" text @click="handleRemoveShare(row.id)">取消</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-dialog>

    <!-- 列设置对话框 -->
    <el-dialog v-model="columnSettingsVisible" title="列设置" width="560px">
      <div class="column-settings-body">
        <div class="column-settings-hint">拖动列名可调整顺序，勾选控制可见性</div>
        <div ref="columnSortRef" class="column-sort-list">
          <div v-for="col in result.columns" :key="col.field" class="column-sort-item" :data-field="col.field">
            <el-checkbox v-model="columnVisibility[col.field]" @change="onColumnVisibilityChange" />
            <span class="column-drag-handle">⠿</span>
            <span class="column-name">{{ col.title || col.field }}</span>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="columnSettingsVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import PivotTable from './components/PivotTable.vue'
import { ListTable } from '@visactor/vue-vtable'
import { ref, computed, onMounted, onUnmounted, nextTick, watch, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { reportApi, type ReportDetailDto, type ReportExecuteResult,
         type ReportColumnDto, type ReportTableViewDto, type SaveTableViewRequest,
         type TableViewShareDto } from '@/api/report'
import { erpSettingsApi } from '@/api/erpSettings'
import request from '@/utils/request'
import { ElMessage } from 'element-plus'
import { ArrowDown, Setting } from '@element-plus/icons-vue'
import * as echarts from 'echarts'
import Sortable from 'sortablejs'
import { formatSummaryValue } from '@/utils/date'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const canManage = ref(false)
const canExport = ref(false)
const activeTab = ref('table')
const report = ref<ReportDetailDto | null>(null)
const result = ref<ReportExecuteResult>({ columns: [], rows: [] })
const executionInfo = ref<{ durationMs: number; rowCount: number } | null>(null)
const filterValues = ref<Record<string, any>>({})
const currentPage = ref(1)
const pageSize = ref(50)
const currentSort = ref<{ field: string; direction: string }[]>([])
const pivotRows = ref<Record<string, unknown>[]>([])
const pivotLoading = ref(false)
const totalLoadTime = ref(0)

// ===== 列设置：可见性 + 拖拽排序 =====
const columnVisibility = reactive<Record<string, boolean>>({})
const columnSettingsVisible = ref(false)
const columnSortRef = ref<HTMLDivElement | null>(null)
let sortableInstance: Sortable | null = null

/** 仅显示可见的列 */
const visibleColumns = computed(() => result.value.columns.filter(c => columnVisibility[c.field] !== false))

/** 从 result.columns 初始化 columnVisibility */
function initColumnVisibility() {
  result.value.columns.forEach(col => {
    if (columnVisibility[col.field] === undefined) {
      columnVisibility[col.field] = true
    }
  })
}

function onColumnVisibilityChange() {
  // 触发 computed 重新求值即可驱动 VTable 更新
}

/** 打开列设置对话框（初始化可见性+启动拖拽） */
function showColumnSettings() {
  initColumnVisibility()
  columnSettingsVisible.value = true
  nextTick(() => {
    initColumnSort()
  })
}

/** 初始化拖拽排序 */
function initColumnSort() {
  if (!columnSortRef.value) return
  if (sortableInstance) sortableInstance.destroy()
  sortableInstance = new Sortable(columnSortRef.value, {
    handle: '.column-drag-handle',
    animation: 150,
    onEnd: () => {
      // 根据拖拽后的 DOM 顺序更新 result.columns
      const items = columnSortRef.value!.querySelectorAll('.column-sort-item')
      const newOrder: string[] = []
      items.forEach(el => {
        const field = (el as HTMLElement).dataset.field
        if (field) newOrder.push(field)
      })
      result.value.columns.sort((a, b) => {
        const ai = newOrder.indexOf(a.field)
        const bi = newOrder.indexOf(b.field)
        return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi)
      })
    }
  })
}

// 组件卸载时清理
onUnmounted(() => {
  sortableInstance?.destroy()
})

// 监听 result.columns 变化，为新列设置默认可见性
watch(() => result.value.columns, () => {
  initColumnVisibility()
}, { deep: true })

// 存储过程报表不支持服务端分页，直接加载全量数据
const isStoredProcedure = computed(() => report.value?.queryType === 'proc')

// ===== VTable 配置 =====

/** VTable columns：将 result.columns 转为 VTable 列定义，同时处理可见性和排序 */
const vTableColumns = computed(() => {
  return visibleColumns.value.map(col => {
    const colDef: Record<string, unknown> = {
      field: col.field,
      title: col.title || col.field,
      width: col.width || 120,
      sort: true,
    }
    return colDef
  })
})

/** 当前排序状态（同步到 VTable） */
const vTableSortState = computed(() => {
  if (!currentSort.value.length) return undefined
  return {
    field: currentSort.value[0].field,
    order: currentSort.value[0].direction === 'ASC' ? 'asc' : ('desc' as 'asc' | 'desc')
  }
})

/** VTable options */
const vTableHeight = computed(() => {
  const count = result.value.rows.length
  return Math.min(600, Math.max(200, count * 36 + 40))
})

/** VTable records（作为独立 prop 传递，保证 records watch 正确触发） */
const vTableRecords = computed(() => result.value.rows)

const vTableOptions = computed(() => {
  return {
    columns: vTableColumns.value,
    records: vTableRecords.value,
    width: '100%',
    defaultColWidth: 120,
    sortState: vTableSortState.value,
    columnResize: true,
  }
})

/** VTable 错误回调 */
function handleVTableError(err: unknown) {
  console.error('[VTable Error]', err)
}

/** VTable 排序回调 */
function handleVTableSortChange(state: { field: string; order: 'asc' | 'desc' | 'normal' }) {
  if (state && state.field && state.order && state.order !== 'normal') {
    currentSort.value = [{ field: state.field, direction: state.order === 'asc' ? 'ASC' : 'DESC' }]
  } else {
    currentSort.value = []
  }
  currentPage.value = 1
  handleExecute()
}

async function handlePageChange(page: number) {
  await fetchPage(page, pageSize.value)
}

async function handleSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  await fetchPage(1, size)
}

async function fetchPage(page: number, size: number) {
  if (!report.value) return
  loading.value = true
  try {
    const params: Record<string, unknown> = {}
    Object.entries(filterValues.value).forEach(([k, v]) => {
      if (v !== null && v !== undefined && v !== '') params[k] = v
    })
    const req: any = {
      params,
      sort: currentSort.value.length ? currentSort.value : undefined
    }
    // 存储过程报表不发分页参数（后端不支持）
    if (!isStoredProcedure.value) {
      req.pagination = { page, pageSize: size }
    }
    const res = await reportApi.executeReport(report.value.id, req)
    result.value = res.data || { columns: [], rows: [] }
    executionInfo.value = res.data?.executionInfo || null
  } catch (e) {
    ElMessage.error('加载失败，请重试')
    console.warn('[ReportViewer] fetchPage 失败', e)
  } finally {
    loading.value = false
  }
}

const SESSION_KEY_PREFIX = 'report_viewer_params_'

const salespersonList = ref<{ code: string; name: string }[]>([])
const currentSalesperson = ref<{ isSalesperson: boolean; type?: string; salespersonCode?: string; salespersonName?: string } | null>(null)
const selectOptions = ref<Record<string, { value: string | null; label: string }[]>>({})

// 业务员（非主管）只能看到自己的选项，主管/非业务员能看到所有业务员
const isRestrictedSalesperson = computed(() =>
  currentSalesperson.value?.isSalesperson && currentSalesperson.value?.type !== 'supervisor'
)
const filteredSalespersonList = computed(() => {
  if (isRestrictedSalesperson.value && currentSalesperson.value?.salespersonCode) {
    return salespersonList.value.filter(sp => sp.code === currentSalesperson.value?.salespersonCode)
  }
  return salespersonList.value
})

// 日期范围快捷选项
const dateShortcuts = [
  {
    text: '本月',
    value: () => {
      const now = new Date()
      const start = new Date(now.getFullYear(), now.getMonth(), 1)
      const end = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59)
      return [start, end]
    }
  },
  {
    text: '上月',
    value: () => {
      const now = new Date()
      const start = new Date(now.getFullYear(), now.getMonth() - 1, 1)
      const end = new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59)
      return [start, end]
    }
  },
  {
    text: '本年',
    value: () => {
      const now = new Date()
      const start = new Date(now.getFullYear(), 0, 1)
      const end = new Date(now.getFullYear(), 11, 31, 23, 59, 59)
      return [start, end]
    }
  },
  {
    text: '今天',
    value: () => {
      const now = new Date()
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate())
      const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59)
      return [start, end]
    }
  },
  {
    text: '最近7天',
    value: () => {
      const now = new Date()
      const start = new Date(now.getTime() - 6 * 24 * 60 * 60 * 1000)
      const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59)
      return [start, end]
    }
  },
  {
    text: '最近30天',
    value: () => {
      const now = new Date()
      const start = new Date(now.getTime() - 29 * 24 * 60 * 60 * 1000)
      const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59)
      return [start, end]
    }
  }
]

onMounted(async () => {
  const id = route.params.id
  if (id) {
    // 并行加载报表详情、业务员信息和权限检查
    const [detailRes, salespersonRes, managePermRes, exportPermRes] = await Promise.allSettled([
      reportApi.getReportDetail(Number(id)),
      reportApi.getCurrentSalesperson(),
      reportApi.checkPermission(Number(id), 'manage'),
      reportApi.checkPermission(Number(id), 'export')
    ])
    if (detailRes.status === 'fulfilled') {
      report.value = detailRes.value.data || null
      // 使用报表配置的默认标签页
      if (report.value?.defaultTab) {
        activeTab.value = report.value.defaultTab
      }
    }
    if (salespersonRes.status === 'fulfilled') {
      currentSalesperson.value = salespersonRes.value.data || null
    }
    if (managePermRes.status === 'fulfilled') {
      const data = managePermRes.value.data as any
      canManage.value = data?.hasPermission === true
    }
    if (exportPermRes.status === 'fulfilled') {
      const data = exportPermRes.value.data as any
      canExport.value = data?.hasPermission === true
    }

    // 加载业务员列表（从业务员映射表获取）
    const spListRes = await erpSettingsApi.getSalespersons()
    if (spListRes.data) salespersonList.value = spListRes.data

    // 加载 select 类型的动态选项（dataUrl）
    if (report.value?.filters) {
      const urlFilters = report.value.filters.filter(f => f.controlType === 'select' && f.dataUrl)
      await Promise.all(urlFilters.map(async (f) => {
        if (!f.dataUrl) return
        try {
          const res = await request.get(f.dataUrl)
          const data = res.data
          // 支持数组格式或 {data: [...]} 格式
          const list = Array.isArray(data) ? data : data.data || data
          selectOptions.value[f.fieldName] = list
        } catch (e) {
          console.warn(`加载下拉选项失败: ${f.fieldName}`, e)
          selectOptions.value[f.fieldName] = []
        }
      }))
    }

    // select 类型过滤器如果没有自己的选项，从 salespersonList 回退（如客户经理下拉）
    if (report.value?.filters && salespersonList.value.length > 0) {
      const spList = isRestrictedSalesperson.value
        ? salespersonList.value.filter(sp => sp.code === currentSalesperson.value?.salespersonCode)
        : salespersonList.value
      report.value.filters
        .filter(f => f.controlType === 'select' && (!selectOptions.value[f.fieldName] || selectOptions.value[f.fieldName].length === 0))
        .forEach(f => {
          const opts = spList.map(sp => ({ value: sp.code, label: sp.name }))
          selectOptions.value[f.fieldName] = isRestrictedSalesperson.value ? opts : [{ value: '', label: '全部' }, ...opts]
        })
    }

    // 初始化过滤默认值：缓存 > 系统默认值
    if (report.value?.filters) {
      const cached = sessionStorage.getItem(SESSION_KEY_PREFIX + id)
      const cachedParams: Record<string, any> = cached ? JSON.parse(cached) : {}
      report.value.filters.forEach(f => {
        if ((f.controlType === 'salesperson' || f.controlType === 'select') && isRestrictedSalesperson.value) {
          // 业务员（非主管）强制使用自己的编码
          filterValues.value[f.fieldName] = currentSalesperson.value?.salespersonCode
        } else {
          filterValues.value[f.fieldName] = cachedParams[f.fieldName] ?? f.defaultValue ?? undefined
        }
      })
    }
    handleExecute()
  }
})

watch(() => result.value.rows, () => {
  nextTick(() => renderCharts())
})

// 切换到透视表标签时，加载全量数据
watch(activeTab, async (tab) => {
  if (tab !== 'pivot' || !report.value) return
  // 存储过程报表：数据已在 result.rows 中，无需重新请求
  if (isStoredProcedure.value) {
    pivotRows.value = result.value.rows
    return
  }
  // SQL报表：如果数据量超过当前已加载的，请求全量
  if (result.value.pagination && result.value.pagination.total > pivotRows.value.length) {
    await fetchPivotAllData()
  }
})

async function fetchPivotAllData() {
  if (!report.value) return
  pivotLoading.value = true
  try {
    const params: Record<string, unknown> = {}
    Object.entries(filterValues.value).forEach(([k, v]) => {
      if (v !== null && v !== undefined && v !== '') params[k] = v
    })
    const res = await reportApi.executeReport(report.value.id, {
      params,
      sort: currentSort.value.length ? currentSort.value : undefined
    })
    pivotRows.value = res.data?.rows || []
  } catch (e) {
    ElMessage.error('透视表数据加载失败')
    console.warn('[ReportViewer] fetchPivotAllData 失败', e)
  } finally {
    pivotLoading.value = false
  }
}

async function handleExecute() {
  if (!report.value) return
  loading.value = true
  totalLoadTime.value = 0
  const startTime = Date.now()
  try {
    const params: Record<string, unknown> = {}
    Object.entries(filterValues.value).forEach(([k, v]) => {
      if (v !== null && v !== undefined && v !== '') params[k] = v
    })
    const req: any = {
      params,
      sort: currentSort.value.length ? currentSort.value : undefined
    }
    // SQL报表发送分页，存储过程报表不发
    if (!isStoredProcedure.value) {
      req.pagination = { page: 1, pageSize: pageSize.value }
    }
    const res = await reportApi.executeReport(report.value.id, req)
    result.value = res.data || { columns: [], rows: [] }
    executionInfo.value = res.data?.executionInfo || null
    // 存储过程报表：透视表直接复用数据，无需清空
    if (!isStoredProcedure.value) {
      pivotRows.value = []
    }
    currentPage.value = 1
    // 缓存本次查询参数
    sessionStorage.setItem(SESSION_KEY_PREFIX + report.value.id, JSON.stringify(params))
  } catch (e) {
    result.value = { columns: [], rows: [] }
    ElMessage.error('查询失败，请重试')
    console.warn('[ReportViewer] handleExecute 失败', e)
  } finally {
    loading.value = false
    // 等多帧渲染完成后计算总耗时
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        totalLoadTime.value = Date.now() - startTime
      })
    })
  }
}

function handleReset() {
  filterValues.value = {}
  if (report.value) sessionStorage.removeItem(SESSION_KEY_PREFIX + report.value.id)
  currentPage.value = 1
  currentSort.value = []
  handleExecute()
}

function handleBack() {
  router.push({ name: 'ReportList' })
}

function handleEdit() {
  if (report.value) router.push({ name: 'ReportDesigner', params: { id: report.value.id } })
}

async function handleExport(format: string) {
  if (!report.value) return
  try {
    // 过滤空值参数，与查询时保持一致
    const params: Record<string, unknown> = {}
    Object.entries(filterValues.value).forEach(([k, v]) => {
      if (v !== null && v !== undefined && v !== '') params[k] = v
    })
    const blob: Blob = await reportApi.exportReport(report.value.id, format, params) as any
    // 创建下载链接
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${report.value.name}.${format}`
    a.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (e) {
    ElMessage.error('导出失败')
    console.warn('[ReportViewer] handleExport 失败', e)
  }
}

function renderCharts() {
  if (!report.value?.charts) return
  report.value.charts.forEach(chart => {
    const el = document.getElementById('chart-' + chart.title)
    if (!el) return
    // 复用已有 ECharts 实例，避免重复创建/销毁
    const myChart = echarts.getInstanceByDom(el) || echarts.init(el)
    const option: Record<string, unknown> = {
      title: { text: chart.title, left: 'center' },
      tooltip: { trigger: 'axis' }
    }
    const dataField = chart.dataField
    const categoryField = chart.categoryField
    const categories = result.value.rows.map(r => r[categoryField])
    const values = result.value.rows.map(r => Number(r[dataField]) || 0)

    if (chart.chartType === 'pie') {
      option.series = [{
        type: 'pie',
        data: categories.map((name, i) => ({ name, value: values[i] })),
        radius: '50%'
      }]
    } else {
      option.xAxis = { type: 'category', data: categories }
      option.yAxis = { type: 'value' }
      option.series = [{
        type: chart.chartType,
        data: values,
        smooth: chart.chartType === 'line'
      }]
    }
    myChart.setOption(option as echarts.EChartsOption)
  })
}

// ===== 数据表视图格式管理（类似透视表样式保存/分享） =====

/** 当前数据表格式对应 viewParams JSON */
function buildViewParams(): string {
  return JSON.stringify({
    columns: result.value.columns.map((col, i) => ({
      field: col.field,
      title: col.title,
      width: col.width || 120,
      align: col.align || 'left',
      order: i,
      visible: columnVisibility[col.field] !== false
    }))
  })
}

/** 当前激活的视图名称 */
const activeViewName = computed(() => {
  if (!report.value?.id) return ''
  const lastView = tableViews.value.find(v => v.isLast)
  return lastView ? lastView.viewName : ''
})

/** 格式工具栏命令处理 */
function handleTableViewCommand(cmd: string) {
  switch (cmd) {
    case 'save': showSaveTableView(); break
    case 'load': showLoadTableView(); break
    case 'share': showTableViewShare(selectedViewId.value); break
  }
}

const tableViews = ref<ReportTableViewDto[]>([])
const tableViewDialogVisible = ref(false)
const tableViewLoadVisible = ref(false)
const tableViewShareVisible = ref(false)
const tableViewSaveLoading = ref(false)
const editingView = ref<SaveTableViewRequest>({ reportId: 0, viewName: '', viewParams: '{}', isDefault: false, isLast: false })
const selectedViewId = ref<number>(0)
const currentShares = ref<TableViewShareDto[]>([])
const shareTargetType = ref('user')
const shareTargetId = ref<number>(0)
const shareLoading = ref(false)

/** 加载数据表视图列表 */
async function loadTableViews() {
  if (!report.value?.id) return
  try {
    const res = await reportApi.getTableViews(report.value.id)
    tableViews.value = res.data || []
    // 自动应用 isLast 的视图
    const lastView = tableViews.value.find(v => v.isLast)
    if (lastView) {
      applyViewParams(lastView.viewParams)
    }
  } catch (e) {
    console.warn('[ReportViewer] 加载数据表视图失败', e)
  }
}

/** 应用视图参数到当前列显示 */
function applyViewParams(viewParams: string) {
  if (!viewParams || viewParams === '{}') return
  try {
    const config = JSON.parse(viewParams)
    if (config.columns && Array.isArray(config.columns)) {
      // 应用可见性，然后宽度、对齐
      config.columns.forEach((saved: any) => {
        const found = result.value.columns.find((c: any) => c.field === saved.field)
        if (found) {
          found.width = saved.width
          found.align = saved.align
          // 应用可见性
          columnVisibility[saved.field] = saved.visible !== false
        }
      })
      // 按保存的顺序排列
      const orderedFields = config.columns.map((c: any) => c.field)
      result.value.columns.sort((a: any, b: any) => {
        const ai = orderedFields.indexOf(a.field)
        const bi = orderedFields.indexOf(b.field)
        return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi)
      })
    }
  } catch (e) {
    console.warn('[ReportViewer] 应用视图参数解析错误', e)
  }
}

/** 打开保存格式对话框 */
function showSaveTableView() {
  if (!report.value) return
  editingView.value = {
    reportId: report.value.id,
    viewName: '',
    viewParams: buildViewParams(),
    isDefault: false,
    isLast: false
  }
  tableViewDialogVisible.value = true
}

/** 保存数据表视图 */
async function handleSaveTableView() {
  if (!editingView.value.viewName.trim()) {
    ElMessage.warning('请输入视图名称')
    return
  }
  tableViewSaveLoading.value = true
  try {
    editingView.value.viewParams = buildViewParams()
    editingView.value.creatorName = authStore.employeeName || authStore.username
    await reportApi.saveTableView(editingView.value)
    ElMessage.success('格式保存成功')
    tableViewDialogVisible.value = false
    await loadTableViews()
    // 保存后自动选中刚保存的视图，使"共享格式"按钮可用
    const saved = tableViews.value.find(v => v.viewName === editingView.value.viewName && v.isLast)
    if (saved) selectedViewId.value = saved.id
  } catch (e) {
    ElMessage.error('保存失败')
    console.warn('[ReportViewer] handleSaveTableView 失败', e)
  } finally {
    tableViewSaveLoading.value = false
  }
}

/** 打开加载格式对话框 */
async function showLoadTableView() {
  if (!report.value) return
  await loadTableViews()
  selectedViewId.value = 0
  tableViewLoadVisible.value = true
}

/** 选择并应用一个视图 */
async function applySelectedView() {
  if (!selectedViewId.value) {
    ElMessage.warning('请选择一个格式')
    return
  }
  const view = tableViews.value.find(v => v.id === selectedViewId.value)
  if (!view) return
  applyViewParams(view.viewParams)
  // 标记为 isLast
  try {
    await reportApi.saveTableView({
      id: view.id,
      reportId: view.reportId,
      viewName: view.viewName,
      viewParams: view.viewParams,
      isDefault: false,
      isLast: true
    })
  } catch (e) {
    console.warn('[ReportViewer] 标记 isLast 失败', e)
  }
  tableViewLoadVisible.value = false
  ElMessage.success(`已应用格式: ${view.viewName}`)
}

/** 删除视图 */
async function handleDeleteTableView(id: number) {
  try {
    await reportApi.deleteTableView(id)
    ElMessage.success('已删除')
    await loadTableViews()
  } catch (e) {
    ElMessage.error('删除失败')
    console.warn('[ReportViewer] handleDeleteTableView 失败', e)
  }
}

/** 打开共享管理对话框 */
async function showTableViewShare(viewId: number) {
  selectedViewId.value = viewId
  await loadTableViewShares(viewId)
  tableViewShareVisible.value = true
}

/** 加载共享目标列表 */
async function loadTableViewShares(viewId: number) {
  try {
    const res = await reportApi.getTableViewShares(viewId)
    currentShares.value = res.data || []
  } catch (e) {
    currentShares.value = []
    console.warn('[ReportViewer] loadTableViewShares 失败', e)
  }
}

/** 添加共享目标 */
async function handleAddShare() {
  if (!shareTargetId.value) {
    ElMessage.warning('请选择共享目标')
    return
  }
  shareLoading.value = true
  try {
    await reportApi.addTableViewShare(selectedViewId.value, {
      targetType: shareTargetType.value,
      targetId: shareTargetId.value
    })
    ElMessage.success('已共享')
    shareTargetId.value = 0
    await loadTableViewShares(selectedViewId.value)
  } catch (e) {
    ElMessage.error('共享失败')
    console.warn('[ReportViewer] handleAddShare 失败', e)
  } finally {
    shareLoading.value = false
  }
}

/** 移除共享目标 */
async function handleRemoveShare(shareId: number) {
  try {
    await reportApi.removeTableViewShare(selectedViewId.value, shareId)
    ElMessage.success('已取消共享')
    await loadTableViewShares(selectedViewId.value)
  } catch (e) {
    ElMessage.error('取消共享失败')
    console.warn('[ReportViewer] handleRemoveShare 失败', e)
  }
}

</script>

<style scoped>
.filter-card {
  margin-bottom: 12px;
}
.execution-bar {
  margin-bottom: 8px;
  font-size: 13px;
  color: #909399;
}
.summary-row {
  padding: 12px 16px;
  background: #f5f7fa;
  border-top: 1px solid #ebeef5;
  font-size: 13px;
}
.summary-item {
  margin-right: 16px;
}
.pagination-row {
  display: flex;
  justify-content: flex-end;
  padding: 12px 0;
}
.chart-area {
  margin-top: 16px;
}
.pivot-card {
  margin-top: 16px;
}

/* 数据表格式工具栏 */
.table-view-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}
.active-view-name {
  font-size: 13px;
  color: #909399;
}
.form-desc {
  font-size: 13px;
  color: #909399;
}
.empty-hint {
  text-align: center;
  color: #909399;
  padding: 24px 0;
  font-size: 14px;
}
.table-view-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.table-view-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #ebeef5;
  border-radius: 6px;
  transition: border-color 0.2s;
}
.table-view-item:hover {
  border-color: #409eff;
}
.view-item-content {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.view-name {
  font-weight: 500;
  font-size: 14px;
}
.view-meta {
  font-size: 12px;
  color: #909399;
}
.share-section {
  min-height: 200px;
}
.share-form-row {
  display: flex;
  gap: 8px;
  align-items: center;
}

/* 列设置对话框 */
.column-settings-body {
  min-height: 200px;
}
.column-settings-hint {
  font-size: 13px;
  color: #909399;
  margin-bottom: 12px;
}
.column-sort-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.column-sort-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border: 1px solid #ebeef5;
  border-radius: 6px;
  background: #fff;
  transition: border-color 0.2s, box-shadow 0.2s;
  cursor: default;
}
.column-sort-item:hover {
  border-color: #409eff;
  box-shadow: 0 1px 4px rgba(64, 158, 255, 0.12);
}
.column-drag-handle {
  cursor: grab;
  color: #c0c4cc;
  font-size: 16px;
  user-select: none;
  line-height: 1;
}
.column-drag-handle:active {
  cursor: grabbing;
}
.column-name {
  font-weight: 500;
  font-size: 14px;
  flex: 1;
}
.column-field-name {
  font-size: 12px;
  color: #c0c4cc;
}
</style>