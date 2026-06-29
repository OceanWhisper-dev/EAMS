<template>
  <div>
    <div class="page-header">
      <h3>数据源配置</h3>
      <el-button type="primary" size="small" @click="handleAdd">新增数据源</el-button>
    </div>
    <el-card>
      <el-table :data="paginatedDataSources" border size="small" v-loading="loading" @sort-change="handleSortChange">
        <el-table-column prop="name" label="标识" width="150" sortable="custom" />
        <el-table-column prop="displayName" label="显示名称" width="180" sortable="custom" />
        <el-table-column prop="dbType" label="类型" width="120" sortable="custom">
          <template #default="{ row }">
            <el-tag :type="row.dbType === 'postgresql' ? 'success' : 'warning'" size="small">
              {{ row.dbType === 'postgresql' ? 'PostgreSQL' : 'SQL Server' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="connectionString" label="连接字符串" min-width="300">
          <template #default="{ row }">
            <span class="conn-str">{{ row.connectionString }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sortOrder" label="排序" width="60" align="center" sortable="custom" />
        <el-table-column prop="isEnabled" label="启用" width="60" align="center" sortable="custom">
          <template #default="{ row }">
            <el-switch v-model="row.isEnabled" disabled size="small" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleTest(row)">测试</el-button>
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-popconfirm title="确认删除?" @confirm="handleDelete(row)">
              <template #reference>
                <el-button size="small" type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination-wrapper" v-if="dataSources.length > 0">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :total="dataSources.length"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next"
        />
      </div>
    </el-card>

    <DataSourceDialog
      :visible="dialogVisible"
      :data="editingData"
      @update:visible="dialogVisible = $event"
      @saved="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { erpSettingsApi } from '@/api/erpSettings'
import { ElMessage, ElMessageBox } from 'element-plus'
import DataSourceDialog from './components/DataSourceDialog.vue'

/** 统一 API 响应格式 */
interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

/** 数据源 DTO */
interface DataSourceDto {
  id: number
  name: string
  displayName: string
  dbType: string
  connectionString: string
  description?: string
  sortOrder: number
  isEnabled: boolean
}

const loading = ref(false)
const dataSources = ref<DataSourceDto[]>([])
const dialogVisible = ref(false)
const editingData = ref<DataSourceDto | null>(null)

// 排序状态
let sortState: { prop: string; order: 'ascending' | 'descending' } | null = null

// 分页状态
const currentPage = ref(1)
const pageSize = ref(20)

// 排序 + 分页后的数据
const paginatedDataSources = computed(() => {
  let list = [...dataSources.value]
  // 排序
  if (sortState) {
    const { prop, order } = sortState
    const asc = order === 'ascending'
    list.sort((a: DataSourceDto, b: DataSourceDto) => {
      const va = a[prop as keyof DataSourceDto]
      const vb = b[prop as keyof DataSourceDto]
      if (va == null) return asc ? -1 : 1
      if (vb == null) return asc ? 1 : -1
      if (typeof va === 'string' && typeof vb === 'string') {
        return asc ? va.localeCompare(vb, 'zh-CN') : vb.localeCompare(va, 'zh-CN')
      }
      return asc ? (va > vb ? 1 : -1) : (va < vb ? 1 : -1)
    })
  }
  // 分页
  const start = (currentPage.value - 1) * pageSize.value
  return list.slice(start, start + pageSize.value)
})

onMounted(() => {
  loadData()
})

async function loadData() {
  loading.value = true
  try {
    const res = await erpSettingsApi.getDataSources() as unknown as ApiResponse<DataSourceDto[]>
    dataSources.value = res.data || []
    currentPage.value = 1
  } finally {
    loading.value = false
  }
}

function handleSortChange(sort: { prop: string | null; order: string | null }) {
  currentPage.value = 1
  if (!sort.prop || !sort.order) {
    sortState = null
  } else {
    sortState = { prop: sort.prop, order: sort.order as 'ascending' | 'descending' }
  }
}

function handleAdd() {
  editingData.value = null
  dialogVisible.value = true
}

function handleEdit(row: DataSourceDto) {
  editingData.value = row
  dialogVisible.value = true
}

async function handleDelete(row: DataSourceDto) {
  try {
    await erpSettingsApi.deleteDataSource(row.id)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: unknown) {
    const errMsg = (e as { message?: string })?.message || '删除失败'
    ElMessage.error(errMsg)
  }
}

async function handleTest(row: DataSourceDto) {
  try {
    await erpSettingsApi.testDataSource({
      dbType: row.dbType,
      connectionString: row.connectionString
    }) as unknown as ApiResponse<unknown>
    ElMessage.success('连接成功')
  } catch (e: unknown) {
    const axiosErr = e as { response?: { data?: { message?: string } }; message?: string }
    const msg = axiosErr?.response?.data?.message || axiosErr?.message || '连接失败'
    ElMessage.error(msg)
  }
}
</script>

<style scoped>
.conn-str {
  font-family: monospace;
  font-size: 12px;
  word-break: break-all;
}
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>