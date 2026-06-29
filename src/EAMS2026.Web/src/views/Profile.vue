<template>
  <div>
    <div class="page-header">
      <h3>个人信息</h3>
      <el-button type="primary" @click="openPwdDialog">修改密码</el-button>
    </div>
    <el-row :gutter="20">
      <el-col :span="16">
        <el-card>
          <template #header>基本信息</template>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="用户名">{{ profile.username }}</el-descriptions-item>
            <el-descriptions-item label="姓名">{{ profile.employee?.name || '-' }}</el-descriptions-item>
            <el-descriptions-item label="角色">
              <el-tag v-for="r in (profile.roles || [])" :key="r.id" style="margin-right: 4px;">{{ r.name }}</el-tag>
              <span v-if="!profile.roles?.length">-</span>
            </el-descriptions-item>
            <el-descriptions-item label="状态">
              <el-tag :type="profile.status ? 'success' : 'danger'">{{ profile.status ? '启用' : '禁用' }}</el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="最后登录">{{ formatDateTime(profile.lastLoginAt) }}</el-descriptions-item>
            <el-descriptions-item label="部门">{{ profile.employee?.departmentName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="工号">{{ profile.employee?.employeeNo || '-' }}</el-descriptions-item>
            <el-descriptions-item label="职位">{{ profile.employee?.position || '-' }}</el-descriptions-item>
            <el-descriptions-item label="手机">{{ profile.employee?.phone || '-' }}</el-descriptions-item>
            <el-descriptions-item label="邮箱">{{ profile.employee?.email || '-' }}</el-descriptions-item>
          </el-descriptions>
          <div style="margin-top: 16px; text-align: right;">
            <el-button @click="openEditDialog">编辑信息</el-button>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card>
          <template #header>操作日志</template>
          <el-timeline v-if="logs.length">
            <el-timeline-item v-for="log in logs" :key="log.id" :timestamp="formatDateTime(log.createdAt)" placement="top">
              <el-card size="small">
                <p>{{ log.description }}</p>
                <p style="color: #999; font-size: 12px;">{{ log.operationType }} | {{ log.module }}</p>
              </el-card>
            </el-timeline-item>
          </el-timeline>
          <el-empty v-else description="暂无操作记录" />
        </el-card>
      </el-col>
    </el-row>

    <el-dialog v-model="pwdDialogVisible" title="修改密码" width="440px" :close-on-click-modal="!authStore.forceChangePassword">
      <el-form ref="pwdFormRef" :model="pwdForm" :rules="pwdRules" label-width="100px">
        <el-form-item label="原密码" prop="oldPassword" v-if="!authStore.forceChangePassword">
          <el-input v-model="pwdForm.oldPassword" type="password" show-password placeholder="请输入原密码" />
        </el-form-item>
        <el-form-item v-if="authStore.forceChangePassword">
          <el-alert title="密码已被管理员重置，请设置新密码" type="warning" :closable="false" show-icon />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="pwdForm.newPassword" type="password" show-password placeholder="请输入新密码" />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input v-model="pwdForm.confirmPassword" type="password" show-password placeholder="请再次输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="pwdDialogVisible = false" :disabled="authStore.forceChangePassword">取消</el-button>
        <el-button type="primary" @click="handleChangePassword" :loading="changingPwd">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="editDialogVisible" title="编辑个人信息" width="500px">
      <el-form ref="editFormRef" :model="editForm" label-width="80px">
        <el-form-item label="手机">
          <el-input v-model="editForm.phone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="editForm.email" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item label="职位">
          <el-input v-model="editForm.position" placeholder="请输入职位" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveProfile" :loading="saving">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { authApi, operationLogApi } from '@/api/system'
import request from '@/utils/request'
import { formatDateTime } from '@/utils/date'

const authStore = useAuthStore()
const profile = ref<any>({})
const logs = ref<any[]>([])
const changingPwd = ref(false)
const pwdDialogVisible = ref(false)
const pwdFormRef = ref<FormInstance>()
const pwdForm = ref({ oldPassword: '', newPassword: '', confirmPassword: '' })

const editDialogVisible = ref(false)
const editFormRef = ref<FormInstance>()
const editForm = ref({ phone: '', email: '', position: '' })
const saving = ref(false)

const pwdRules = computed(() => {
  const rules: FormRules = {
    newPassword: [
      { required: true, message: '请输入新密码' }
    ],
    confirmPassword: [
      { required: true, message: '请再次输入新密码' },
      {
        validator: (_: any, value: string, callback: Function) => {
          if (value !== pwdForm.value.newPassword) {
            callback(new Error('两次输入的密码不一致'))
          } else {
            callback()
          }
        }
      }
    ]
  }
  if (!authStore.forceChangePassword) {
    rules.oldPassword = [{ required: true, message: '请输入原密码' }]
  }
  return rules
})

async function fetchProfile() {
  try {
    const res: any = await authApi.getProfile()
    if (res.data) profile.value = res.data
  } catch { /* ignore */ }
}

async function fetchLogs() {
  try {
    const res: any = await operationLogApi.getMine({ page: 1, pageSize: 10 })
    logs.value = res.data?.items || []
  } catch { /* ignore */ }
}

function openPwdDialog() {
  pwdForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
  pwdDialogVisible.value = true
}

async function handleChangePassword() {
  const valid = await pwdFormRef.value?.validate().catch(() => false)
  if (!valid) return
  changingPwd.value = true
  try {
    const res: any = await request.post('/auth/change-password', {
      oldPassword: pwdForm.value.oldPassword,
      newPassword: pwdForm.value.newPassword
    })
    ElMessage.success(res.message || '密码修改成功')
    pwdForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
    pwdDialogVisible.value = false
    authStore.clearForceChange()
  } finally { changingPwd.value = false }
}

function openEditDialog() {
  editForm.value = {
    phone: profile.value.employee?.phone || '',
    email: profile.value.employee?.email || '',
    position: profile.value.employee?.position || ''
  }
  editDialogVisible.value = true
}

async function handleSaveProfile() {
  saving.value = true
  try {
    await authApi.updateProfile(editForm.value)
    ElMessage.success('个人信息更新成功')
    editDialogVisible.value = false
    fetchProfile()
    fetchLogs()
  } finally { saving.value = false }
}

onMounted(() => {
  fetchProfile()
  fetchLogs()
  if (authStore.forceChangePassword) {
    openPwdDialog()
  }
})
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
</style>