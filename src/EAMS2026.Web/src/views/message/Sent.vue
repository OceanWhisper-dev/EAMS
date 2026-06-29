<template>
  <div>
    <div class="page-header">
      <h3>已发送</h3>
      <el-button type="primary" @click="handleCompose">写消息</el-button>
    </div>
    <el-card>
      <el-table :data="messages" border v-loading="loading">
        <el-table-column prop="title" label="标题" min-width="200" />
        <el-table-column prop="receiverName" label="收件人" width="120" />
        <el-table-column prop="priority" label="优先级" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.priority === 'urgent'" type="danger" size="small">紧急</el-tag>
            <el-tag v-else-if="row.priority === 'high'" type="warning" size="small">高</el-tag>
            <el-tag v-else size="small">普通</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isRead" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isRead ? 'success' : 'info'" size="small">{{ row.isRead ? '已读' : '未读' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="发送时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleView(row)">查看</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination-wrap">
        <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :total="total" layout="total, prev, pager, next" @current-change="fetchData" />
      </div>
    </el-card>

    <el-dialog v-model="detailVisible" title="消息详情" width="600px">
      <div v-if="detail" class="message-detail">
        <div class="detail-header">
          <h2>{{ detail.title }}</h2>
          <div class="detail-meta">
            <span>收件人: {{ detail.receiverName }}</span>
            <span>发送时间: {{ formatDateTime(detail.createdAt) }}</span>
            <el-tag :type="detail.isRead ? 'success' : 'info'" size="small">{{ detail.isRead ? '已读' : '未读' }}</el-tag>
          </div>
        </div>
        <el-divider />
        <div class="detail-content" v-html="detail.content.replace(/\n/g, '<br>')" />
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRouter } from 'vue-router'
import { messageApi } from '@/api/system'
import { formatDateTime } from '@/utils/date'

const router = useRouter()
const loading = ref(false)
const messages = ref<any[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const detailVisible = ref(false)
const detail = ref<any>(null)

async function fetchData() {
  loading.value = true
  try {
    const res: any = await messageApi.getSent(page.value, pageSize.value)
    messages.value = res.data || []
    total.value = messages.value.length
  } finally { loading.value = false }
}

function handleView(row: any) {
  detail.value = row
  detailVisible.value = true
}

function handleCompose() {
  router.push('/message/compose')
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm('确定删除此消息吗？', '提示')
    await messageApi.delete(row.id)
    ElMessage.success('删除成功')
    fetchData()
  } catch { /* cancelled */ }
}

onMounted(fetchData)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.message-detail .detail-header h2 { margin: 0 0 12px; font-size: 18px; }
.message-detail .detail-meta { display: flex; gap: 16px; align-items: center; color: #999; font-size: 13px; }
.message-detail .detail-content { padding: 12px 0; line-height: 1.8; min-height: 100px; }
.pagination-wrap { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>