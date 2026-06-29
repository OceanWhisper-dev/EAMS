import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import request from '@/utils/request'
import { useTabsStore } from './tabs'

/**
 * 认证状态管理 Store。
 * 管理用户登录状态、Token、角色和权限信息。
 * 所有数据双写：Pinia 内存 + localStorage 持久化。
 *
 * ⚠️ 安全说明：
 * - Token 存储在 localStorage 中，存在 XSS 攻击风险（恶意脚本可读取 localStorage）。
 * - sessionStorage 是比 localStorage 略好的选择（关闭标签页后自动清除），但仍无法防御 XSS。
 * - 生产环境推荐使用 httpOnly Cookie 方式，由后端通过 Set-Cookie 响应头设置 Token，
 *   这样前端 JavaScript 无法读取 Token 内容，从根本上防御 XSS 窃取 Token 的风险。
 * - 当前 localStorage 方式适用于开发环境和内网低风险场景。
 *
 * 关键状态：
 * - token: JWT 令牌，每次请求携带于 Authorization 头
 * - roles: 角色编码数组，super_admin 角色拥有所有权限
 * - permissions: 权限编码数组，用于前端 v-permission 指令和菜单过滤
 * - forceChangePassword: 强制修改密码标志
 */
export const useAuthStore = defineStore('auth', () => {
  // ==================== 状态 ====================
  const token = ref(localStorage.getItem('token') || '')
  // Store token in both localStorage (for SPA access) and note the security implications
  // For production, consider using httpOnly cookies via the backend Set-Cookie header
  const username = ref(localStorage.getItem('username') || '')
  const employeeName = ref(localStorage.getItem('employeeName') || '')
  const userId = ref(Number(localStorage.getItem('userId') || 0))
  const roles = ref<string[]>(JSON.parse(localStorage.getItem('roles') || '[]'))
  const permissions = ref<string[]>(JSON.parse(localStorage.getItem('permissions') || '[]'))
  const forceChangePassword = ref(localStorage.getItem('forceChangePassword') === 'true')

  // ==================== 计算属性 ====================
  const isLoggedIn = computed(() => !!token.value)

  /**
   * 是否为超级管理员。
   * 超级管理员拥有 roles.code = 'super_admin'，拥有系统所有权限。
   */
  const isAdmin = computed(() => roles.value.includes('super_admin'))

  // ==================== 方法 ====================

  /**
   * 用户登录。
   * 发送 POST /api/auth/login，成功后将 Token、角色、权限写入状态和 localStorage。
   * @param loginData - { username, password }
   * @returns 登录响应数据（含 token、roles、permissions 等）
   */
  async function login(loginData: { username: string; password: string }) {
    // 登录前清空旧会话的标签页，避免新用户看到旧标签
    useTabsStore().reset()
    const res = await request.post('/auth/login', loginData)
    const { data } = res as any
    token.value = data.token
    username.value = data.username
    employeeName.value = data.employeeName || ''
    userId.value = data.userId
    roles.value = data.roles || []
    permissions.value = data.permissions || []
    forceChangePassword.value = data.forceChangePassword || false

    // 双写：内存 + localStorage 持久化
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
    localStorage.setItem('employeeName', data.employeeName || '')
    localStorage.setItem('userId', String(data.userId))
    localStorage.setItem('roles', JSON.stringify(data.roles || []))
    localStorage.setItem('permissions', JSON.stringify(data.permissions || []))
    localStorage.setItem('forceChangePassword', String(data.forceChangePassword || false))
    return data
  }

  /**
   * 清除强制修改密码标志。
   * 在用户完成密码修改后调用。
   */
  function clearForceChange() {
    forceChangePassword.value = false
    localStorage.setItem('forceChangePassword', 'false')
  }

  /**
   * 检查当前用户是否拥有指定权限。
   * 与 v-permission 指令配合使用可控制组件可见性。
   * @param code - 权限编码，如 'user'、'user:create'
   * @returns 是否拥有该权限
   */
  function hasPermission(code: string): boolean {
    return permissions.value.includes(code)
  }

  /**
   * 用户登出。
   * 清除所有状态和 localStorage，路由守卫会自动跳转到登录页。
   */
  function logout() {
    token.value = ''
    username.value = ''
    employeeName.value = ''
    userId.value = 0
    roles.value = []
    permissions.value = []
    forceChangePassword.value = false
    localStorage.clear()
    // 清除多标签页状态，避免新用户看到旧用户的标签
    useTabsStore().reset()
  }

  return {
    token,
    username,
    employeeName,
    userId,
    roles,
    permissions,
    forceChangePassword,
    isLoggedIn,
    isAdmin,
    login,
    clearForceChange,
    hasPermission,
    logout
  }
})