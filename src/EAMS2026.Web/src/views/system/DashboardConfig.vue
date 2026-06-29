<template>
  <div>
    <div class="page-header">
      <h3>仪表盘配置</h3>
      <el-button type="primary" @click="showWidgetDialog = true">管理组件</el-button>
    </div>
    <el-tabs v-model="activeTab">
      <el-tab-pane label="角色配置" name="role">
        <el-form :inline="true" class="query-form">
          <el-form-item label="选择角色">
            <el-select v-model="selectedRoleId" placeholder="请选择角色" @change="handleRoleChange" style="width: 200px;">
              <el-option v-for="r in roles" :key="r.id" :label="r.name" :value="r.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSave" :loading="saving" :disabled="!selectedRoleId">保存配置</el-button>
          </el-form-item>
        </el-form>

        <el-table :data="widgetConfigs" border v-loading="loading" stripe>
          <el-table-column label="排序" width="80">
            <template #default="{ row }">
              <el-input-number v-model="row.sortOrder" :min="0" :max="100" size="small" controls-position="right" />
            </template>
          </el-table-column>
          <el-table-column prop="widgetName" label="组件名称" width="150" />
          <el-table-column prop="widgetType" label="组件类型" width="120">
            <template #default="{ row }">
              <el-tag size="small">{{ typeLabel(row.widgetType) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="description" label="描述" min-width="200" />
          <el-table-column label="启用" width="80" align="center">
            <template #default="{ row }">
              <el-switch v-model="row.isEnabled" />
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="showWidgetDialog" title="组件管理" width="900px" @close="fetchWidgets">
      <el-button type="primary" @click="handleAddWidget" style="margin-bottom: 16px;">新增组件</el-button>
      <el-table :data="widgets" border v-loading="widgetLoading">
        <el-table-column prop="widgetKey" label="组件Key" width="150" />
        <el-table-column prop="widgetName" label="组件名称" width="120" />
        <el-table-column prop="widgetType" label="类型" width="100">
          <template #default="{ row }">
            <el-tag size="small">{{ typeLabel(row.widgetType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="dataSourceType" label="数据源" width="100">
          <template #default="{ row }">
            <el-tag size="small" :type="row.dataSourceType === 'sql' ? 'success' : 'warning'">{{ row.dataSourceType }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEditWidget(row)">编辑</el-button>
            <el-popconfirm title="确定删除此组件？" @confirm="handleDeleteWidget(row.id)">
              <template #reference>
                <el-button link type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <el-dialog v-model="showWidgetForm" :title="widgetForm.id ? '编辑组件' : '新增组件'" width="700px">
      <el-form :model="widgetForm" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="组件Key" required>
              <el-input v-model="widgetForm.widgetKey" placeholder="唯一标识，如: stat_orders" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="组件名称" required>
              <el-input v-model="widgetForm.widgetName" placeholder="显示名称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="组件类型" required>
              <el-select v-model="widgetForm.widgetType" style="width: 100%">
                <el-option label="统计卡片" value="stat_card" />
                <el-option label="列表" value="list" />
                <el-option label="快捷链接" value="quick_links" />
                <el-option label="信息卡片" value="info_card" />
                <el-option label="图表" value="chart" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="图标">
              <el-input v-model="widgetForm.icon" placeholder="Element Plus图标名" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="描述">
          <el-input v-model="widgetForm.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="数据源类型" required>
              <el-select v-model="widgetForm.dataSourceType" style="width: 100%">
                <el-option label="SQL查询" value="sql" />
                <el-option label="计数查询" value="count" />
                <el-option label="API调用" value="api" />
                <el-option label="最近日志" value="recent_logs" />
                <el-option label="未读消息" value="unread_messages" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="刷新间隔(秒)">
              <el-input-number v-model="widgetForm.refreshInterval" :min="0" :max="3600" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="数据源配置" required>
          <el-input v-model="widgetForm.dataSourceConfig" type="textarea" :rows="4" placeholder='JSON格式，如: {"table": "orders", "condition": "is_deleted = FALSE"}' />
          <el-button size="small" @click="handlePreview" style="margin-top: 8px;">预览数据</el-button>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="布局配置">
              <el-input v-model="widgetForm.layoutConfig" type="textarea" :rows="2" placeholder='{"span": 12}' />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="排序">
              <el-input-number v-model="widgetForm.sortOrder" :min="0" :max="100" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="showWidgetForm = false">取消</el-button>
        <el-button type="primary" @click="handleSaveWidget" :loading="savingWidget">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showPreviewDialog" title="数据预览" width="800px">
      <pre style="max-height: 400px; overflow: auto; background: #f5f7fa; padding: 16px; border-radius: 4px;">{{ previewData }}</pre>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { dashboardApi } from '@/api/dashboard'
import { roleApi } from '@/api/system'

const loading = ref(false)
const saving = ref(false)
const roles = ref<any[]>([])
const selectedRoleId = ref<number | null>(null)
const widgetConfigs = ref<any[]>([])
const activeTab = ref('role')

const showWidgetDialog = ref(false)
const widgetLoading = ref(false)
const widgets = ref<any[]>([])

const showWidgetForm = ref(false)
const savingWidget = ref(false)
const widgetForm = ref<any>({
  widgetKey: '',
  widgetName: '',
  widgetType: 'stat_card',
  description: '',
  icon: '',
  dataSourceType: 'sql',
  dataSourceConfig: '',
  layoutConfig: '{"span": 24}',
  refreshInterval: 0,
  sortOrder: 0
})

const showPreviewDialog = ref(false)
const previewData = ref('')

const typeMap: Record<string, string> = {
  stat_card: '统计卡片',
  list: '列表',
  quick_links: '快捷链接',
  info_card: '信息卡片',
  chart: '图表'
}

function typeLabel(type: string) {
  return typeMap[type] || type
}

async function fetchRoles() {
  try {
    const res: any = await roleApi.getAll()
    roles.value = res.data || []
  } catch (e: unknown) { console.warn('[DashboardConfig] fetchRoles failed', e) }
}

async function handleRoleChange() {
  if (!selectedRoleId.value) return
  loading.value = true
  try {
    const res: any = await dashboardApi.getRoleConfig(selectedRoleId.value)
    widgetConfigs.value = (res.data || []).map((item: any) => ({
      widgetKey: item.widgetKey,
      widgetName: item.widgetName,
      widgetType: item.widgetType,
      description: item.description,
      icon: item.icon,
      isEnabled: item.isEnabled,
      config: item.config,
      sortOrder: item.sortOrder
    }))
  } finally { loading.value = false }
}

async function handleSave() {
  if (!selectedRoleId.value) return
  saving.value = true
  try {
    const sorted = [...widgetConfigs.value].sort((a, b) => a.sortOrder - b.sortOrder)
    await dashboardApi.saveRoleConfig(selectedRoleId.value, sorted)
    ElMessage.success('保存成功')
  } finally { saving.value = false }
}

async function fetchWidgets() {
  widgetLoading.value = true
  try {
    const res: any = await dashboardApi.getWidgets()
    widgets.value = res.data || []
  } finally { widgetLoading.value = false }
}

function handleAddWidget() {
  widgetForm.value = {
    id: null,
    widgetKey: '',
    widgetName: '',
    widgetType: 'stat_card',
    description: '',
    icon: '',
    dataSourceType: 'sql',
    dataSourceConfig: '',
    layoutConfig: '{"span": 24}',
    refreshInterval: 0,
    sortOrder: 0
  }
  showWidgetForm.value = true
}

function handleEditWidget(row: any) {
  widgetForm.value = {
    id: row.id,
    widgetKey: row.widgetKey,
    widgetName: row.widgetName,
    widgetType: row.widgetType,
    description: row.description || '',
    icon: row.icon || '',
    dataSourceType: row.dataSourceType || 'sql',
    dataSourceConfig: row.dataSourceConfig ? JSON.stringify(row.dataSourceConfig, null, 2) : '',
    layoutConfig: row.layoutConfig ? JSON.stringify(row.layoutConfig, null, 2) : '{"span": 24}',
    refreshInterval: row.refreshInterval || 0,
    sortOrder: row.sortOrder || 0
  }
  showWidgetForm.value = true
}

async function handleSaveWidget() {
  if (!widgetForm.value.widgetKey || !widgetForm.value.widgetName || !widgetForm.value.widgetType) {
    ElMessage.warning('请填写必填字段')
    return
  }
  savingWidget.value = true
  try {
    const data = {
      widgetKey: widgetForm.value.widgetKey,
      widgetName: widgetForm.value.widgetName,
      widgetType: widgetForm.value.widgetType,
      description: widgetForm.value.description,
      icon: widgetForm.value.icon,
      defaultConfig: widgetForm.value.layoutConfig,
      dataSourceType: widgetForm.value.dataSourceType,
      dataSourceConfig: widgetForm.value.dataSourceConfig,
      layoutConfig: widgetForm.value.layoutConfig,
      refreshInterval: widgetForm.value.refreshInterval,
      sortOrder: widgetForm.value.sortOrder
    }

    if (widgetForm.value.id) {
      await dashboardApi.updateWidget(widgetForm.value.id, data)
      ElMessage.success('更新成功')
    } else {
      await dashboardApi.addWidget(data)
      ElMessage.success('新增成功')
    }
    showWidgetForm.value = false
    fetchWidgets()
  } finally { savingWidget.value = false }
}

async function handleDeleteWidget(id: number) {
  try {
    await dashboardApi.deleteWidget(id)
    ElMessage.success('删除成功')
    fetchWidgets()
  } catch (e: unknown) { console.warn('[DashboardConfig] handleDeleteWidget failed', e) }
}

async function handlePreview() {
  try {
    const res: any = await dashboardApi.previewWidgetData({
      dataSourceType: widgetForm.value.dataSourceType,
      dataSourceConfig: widgetForm.value.dataSourceConfig
    })
    previewData.value = JSON.stringify(res.data, null, 2)
    showPreviewDialog.value = true
  } catch (e: any) {
    ElMessage.error(e.response?.data?.message || '预览失败')
  }
}

onMounted(() => { fetchRoles() })
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.query-form { margin-bottom: 16px; }
</style>
