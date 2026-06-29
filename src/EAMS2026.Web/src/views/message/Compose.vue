<template>
  <div>
    <div class="page-header">
      <h3>写消息</h3>
      <el-button @click="handleBack">返回</el-button>
    </div>
    <el-card>
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="收件人" prop="receiverId">
          <el-select v-model="form.receiverId" filterable remote :remote-method="searchUser" placeholder="请搜索选择收件人" style="width: 100%">
            <el-option v-for="u in userList" :key="u.id" :label="`${u.username} (${u.employee?.name || ''})`" :value="u.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级" prop="priority">
          <el-radio-group v-model="form.priority">
            <el-radio value="low">低</el-radio>
            <el-radio value="normal">普通</el-radio>
            <el-radio value="high">高</el-radio>
            <el-radio value="urgent">紧急</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入消息标题" />
        </el-form-item>
        <el-form-item label="内容" prop="content">
          <el-input v-model="form.content" type="textarea" :rows="8" placeholder="请输入消息内容" />
        </el-form-item>
      </el-form>
      <div class="form-actions">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">发送</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import type { FormInstance, FormRules } from 'element-plus'
import { messageApi, userApi } from '@/api/system'

const router = useRouter()
const submitting = ref(false)
const formRef = ref<FormInstance>()
const userList = ref<any[]>([])

const form = ref({ receiverId: null, receiverName: '', title: '', content: '', priority: 'normal' })
const rules: FormRules = {
  receiverId: [{ required: true, message: '请选择收件人' }],
  title: [{ required: true, message: '请输入消息标题' }],
  content: [{ required: true, message: '请输入消息内容' }]
}

async function searchUser(query: string) {
  if (!query) return
  try {
    const res: any = await userApi.getAll()
    const users = res.data || []
    userList.value = users.filter((u: any) => u.username.includes(query) || (u.employee?.name || '').includes(query))
  } catch { userList.value = [] }
}

async function loadUsers() {
  try {
    const res: any = await userApi.getAll()
    userList.value = res.data || []
  } catch { userList.value = [] }
}

function handleBack() {
  router.push('/message/inbox')
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  const user = userList.value.find((u: any) => u.id === form.value.receiverId)
  form.value.receiverName = user?.employee?.name || user?.username || ''

  submitting.value = true
  try {
    await messageApi.send(form.value)
    ElMessage.success('发送成功')
    router.push('/message/sent')
  } finally { submitting.value = false }
}

onMounted(loadUsers)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.form-actions { text-align: center; margin-top: 24px; }
</style>