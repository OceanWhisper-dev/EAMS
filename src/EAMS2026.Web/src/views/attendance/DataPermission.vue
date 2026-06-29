<template>
  <div>
    <div class="page-header">
      <h3>数据权限配置</h3>
      <div class="header-actions">
        <el-button @click="handleImport">导入</el-button>
        <el-button @click="handleExport">导出</el-button>
        <el-button @click="handlePrint">打印</el-button>
      </div>
    </div>
    <el-card>
      <el-alert title="配置各角色在考勤管理模块中可以查看的数据范围" type="info" show-icon :closable="false" style="margin-bottom:16px" />
      <el-table :data="tableData" border v-loading="loading">
        <el-table-column prop="roleName" label="角色名称" min-width="180" />
        <el-table-column label="数据范围" width="250">
          <template #default="{ row }">
            <el-select v-model="row.dataScope" placeholder="请选择数据范围" style="width:180px" @change="(val: string) => handleScopeChange(row, val)">
              <el-option label="全部数据" value="ALL" />
              <el-option label="本部门数据" value="DEPARTMENT" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120">
          <template #default="{ row }">
            <el-button size="small" type="danger" :disabled="!row.dataScope" @click="handleClear(row)">清除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { attendanceApi } from '@/api/attendance'
import { downloadBlob, importExportApi } from '@/api/system'
import { openPrintWindow } from '@/utils/print'

const loading = ref(false)
const roles = ref<any[]>([])
const rules = ref<any[]>([])
const tableData = ref<any[]>([])

onMounted(async () => {
  loading.value = true
  try
  {
    const [rolesRes, rulesRes] = await Promise.all([
      attendanceApi.getDataPermissionRoles(),
      attendanceApi.getDataPermissionRules('attendance')
    ])
    roles.value = rolesRes.data || []
    rules.value = rulesRes.data || []

    const ruleMap = new Map<number, string>()
    for (const r of rules.value)
      ruleMap.set(r.roleId, r.dataScope)

    tableData.value = roles.value.map((role: any) => ({
      roleId: role.id,
      roleName: role.name,
      dataScope: ruleMap.get(role.id) || ''
    }))
  }
  catch (e: any)
  {
    ElMessage.error('加载数据失败: ' + (e.message || ''))
  }
  finally
  {
    loading.value = false
  }
})

async function handleScopeChange(row: any, val: string)
{
  try
  {
    await attendanceApi.saveDataPermissionRule('attendance', {
      roleId: row.roleId,
      dataScope: val
    })
    ElMessage.success('保存成功')
  }
  catch (e: any)
  {
    ElMessage.error('保存失败: ' + (e.message || ''))
  }
}

async function handleClear(row: any)
{
  try
  {
    await attendanceApi.deleteDataPermissionRule('attendance', row.roleId)
    row.dataScope = ''
    ElMessage.success('已清除')
  }
  catch (e: any)
  {
    ElMessage.error('清除失败: ' + (e.message || ''))
  }
}

function handleExport() {
  ElMessage.info('导出功能开发中')
}

function handleImport() {
  ElMessage.info('导入功能开发中')
}

function handlePrint() {
  if (!tableData.value.length) {
    ElMessage.warning('暂无数据可打印')
    return
  }
  openPrintWindow('数据权限配置', [
    { label: '角色名称', value: (r: any) => r.roleName },
    { label: '数据范围', value: (r: any) => r.dataScope === 'ALL' ? '全部数据' : r.dataScope === 'DEPARTMENT' ? '本部门数据' : '' }
  ], tableData.value)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.header-actions { display: flex; gap: 8px; }
</style>