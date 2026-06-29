<template>
  <div>
    <div class="page-header">
      <h3>业务员对照管理</h3>
      <div class="header-actions">
        <el-button type="primary" @click="handleAdd">新增对照</el-button>
      </div>
    </div>

    <el-card>
      <!-- 已映射列表 -->
      <el-table :data="paginatedMappedList" border v-loading="loading" @sort-change="handleMappedSortChange">
        <el-table-column label="员工编号" prop="employeeNo" width="120" sortable="custom" />
        <el-table-column label="员工姓名" prop="employeeName" width="120" sortable="custom" />
        <el-table-column label="所属部门" prop="departmentName" width="150" sortable="custom" />
        <el-table-column label="业务员编码" prop="salespersonCode" width="120" sortable="custom" />
        <el-table-column label="业务员名称" prop="salespersonName" width="150" sortable="custom" />
        <el-table-column label="类型" prop="type" width="100" sortable="custom">
          <template #default="{ row }">
            <el-tag :type="row.type === 'supervisor' ? 'warning' : ''">{{ row.type === 'supervisor' ? '业务主管' : '业务员' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="180" sortable="custom">
          <template #default="{ row }">{{ row.createdAt ? new Date(row.createdAt).toLocaleString() : '--' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination-wrapper" v-if="mappedList.length > 0">
        <el-pagination
          v-model:current-page="mappedCurrentPage"
          v-model:page-size="mappedPageSize"
          :total="mappedList.length"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next"
        />
      </div>

      <!-- 无映射员工列表 -->
      <div v-if="unmappedEmployees.length" style="margin-top:16px">
        <h4 style="margin-bottom:8px">未对照员工</h4>
        <el-table :data="sortedUnmappedEmployees" border stripe max-height="400" @sort-change="handleUnmappedSortChange">
          <el-table-column label="员工编号" prop="employeeNo" width="120" sortable="custom" />
          <el-table-column label="员工姓名" prop="name" width="120" sortable="custom" />
          <el-table-column label="所属部门" prop="departmentName" width="200" sortable="custom" />
          <el-table-column label="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button size="small" type="primary" @click="handleMapFromUnmapped(row)">建立对照</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" title="业务员对照" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="员工" prop="employeeId">
          <el-select v-model="form.employeeId" placeholder="选择员工" filterable style="width:100%" :disabled="isEdit">
            <el-option v-for="emp in bizEmployees" :key="emp.id" :label="emp.name + ' (' + emp.employeeNo + ')'" :value="emp.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="业务员" prop="salespersonCode">
          <el-select v-model="form.salespersonCode" placeholder="选择业务员" filterable style="width:100%">
            <el-option v-for="sp in salespersonList" :key="sp.code" :label="sp.name + ' (' + sp.code + ')'" :value="sp.code" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型" prop="type">
          <el-radio-group v-model="form.type">
            <el-radio value="salesperson">业务员（受限，只看自己数据）</el-radio>
            <el-radio value="supervisor">业务主管（不受限，看全部数据）</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { erpSettingsApi } from '@/api/erpSettings'
import { employeeApi } from '@/api/system'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const submitting = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)

interface Employee {
  id: number
  employeeNo: string
  name: string
  departmentName?: string
}

interface SalespersonMapping {
  employeeId: number
  employeeName: string
  employeeNo: string
  departmentName?: string
  salespersonCode: string
  salespersonName: string
  type: string
  createdAt: string
}

const mappedList = ref<SalespersonMapping[]>([])
const allEmployees = ref<Employee[]>([])
const salespersonList = ref<{ code: string; name: string }[]>([])

// 已映射列表：排序 + 分页
let mappedSortState: { prop: string; order: 'ascending' | 'descending' } | null = null
const mappedCurrentPage = ref(1)
const mappedPageSize = ref(20)
const paginatedMappedList = computed(() => {
  let list = [...mappedList.value]
  if (mappedSortState) {
    const { prop, order } = mappedSortState
    const asc = order === 'ascending'
    list.sort((a: any, b: any) => {
      const va = a[prop]
      const vb = b[prop]
      if (va == null) return asc ? -1 : 1
      if (vb == null) return asc ? 1 : -1
      if (typeof va === 'string' && typeof vb === 'string') {
        return asc ? va.localeCompare(vb, 'zh-CN') : vb.localeCompare(va, 'zh-CN')
      }
      return asc ? (va > vb ? 1 : -1) : (va < vb ? 1 : -1)
    })
  }
  const start = (mappedCurrentPage.value - 1) * mappedPageSize.value
  return list.slice(start, start + mappedPageSize.value)
})

// 未映射员工列表：排序（无需分页，数据量小）
let unmappedSortState: { prop: string; order: 'ascending' | 'descending' } | null = null
const sortedUnmappedEmployees = computed(() => {
  let list = [...unmappedEmployees.value]
  if (unmappedSortState) {
    const { prop, order } = unmappedSortState
    const asc = order === 'ascending'
    list.sort((a: any, b: any) => {
      const va = a[prop]
      const vb = b[prop]
      if (va == null) return asc ? -1 : 1
      if (vb == null) return asc ? 1 : -1
      if (typeof va === 'string' && typeof vb === 'string') {
        return asc ? va.localeCompare(vb, 'zh-CN') : vb.localeCompare(va, 'zh-CN')
      }
      return asc ? (va > vb ? 1 : -1) : (va < vb ? 1 : -1)
    })
  }
  return list
})

// 业务部员工：部门名称包含"业务"
const bizEmployees = computed(() =>
  allEmployees.value.filter(emp => emp.name && emp.departmentName?.includes('业务'))
)

const form = ref<{ employeeId: number | null; salespersonCode: string; type: string }>({
  employeeId: null,
  salespersonCode: '',
  type: 'salesperson'
})

const rules = {
  employeeId: [{ required: true, message: '请选择员工', trigger: 'change' }],
  salespersonCode: [{ required: true, message: '请选择业务员', trigger: 'change' }]
}

const unmappedEmployees = computed(() => {
  const mappedIds = new Set(mappedList.value.map(m => m.employeeId))
  return bizEmployees.value.filter(emp => !mappedIds.has(emp.id))
})

onMounted(async () => {
  loading.value = true
  try {
    // 并行加载映射列表和业务员列表（不需要 employee 权限）
    const [mappingRes, spRes] = await Promise.all([
      erpSettingsApi.getSalespersonMappings(),
      erpSettingsApi.getSalespersons()
    ])
    mappedList.value = mappingRes.data || []
    salespersonList.value = spRes.data || []

    // 单独加载员工列表（可能需要 employee 权限，无权限时报 403 不阻塞页面）
    try {
      const empRes = await employeeApi.getAll()
      allEmployees.value = empRes.data || []
    } catch (empErr: any) {
      if (empErr?.response?.status === 403) {
        console.warn('无 employee 权限，员工列表不可用')
      } else {
        console.error('加载员工列表失败', empErr)
      }
    }
  } catch {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
})

function handleMappedSortChange(sort: { prop: string | null; order: string | null }) {
  mappedCurrentPage.value = 1
  if (!sort.prop || !sort.order) {
    mappedSortState = null
  } else {
    mappedSortState = { prop: sort.prop, order: sort.order as 'ascending' | 'descending' }
  }
}

function handleUnmappedSortChange(sort: { prop: string | null; order: string | null }) {
  if (!sort.prop || !sort.order) {
    unmappedSortState = null
  } else {
    unmappedSortState = { prop: sort.prop, order: sort.order as 'ascending' | 'descending' }
  }
}

function handleAdd() {
  isEdit.value = false
  form.value = { employeeId: null, salespersonCode: '', type: 'salesperson' }
  dialogVisible.value = true
}

function handleEdit(row: SalespersonMapping) {
  isEdit.value = true
  form.value = {
    employeeId: row.employeeId,
    salespersonCode: row.salespersonCode,
    type: row.type || 'salesperson'
  }
  dialogVisible.value = true
}

function handleMapFromUnmapped(emp: Employee) {
  isEdit.value = false
  form.value = { employeeId: emp.id, salespersonCode: '', type: 'salesperson' }
  dialogVisible.value = true
}

async function handleSubmit() {
  if (!form.value.employeeId || !form.value.salespersonCode) return
  submitting.value = true
  try {
    const sp = salespersonList.value.find(s => s.code === form.value.salespersonCode)
    const res: any = await erpSettingsApi.saveSalespersonMapping({
      employeeId: form.value.employeeId,
      salespersonCode: form.value.salespersonCode,
      salespersonName: sp?.name || '',
      type: form.value.type
    })
    if (res.success) {
      ElMessage.success('保存成功')
      dialogVisible.value = false
      // 刷新列表
      const mappingRes: any = await erpSettingsApi.getSalespersonMappings()
      mappedList.value = mappingRes.data || []
    } else {
      ElMessage.error(res.message || '保存失败')
    }
  } catch {
    ElMessage.error('保存失败')
  } finally {
    submitting.value = false
  }
}

async function handleDelete(row: SalespersonMapping) {
  try {
    await ElMessageBox.confirm(`确定删除员工「${row.employeeName}」的业务员映射？`, '确认删除')
    const res: any = await erpSettingsApi.deleteSalespersonMapping(row.employeeId)
    if (res.success) {
      ElMessage.success('删除成功')
      const mappingRes: any = await erpSettingsApi.getSalespersonMappings()
      mappedList.value = mappingRes.data || []
    } else {
      ElMessage.error(res.message || '删除失败')
    }
  } catch {
    // 用户取消
  }
}
</script>

<style scoped>
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 4px;
  margin-bottom: 20px;
}
</style>