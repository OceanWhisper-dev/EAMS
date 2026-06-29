<template>
  <div>
    <div class="page-header">
      <h3>收件箱</h3>
      <div class="header-actions">
        <el-badge :value="unreadCount" :hidden="unreadCount === 0" class="unread-badge">
          <el-button @click="handleMarkAllRead" :disabled="unreadCount === 0">全部标为已读</el-button>
        </el-badge>
        <el-button type="primary" @click="handleCompose">写消息</el-button>
      </div>
    </div>
    <el-card>
      <el-table :data="messages" border v-loading="loading" @row-click="handleRowClick">
        <el-table-column width="50">
          <template #default="{ row }">
            <el-icon v-if="!row.isRead" color="#409eff"><Message /></el-icon>
            <el-icon v-else color="#ccc"><Message /></el-icon>
          </template>
        </el-table-column>
        <el-table-column prop="title" label="标题" min-width="200">
          <template #default="{ row }">
            <span :class="{ 'unread': !row.isRead }">{{ row.title }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="senderName" label="发件人" width="120" />
        <el-table-column prop="priority" label="优先级" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.priority === 'urgent'" type="danger" size="small">紧急</el-tag>
            <el-tag v-else-if="row.priority === 'high'" type="warning" size="small">高</el-tag>
            <el-tag v-else size="small">普通</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click.stop="handleView(row)">查看</el-button>
            <el-button size="small" type="danger" @click.stop="handleDelete(row)">删除</el-button>
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
            <span>发件人: {{ detail.senderName }}</span>
            <span>时间: {{ formatDateTime(detail.createdAt) }}</span>
            <el-tag v-if="detail.priority === 'urgent'" type="danger" size="small">紧急</el-tag>
            <el-tag v-else-if="detail.priority === 'high'" type="warning" size="small">高</el-tag>
          </div>
        </div>
        <el-divider />
        <div class="detail-content" v-html="detail.content.replace(/\n/g, '<br>')" />
        <el-divider />
        <div class="detail-reply">
          <el-input v-model="replyContent" type="textarea" :rows="3" placeholder="输入回复内容..." />
          <div class="reply-actions">
            <el-button type="primary" @click="handleReply" :loading="replying">回复</el-button>
          </div>
        </div>
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
const replying = ref(false)
const messages = ref<any[]>([])
const unreadCount = ref(0)
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const detailVisible = ref(false)
const detail = ref<any>(null)
const replyContent = ref('')

async function fetchData() {
  loading.value = true
  try {
    const [listRes, unreadRes]: any = await Promise.all([
      messageApi.getReceived(page.value, pageSize.value),
      messageApi.getUnreadCount()
    ])
    messages.value = listRes.data?.messages || []
    total.value = messages.value.length
    unreadCount.value = unreadRes.data || 0
  } finally { loading.value = false }
}

async function handleRowClick(row: any) {
  if (!row.isRead) {
    await messageApi.markAsRead(row.id)
    row.isRead = true
    unreadCount.value = Math.max(0, unreadCount.value - 1)
  }
}

function handleView(row: any) {
  detail.value = row
  replyContent.value = ''
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

async function handleMarkAllRead() {
  await messageApi.markAllAsRead()
  ElMessage.success('全部标为已读')
  fetchData()
}

async function handleReply() {
  if (!replyContent.value.trim()) {
    ElMessage.warning('请输入回复内容')
    return
  }
  replying.value = true
  try {
    await messageApi.reply(detail.value.id, replyContent.value)
    ElMessage.success('回复成功')
    replyContent.value = ''
  } finally { replying.value = false }
}

onMounted(fetchData)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; align-items: center; }
.unread { font-weight: bold; }
.unread-badge :deep(.el-badge__content) { top: 8px; }
.message-detail .detail-header h2 { margin: 0 0 12px; font-size: 18px; }
.message-detail .detail-meta { display: flex; gap: 16px; align-items: center; color: #999; font-size: 13px; }
.message-detail .detail-content { padding: 12px 0; line-height: 1.8; min-height: 100px; }
.detail-reply { margin-top: 12px; }
.reply-actions { margin-top: 8px; text-align: right; }
.pagination-wrap { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>