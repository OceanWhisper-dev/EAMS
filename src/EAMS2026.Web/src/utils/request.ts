import axios from 'axios'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

/**
 * Axios 实例封装。
 *
 * 功能：
 * 1. 请求拦截器：自动携带 JWT Token
 * 2. 响应拦截器：统一处理业务错误（success=false）和 HTTP 错误（401/网络错误）
 *
 * 使用方式：
 *   import request from '@/utils/request'
 *   const res = await request.get('/user?page=1')
 *   const res = await request.post('/user', { username: 'test' })
 */
const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE ?? '/api',    // 所有请求前缀 /api，Vite 代理到后端 http://localhost:5106
  timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 30000      // 默认30秒超时
})

// ==================== 请求拦截器 ====================
request.interceptors.request.use(config => {
  const authStore = useAuthStore()
  // 自动携带 JWT Token，格式: Bearer <token>
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

// ==================== 响应拦截器 ====================
request.interceptors.response.use(
  // 成功响应处理
  response => {
    const data = response.data
    // 后端统一返回 { success, data, message }
    // success=false 表示业务逻辑失败（如"用户名已存在"），弹出错误提示
    if (data.success === false) {
      ElMessage.error(data.message || '请求失败')
      return Promise.reject(new Error(data.message))
    }
    return data
  },
  // 错误响应处理
  error => {
    const { config, response } = error
    if (response?.status === 401) {
      // 401 未授权：Token 过期或无效
      // 排除登录请求本身（登录失败不应触发登出）
      const isLoginRequest = config?.url?.includes('/auth/login')
      if (!isLoginRequest) {
        const authStore = useAuthStore()
        authStore.logout()
        window.location.href = '/login'
      }
    } else {
      // 其他错误（网络中断、500等）弹出提示
      ElMessage.error(response?.data?.message || error.message || '网络错误')
    }
    return Promise.reject(error)
  }
)

export default request