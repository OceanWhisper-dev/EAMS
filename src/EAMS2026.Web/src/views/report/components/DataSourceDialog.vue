<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑数据源' : '新增数据源'"
    width="640px"
    @update:model-value="$emit('update:visible', $event)"
    @closed="handleClosed"
    append-to-body
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" size="small">
      <el-form-item label="标识" prop="name" v-if="!isEdit">
        <el-input v-model="form.name" placeholder="唯一标识，如 sqlserver_report" :disabled="isEdit" />
        <div class="el-form-item__tip">不能重复，创建后不可修改</div>
      </el-form-item>
      <el-form-item label="显示名称" prop="displayName">
        <el-input v-model="form.displayName" placeholder="如：生产环境 SQL Server" />
      </el-form-item>
      <el-form-item label="数据库类型" prop="dbType">
        <el-radio-group v-model="form.dbType">
          <el-radio value="postgresql">PostgreSQL</el-radio>
          <el-radio value="sqlserver">SQL Server</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="连接字符串" prop="connectionString">
        <el-input
          v-model="form.connectionString"
          type="textarea"
          :rows="3"
          placeholder="Host=xxx;Port=5432;Database=xxx;Username=xxx;Password=xxx"
        />
      </el-form-item>
      <el-form-item label="描述">
        <el-input v-model="form.description" type="textarea" :rows="2" placeholder="可选" />
      </el-form-item>
      <el-form-item label="排序">
        <el-input-number v-model="form.sortOrder" :min="0" :max="999" />
      </el-form-item>
      <el-form-item label="启用">
        <el-switch v-model="form.isEnabled" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button size="small" @click="handleTest" :loading="testing">
        {{ testing ? '测试中...' : '测试连接' }}
      </el-button>
      <el-button size="small" @click="$emit('update:visible', false)">取消</el-button>
      <el-button type="primary" size="small" @click="handleSubmit" :loading="submitting">
        {{ submitting ? '保存中...' : '保存' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { erpSettingsApi } from '@/api/erpSettings'
import { ElMessage, ElMessageBox } from 'element-plus'

/** 统一 API 响应格式 */
interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

/** 数据源 DTO */
interface DataSourceDto {
  id: number
  name: string
  displayName: string
  dbType: string
  connectionString: string
  description?: string
  sortOrder: number
  isEnabled: boolean
}

const props = defineProps<{
  visible: boolean
  data?: DataSourceDto | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  saved: []
}>()

const isEdit = ref(false)
const submitting = ref(false)
const testing = ref(false)
const formRef = ref<any>(null)

const form = reactive({
  name: '',
  displayName: '',
  dbType: 'postgresql',
  connectionString: '',
  description: '',
  sortOrder: 0,
  isEnabled: true
})

const rules = {
  name: [{ required: true, message: '请输入标识', trigger: 'blur' }],
  displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
  dbType: [{ required: true, message: '请选择数据库类型', trigger: 'change' }],
  connectionString: [{ required: true, message: '请输入连接字符串', trigger: 'blur' }]
}

watch(() => props.visible, (val) => {
  if (val && props.data) {
    isEdit.value = true
    form.name = props.data.name
    form.displayName = props.data.displayName
    form.dbType = props.data.dbType
    form.connectionString = props.data.connectionString
    form.description = props.data.description || ''
    form.sortOrder = props.data.sortOrder
    form.isEnabled = props.data.isEnabled
  } else if (val) {
    isEdit.value = false
    resetForm()
  }
})

function resetForm() {
  form.name = ''
  form.displayName = ''
  form.dbType = 'postgresql'
  form.connectionString = ''
  form.description = ''
  form.sortOrder = 0
  form.isEnabled = true
}

function handleClosed() {
  resetForm()
  formRef.value?.clearValidate()
}

async function handleTest() {
  if (!form.connectionString) {
    ElMessage.warning('请先输入连接字符串')
    return
  }
  testing.value = true
  try {
    const res = await erpSettingsApi.testDataSource({
      dbType: form.dbType,
      connectionString: form.connectionString
    }) as unknown as ApiResponse<unknown>
    ElMessage.success('连接成功')
  } catch (e: unknown) {
    const axiosErr = e as { response?: { data?: { message?: string } }; message?: string }
    const msg = axiosErr?.response?.data?.message || axiosErr?.message || '连接失败'
    ElMessage.error(msg)
  } finally {
    testing.value = false
  }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      const res = await erpSettingsApi.updateDataSource(props.data!.id, {
        displayName: form.displayName,
        dbType: form.dbType,
        connectionString: form.connectionString,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
        isEnabled: form.isEnabled
      }) as unknown as ApiResponse<{ success?: boolean; message?: string }>
      if (res.data?.success !== false) {
        ElMessage.success('更新成功')
        emit('update:visible', false)
        emit('saved')
      } else {
        ElMessage.error(res.data?.message || '更新失败')
      }
    } else {
      const res = await erpSettingsApi.addDataSource({
        name: form.name,
        displayName: form.displayName,
        dbType: form.dbType,
        connectionString: form.connectionString,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
        isEnabled: form.isEnabled
      }) as unknown as ApiResponse<{ success?: boolean; message?: string }>
      if (res.data?.success !== false) {
        ElMessage.success('创建成功')
        emit('update:visible', false)
        emit('saved')
      } else {
        ElMessage.error(res.data?.message || '创建失败')
      }
    }
  } catch (e: unknown) {
    const errMsg = (e as { message?: string })?.message || '操作失败'
    ElMessage.error(errMsg)
  } finally {
    submitting.value = false
  }
}
</script>