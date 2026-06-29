<template>
  <div class="tab-bar" @contextmenu.prevent>
    <el-tabs
      v-model="store.activeTab"
      type="card"
      closable
      @tab-click="handleTabClick"
      @tab-remove="handleTabRemove"
    >
      <el-tab-pane
        v-for="tab in store.tabs"
        :key="tab.path"
        :label="tab.title"
        :name="tab.path"
        :closable="tab.closable"
        @contextmenu.prevent.stop="showContextMenu($event, tab)"
      />
    </el-tabs>

    <div class="tab-actions">
      <el-dropdown trigger="click" @command="handleAction">
        <el-icon class="action-btn"><ArrowDown /></el-icon>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="closeOthers">关闭其他</el-dropdown-item>
            <el-dropdown-item command="closeAll">关闭全部</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <ul
      v-if="contextMenu.visible"
      class="context-menu"
      :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
    >
      <li @click="handleContextAction('closeCurrent')">关闭当前</li>
      <li @click="handleContextAction('closeOthers')">关闭其他</li>
      <li @click="handleContextAction('closeRight')">关闭右侧</li>
      <li @click="handleContextAction('closeAll')">关闭全部</li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTabsStore, type TabItem } from '@/stores/tabs'
import { ArrowDown } from '@element-plus/icons-vue'

const router = useRouter()
const store = useTabsStore()

const contextMenu = reactive({
  visible: false,
  x: 0,
  y: 0,
  tab: null as TabItem | null
})

function handleTabClick(pane: any) {
  router.push(pane.props.name)
}

function handleTabRemove(path: string) {
  const target = store.closeTab(path)
  router.push(target)
}

function showContextMenu(e: MouseEvent, tab: TabItem) {
  contextMenu.visible = true
  contextMenu.x = e.clientX
  contextMenu.y = e.clientY
  contextMenu.tab = tab
}

function hideContextMenu() {
  contextMenu.visible = false
  contextMenu.tab = null
}

function handleContextAction(action: string) {
  const tab = contextMenu.tab
  if (!tab) return

  let target = ''
  switch (action) {
    case 'closeCurrent':
      if (tab.closable) {
        target = store.closeTab(tab.path)
        router.push(target)
      }
      break
    case 'closeOthers':
      store.closeOthers(tab.path)
      if (store.activeTab !== tab.path) {
        router.push(tab.path)
      }
      break
    case 'closeRight':
      store.closeRight(tab.path)
      break
    case 'closeAll':
      store.closeAll()
      router.push('/dashboard')
      break
  }
  hideContextMenu()
}

function handleAction(command: string) {
  switch (command) {
    case 'closeOthers':
      store.closeOthers(store.activeTab)
      break
    case 'closeAll':
      store.closeAll()
      router.push('/dashboard')
      break
  }
}

function globalClick() {
  if (contextMenu.visible) hideContextMenu()
}

onMounted(() => {
  document.addEventListener('click', globalClick)
})

onUnmounted(() => {
  document.removeEventListener('click', globalClick)
})
</script>

<style scoped>
.tab-bar {
  display: flex;
  align-items: center;
  background: #fff;
  border-bottom: 1px solid #e6e6e6;
  padding: 0 8px;
  position: relative;
  user-select: none;
}
.tab-bar .el-tabs {
  flex: 1;
  overflow: hidden;
}
.tab-bar :deep(.el-tabs__header) {
  margin: 0;
  border: none;
}
.tab-bar :deep(.el-tabs__nav-wrap) {
  margin-bottom: 0;
}
.tab-bar :deep(.el-tabs__nav) {
  border: none;
}
.tab-bar :deep(.el-tabs__item) {
  height: 34px;
  line-height: 34px;
  font-size: 13px;
  padding: 0 16px;
  border: 1px solid #d8dce5 !important;
  border-radius: 3px 3px 0 0;
  margin-right: 3px;
  background: #fff;
  color: #495060;
  transition: all 0.2s;
}
.tab-bar :deep(.el-tabs__item.is-active) {
  background: #409eff;
  color: #fff;
  border-color: #409eff !important;
}
.tab-bar :deep(.el-tabs__item:hover) {
  color: #409eff;
}
.tab-bar :deep(.el-tabs__item.is-active:hover) {
  color: #fff;
}
.tab-bar :deep(.el-tabs__item .is-icon-close) {
  margin-left: 4px;
  border-radius: 50%;
  width: 16px;
  height: 16px;
  line-height: 16px;
  font-size: 12px;
  vertical-align: middle;
  transition: all 0.2s;
}
.tab-bar :deep(.el-tabs__item .is-icon-close:hover) {
  background: rgba(255,255,255,0.3);
}
.tab-bar :deep(.el-tabs__nav-next),
.tab-bar :deep(.el-tabs__nav-prev) {
  height: 34px;
  line-height: 34px;
}
.tab-bar :deep(.el-tabs__content) { display: none; }
.tab-actions {
  margin-left: 8px;
  flex-shrink: 0;
}
.action-btn {
  font-size: 14px;
  cursor: pointer;
  padding: 4px;
  border-radius: 3px;
  color: #909399;
  transition: all 0.2s;
}
.action-btn:hover {
  background: #f0f2f5;
  color: #409eff;
}
.context-menu {
  position: fixed;
  z-index: 9999;
  background: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  box-shadow: 0 2px 12px 0 rgba(0,0,0,0.1);
  padding: 5px 0;
  list-style: none;
  min-width: 120px;
}
.context-menu li {
  padding: 8px 16px;
  font-size: 13px;
  color: #606266;
  cursor: pointer;
  transition: background 0.2s;
}
.context-menu li:hover {
  background: #ecf5ff;
  color: #409eff;
}
</style>