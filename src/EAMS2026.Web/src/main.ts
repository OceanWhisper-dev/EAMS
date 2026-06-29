import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import 'element-plus/dist/index.css'
import zhCn from 'element-plus/es/locale/lang/zh-cn'
import App from './App.vue'
import router from './router'
import { permission } from './directives/permission'
import './styles/global.css'

/**
 * EAMS2026 企业管理系统 - 前端入口
 *
 * 初始化顺序：
 * 1. 创建 Vue 应用实例
 * 2. 注册 Pinia（状态管理）
 * 3. 配置 Vue Router（路由管理）
 * 4. 注册 Element Plus UI 组件库（中文语言包）
 * 5. 全局注册 Element Plus 图标（所有图标均可直接使用）
 * 6. 注册自定义权限指令 v-permission
 * 7. 挂载到 #app 元素
 */
const app = createApp(App)

// 全局注册 Element Plus 所有图标，组件中可直接使用
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.use(createPinia())
app.use(router)
app.use(ElementPlus, { locale: zhCn })
app.directive('permission', permission)

app.mount('#app')