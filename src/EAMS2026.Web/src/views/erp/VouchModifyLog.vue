<template>
  <div class="vouch-modify-log">
    <el-card>
      <el-form :model="query" inline>
        <el-form-item label="单据类型">
          <el-select v-model="query.vouchType" placeholder="全部" clearable style="width: 120px">
            <el-option label="订单" value="order" />
            <el-option label="发货单" value="dispatch" />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人">
          <el-input v-model="query.operatorId" placeholder="操作人ID" clearable style="width: 120px" />
        </el-form-item>
        <el-form-item label="时间范围">
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

      <el-table :data="logs" v-loading="loading" border>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="vouchType" label="单据类型" width="90" align="center">
          <template #default="scope">
            <el-tag size="small">{{ scope.row.vouchType === 'order' ? '订单' : '发货单' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="vouchCode" label="单据编号" width="140" />
        <el-table-column prop="fieldName" label="修改字段" width="120" />
        <el-table-column prop="oldValue" label="原值" min-width="160" show-overflow-tooltip />
        <el-table-column prop="newValue" label="新值" min-width="160" show-overflow-tooltip />
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column prop="operateAt" label="操作时间" width="170">
          <template #default="scope">
            {{ new Date(scope.row.operateAt).toLocaleString() }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.status === 'SUCCESS' ? 'success' : 'danger'" size="small">
              {{ scope.row.status === 'SUCCESS' ? '成功' : '失败' }}
            </el-tag>
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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { vouchModifyApi } from '@/api/erp'
import type { VouchModifyLog } from '@/api/erp'

const loading = ref(false)

const query = ref({
  vouchType: '',
  operatorId: '',
  page: 1,
  pageSize: 20
})
const queryDateRange = ref<[string, string] | null>(null)

const logs = ref<VouchModifyLog[]>([])
const total = ref(0)

async function handleQuery() {
  loading.value = true
  try {
    const res = await vouchModifyApi.queryLogs({
      vouchType: query.value.vouchType || undefined,
      operatorId: query.value.operatorId ? Number(query.value.operatorId) : undefined,
      from: queryDateRange.value?.[0],
      to: queryDateRange.value?.[1],
      page: query.value.page,
      pageSize: query.value.pageSize
    })
    logs.value = ((res as any)?.data?.items || [])
    total.value = ((res as any)?.data?.total || 0)
  } finally {
    loading.value = false
  }
}

function resetQuery() {
  query.value = { vouchType: '', operatorId: '', page: 1, pageSize: 20 }
  queryDateRange.value = null
  handleQuery()
}

onMounted(() => {
  handleQuery()
})
</script>

<style scoped>
.pagination {
  margin-top: 16px;
  justify-content: flex-end;
}
</style>