import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

/**
 * 检查 JWT Token 是否已过期。
 * 解析 Token 的 payload（Base64 编码的 JSON），比对 exp 字段与当前时间。
 *
 * @param token - JWT Token 字符串
 * @returns true 表示 Token 已过期或格式无效
 */
function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const now = Math.floor(Date.now() / 1000)
    return payload.exp && payload.exp < now
  } catch {
    return true
  }
}

/**
 * 应用路由配置。
 *
 * 路由采用嵌套结构：
 * - /login: 登录页面（无需认证）
 * - /: 主布局 MainLayout（需要认证），所有业务页面作为其子路由
 *
 * meta 字段说明：
 * - requiresAuth: 是否需要登录认证（默认 true）
 * - title: 页面标题（用于面包屑导航）
 * - icon: Element Plus 图标名称（用于侧边栏菜单）
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue'),
        meta: { title: '仪表盘', icon: 'Odometer' }
      },
      {
        path: 'system/department',
        name: 'Department',
        component: () => import('@/views/system/Department.vue'),
        meta: { title: '部门管理', icon: 'OfficeBuilding' }
      },
      {
        path: 'system/employee',
        name: 'Employee',
        component: () => import('@/views/system/Employee.vue'),
        meta: { title: '员工管理', icon: 'User' }
      },
      {
        path: 'system/user',
        name: 'User',
        component: () => import('@/views/system/User.vue'),
        meta: { title: '用户管理', icon: 'Avatar' }
      },
      {
        path: 'system/role',
        name: 'Role',
        component: () => import('@/views/system/Role.vue'),
        meta: { title: '角色管理', icon: 'Key' }
      },
      {
        path: 'system/permission',
        name: 'Permission',
        component: () => import('@/views/system/Permission.vue'),
        meta: { title: '权限管理', icon: 'Lock' }
      },
      {
        path: 'test',
        name: 'TestMenu',
        component: () => import('@/views/test/TestMenu.vue'),
        meta: { title: '测试菜单', icon: 'Tools' }
      },
      {
        path: 'test/permission1',
        name: 'TestPermission1',
        component: () => import('@/views/test/TestPermission1.vue'),
        meta: { title: '权限1子菜单', icon: 'Tools' }
      },
      {
        path: 'test/permission2',
        name: 'TestPermission2',
        component: () => import('@/views/test/TestPermission2.vue'),
        meta: { title: '权限2子菜单', icon: 'Tools' }
      },
      {
        path: 'system/dict',
        name: 'Dict',
        component: () => import('@/views/system/Dict.vue'),
        meta: { title: '字典管理', icon: 'Notebook' }
      },
      {
        path: 'system/operation-log',
        name: 'OperationLog',
        component: () => import('@/views/system/OperationLog.vue'),
        meta: { title: '操作日志', icon: 'Document' }
      },
      {
        path: 'system/dashboard-config',
        name: 'DashboardConfig',
        component: () => import('@/views/system/DashboardConfig.vue'),
        meta: { title: '仪表盘配置', icon: 'Monitor' }
      },
      {
        path: 'system/data-permission',
        name: 'DataPermission',
        component: () => import('@/views/attendance/DataPermission.vue'),
        meta: { title: '数据权限配置', icon: 'Setting' }
      },
      {
        path: 'message/inbox',
        name: 'MessageInbox',
        component: () => import('@/views/message/Inbox.vue'),
        meta: { title: '收件箱', icon: 'Message' }
      },
      {
        path: 'message/sent',
        name: 'MessageSent',
        component: () => import('@/views/message/Sent.vue'),
        meta: { title: '已发送', icon: 'Promotion' }
      },
      {
        path: 'message/compose',
        name: 'MessageCompose',
        component: () => import('@/views/message/Compose.vue'),
        meta: { title: '写消息', icon: 'Edit' }
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile.vue'),
        meta: { title: '个人信息', icon: 'Avatar' }
      },
      {
        path: 'attendance/report',
        name: 'AttendanceReport',
        component: () => import('@/views/attendance/Report.vue'),
        meta: { title: '考勤报表', icon: 'Calendar' }
      },
      {
        path: 'attendance/day-type',
        name: 'AttendanceDayType',
        component: () => import('@/views/attendance/DayType.vue'),
        meta: { title: '考勤类型', icon: 'CollectionTag' }
      },
      {
        path: 'attendance/scheme-class',
        name: 'AttendanceSchemeClass',
        component: () => import('@/views/attendance/SchemeClass.vue'),
        meta: { title: '排班类别', icon: 'Tickets' }
      },
      {
        path: 'attendance/plan-time',
        name: 'AttendancePlanTime',
        component: () => import('@/views/attendance/PlanTime.vue'),
        meta: { title: '计划标准时间', icon: 'Clock' }
      },
      {
        path: 'attendance/holiday',
        name: 'AttendanceHoliday',
        component: () => import('@/views/attendance/Holiday.vue'),
        meta: { title: '节假日管理', icon: 'Sunny' }
      },
      {
        path: 'attendance/fee-calculator',
        name: 'AttendanceFeeCalculator',
        component: () => import('@/views/attendance/FeeCalculator.vue'),
        meta: { title: '费用计算规则', icon: 'Coin' }
      },
      {
        path: 'attendance/import',
        name: 'AttendanceImport',
        component: () => import('@/views/attendance/Import.vue'),
        meta: { title: '数据导入', icon: 'User' }
      },
      {
        path: 'attendance/employee-class-ref',
        name: 'AttendanceEmployeeRefClass',
        component: () => import('@/views/attendance/EmployeeClassRef.vue'),
        meta: { title: '员工关联班次', icon: 'Link' }
      },
      // ===== ERP辅助 - 单据修改 =====
      {
        path: 'erp/vouch-date',
        name: 'ErpVouchModifyDate',
        component: () => import('@/views/erp/VouchModifyDate.vue'),
        meta: { title: '发货日期修改', icon: 'Calendar' }
      },
      {
        path: 'erp/vouch-order',
        name: 'ErpVouchModifyOrder',
        component: () => import('@/views/erp/VouchModifyOrder.vue'),
        meta: { title: '订单客户修改', icon: 'Tickets' }
      },
      {
        path: 'erp/vouch-dispatch',
        name: 'ErpVouchModifyDispatch',
        component: () => import('@/views/erp/VouchModifyDispatch.vue'),
        meta: { title: '发货单客户修改', icon: 'Tickets' }
      },
      {
        path: 'erp/vouch-log',
        name: 'ErpVouchModifyLog',
        component: () => import('@/views/erp/VouchModifyLog.vue'),
        meta: { title: '修改日志', icon: 'DocumentChecked' }
      },
      // ===== ERP辅助 - 报表管理 =====
      {
        path: 'erp/reports',
        name: 'ReportList',
        component: () => import('@/views/report/ReportList.vue'),
        meta: { title: '报表列表', icon: 'List' }
      },
      {
        path: 'erp/reports/designer/:id?',
        name: 'ReportDesigner',
        component: () => import('@/views/report/ReportDesigner.vue'),
        meta: { title: '报表设计', icon: 'Edit' }
      },
      {
        path: 'erp/reports/view/:id',
        name: 'ReportViewer',
        component: () => import('@/views/report/ReportViewer.vue'),
        meta: { title: '报表查看', icon: 'View' }
      },
      // ===== ERP辅助 - 设置 =====
      {
        path: 'erp/settings/datasources',
        name: 'ErpSettingsDatasources',
        component: () => import('@/views/report/DataSourceManage.vue'),
        meta: { title: '数据源配置', icon: 'Coin' }
      },
      {
        path: 'erp/settings/salespersons',
        name: 'ErpSettingsSalespersons',
        component: () => import('@/views/report/SalespersonMap.vue'),
        meta: { title: '业务员对照', icon: 'Avatar' }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(), // HTML5 History 模式，不需要 URL 中的 #
  routes
})

/**
 * 全局路由守卫。
 * 在每次路由跳转前检查：
 * 1. 目标页面是否需要认证（meta.requiresAuth !== false）
 * 2. 未登录或 Token 过期 → 清除状态并跳转登录页
 * 3. 已登录 → 正常放行
 */
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth !== false) {
    if (!authStore.token || isTokenExpired(authStore.token)) {
      authStore.logout()
      next('/login')
      return
    }
  }
  next()
})

export default router