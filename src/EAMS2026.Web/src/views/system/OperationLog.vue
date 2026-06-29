<template>
  <div>
    <div class="page-header">
      <h3>{{ authStore.isAdmin ? '操作日志' : '我的操作日志' }}</h3>
      <el-button v-if="authStore.isAdmin" type="danger" @click="handleClear">清理日志</el-button>
    </div>
    <el-form v-if="authStore.isAdmin" :inline="true" :model="queryForm" class="query-form">
      <el-form-item label="用户">
        <el-select v-model="queryForm.userId" placeholder="全部" clearable filterable style="width: 150px;">
          <el-option v-for="u in users" :key="u.id" :label="u.employeeName || u.username" :value="u.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="模块">
        <el-select v-model="queryForm.module" placeholder="全部" clearable style="width: 150px;">
          <el-option label="用户管理" value="User" />
          <el-option label="员工管理" value="Employee" />
          <el-option label="部门管理" value="Department" />
          <el-option label="角色管理" value="Role" />
          <el-option label="权限管理" value="Permission" />
          <el-option label="字典管理" value="Dict" />
          <el-option label="考勤模块" value="Attendance" />
          <el-option label="数据权限" value="DataPermission" />
          <el-option label="个人资料" value="Profile" />
        </el-select>
      </el-form-item>
      <el-form-item label="日期范围">
        <el-date-picker
          v-model="queryForm.dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
        />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleQuery">查询</el-button>
        <el-button @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>
    <el-table :data="tableData" v-loading="loading" border stripe>
      <el-table-column prop="id" label="ID" width="80" />
      <el-table-column prop="username" label="操作用户" width="120" />
      <el-table-column prop="module" label="模块" width="100" />
      <el-table-column prop="operationType" label="操作类型" width="100" />
      <el-table-column prop="description" label="操作描述" min-width="200" />
      <el-table-column prop="ipAddress" label="IP地址" width="140" />
      <el-table-column label="操作时间" width="180">
        <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
      </el-table-column>
    </el-table>
    <el-pagination
      v-model:current-page="pagination.page"
      v-model:page-size="pagination.pageSize"
      :total="pagination.total"
      :page-sizes="[20, 50, 100]"
      layout="total, sizes, prev, pager, next"
      @current-change="fetchData"
      @size-change="fetchData"
      style="margin-top: 16px; justify-content: flex-end;"
    />

    <el-dialog v-model="clearDialogVisible" title="清理日志" width="450px">
      <el-form label-width="100px">
        <el-form-item label="清理范围">
          <el-select v-model="clearType" style="width: 100%;">
            <el-option label="清理全部日志" value="all" />
            <el-option label="清理指定日期之前的日志" value="before" />
          </el-select>
        </el-form-item>
        <el-form-item label="截止日期" v-if="clearType === 'before'">
          <el-date-picker v-model="clearBeforeDate" type="date" value-format="YYYY-MM-DD" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="操作用户">
          <el-select v-model="clearUserId" placeholder="全部用户" clearable filterable style="width: 100%;">
            <el-option v-for="u in users" :key="u.id" :label="u.employeeName || u.username" :value="u.id" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="clearDialogVisible = false">取消</el-button>
        <el-button type="danger" @click="confirmClear" :loading="clearing">确定清理</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { operationLogApi, userApi } from '@/api/system'
import request from '@/utils/request'
import { formatDateTime } from '@/utils/date'

const authStore = useAuthStore()
const loading = ref(false)
const tableData = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 20, total: 0 })
const queryForm = ref({ userId: null as number | null, module: '', dateRange: [] as string[] })
const users = ref<any[]>([])

const clearDialogVisible = ref(false)
const clearType = ref('all')
const clearBeforeDate = ref('')
const clearUserId = ref<number | null>(null)
const clearing = ref(false)

async function fetchUsers() {
  try {
    const res: any = await userApi.getAll()
    users.value = res.data || []
  } catch { /* ignore */ }
}

async function fetchData() {
  loading.value = true
  try {
    const params: any = { page: pagination.value.page, pageSize: pagination.value.pageSize }
    if (authStore.isAdmin) {
      if (queryForm.value.userId) params.userId = queryForm.value.userId
      if (queryForm.value.module) params.module = queryForm.value.module
      if (queryForm.value.dateRange?.length === 2) {
        params.startDate = queryForm.value.dateRange[0]
        params.endDate = queryForm.value.dateRange[1]
      }
      const res: any = await operationLogApi.getPaged(params)
      tableData.value = res.data?.items || []
      pagination.value.total = res.data?.total || 0
    } else {
      const res: any = await operationLogApi.getMine(params)
      tableData.value = res.data?.items || []
      pagination.value.total = res.data?.total || 0
    }
  } finally { loading.value = false }
}

function handleQuery() {
  pagination.value.page = 1
  fetchData()
}

function handleReset() {
  queryForm.value = { userId: null, module: '', dateRange: [] }
  handleQuery()
}

function handleClear() {
  clearDialogVisible.value = true
  clearType.value = 'all'
  clearBeforeDate.value = ''
  clearUserId.value = null
}

async function confirmClear() {
  clearing.value = true
  try {
    const params: any = {}
    if (clearType.value === 'before' && clearBeforeDate.value) {
      params.beforeDate = clearBeforeDate.value
    }
    if (clearUserId.value) {
      params.userId = clearUserId.value
    }
    await request.delete('/operation-log/clear', { params })
    ElMessage.success('清理成功')
    clearDialogVisible.value = false
    fetchData()
  } finally { clearing.value = false }
}

onMounted(() => { fetchUsers(); fetchData() })
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.query-form { margin-bottom: 16px; }
</style>