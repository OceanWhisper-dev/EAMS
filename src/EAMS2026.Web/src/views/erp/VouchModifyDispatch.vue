<template>
  <div class="vouch-modify-dispatch">
    <el-card>
      <el-collapse v-model="activeQueryPanel">
        <el-collapse-item title="查询条件" name="query">
          <el-form :model="query" inline>
            <el-form-item label="客户编码">
              <el-input v-model="query.cusCode" placeholder="精确匹配" clearable />
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
            <el-form-item label="审核状态">
              <el-select v-model="query.verifierStatus" placeholder="全部" clearable style="width:120px">
                <el-option label="未审核" value="unverified" />
                <el-option label="已审核" value="verified" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleQuery">查询</el-button>
              <el-button @click="resetQuery">重置</el-button>
            </el-form-item>
          </el-form>
        </el-collapse-item>
      </el-collapse>

      <el-table :data="dispatches" v-loading="loading" border row-key="dlid">
        <el-table-column type="expand">
          <template #default="scope">
            <div class="expand-content">
              <h4>发货单明细</h4>
              <el-table :data="scope.row.dispatchDetails" size="small" border>
                <el-table-column prop="dlInvCode" label="存货编码" width="120" />
                <el-table-column prop="dlInvName" label="存货名称" />
                <el-table-column prop="dlInvStd" label="规格" width="120" />
                <el-table-column prop="dlQuantity" label="数量" width="100" />
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="cdlCode" label="发货单号" width="140" sortable />
        <el-table-column prop="dlDate" label="日期" width="110" sortable>
          <template #default="scope">{{ scope.row.dlDate?.split('T')[0] || '-' }}</template>
        </el-table-column>
        <el-table-column prop="dlCusCode" label="客户编码" width="100" sortable />
        <el-table-column prop="dlCusName" label="客户名称" min-width="180" sortable />
        <el-table-column prop="cVerifier" label="审核状态" width="100" align="center" sortable>
          <template #default="scope">
            <el-tag :type="scope.row.cVerifier ? 'warning' : 'info'" size="small">
              {{ scope.row.cVerifier ? '已审核' : '未审核' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button
              type="primary"
              size="small"
              link
              :disabled="!!scope.row.cVerifier"
              @click="showModifyDialog(scope.row)"
            >
              修改客户
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :page-sizes="[10, 20, 50]"
        :total="total"
        layout="total, sizes, prev, pager, next"
        @size-change="handleQuery"
        @current-change="handleQuery"
        class="pagination"
      />
    </el-card>

    <!-- 修改客户对话框 -->
    <el-dialog v-model="modifyDialogVisible" title="修改发货单客户" width="450px">
      <el-form :model="modifyForm" label-width="100px">
        <el-form-item label="原客户编码">
          <el-input :model-value="currentDispatch?.dlCusCode" disabled />
        </el-form-item>
        <el-form-item label="原客户名称">
          <el-input :model-value="currentDispatch?.dlCusName" disabled />
        </el-form-item>
        <el-form-item label="新客户编码" required>
          <el-input v-model="modifyForm.newCusCode" placeholder="输入客户编码后验证" @blur="validateCustomer">
            <template #append>
              <el-button :loading="validating" @click="validateCustomer">验证</el-button>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item label="新客户名称">
          <el-input v-model="modifyForm.newCusName" disabled />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="modifyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="modifying" :disabled="!modifyForm.newCusName" @click="submitModify">
          确认修改
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { vouchModifyApi } from '@/api/erp'
import { reportApi } from '@/api/report'
import type { DispatchDto, VouchQueryParam } from '@/api/erp'

const loading = ref(false)
const modifying = ref(false)
const validating = ref(false)
const activeQueryPanel = ref(['query'])
const modifyDialogVisible = ref(false)

const query = ref<VouchQueryParam>({
  cusCode: '', cusName: '', cusAbbName: '', cusPPerson: '',
  vouchCode: '', page: 1, pageSize: 20
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

const dispatches = ref<DispatchDto[]>([])
const total = ref(0)
const currentDispatch = ref<DispatchDto | null>(null)

const modifyForm = ref({
  newCusCode: '',
  newCusName: ''
})

async function handleQuery() {
  loading.value = true
  try {
    const params: VouchQueryParam = {
      ...query.value,
      vouchDateFrom: queryDateRange.value?.[0],
      vouchDateTo: queryDateRange.value?.[1]
    }
    const res = await vouchModifyApi.queryDispatches(params)
    dispatches.value = ((res as any)?.data?.items || [])
    total.value = ((res as any)?.data?.total || 0)
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.value = { cusCode: '', cusName: '', cusAbbName: '', cusPPerson: '',
    vouchCode: '', verifierStatus: '', page: 1, pageSize: 20 }
  queryDateRange.value = null
  handleQuery()
}

function showModifyDialog(dispatch: DispatchDto) {
  currentDispatch.value = dispatch
  modifyForm.value = { newCusCode: '', newCusName: '' }
  modifyDialogVisible.value = true
}

async function validateCustomer() {
  if (!modifyForm.value.newCusCode) {
    ElMessage.warning('请输入客户编码')
    return
  }
  validating.value = true
  try {
    const res = await vouchModifyApi.getCustomerRef(modifyForm.value.newCusCode)
    modifyForm.value.newCusName = (res as any)?.data?.cusName ?? ''
    ElMessage.success(`验证通过: ${(res as any)?.data?.cusName ?? ''}`)
  } catch {
    modifyForm.value.newCusName = ''
  } finally {
    validating.value = false
  }
}

async function submitModify() {
  if (!currentDispatch.value || !modifyForm.value.newCusCode) return
  modifying.value = true
  try {
    await vouchModifyApi.updateDispatchCustomer({
      dlid: currentDispatch.value.dlid,
      newCusCode: modifyForm.value.newCusCode,
      newCusName: modifyForm.value.newCusName,
      oldCusCode: currentDispatch.value.dlCusCode,
      oldCusName: currentDispatch.value.dlCusName,
      syncDispatches: false
    })
    ElMessage.success('发货单客户修改成功')
    modifyDialogVisible.value = false
    handleQuery()
  } finally {
    modifying.value = false
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
    currentSalesperson.value = curRes as unknown as { isSalesperson: boolean; salespersonCode?: string; type?: string }
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
.expand-content {
  padding: 12px 24px;
}
.expand-content h4 {
  margin: 0 0 8px 0;
  font-size: 14px;
  color: #409eff;
}
.pagination {
  margin-top: 16px;
  justify-content: flex-end;
}
</style>