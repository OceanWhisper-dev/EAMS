<template>
  <div class="vouch-modify-date">
    <el-card>
      <el-collapse v-model="activeQueryPanel">
        <el-collapse-item title="查询条件" name="query">
          <el-form :model="query" inline>
            <el-form-item label="客户编码">
              <el-input v-model="query.cusCode" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="客户名称">
              <el-input v-model="query.cusName" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="发货单号">
              <el-input v-model="query.vouchCode" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="业务员">
              <el-select v-model="query.cusPPerson" placeholder="全部" clearable filterable style="width:160px">
                <el-option v-for="sp in filteredSalespersonList" :key="sp.code" :label="sp.name" :value="sp.code" />
              </el-select>
            </el-form-item>
            <el-form-item label="日期范围">
              <el-date-picker
                v-model="queryDateRange"
                type="daterange"
                range-separator="至"
                start-placeholder="开始日期"
                end-placeholder="结束日期"
                value-format="YYYY-MM-DD"
              />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleQuery">查询</el-button>
              <el-button @click="resetQuery">重置</el-button>
            </el-form-item>
          </el-form>
        </el-collapse-item>
      </el-collapse>

      <div class="toolbar">
        <div class="toolbar-left">
          <span>已选择 <b>{{ selection.length }}</b> 条未审核单据</span>
        </div>
        <div class="toolbar-right">
          <el-tag type="success" size="large">下月第一个周日: {{ computedTargetDate }}</el-tag>
          <el-date-picker
            v-model="manualDate"
            placeholder="手动选择日期"
            value-format="YYYY-MM-DD"
            style="width: 160px; margin-left: 12px"
          />
          <el-button type="warning" :disabled="selection.length === 0" @click="showConfirmDialog">
            批量修改日期（{{ selection.length }}）
          </el-button>
        </div>
      </div>

      <el-table
        :data="rows"
        @selection-change="handleSelectionChange"
        v-loading="loading"
        border
        stripe
      >
        <el-table-column type="selection" :selectable="(row: UnverifiedDispatchRow) => !row.cVerifier" width="50" />
        <el-table-column prop="cdlCode" label="发货单号" width="140" />
        <el-table-column prop="cusCode" label="客户编码" width="100" />
        <el-table-column prop="cusName" label="客户名称" min-width="180" />
        <el-table-column prop="dDate" label="原日期" width="110">
          <template #default="scope">
            {{ scope.row.dDate?.split('T')[0] || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="目标日期" width="130">
          <template #default="scope">
            <el-tag v-if="selection.includes(scope.row)" type="primary" size="small">
              {{ getTargetDate() }}
            </el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="cPersonName" label="业务员" width="100" />
        <el-table-column prop="cVerifier" label="审核状态" width="100" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.cVerifier ? 'warning' : 'info'" size="small">
              {{ scope.row.cVerifier ? '已审核' : '未审核' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="total"
        layout="total, sizes, prev, pager, next"
        @size-change="handleQuery"
        @current-change="handleQuery"
        class="pagination"
      />
    </el-card>

    <!-- 确认对话框 -->
    <el-dialog v-model="confirmDialogVisible" title="确认批量修改日期" width="450px">
      <el-descriptions :column="1" border>
        <el-descriptions-item label="待修改单据数">{{ selection.length }} 条</el-descriptions-item>
        <el-descriptions-item label="目标日期">
          <el-tag type="danger">{{ getTargetDate() }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="提示">
          仅修改未审核单据，已审核单据将自动跳过。
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="confirmDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="confirmModify">确认修改</el-button>
      </template>
    </el-dialog>

    <!-- 结果对话框 -->
    <el-dialog v-model="resultDialogVisible" title="修改结果" width="500px">
      <el-alert
        :type="resultFailCount > 0 ? 'warning' : 'success'"
        :title="`成功 ${resultSuccessCount} 条，失败 ${resultFailCount} 条`"
        :closable="false"
        show-icon
      />
      <el-table v-if="resultFailures.length > 0" :data="resultFailures" style="margin-top: 12px" max-height="300">
        <el-table-column prop="dlid" label="单据ID" width="80" />
        <el-table-column prop="errorMessage" label="失败原因" />
      </el-table>
      <template #footer>
        <el-button type="primary" @click="resultDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { vouchModifyApi } from '@/api/erp'
import { reportApi } from '@/api/report'
import type { UnverifiedDispatchRow, VouchQueryParam } from '@/api/erp'

const loading = ref(false)
const submitting = ref(false)
const activeQueryPanel = ref(['query'])
const confirmDialogVisible = ref(false)
const resultDialogVisible = ref(false)

const query = ref<VouchQueryParam>({
  cusCode: '',
  cusName: '',
  vouchCode: '',
  cusPPerson: '',
  page: 1,
  pageSize: 20
})
const queryDateRange = ref<[string, string] | null>(null)

// 业务员下拉
const salespersonList = ref<{ code: string; name: string }[]>([])
const currentSalesperson = ref<{ isSalesperson: boolean; salespersonCode?: string; type?: string }>({ isSalesperson: false })
const isRestrictedSalesperson = computed(() => {
  if (!currentSalesperson.value?.isSalesperson) return false
  return currentSalesperson.value.type !== 'supervisor'
})
const filteredSalespersonList = computed(() => {
  if (isRestrictedSalesperson.value) {
    return salespersonList.value.filter(sp => sp.code === currentSalesperson.value?.salespersonCode)
  }
  return salespersonList.value
})

const rows = ref<UnverifiedDispatchRow[]>([])
const total = ref(0)
const selection = ref<UnverifiedDispatchRow[]>([])

const manualDate = ref<string | null>(null)
const resultSuccessCount = ref(0)
const resultFailCount = ref(0)
const resultFailures = ref<{ dlid: number; errorMessage: string }[]>([])

// 计算下月第一个周日
const computedTargetDate = computed(() => getTargetDateRaw())

function getTargetDateRaw(): string {
  const today = new Date()
  const firstOfNextMonth = new Date(today.getFullYear(), today.getMonth() + 1, 1)
  const dayOfWeek = firstOfNextMonth.getDay()
  const daysToAdd = dayOfWeek === 0 ? 0 : 7 - dayOfWeek
  firstOfNextMonth.setDate(firstOfNextMonth.getDate() + daysToAdd)
  return firstOfNextMonth.toISOString().split('T')[0]
}

function getTargetDate(): string {
  return manualDate.value || computedTargetDate.value
}

async function handleQuery() {
  loading.value = true
  try {
    const params: VouchQueryParam = {
      ...query.value,
      vouchDateFrom: queryDateRange.value?.[0],
      vouchDateTo: queryDateRange.value?.[1]
    }
    const res = await vouchModifyApi.queryUnverifiedDispatches(params)
    const data = (res as any)?.data || {}
    rows.value = data.items || []
    total.value = data.total || 0
  } catch {
    // error handled by interceptor
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.value = { cusCode: '', cusName: '', vouchCode: '', cusPPerson: '', page: 1, pageSize: 20 }
  queryDateRange.value = null
  handleQuery()
}

function handleSelectionChange(val: UnverifiedDispatchRow[]) {
  selection.value = val
}

function showConfirmDialog() {
  if (selection.value.length === 0) {
    ElMessage.warning('请先选择需要修改的单据')
    return
  }
  confirmDialogVisible.value = true
}

async function confirmModify() {
  submitting.value = true
  try {
    const res = await vouchModifyApi.batchUpdateDispatchDate({
      dlids: selection.value.map(item => item.dlid),
      newDate: getTargetDate(),
      autoCalculate: !manualDate.value
    })
    const data = (res as any)?.data || {}
    resultSuccessCount.value = data.successCount || 0
    resultFailCount.value = data.failCount || 0
    resultFailures.value = data.failures || []
    confirmDialogVisible.value = false
    resultDialogVisible.value = true
    selection.value = []
    await handleQuery()
  } catch {
    // error handled by interceptor
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  // 加载业务员下拉选项
  try {
    const [spRes, curRes] = await Promise.all([
      vouchModifyApi.getSalespersons(),
      reportApi.getCurrentSalesperson()
    ])
    salespersonList.value = ((spRes as any)?.data || []) as { code: string; name: string }[]
    currentSalesperson.value = ((curRes as any)?.data || { isSalesperson: false }) as { isSalesperson: boolean; salespersonCode?: string; type?: string }
    // 受限业务员自动选中自己
    if (currentSalesperson.value.isSalesperson && currentSalesperson.value.type !== 'supervisor') {
      query.value.cusPPerson = currentSalesperson.value.salespersonCode || ''
    }
  } catch {
    // 加载失败不影响主功能
  }
  handleQuery()
})
</script>

<style scoped>
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 12px 0;
  padding: 8px 0;
}
.toolbar-left {
  font-size: 14px;
}
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}
.pagination {
  margin-top: 16px;
  justify-content: flex-end;
}
</style>