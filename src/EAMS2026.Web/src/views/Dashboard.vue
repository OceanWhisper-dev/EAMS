<template>
  <div class="dashboard">
    <el-row :gutter="20">
      <el-col v-for="widget in widgets" :key="widget.widgetKey" :span="widget.layoutConfig?.span ?? 24">
        <el-card shadow="hover" class="widget-card">
          <template #header>
            <div class="widget-header">
              <el-icon v-if="widget.config?.icon" :size="18"><component :is="widget.config.icon" /></el-icon>
              <span>{{ widget.widgetName }}</span>
            </div>
          </template>

          <div v-if="widget.widgetType === 'stat_card'" class="stat-widget">
            <div class="stat-value">{{ widget.data?.value ?? '-' }}</div>
          </div>

          <div v-else-if="widget.widgetType === 'info_card'" class="welcome-widget">
            <h3>欢迎使用 EAMS2026 企业管理系统</h3>
            <p>当前登录用户：{{ authStore.employeeName || authStore.username }}</p>
            <p>拥有角色：{{ authStore.roles.join(', ') || '无' }}</p>
          </div>

          <div v-else-if="widget.widgetType === 'list'" class="list-widget">
            <el-table v-if="widget.data?.value?.length" :data="widget.data.value" size="small" border>
              <el-table-column prop="username" label="操作人" width="100" />
              <el-table-column prop="module" label="模块" width="100" />
              <el-table-column prop="operationType" label="操作类型" width="100" />
              <el-table-column prop="description" label="描述" />
              <el-table-column prop="createdAt" label="时间" width="180" />
            </el-table>
            <el-empty v-else description="暂无数据" />
          </div>

          <div v-else-if="widget.widgetType === 'quick_links'" class="quick-links-widget">
            <el-row :gutter="16">
              <el-col :span="6" v-for="link in widget.config?.links" :key="link.path">
                <router-link :to="link.path" class="quick-link-item">
                  <el-icon :size="24"><component :is="widget.config?.icon || 'Link'" /></el-icon>
                  <span>{{ link.label }}</span>
                </router-link>
              </el-col>
            </el-row>
          </div>

          <div v-else-if="widget.widgetType === 'chart'" class="chart-widget">
            <div :ref="el => setChartRef(widget.widgetKey, el)" style="height: 300px;"></div>
          </div>

          <div v-else class="default-widget">
            <pre>{{ JSON.stringify(widget.data, null, 2) }}</pre>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-empty v-if="!widgets.length && !loading" description="暂无仪表盘组件，请联系管理员配置" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { dashboardApi } from '@/api/dashboard'

const authStore = useAuthStore()
const loading = ref(false)
const widgets = ref<any[]>([])
const chartRefs = ref<Record<string, any>>({})
let refreshTimers: ReturnType<typeof setInterval>[] = []

function setChartRef(key: string, el: any) {
  if (el) chartRefs.value[key] = el
}

async function fetchDashboard() {
  loading.value = true
  try {
    const res: any = await dashboardApi.getMyDashboard()
    widgets.value = res.data || []
    await nextTick()
    setupRefreshTimers()
  } finally { loading.value = false }
}

function setupRefreshTimers() {
  refreshTimers.forEach(t => clearInterval(t))
  refreshTimers = []

  widgets.value.forEach(widget => {
    if (widget.refreshInterval && widget.refreshInterval > 0) {
      const timer = setInterval(async () => {
        try {
          const res: any = await dashboardApi.getMyDashboard()
          const updated = (res.data || []).find((w: any) => w.widgetKey === widget.widgetKey)
          if (updated) {
            const index = widgets.value.findIndex(w => w.widgetKey === widget.widgetKey)
            if (index !== -1) widgets.value[index] = updated
          }
        } catch { /* ignore */ }
      }, widget.refreshInterval * 1000)
      refreshTimers.push(timer)
    }
  })
}

onMounted(fetchDashboard)
onUnmounted(() => refreshTimers.forEach(t => clearInterval(t)))
</script>

<style scoped>
.dashboard { padding: 0; }
.widget-card { margin-bottom: 20px; }
.widget-header { display: flex; align-items: center; gap: 8px; font-size: 16px; font-weight: 500; }
.stat-widget { text-align: center; padding: 20px 0; }
.stat-value { font-size: 36px; font-weight: bold; color: #409eff; }
.welcome-widget h3 { margin: 0 0 12px; font-size: 18px; }
.welcome-widget p { margin: 8px 0; color: #666; }
.list-widget { padding: 10px 0; }
.log-meta { color: #999; font-size: 12px; margin-top: 4px; }
.quick-links-widget { padding: 10px 0; }
.quick-link-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 20px;
  border-radius: 8px;
  background: #f5f7fa;
  text-decoration: none;
  color: #606266;
  transition: all 0.3s;
}
.quick-link-item:hover {
  background: #ecf5ff;
  color: #409eff;
  transform: translateY(-2px);
}
.chart-widget { min-height: 300px; }
.default-widget pre { white-space: pre-wrap; word-break: break-all; color: #999; font-size: 12px; }
</style>
