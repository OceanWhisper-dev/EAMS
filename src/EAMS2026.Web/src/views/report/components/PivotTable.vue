<template>
  <div class="pivot-table-wrapper">
    <div class="pivot-toolbar">
      <div class="pivot-format-select">
        <label>格式名：</label>
        <el-select v-model="currentFormatId" size="small" @change="onFormatChange" style="width:200px">
          <el-option
            v-for="pv in pivotViews"
            :key="pv.id"
            :label="pv.isShared && pv.creatorName ? `${pv.creatorName} - ${pv.pivotName}` : pv.pivotName"
            :value="pv.id"
          />
        </el-select>
        <el-button size="small" @click="saveAs">另存</el-button>
        <el-button size="small" @click="deleteFormat" :disabled="!currentFormatId || !isOwnFormat">删除</el-button>
        <el-button size="small" @click="showShareDialog" :disabled="!currentFormatId || !isOwnFormat" type="warning">分享</el-button>
        <el-tag v-if="currentFormatId && !isOwnFormat" type="info" size="small" effect="plain" style="margin-left:8px;">只读 - 可另存为副本</el-tag>
        <el-button size="small" @click="showConfigDialog" type="primary">字段配置</el-button>
        <el-button size="small" @click="handleExport" type="success" v-if="canExport">导出</el-button>
      </div>
      <div class="pivot-info">
        共 {{ totalRecords }} 条记录
      </div>
    </div>

    <PivotTable
      v-if="hasValidConfig"
      :options="pivotOptions"
      style="width:100%;height:600px;"
    />
    <el-empty v-else description="请点击「字段配置」设置透视表的行、列和值字段" />

    <!-- 字段配置对话框 -->
    <el-dialog v-model="configDialogVisible" title="透视表字段配置" width="600px">
      <el-form label-width="100px">
        <el-form-item label="行字段">
          <el-select v-model="pivotConfig.rows" multiple size="small" placeholder="选择行字段" style="width:100%">
            <el-option v-for="col in props.columns" :key="col.field" :label="col.title || col.field" :value="col.field" />
          </el-select>
        </el-form-item>
        <el-form-item label="列字段">
          <el-select v-model="pivotConfig.columns" multiple size="small" placeholder="选择列字段" style="width:100%">
            <el-option v-for="col in props.columns" :key="col.field" :label="col.title || col.field" :value="col.field" />
          </el-select>
        </el-form-item>
        <el-form-item label="值字段">
          <div style="width:100%;">
            <div v-for="(indicator, idx) in pivotConfig.indicators" :key="idx" class="indicator-row">
              <el-select v-model="indicator.field" size="small" placeholder="选择字段" style="flex:1;">
                <el-option v-for="col in props.columns" :key="col.field" :label="col.title || col.field" :value="col.field" />
              </el-select>
              <el-select v-model="indicator.aggFunc" size="small" style="width:120px;">
                <el-option label="求和" value="SUM" />
                <el-option label="计数" value="COUNT" />
                <el-option label="平均值" value="AVG" />
                <el-option label="最大值" value="MAX" />
                <el-option label="最小值" value="MIN" />
              </el-select>
              <el-button size="small" type="danger" text @click="removeIndicator(idx)" v-if="pivotConfig.indicators.length > 1">×</el-button>
            </div>
            <el-button size="small" @click="addIndicator" style="margin-top:4px;">+ 添加值字段</el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="configDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="applyConfig">应用</el-button>
      </template>
    </el-dialog>

    <!-- 保存格式对话框 -->
    <el-dialog v-model="saveDialogVisible" title="保存格式" width="400px">
      <el-form @submit.prevent="confirmSave">
        <el-form-item label="格式名">
          <el-input v-model="saveName" placeholder="请输入格式名" @keyup.enter="confirmSave" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" native-type="submit" @click="confirmSave">确定</el-button>
          <el-button @click="saveDialogVisible = false">取消</el-button>
        </el-form-item>
      </el-form>
    </el-dialog>

    <!-- 共享格式对话框 -->
    <el-dialog v-model="shareDialogVisible" title="共享格式" width="500px">
      <div v-if="shareLoading" style="text-align:center;padding:20px;color:#909399;">加载中...</div>
      <template v-else>
        <div style="margin-bottom:12px;font-size:13px;color:#606266;">
          当前格式：<strong>{{ currentPivotName }}</strong>
        </div>
        <el-divider style="margin:8px 0;" />
        <div style="margin-bottom:8px;font-weight:bold;font-size:13px;">已分享给：</div>
        <div v-if="shareList.length === 0" style="color:#909399;font-size:13px;padding:8px 0;">暂无共享目标</div>
        <el-table v-else :data="shareList" size="small" max-height="200">
          <el-table-column label="类型" width="80">
            <template #default="{ row }">
              <el-tag :type="row.targetType === 'role' ? 'primary' : 'success'" size="small">
                {{ row.targetType === 'role' ? '角色' : '用户' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="名称" prop="targetName" />
          <el-table-column label="操作" width="80">
            <template #default="{ row }">
              <el-button size="small" type="danger" link @click="removeShare(row.id)">取消</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-divider style="margin:12px 0;" />
        <div style="margin-bottom:8px;font-weight:bold;font-size:13px;">添加共享：</div>
        <el-form @submit.prevent="addShare" :inline="true">
          <el-form-item label="类型" style="margin-bottom:0;">
            <el-select v-model="newShareTargetType" size="small" style="width:90px;">
              <el-option label="用户" value="user" />
              <el-option label="角色" value="role" />
            </el-select>
          </el-form-item>
          <el-form-item label="目标" style="margin-bottom:0;">
            <el-select v-model="newShareTargetId" size="small" filterable style="width:160px;" placeholder="请选择">
              <el-option
                v-for="opt in filteredShareTargets"
                :key="opt.id + ':' + opt.type"
                :label="opt.name"
                :value="opt.id"
              >
                <span>{{ opt.name }}</span>
                <span style="float:right;color:#909399;font-size:12px;">{{ opt.type === 'role' ? '角色' : '用户' }}</span>
              </el-option>
            </el-select>
          </el-form-item>
          <el-form-item style="margin-bottom:0;">
            <el-button size="small" type="primary" @click="addShare">添加</el-button>
          </el-form-item>
        </el-form>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed, reactive } from 'vue'
import { PivotTable } from '@visactor/vue-vtable'
import { reportApi, type ReportPivotDto, type ReportColumnDto, type PivotViewShareDto } from '@/api/report'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

/** 后端统一响应格式 */
interface ApiResult<T = any> {
  success: boolean
  data?: T
  message?: string
}

interface IndicatorConfig {
  field: string
  title?: string
  aggFunc: 'SUM' | 'COUNT' | 'AVG' | 'MAX' | 'MIN'
}

interface PivotConfig {
  rows: string[]
  columns: string[]
  indicators: IndicatorConfig[]
}

const props = defineProps<{
  reportId: number
  columns: ReportColumnDto[]
  rows: Record<string, unknown>[]
}>()

const emit = defineEmits<{
  save: [params: Record<string, unknown>]
}>()

const authStore = useAuthStore()

const pivotViews = ref<ReportPivotDto[]>([])
const currentFormatId = ref<number | undefined>()
const totalRecords = ref(0)
const saveDialogVisible = ref(false)
const saveName = ref('')
const canExport = ref(false)
const configApplied = ref(false)

/** 当前透视表配置 */
const pivotConfig = reactive<PivotConfig>({
  rows: [],
  columns: [],
  indicators: [{ field: '', aggFunc: 'SUM' }]
})

/** 原始 pivotParams 字符串（用于保存/加载） */
const savedPivotParams = ref('{}')

const configDialogVisible = ref(false)

/** 是否有有效配置 */
const hasValidConfig = computed(() => {
  return (pivotConfig.rows.length > 0 || pivotConfig.columns.length > 0) &&
    pivotConfig.indicators.length > 0 &&
    pivotConfig.indicators.some(i => i.field)
})

/** 转换为 VTable PivotTable options */
const pivotOptions = computed(() => {
  if (!hasValidConfig.value) return {}

  const validIndicators = pivotConfig.indicators.filter(i => i.field)
  const rows = pivotConfig.rows
  const columns = pivotConfig.columns

  return {
    rows,
    columns,
    indicators: validIndicators.map(ind => ({
      indicatorKey: ind.field,
      title: ind.title || ind.field,
      width: 'auto',
      format: (value: any) => {
        if (value === null || value === undefined) return ''
        if (typeof value === 'number') {
          // 金额显示两位小数
          return value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
        }
        return String(value)
      }
    })),
    records: props.rows,
    dataConfig: {
      aggregateRules: validIndicators.map(ind => ({
        field: ind.field,
        aggFunc: ind.aggFunc as any
      }))
    },
    corner: {
      titleOnDimension: 'row'
    },
    width: '100%',
    height: 600,
    dragHeader: true,
    columnResize: true,
  }
})

onMounted(async () => {
  // 检查导出权限
  try {
    const permRes = await reportApi.checkPermission(props.reportId, 'export')
    canExport.value = (permRes.data as any)?.hasPermission === true
  } catch (e) {
    canExport.value = false
    console.warn('[PivotTable] 检查导出权限失败', e)
  }
  await loadPivotViews()
})

watch(() => props.rows, () => {
  totalRecords.value = props.rows.length
}, { deep: false })

async function loadPivotViews() {
  try {
    const res = await reportApi.getPivotViews(props.reportId)
    pivotViews.value = res.data || []
    // 找到 isLast 的配置，没有则取第一个
    const last = pivotViews.value.find(p => p.isLast) || pivotViews.value[0]
    if (last) {
      currentFormatId.value = last.id
      savedPivotParams.value = last.pivotParams
      applySavedParams(last.pivotParams)
    }
  } catch (e) {
    console.warn('[PivotTable] 加载透视表视图失败', e)
  }
}

/** 从保存的 pivotParams JSON 恢复配置 */
function applySavedParams(params: string) {
  if (!params || params === '{}') return
  try {
    const config = JSON.parse(params)
    if (config.rows) pivotConfig.rows = config.rows
    if (config.columns) pivotConfig.columns = config.columns
    if (config.indicators && config.indicators.length > 0) {
      pivotConfig.indicators.length = 0
      config.indicators.forEach((ind: IndicatorConfig) => {
        pivotConfig.indicators.push(ind)
      })
    }
    configApplied.value = true
  } catch (e) {
    console.warn('[PivotTable] 透视表参数解析失败', e)
  }
}

/** 构建保存用的 params JSON */
function buildPivotParams(): string {
  return JSON.stringify({
    rows: pivotConfig.rows,
    columns: pivotConfig.columns,
    indicators: pivotConfig.indicators
  })
}

/** 打开字段配置对话框 */
function showConfigDialog() {
  configDialogVisible.value = true
}

/** 应用配置 */
function applyConfig() {
  configDialogVisible.value = false
  configApplied.value = true
  savedPivotParams.value = buildPivotParams()
  ElMessage.success('透视表配置已更新')
}

/** 添加值字段 */
function addIndicator() {
  pivotConfig.indicators.push({ field: '', aggFunc: 'SUM' })
}

/** 移除值字段 */
function removeIndicator(idx: number) {
  pivotConfig.indicators.splice(idx, 1)
}

async function onFormatChange(id: number) {
  const pv = pivotViews.value.find(p => p.id === id)
  if (pv) {
    currentFormatId.value = id
    savedPivotParams.value = pv.pivotParams
    applySavedParams(pv.pivotParams)
  }
}

function saveAs() {
  saveName.value = ''
  saveDialogVisible.value = true
}

async function confirmSave() {
  if (!saveName.value.trim()) {
    ElMessage.warning('请输入格式名')
    return
  }

  try {
    await reportApi.savePivotView({
      reportId: props.reportId,
      pivotName: saveName.value,
      pivotParams: buildPivotParams(),
      isDefault: false,
      isLast: false,
      creatorName: authStore.employeeName || authStore.username
    })
    ElMessage.success('保存成功')
    saveDialogVisible.value = false
    await loadPivotViews()
  } catch (e) {
    ElMessage.error('保存失败')
    console.warn('[PivotTable] 保存格式失败', e)
  }
}

async function deleteFormat() {
  if (!currentFormatId.value) return

  try {
    await ElMessageBox.confirm('确定删除此格式？', '提示')
    await reportApi.deletePivotView(currentFormatId.value)
    ElMessage.success('删除成功')
    currentFormatId.value = undefined
    savedPivotParams.value = '{}'
    await loadPivotViews()
  } catch (e) {
    // 用户取消删除或删除失败
    console.warn('[PivotTable] 删除格式失败', e)
  }
}

// ===== 格式名称计算（当前选中格式）=====
const currentPivotName = computed(() => {
  const pv = pivotViews.value.find(p => p.id === currentFormatId.value)
  return pv?.pivotName || ''
})

// ===== 是否为自有格式（非他人共享）=====
const isOwnFormat = computed(() => {
  if (!currentFormatId.value) return false
  const pv = pivotViews.value.find(p => p.id === currentFormatId.value)
  return pv ? pv.userId === authStore.userId : false
})

// ===== 透视表共享管理 =====
const shareDialogVisible = ref(false)
const shareLoading = ref(false)
const shareList = ref<PivotViewShareDto[]>([])
const shareTargetOptions = ref<{ id: number; name: string; type: string }[]>([])
const newShareTargetType = ref('user')
const newShareTargetId = ref<number | undefined>()

// 按所选类型过滤目标选项
const filteredShareTargets = computed(() =>
  shareTargetOptions.value.filter(o => o.type === newShareTargetType.value)
)

// 切换类型时重置已选目标
watch(newShareTargetType, () => {
  newShareTargetId.value = undefined
})

async function showShareDialog() {
  if (!currentFormatId.value) return
  shareDialogVisible.value = true
  shareLoading.value = true
  shareList.value = []
  newShareTargetType.value = 'user'
  newShareTargetId.value = undefined
  try {
    const [shareRes, principalRes] = await Promise.all([
      reportApi.getShares(currentFormatId.value),
      reportApi.getPrincipalOptions()
    ])
    shareList.value = shareRes.data || []
    shareTargetOptions.value = principalRes.data || []
  } catch (e) {
    ElMessage.error('加载共享信息失败')
    console.warn('[PivotTable] 加载共享信息失败', e)
  } finally {
    shareLoading.value = false
  }
}

async function addShare() {
  if (!currentFormatId.value || !newShareTargetId.value) {
    ElMessage.warning('请选择共享目标')
    return
  }
  try {
    const res = await reportApi.addShare(currentFormatId.value, {
      targetType: newShareTargetType.value,
      targetId: newShareTargetId.value
    }) as unknown as ApiResult
    if (res.success) {
      ElMessage.success('共享成功')
      // 刷新共享列表
      const shareRes = await reportApi.getShares(currentFormatId.value) as unknown as ApiResult<unknown[]>
      shareList.value = (shareRes.data || []) as PivotViewShareDto[]
      newShareTargetId.value = undefined
    } else {
      ElMessage.error(res.message || '共享失败')
    }
  } catch (e) {
    ElMessage.error('共享失败')
    console.warn('[PivotTable] 添加共享失败', e)
  }
}

async function removeShare(shareId: number) {
  if (!currentFormatId.value) return
  try {
    await ElMessageBox.confirm('确定取消对此目标的共享？', '提示')
    const res = await reportApi.removeShare(currentFormatId.value, shareId) as unknown as ApiResult
    if (res.success) {
      ElMessage.success('已取消共享')
      // 刷新共享列表
      const shareRes = await reportApi.getShares(currentFormatId.value) as unknown as ApiResult<unknown[]>
      shareList.value = (shareRes.data || []) as PivotViewShareDto[]
    } else {
      ElMessage.error(res.message || '取消共享失败')
    }
  } catch (e) {
    // 用户取消操作或移除失败
    console.warn('[PivotTable] 移除共享失败', e)
  }
}

async function handleExport() {
  // 使用 XLSX 导出（保留原有功能）
  try {
    const XLSX = await import('xlsx')
    // 将 VTable 的 records 转为 sheet 数据
    const headers = props.columns.map(c => c.title || c.field)
    const dataRows = props.rows.map(row =>
      props.columns.map(c => row[c.field] ?? '')
    )
    const ws = XLSX.utils.aoa_to_sheet([headers, ...dataRows])
    const wb = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(wb, ws, '透视表')
    XLSX.writeFile(wb, `透视表_${new Date().toISOString().slice(0, 10)}.xlsx`)
    ElMessage.success('导出成功')
  } catch (e) {
    ElMessage.error('导出失败')
    console.warn('[PivotTable] 导出失败', e)
  }
}
</script>

<style scoped>
.pivot-table-wrapper {
  width: 100%;
  overflow: visible;
}
.pivot-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  padding: 8px 12px;
  background: #f5f7fa;
  border-radius: 4px;
}
.pivot-format-select {
  display: flex;
  align-items: start;
  gap: 8px;
}
.pivot-format-select label {
  font-size: 13px;
  color: #606266;
  white-space: nowrap;
}
.pivot-info {
  font-size: 13px;
  color: #909399;
}
.indicator-row {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-bottom: 4px;
}
</style>
