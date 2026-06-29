<template>
  <div class="vouch-modify-order">
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
            <el-form-item label="客户简称">
              <el-input v-model="query.cusAbbName" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="联系人">
              <el-input v-model="query.cusContact" placeholder="联系人编码" clearable />
            </el-form-item>
            <el-form-item label="业务员">
              <el-select v-model="query.cusPPerson" placeholder="全部" clearable filterable style="width:160px">
                <el-option v-for="sp in filteredSalespersonList" :key="sp.code" :label="sp.name" :value="sp.code" />
              </el-select>
            </el-form-item>
            <el-form-item label="联系电话">
              <el-input v-model="query.cusPhone" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="手机号">
              <el-input v-model="query.cusMobile" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="客户地址">
              <el-input v-model="query.cusAddr" placeholder="模糊匹配" clearable />
            </el-form-item>
            <el-form-item label="订单号">
              <el-input v-model="query.vouchCode" placeholder="模糊匹配" clearable />
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

      <el-table :data="orders" v-loading="loading" border row-key="soid">
        <el-table-column type="expand">
          <template #default="scope">
            <div class="expand-content">
              <h4>订单明细</h4>
              <el-table :data="scope.row.orderDetails" size="small" border>
                <el-table-column prop="soInvCode" label="存货编码" width="120" />
                <el-table-column prop="soInvName" label="存货名称" />
                <el-table-column prop="soInvStd" label="规格" width="120" />
                <el-table-column prop="soQuantity" label="数量" width="100" />
              </el-table>

              <h4 v-if="scope.row.dispatches.length > 0" style="margin-top: 16px">关联发货单</h4>
              <el-table v-if="scope.row.dispatches.length > 0" :data="scope.row.dispatches" size="small" border>
                <el-table-column type="expand">
                  <template #default="ds">
                    <el-table :data="ds.row.dispatchDetails" size="small" border>
                      <el-table-column prop="dlInvCode" label="存货编码" width="120" />
                      <el-table-column prop="dlInvName" label="存货名称" />
                      <el-table-column prop="dlInvStd" label="规格" width="120" />
                      <el-table-column prop="dlQuantity" label="数量" width="100" />
                    </el-table>
                  </template>
                </el-table-column>
                <el-table-column prop="cdlCode" label="发货单号" width="140" />
                <el-table-column prop="dlDate" label="日期" width="110">
                  <template #default="ds">{{ ds.row.dlDate?.split('T')[0] || '-' }}</template>
                </el-table-column>
                <el-table-column prop="dlCusName" label="客户" />
                <el-table-column prop="cVerifier" label="审核状态" width="100">
                  <template #default="ds">
                    <el-tag :type="ds.row.cVerifier ? 'warning' : 'info'" size="small">
                      {{ ds.row.cVerifier ? '已审核' : '未审核' }}
                    </el-tag>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="csoCode" label="订单号" width="140" sortable />
        <el-table-column prop="soDate" label="日期" width="110" sortable>
          <template #default="scope">{{ scope.row.soDate?.split('T')[0] || '-' }}</template>
        </el-table-column>
        <el-table-column prop="soCusCode" label="客户编码" width="100" sortable />
        <el-table-column prop="soCusName" label="客户名称" min-width="180" sortable />
        <el-table-column prop="cPersonName" label="业务员" width="100" sortable />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button type="primary" size="small" link @click="showModifyDialog(scope.row)">
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
    <el-dialog v-model="modifyDialogVisible" title="修改客户" width="450px">
      <el-form :model="modifyForm" label-width="100px">
        <el-form-item label="原客户编码">
          <el-input :model-value="currentOrder?.soCusCode" disabled />
        </el-form-item>
        <el-form-item label="原客户名称">
          <el-input :model-value="currentOrder?.soCusName" disabled />
        </el-form-item>
        <el-form-item label="新客户编码" required>
          <el-input v-model="modifyForm.newCusCode" placeholder="输入客户编码后回车验证" @blur="validateCustomer">
            <template #append>
              <el-button :loading="validating" @click="validateCustomer">验证</el-button>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item label="新客户名称">
          <el-input v-model="modifyForm.newCusName" disabled />
        </el-form-item>
        <el-form-item label="同步发货单">
          <el-switch v-model="modifyForm.syncDispatches" />
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
import { ElMessage, ElMessageBox } from 'element-plus'
import { vouchModifyApi } from '@/api/erp'
import { reportApi } from '@/api/report'
import type { OrderDto, VouchQueryParam, PagedResult, CustomerRef } from '@/api/erp'

/** 后端统一响应格式 { success, data } */
interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

const loading = ref(false)
const modifying = ref(false)
const validating = ref(false)
const activeQueryPanel = ref(['query'])
const modifyDialogVisible = ref(false)

const query = ref<VouchQueryParam>({
  cusCode: '', cusName: '', cusAbbName: '', cusContact: '',
  cusPPerson: '', cusPhone: '', cusMobile: '', cusAddr: '', vouchCode: '',
  page: 1, pageSize: 20
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

const orders = ref<OrderDto[]>([])
const total = ref(0)
const currentOrder = ref<OrderDto | null>(null)

const modifyForm = ref({
  newCusCode: '',
  newCusName: '',
  syncDispatches: false
})

async function handleQuery() {
  loading.value = true
  try {
    const params: VouchQueryParam = {
      ...query.value,
      vouchDateFrom: queryDateRange.value?.[0],
      vouchDateTo: queryDateRange.value?.[1]
    }
    const res = await vouchModifyApi.queryOrders(params) as unknown as ApiResponse<PagedResult<OrderDto>>
    orders.value = res.data?.items || []
    total.value = res.data?.total || 0
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.value = { cusCode: '', cusName: '', cusAbbName: '', cusContact: '',
    cusPPerson: '', cusPhone: '', cusMobile: '', cusAddr: '', vouchCode: '',
    page: 1, pageSize: 20 }
  queryDateRange.value = null
  handleQuery()
}

function showModifyDialog(order: OrderDto) {
  currentOrder.value = order
  modifyForm.value = { newCusCode: '', newCusName: '', syncDispatches: false }
  modifyDialogVisible.value = true
}

async function validateCustomer() {
  if (!modifyForm.value.newCusCode) {
    ElMessage.warning('请输入客户编码')
    return
  }
  validating.value = true
  try {
    const res = await vouchModifyApi.getCustomerRef(modifyForm.value.newCusCode) as unknown as ApiResponse<CustomerRef>
    modifyForm.value.newCusName = res.data?.cusName ?? ''
    ElMessage.success(`验证通过: ${res.data?.cusName ?? ''}`)
  } catch {
    modifyForm.value.newCusName = ''
  } finally {
    validating.value = false
  }
}

async function submitModify() {
  if (!currentOrder.value || !modifyForm.value.newCusCode) return

  // 检查订单是否有已审核的发货单
  modifying.value = true
  try {
    const checkRes = await vouchModifyApi.hasVerifiedDispatches(currentOrder.value.soid) as unknown as ApiResponse<{ hasVerified: boolean }>
    if (checkRes.data?.hasVerified) {
      await ElMessageBox.alert(
        '该销售订单有关联的已审核发货单，无法修改客户信息！',
        '操作警告',
        { type: 'warning', confirmButtonText: '知道了' }
      )
      return
    }
  } finally {
    modifying.value = false
  }

  modifying.value = true
  try {
    await vouchModifyApi.updateOrderCustomer({
      soid: currentOrder.value.soid,
      newCusCode: modifyForm.value.newCusCode,
      newCusName: modifyForm.value.newCusName,
      oldCusCode: currentOrder.value.soCusCode,
      oldCusName: currentOrder.value.soCusName,
      syncDispatches: modifyForm.value.syncDispatches
    })
    ElMessage.success('订单客户修改成功')
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
    salespersonList.value = (spRes as unknown as ApiResponse<{ code: string; name: string }[]>).data || []
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