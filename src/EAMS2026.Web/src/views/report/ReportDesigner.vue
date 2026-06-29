<template>
  <div>
    <div class="page-header">
      <h3>{{ isEdit ? '编辑报表' : '新建报表' }}</h3>
      <div>
        <el-button @click="handleBack">返回</el-button>
        <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
        <el-button v-if="isEdit" @click="handleTest">测试查询</el-button>
      </div>
    </div>
    <el-card>
      <el-tabs v-model="activeTab">
        <el-tab-pane label="基本信息" name="basic">
          <el-form :model="form" label-width="120px" class="form-compact">
            <el-row :gutter="24">
              <el-col :span="12">
                <el-form-item label="报表名称" required>
                  <el-input v-model="form.name" placeholder="英文标识" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="报表标题" required>
                  <el-input v-model="form.title" placeholder="显示名称" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item label="描述">
              <el-input v-model="form.description" type="textarea" :rows="2" />
            </el-form-item>
            <el-row :gutter="24">
              <el-col :span="8">
                <el-form-item label="分类">
                  <el-tree-select
                    v-model="form.categoryId"
                    :data="categories"
                    :props="{ label: 'name', value: 'id' }"
                    placeholder="请选择"
                    clearable
                    check-strictly
                  />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="查询类型">
                  <el-select v-model="form.queryType">
                    <el-option value="sql" label="SQL语句" />
                    <el-option value="proc" label="存储过程" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="数据源">
                  <div style="display:flex;gap:4px;width:100%">
                    <el-select v-model="form.queryDatasource" style="flex:1">
                      <el-option
                        v-for="ds in dataSources"
                        :key="ds.name"
                        :value="ds.name"
                        :label="ds.displayName"
                      />
                    </el-select>
                    <el-button size="small" @click="openAddDataSource">+</el-button>
                  </div>
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="16">
              <el-col :span="8">
                <el-form-item label="默认标签页">
                  <el-select v-model="form.defaultTab">
                    <el-option value="table" label="数据表" />
                    <el-option value="pivot" label="透视表" />
                  </el-select>
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item label="SQL语句">
              <el-input
                v-model="form.queryText"
                type="textarea"
                :rows="8"
                placeholder="SELECT * FROM table WHERE 1=1 /*FILTER*/"
                font-family="monospace"
              />
            </el-form-item>
            <el-form-item label="是否发布">
              <el-switch
                v-model="form.status"
                :active-value="'published'"
                :inactive-value="'draft'"
                active-text="已发布"
                inactive-text="未发布"
              />
              <span style="margin-left:8px;color:#999;font-size:12px">未发布的报表仅管理员可见可查</span>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <el-tab-pane label="字段配置" name="fields">
          <div class="toolbar">
            <el-button size="small" @click="addField">添加字段</el-button>
            <el-button size="small" @click="autoDetectFields">自动检测</el-button>
          </div>
          <el-table :data="form.fields" border size="small">
            <el-table-column label="字段名" width="150">
              <template #default="{ row }">
                <el-input v-model="row.fieldName" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="显示标题" width="150">
              <template #default="{ row }">
                <el-input v-model="row.fieldTitle" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="类型" width="100">
              <template #default="{ row }">
                <el-select v-model="row.fieldType" size="small">
                  <el-option value="string" label="文本" />
                  <el-option value="number" label="数字" />
                  <el-option value="date" label="日期" />
                  <el-option value="boolean" label="布尔" />
                  <el-option value="money" label="金额" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="宽度" width="80">
              <template #default="{ row }">
                <el-input-number v-model="row.width" :min="0" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="对齐" width="80">
              <template #default="{ row }">
                <el-select v-model="row.align" size="small">
                  <el-option value="left" label="左" />
                  <el-option value="center" label="中" />
                  <el-option value="right" label="右" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="可排序" width="70" align="center">
              <template #default="{ row }">
                <el-checkbox v-model="row.isSortable" />
              </template>
            </el-table-column>
            <el-table-column label="可过滤" width="70" align="center">
              <template #default="{ row }">
                <el-checkbox v-model="row.isFilterable" />
              </template>
            </el-table-column>
            <el-table-column label="汇总" width="70" align="center">
              <template #default="{ row }">
                <el-checkbox v-model="row.isSummary" />
              </template>
            </el-table-column>
            <el-table-column label="格式" width="120">
              <template #default="{ row }">
                <el-input v-model="row.formatPattern" size="small" placeholder="{0:N2}" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="60" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="danger" @click="removeField(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="过滤参数" name="filters">
          <div class="toolbar">
            <el-button size="small" @click="addFilter">添加过滤</el-button>
          </div>
          <el-table :data="form.filters" border size="small">
            <el-table-column label="字段名" width="150">
              <template #default="{ row }">
                <el-input v-model="row.fieldName" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="参数标题" width="150">
              <template #default="{ row }">
                <el-input v-model="row.label" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="操作符" width="110">
              <template #default="{ row }">
                <el-select v-model="row.operator" size="small">
                  <el-option value="eq" label="=" />
                  <el-option value="ne" label="!=" />
                  <el-option value="gt" label=">" />
                  <el-option value="ge" label=">=" />
                  <el-option value="lt" label="<" />
                  <el-option value="le" label="<=" />
                  <el-option value="like" label="LIKE" />
                  <el-option value="between" label="BETWEEN" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="控件类型" width="120">
              <template #default="{ row }">
                <el-select v-model="row.controlType" size="small">
                  <el-option value="input" label="文本框" />
                  <el-option value="select" label="下拉框" />
                  <el-option value="date" label="日期" />
                  <el-option value="daterange" label="日期范围" />
                  <el-option value="salesperson" label="业务员" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="默认值" width="120">
              <template #default="{ row }">
                <el-input v-model="row.defaultValue" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="排序" width="70">
              <template #default="{ row }">
                <el-input-number v-model="row.sortOrder" size="small" :min="0" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="60" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="danger" @click="removeFilter(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="排序配置" name="sorts">
          <div class="toolbar">
            <el-button size="small" @click="addSort">添加排序</el-button>
          </div>
          <el-table :data="form.sorts" border size="small">
            <el-table-column label="字段名" width="200">
              <template #default="{ row }">
                <el-input v-model="row.fieldName" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="方向" width="120">
              <template #default="{ row }">
                <el-select v-model="row.direction" size="small">
                  <el-option value="ASC" label="升序" />
                  <el-option value="DESC" label="降序" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="排序" width="100">
              <template #default="{ row }">
                <el-input-number v-model="row.sortOrder" size="small" :min="0" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="60" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="danger" @click="removeSort(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="图表配置" name="charts">
          <div class="toolbar">
            <el-button size="small" @click="addChart">添加图表</el-button>
          </div>
          <el-table :data="form.charts" border size="small">
            <el-table-column label="标题" width="160">
              <template #default="{ row }">
                <el-input v-model="row.title" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="类型" width="120">
              <template #default="{ row }">
                <el-select v-model="row.chartType" size="small">
                  <el-option value="bar" label="柱状图" />
                  <el-option value="line" label="折线图" />
                  <el-option value="pie" label="饼图" />
                  <el-option value="scatter" label="散点图" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="数据字段" width="120">
              <template #default="{ row }">
                <el-input v-model="row.dataField" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="分类字段" width="120">
              <template #default="{ row }">
                <el-input v-model="row.categoryField" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="宽" width="80">
              <template #default="{ row }">
                <el-input-number v-model="row.width" size="small" :min="200" :step="100" />
              </template>
            </el-table-column>
            <el-table-column label="高" width="80">
              <template #default="{ row }">
                <el-input-number v-model="row.height" size="small" :min="200" :step="100" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="60" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="danger" @click="removeChart(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 测试结果对话框 -->
    <el-dialog v-model="testDialogVisible" title="测试查询" width="80%" top="5vh">
      <!-- 参数输入区 -->
      <el-card v-if="form.filters?.length" class="filter-card" style="margin-bottom:12px">
        <el-form :model="testParams" inline>
          <el-form-item v-for="f in form.filters" :key="f.fieldName" :label="f.label || f.fieldName">
            <el-input v-if="f.controlType === 'input' || !f.controlType" v-model="testParams[f.fieldName]" :placeholder="f.defaultValue || ''" />
            <el-select v-else-if="f.controlType === 'select'" v-model="testParams[f.fieldName]" clearable placeholder="全部" style="width:160px">
              <el-option v-for="opt in testSelectOptions[f.fieldName] || []" :key="opt.value" :label="opt.label" :value="opt.value" />
            </el-select>
            <el-date-picker v-else-if="f.controlType === 'date'" v-model="testParams[f.fieldName]" type="daterange" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="dateShortcuts" />
            <el-date-picker v-else-if="f.controlType === 'daterange'" v-model="testParams[f.fieldName]" type="daterange" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="dateShortcuts" />
            <el-input-number v-else-if="f.controlType === 'number'" v-model="testParams[f.fieldName]" />
            <el-select v-else-if="f.controlType === 'salesperson'" v-model="testParams[f.fieldName]" clearable placeholder="选择业务员" style="width:160px" :disabled="testIsRestrictedSalesperson">
              <el-option v-for="sp in filteredTestSalespersonList" :key="sp.code" :label="sp.name" :value="sp.code" />
            </el-select>
            <el-input v-else v-model="testParams[f.fieldName]" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleTestRun" :loading="testLoading">查询</el-button>
            <el-button @click="handleResetTestParams">重置</el-button>
          </el-form-item>
        </el-form>
      </el-card>
      <div v-if="testResult.executionInfo" class="execution-info">
        耗时: {{ testResult.executionInfo.durationMs }}ms | 行数: {{ testResult.executionInfo.rowCount }}
      </div>
      <el-tabs v-if="testResult.rows.length" v-model="testActiveTab">
        <el-tab-pane label="数据表" name="table">
          <el-table :data="paginatedTestRows" border max-height="500" v-loading="testLoading" v-if="testResult.columns"
            @sort-change="handleTestSortChange">
            <el-table-column v-for="col in testResult.columns" :key="col.field"
              :prop="col.field" :label="col.title" :width="col.width || 120" sortable="custom">
              <template #default="{ row }">
                <template v-if="col.type === 'money'">
                  {{ formatMoney(row[col.field]) }}
                </template>
                <template v-else-if="col.type === 'date'">
                  {{ formatDateTimeSafe(row[col.field]) }}
                </template>
                <template v-else>
                  {{ row[col.field] }}
                </template>
              </template>
            </el-table-column>
          </el-table>

          <!-- 测试结果汇总行 -->
          <div v-if="testResult.summary && Object.keys(testResult.summary).length" class="summary-row">
            <strong>汇总: </strong>
            <span v-for="(val, key) in testResult.summary" :key="key" class="summary-item">
              {{ key }}: {{ formatSummaryValue(val) }}
            </span>
          </div>

          <div class="pagination-wrapper" v-if="testResult.rows.length > 0">
            <el-pagination
              v-model:current-page="testCurrentPage"
              v-model:page-size="testPageSize"
              :total="testResult.rows.length"
              :page-sizes="[20, 50, 100, 200]"
              layout="total, sizes, prev, pager, next"
            />
          </div>
        </el-tab-pane>
        <el-tab-pane label="透视表" name="pivot">
          <PivotTable
            :report-id="form.id || 0"
            :columns="testResult.columns"
            :rows="testResult.rows"
          />
        </el-tab-pane>
      </el-tabs>
      <el-empty v-else-if="!testLoading" description="暂无数据" />
      <template #footer>
        <el-button @click="testDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 数据源管理对话框 -->
    <DataSourceDialog
      :visible="datasourceDialogVisible"
      :data="editingDataSource"
      @update:visible="datasourceDialogVisible = $event"
      @saved="onDataSourceSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { reportApi, type ReportCategory, type ReportDetailDto, type ReportExecuteResult } from '@/api/report'
import request from '@/utils/request'
import { ElMessage } from 'element-plus'
import DataSourceDialog from './components/DataSourceDialog.vue'
import PivotTable from './components/PivotTable.vue'
import { formatDateTimeSafe, formatMoney, formatSummaryValue } from '@/utils/date'

const route = useRoute()
const router = useRouter()
const isEdit = ref(false)
const saving = ref(false)
const activeTab = ref('basic')
const categories = ref<ReportCategory[]>([])
const dataSources = ref<any[]>([])
const datasourceDialogVisible = ref(false)
const editingDataSource = ref<any>(null)

const form = ref<ReportDetailDto>({
  id: 0,
  name: '',
  title: '',
  description: null,
  categoryId: null,
  categoryName: null,
  queryType: 'sql',
  queryText: '',
  queryDatasource: 'main',
  defaultTab: 'table',
  isSystem: false,
  status: 'draft',
  fields: [],
  filters: [],
  sorts: [],
  charts: [],
  createdAt: '',
  updatedAt: ''
})

const testDialogVisible = ref(false)
const testResult = ref<ReportExecuteResult>({ columns: [], rows: [] })
const testLoading = ref(false)
const testParams = ref<Record<string, any>>({})
const testActiveTab = ref('table')
const testSelectOptions = ref<Record<string, any[]>>({})
const testSalespersonList = ref<any[]>([])
const testCurrentSalesperson = ref<any>(null)

// 业务员（非主管）只能看到自己的选项，主管/非业务员能看到所有业务员
const testIsRestrictedSalesperson = computed(() =>
  testCurrentSalesperson.value?.isSalesperson && testCurrentSalesperson.value?.type !== 'supervisor'
)
const filteredTestSalespersonList = computed(() => {
  if (testIsRestrictedSalesperson.value && testCurrentSalesperson.value?.salespersonCode) {
    return testSalespersonList.value.filter((sp: any) => sp.code === testCurrentSalesperson.value?.salespersonCode)
  }
  return testSalespersonList.value
})

// 测试结果分页 + 排序
const testCurrentPage = ref(1)
const testPageSize = ref(50)
let testSortState: { prop: string; order: 'ascending' | 'descending' } | null = null
const paginatedTestRows = computed(() => {
  let list = [...testResult.value.rows]
  // 排序
  if (testSortState) {
    const { prop, order } = testSortState
    const asc = order === 'ascending'
    list.sort((a: any, b: any) => {
      const va = a[prop]
      const vb = b[prop]
      if (va == null) return asc ? -1 : 1
      if (vb == null) return asc ? 1 : -1
      if (typeof va === 'string' && typeof vb === 'string') {
        return asc ? va.localeCompare(vb, 'zh-CN') : vb.localeCompare(va, 'zh-CN')
      }
      return asc ? (va > vb ? 1 : -1) : (va < vb ? 1 : -1)
    })
  }
  // 分页
  const start = (testCurrentPage.value - 1) * testPageSize.value
  return list.slice(start, start + testPageSize.value)
})

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
      const y = now.getFullYear()
      const m = now.getMonth()
      const start = new Date(y, m - 1, 1)
      const end = new Date(y, m, 0, 23, 59, 59)
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
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0)
      const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59)
      return [start, end]
    }
  }
]

const SESSION_KEY_PREFIX = 'report_test_params_'

onMounted(async () => {
  await Promise.all([loadCategories(), loadDataSources()])
  const id = route.params.id
  if (id) {
    isEdit.value = true
    const res = await reportApi.getReportDetail(Number(id))
    if (res.data) form.value = res.data
  }
})

watch(() => form.value.filters, () => {
  if (testDialogVisible.value) initTestParams()
}, { deep: true })

async function loadTestOptions() {
  if (!form.value.filters?.length) return
  // 加载业务员列表
  const ds = form.value.queryDatasource || undefined
  const [spListRes, spCurrentRes] = await Promise.allSettled([
    reportApi.getSalespersons(ds),
    reportApi.getCurrentSalesperson()
  ])
  if (spListRes.status === 'fulfilled' && spListRes.value.data) {
    testSalespersonList.value = spListRes.value.data
  }
  if (spCurrentRes.status === 'fulfilled' && spCurrentRes.value.data) {
    testCurrentSalesperson.value = spCurrentRes.value.data
  }

  // 加载 select 类型的动态选项（dataUrl）
  const urlFilters = form.value.filters.filter(f => f.controlType === 'select' && f.dataUrl)
  await Promise.all(urlFilters.map(async (f) => {
    if (!f.dataUrl) return
    try {
      const res = await request.get(f.dataUrl)
      const data = res.data
      const list = Array.isArray(data) ? data : data.data || data
      testSelectOptions.value[f.fieldName] = list
    } catch (e) {
      console.warn(`加载下拉选项失败: ${f.fieldName}`, e)
      testSelectOptions.value[f.fieldName] = []
    }
  }))

  // select 类型过滤器如果没有自己的选项，从 salespersonList 回退
  if (testSalespersonList.value.length > 0) {
    const spList = testIsRestrictedSalesperson.value
      ? testSalespersonList.value.filter((sp: any) => sp.code === testCurrentSalesperson.value?.salespersonCode)
      : testSalespersonList.value
    form.value.filters
      .filter(f => f.controlType === 'select' && (!testSelectOptions.value[f.fieldName] || testSelectOptions.value[f.fieldName].length === 0))
      .forEach(f => {
        const opts = spList.map((sp: any) => ({ value: sp.code, label: sp.name }))
        testSelectOptions.value[f.fieldName] = testIsRestrictedSalesperson.value ? opts : [{ value: null, label: '全部' }, ...opts]
      })
  }
}

function initTestParams() {
  if (!form.value.filters?.length) return
  const cached = sessionStorage.getItem(SESSION_KEY_PREFIX + (form.value.id || 'new'))
  const cachedParams: Record<string, any> = cached ? JSON.parse(cached) : {}
  const params: Record<string, any> = {}
  form.value.filters.forEach(f => {
    const key = f.fieldName
    if ((f.controlType === 'salesperson' || f.controlType === 'select') && testIsRestrictedSalesperson.value) {
      // 业务员（非主管）默认选中自己
      params[key] = testCurrentSalesperson.value?.salespersonCode
    } else if (cachedParams[key] !== undefined && cachedParams[key] !== null && cachedParams[key] !== '') {
      params[key] = cachedParams[key]
    } else if (f.defaultValue) {
      params[key] = f.defaultValue
    } else {
      params[key] = undefined
    }
  })
  testParams.value = params
}

function cacheTestParams() {
  const key = SESSION_KEY_PREFIX + (form.value.id || 'new')
  const toCache: Record<string, any> = {}
  Object.entries(testParams.value).forEach(([k, v]) => {
    if (v !== null && v !== undefined && v !== '') toCache[k] = v
  })
  sessionStorage.setItem(key, JSON.stringify(toCache))
}

async function loadCategories() {
  const res = await reportApi.getCategories()
  categories.value = res.data || []
}

async function loadDataSources() {
  const res = await reportApi.getDataSources()
  dataSources.value = (res.data || []).filter((ds: any) => ds.isEnabled)
}

function openAddDataSource() {
  editingDataSource.value = null
  datasourceDialogVisible.value = true
}

function openEditDataSource(ds: any) {
  editingDataSource.value = ds
  datasourceDialogVisible.value = true
}

async function handleDeleteDataSource(ds: any) {
  await reportApi.deleteDataSource(ds.id)
  ElMessage.success('删除成功')
  await loadDataSources()
}

function onDataSourceSaved() {
  loadDataSources()
}

function handleBack() {
  router.push({ name: 'ReportList' })
}

async function handleSave() {
  if (!form.value.name || !form.value.title) {
    ElMessage.warning('请填写报表名称和标题')
    return
  }
  saving.value = true
  try {
    // 将 isSummary 映射为 summaryType（数字/金额列求和，其他列计数）
    const payload = { ...form.value }
    if (payload.fields) {
      payload.fields = payload.fields.map(f => ({
        ...f,
        summaryType: f.isSummary
          ? (f.fieldType === 'number' || f.fieldType === 'money' ? 'sum' : 'count')
          : ''
      }))
    }
    if (isEdit.value) {
      await reportApi.updateReport(form.value.id, payload)
      ElMessage.success('保存成功')
    } else {
      const res = await reportApi.addReport(payload)
      ElMessage.success('创建成功')
      form.value.id = res.data?.id || 0
      isEdit.value = true
      router.replace({ name: 'ReportDesigner', params: { id: form.value.id } })
    }
  } finally {
    saving.value = false
  }
}

async function handleTest() {
  testDialogVisible.value = true
  testSelectOptions.value = {}
  testSalespersonList.value = []
  testCurrentSalesperson.value = null
  await loadTestOptions()
  initTestParams()
  // 如果没有过滤条件，自动执行一次查询
  if (!form.value.filters?.length) {
    await handleTestRun()
  }
}

async function handleTestRun() {
  testLoading.value = true
  testResult.value = { columns: [], rows: [] }
  testCurrentPage.value = 1
  testSortState = null
  try {
    const params: Record<string, any> = {}
    Object.entries(testParams.value).forEach(([k, v]) => {
      if (v !== null && v !== undefined && v !== '') params[k] = v
    })
    const res = await reportApi.previewReport({
      queryText: form.value.queryText,
      queryType: form.value.queryType,
      queryDatasource: form.value.queryDatasource,
      params,
      filters: form.value.filters?.map(f => ({
        fieldName: f.fieldName,
        label: f.label,
        operator: f.operator,
        defaultValue: f.defaultValue,
        controlType: f.controlType
      })),
      fields: form.value.fields?.map(f => ({
        fieldName: f.fieldName,
        fieldTitle: f.fieldTitle,
        width: f.width,
        align: f.align,
        isSortable: f.isSortable,
        summaryType: f.isSummary
          ? (f.fieldType === 'number' || f.fieldType === 'money' ? 'sum' : 'count')
          : '',
        formatPattern: f.formatPattern
      })),
      pageSize: 50
    })
    testResult.value = res.data || { columns: [], rows: [] }
    cacheTestParams()
  } catch {
    testResult.value = { columns: [], rows: [] }
  } finally {
    testLoading.value = false
  }
}

function handleTestSortChange(sort: { prop: string | null; order: string | null }) {
  testCurrentPage.value = 1
  if (!sort.prop || !sort.order) {
    testSortState = null
  } else {
    testSortState = { prop: sort.prop, order: sort.order as 'ascending' | 'descending' }
  }
}

function handleResetTestParams() {
  testParams.value = {}
  sessionStorage.removeItem(SESSION_KEY_PREFIX + (form.value.id || 'new'))
}

// 字段操作
function addField() {
  form.value.fields.push({
    fieldName: '',
    fieldTitle: '',
    fieldType: 'string',
    sortOrder: form.value.fields.length + 1,
    width: 120,
    align: 'left',
    isDisplay: true,
    isSortable: false,
    isFilterable: false,
    isGroupable: false,
    isSummary: false,
    summaryType: '',
    formatPattern: ''
  })
}

function removeField(row: any) {
  form.value.fields = form.value.fields.filter(f => f !== row)
}

async function autoDetectFields() {
  ElMessage.info('自动检测功能将在保存后可用')
}

// 过滤操作
function addFilter() {
  const idx = form.value.filters.length
  form.value.filters.push({
    fieldName: '',
    label: '',
    operator: 'eq',
    controlType: 'input',
    defaultValue: '',
    dataUrl: '',
    sortOrder: idx + 1
  })
}

function removeFilter(row: any) {
  form.value.filters = form.value.filters.filter(f => f !== row)
}

// 监听控件类型变化，自动更新操作符
watch(() => form.value.filters, (filters) => {
  filters?.forEach(f => {
    if (f.controlType === 'daterange' && f.operator !== 'between') {
      f.operator = 'between'
    }
  })
}, { deep: true })

// 排序操作
function addSort() {
  form.value.sorts.push({
    fieldName: '',
    direction: 'ASC',
    sortOrder: form.value.sorts.length + 1
  })
}

function removeSort(row: any) {
  form.value.sorts = form.value.sorts.filter(s => s !== row)
}

// 图表操作
function addChart() {
  form.value.charts.push({
    title: '',
    chartType: 'bar',
    dataField: '',
    categoryField: '',
    width: 600,
    height: 400,
    options: ''
  })
}

function removeChart(row: any) {
  form.value.charts = form.value.charts.filter(c => c !== row)
}
</script>

<style scoped>
.summary-row {
  padding: 12px 16px;
  background: #f5f7fa;
  border-top: 1px solid #ebeef5;
  font-size: 13px;
}
.summary-item {
  margin-right: 16px;
}
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
.form-compact .el-form-item {
  margin-bottom: 18px;
}
.toolbar {
  margin-bottom: 12px;
}
.execution-info {
  margin-top: 12px;
  color: #909399;
  font-size: 13px;
}
</style>