<template>
  <el-container class="main-layout">
    <el-aside :width="isCollapsed ? '64px' : '220px'" class="sidebar">
      <div class="logo" :class="{ collapsed: isCollapsed }">
        <span v-if="!isCollapsed">EAMS2026</span>
        <span v-else>E</span>
      </div>
      <el-menu
        :default-active="route.path"
        :collapse="isCollapsed"
        :router="true"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
      >
        <el-menu-item index="/dashboard">
          <el-icon><Odometer /></el-icon>
          <span>仪表盘</span>
        </el-menu-item>
        <el-sub-menu v-if="hasAnyPermission(['department','employee','user','role','permission','dict','data-permission'])" index="/system">
          <template #title>
            <el-icon><Setting /></el-icon>
            <span>系统管理</span>
          </template>
          <el-menu-item v-if="hasPermission('department')" index="/system/department">
            <el-icon><OfficeBuilding /></el-icon>
            <span>部门管理</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('employee')" index="/system/employee">
            <el-icon><User /></el-icon>
            <span>员工管理</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('user')" index="/system/user">
            <el-icon><Avatar /></el-icon>
            <span>用户管理</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('role')" index="/system/role">
            <el-icon><Key /></el-icon>
            <span>角色管理</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('permission')" index="/system/permission">
            <el-icon><Lock /></el-icon>
            <span>权限管理</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('data-permission')" index="/system/data-permission">
            <el-icon><Setting /></el-icon>
            <span>数据权限配置</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('dict')" index="/system/dict">
            <el-icon><Notebook /></el-icon>
            <span>字典管理</span>
          </el-menu-item>
          <el-menu-item v-if="hasPermission('operation-log')" index="/system/operation-log">
            <el-icon><Document /></el-icon>
            <span>操作日志</span>
          </el-menu-item>
          <el-menu-item v-if="authStore.isAdmin" index="/system/dashboard-config">
            <el-icon><Monitor /></el-icon>
            <span>仪表盘配置</span>
          </el-menu-item>
        </el-sub-menu>
        <el-sub-menu v-if="hasAnyPermission(['test:menu','test:permission1','test:permission2','test:func11','test:func12','test:func21','test:func22','test:func23'])" index="/test">
          <template #title>
            <el-icon><Tools /></el-icon>
            <span>测试菜单</span>
          </template>
          <el-menu-item v-if="hasAnyPermission(['test:permission1','test:func11','test:func12'])" index="/test/permission1">
            <el-icon><Tools /></el-icon>
            <span>权限1子菜单</span>
          </el-menu-item>
          <el-menu-item v-if="hasAnyPermission(['test:permission2','test:func21','test:func22','test:func23'])" index="/test/permission2">
            <el-icon><Tools /></el-icon>
            <span>权限2子菜单</span>
          </el-menu-item>
        </el-sub-menu>
        <el-sub-menu v-if="hasAnyPermission(['attendance','attendance:report','attendance:day-type','attendance:scheme-class','attendance:plan-time','attendance:holiday','attendance:fee-calculator','attendance:import-data','attendance:employee-ref-class'])" index="/attendance">
          <template #title>
            <el-icon><Calendar /></el-icon>
            <span>考勤管理</span>
          </template>
          <el-menu-item v-if="hasPermission('attendance:report')" index="/attendance/report">
            <el-icon><Calendar /></el-icon>
            <span>考勤报表</span>
          </el-menu-item>
          <el-sub-menu v-if="hasAnyPermission(['attendance:day-type','attendance:scheme-class','attendance:plan-time','attendance:holiday','attendance:fee-calculator','attendance:import-data','attendance:employee-ref-class'])" index="/attendance/settings">
            <template #title>
              <el-icon><Tools /></el-icon>
              <span>考勤设置</span>
            </template>
            <el-menu-item v-if="hasPermission('attendance:day-type')" index="/attendance/day-type">
              <el-icon><CollectionTag /></el-icon>
              <span>考勤类型</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('attendance:scheme-class')" index="/attendance/scheme-class">
              <el-icon><Tickets /></el-icon>
              <span>排班类别</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('attendance:plan-time')" index="/attendance/plan-time">
              <el-icon><Clock /></el-icon>
              <span>计划标准时间</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('attendance:holiday')" index="/attendance/holiday">
              <el-icon><Sunny /></el-icon>
              <span>节假日管理</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('attendance:fee-calculator')" index="/attendance/fee-calculator">
              <el-icon><Coin /></el-icon>
              <span>费用计算规则</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('attendance:import-data')" index="/attendance/import">
              <el-icon><User /></el-icon>
              <span>数据导入</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('attendance:employee-ref-class')" index="/attendance/employee-class-ref">
              <el-icon><Link /></el-icon>
              <span>员工关联班次</span>
            </el-menu-item>
          </el-sub-menu>
        </el-sub-menu>
        <el-sub-menu v-if="hasAnyPermission(['erp-report','erp-vouchmodify:date','erp-vouchmodify:order','erp-vouchmodify:dispatch','erp-vouchmodify:log','erp-settings:datasource','erp-settings:salesperson'])" index="/erp">
          <template #title>
            <el-icon><Setting /></el-icon>
            <span>ERP辅助</span>
          </template>
          <!-- 报表管理 -->
          <el-sub-menu v-if="hasAnyPermission(['erp-report','erp-report:designer'])" index="/erp/reports">
            <template #title>
              <el-icon><DataAnalysis /></el-icon>
              <span>报表管理</span>
            </template>
            <el-menu-item index="/erp/reports">
              <el-icon><List /></el-icon>
              <span>报表列表</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('erp-report:designer')" index="/erp/reports/designer">
              <el-icon><Edit /></el-icon>
              <span>报表设计</span>
            </el-menu-item>
          </el-sub-menu>
          <!-- 单据修改 -->
          <el-sub-menu v-if="hasAnyPermission(['erp-vouchmodify:date','erp-vouchmodify:order','erp-vouchmodify:dispatch','erp-vouchmodify:log'])" index="/erp/vouch-modify">
            <template #title>
              <el-icon><Document /></el-icon>
              <span>单据修改</span>
            </template>
            <el-menu-item v-if="hasPermission('erp-vouchmodify:date')" index="/erp/vouch-date">
              <el-icon><Calendar /></el-icon>
              <span>发货日期修改</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('erp-vouchmodify:order')" index="/erp/vouch-order">
              <el-icon><Tickets /></el-icon>
              <span>订单客户修改</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('erp-vouchmodify:dispatch')" index="/erp/vouch-dispatch">
              <el-icon><Tickets /></el-icon>
              <span>发货单客户修改</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('erp-vouchmodify:log')" index="/erp/vouch-log">
              <el-icon><Document /></el-icon>
              <span>修改日志</span>
            </el-menu-item>
          </el-sub-menu>
          <!-- 设置 -->
          <el-sub-menu v-if="hasAnyPermission(['erp-settings:datasource','erp-settings:salesperson'])" index="/erp/settings">
            <template #title>
              <el-icon><Tools /></el-icon>
              <span>设置</span>
            </template>
            <el-menu-item v-if="hasPermission('erp-settings:datasource')" index="/erp/settings/datasources">
              <el-icon><Coin /></el-icon>
              <span>数据源配置</span>
            </el-menu-item>
            <el-menu-item v-if="hasPermission('erp-settings:salesperson')" index="/erp/settings/salespersons">
              <el-icon><Avatar /></el-icon>
              <span>业务员对照</span>
            </el-menu-item>
          </el-sub-menu>
        </el-sub-menu>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <el-icon class="collapse-btn" @click="isCollapsed = !isCollapsed">
          <Fold v-if="!isCollapsed" />
          <Expand v-else />
        </el-icon>
        <div class="header-right">
          <el-dropdown trigger="click" @command="handleMsgCommand">
            <el-badge :value="unreadCount" :hidden="unreadCount === 0" class="msg-badge">
              <el-icon class="msg-bell" :size="20"><Message /></el-icon>
            </el-badge>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="inbox">
                  <el-badge :value="unreadCount" :hidden="unreadCount === 0" style="margin-right: 8px;">
                    <el-icon><Message /></el-icon>
                  </el-badge>
                  收件箱
                </el-dropdown-item>
                <el-dropdown-item command="sent">
                  <el-icon><Promotion /></el-icon>
                  已发送
                </el-dropdown-item>
                <el-dropdown-item command="compose" divided>
                  <el-icon><Edit /></el-icon>
                  写消息
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
          <el-dropdown @command="handleCommand">
            <span class="user-info">
              <el-icon><Avatar /></el-icon>
              {{ authStore.employeeName || authStore.username }}
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile">个人信息</el-dropdown-item>
                <el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <TabBar />
      <el-main class="main-content">
        <router-view v-slot="{ Component }">
          <keep-alive :include="keepAliveNames">
            <component :is="Component" />
          </keep-alive>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTabsStore } from '@/stores/tabs'
import TabBar from '@/components/TabBar.vue'
import { messageApi } from '@/api/system'
import {
  Fold, Expand, ArrowDown, Setting, Odometer,
  OfficeBuilding, User, Avatar, Key, Lock, Notebook,
  Message, Promotion, Edit, Tools, Document, Monitor,
  Calendar, CollectionTag, Tickets, Clock, Sunny, Coin, Link,
  DataAnalysis, List, Memo
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const tabsStore = useTabsStore()
const isCollapsed = ref(false)
const unreadCount = ref(0)

const keepAliveNames = computed(() =>
  tabsStore.tabs.map(t => t.name)
)

watch(() => route.path, () => {
  tabsStore.addTab(route)
}, { immediate: true })
let pollTimer: ReturnType<typeof setInterval> | null = null

function hasPermission(code: string): boolean {
  return authStore.isAdmin || authStore.hasPermission(code)
}

function hasAnyPermission(codes: string[]): boolean {
  return authStore.isAdmin || codes.some(c => authStore.hasPermission(c))
}

async function fetchUnreadCount() {
  try {
    const res: any = await messageApi.getUnreadCount()
    unreadCount.value = res.data || 0
  } catch {
    // 后端不可用时停止轮询，避免控制台频繁报错
    if (pollTimer) {
      clearInterval(pollTimer)
      pollTimer = null
    }
  }
}

function handleMsgCommand(command: string) {
  if (command === 'inbox') router.push('/message/inbox')
  else if (command === 'sent') router.push('/message/sent')
  else if (command === 'compose') router.push('/message/compose')
}

function handleCommand(command: string) {
  if (command === 'profile') {
    router.push('/profile')
  } else if (command === 'logout') {
    authStore.logout()
    router.push('/login')
  }
}

onMounted(() => {
  fetchUnreadCount()
  pollTimer = setInterval(fetchUnreadCount, 30000)
})

onUnmounted(() => {
  if (pollTimer) clearInterval(pollTimer)
})
</script>

<style scoped>
.main-layout { height: 100vh; }
.sidebar { background-color: #304156; transition: width 0.3s; overflow: hidden; display: flex; flex-direction: column; }
.sidebar .el-menu { flex: 1; overflow-y: auto; overflow-x: hidden; }
.sidebar .el-menu::-webkit-scrollbar { width: 4px; }
.sidebar .el-menu::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.2); border-radius: 2px; }
.logo {
  height: 60px; display: flex; align-items: center; justify-content: center;
  color: #fff; font-size: 20px; font-weight: bold; letter-spacing: 2px;
  border-bottom: 1px solid rgba(255,255,255,0.1); transition: all 0.3s;
}
.logo.collapsed { font-size: 24px; }
.header {
  display: flex; align-items: center; justify-content: space-between;
  background: #fff; border-bottom: 1px solid #e6e6e6; padding: 0 20px;
}
.collapse-btn { font-size: 20px; cursor: pointer; }
.header-right { display: flex; align-items: center; gap: 16px; }
.user-info { display: flex; align-items: center; gap: 6px; cursor: pointer; color: #333; }
.msg-bell { cursor: pointer; color: #606266; }
.msg-bell:hover { color: #409eff; }
.msg-badge :deep(.el-badge__content) { top: 4px; right: 2px; }
.main-content { background: #f0f2f5; padding: 20px; overflow-y: auto; }
</style>