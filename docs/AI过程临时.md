
## 透视表组件替换方案评测

### 当前使用的功能清单

根据 [PivotTable.vue](file:///d:/Codes/Projects/eams2026/src/EAMS2026.Web/src/views/report/components/PivotTable.vue) 代码，当前依赖 pivottable.js 的核心功能：

| 功能 | 说明 |
|------|------|
| `pivotUI()` | 拖拽式透视表 UI（行/列/值/过滤器区域） |
| 字段拖拽排序 | jQuery UI sortable，拖拽字段到行/列/值区域 |
| 聚合器选择 | 下拉选择 Sum/Count 等聚合方式 |
| 渲染器选择 | 下拉选择 Table/Heatmap/Bar Chart 等 |
| 字段筛选框 | `.pvtFilterBox` — 勾选字段值过滤 |
| `onRefresh` 回调 | 保存/恢复透视配置（序列化为 JSON） |
| 中文语言 | `pivot.zh` 本地化 |
| 导出 Excel | 通过 XLSX 库读取渲染结果 table |

---

### 候选组件对比

| 维度 | **vue-pivottable** | **@visactor/vue-vtable** | **@click2buy/vue-pivot-table** |
|------|:---:|:---:|:---:|
| Vue 3 原生 | ✅ Composition API | ✅ Vue 3 封装 | ⚠️ Vue 2/3 兼容但偏 Vue 2 |
| jQuery 依赖 | ❌ 无 | ❌ 无 | ❌ 无 |
| 拖拽式 UI | ✅ 与 pivottable.js 几乎一致 | ⚠️ 需自行实现拖拽区域 | ✅ 有拖拽区域 |
| 聚合器/渲染器 | ✅ 内置多种 | ✅ 内置多种 | ✅ 内置 |
| 字段筛选 | ✅ 内置 | ⚠️ 需自行实现 | ✅ 内置 |
| onRefresh 配置保存 | ✅ 可获取完整配置 | ⚠️ 需自行序列化 | ⚠️ 部分支持 |
| 中文本地化 | ✅ 支持 | ✅ 支持 | ⚠️ 需自行处理 |
| 渲染性能 | 一般（DOM） | **极高（Canvas）** | 一般（DOM） |
| npm 周下载量 | ~1,029 | ~225（vue 封装层） | ~110 |
| 最后更新 | 2 个月前 | 1 个月前（持续活跃） | **2023年2月（已停更）** |
| GitHub Stars | 较少 | 2.7k+（字节跳动） | 较少 |
| 体积 | ~50KB | ~500KB+ | ~6.4MB |
| 许可证 | MIT | MIT | MIT |

---

### 详细分析

#### 1. `@click2buy/vue-pivot-table` — ❌ 不推荐

- **已停更 3 年**（最后提交 2023-02），存在兼容性风险
- 体积 6.4MB，异常臃肿
- 周下载量仅 110，社区几乎无活跃度
- 拖拽 UI 与 pivottable.js 相似度最高，但风险太大

#### 2. `vue-pivottable` — ✅ 推荐作为**直接替换方案**

- **与 pivottable.js 界面和交互最接近**，它是 react-pivottable 的 Vue 3 移植版，而 react-pivottable 本身就是 pivottable.js 的 React 重写
- 拖拽 UI、字段筛选、聚合器/渲染器选择、配置保存 — 功能一一对应
- 无 jQuery 依赖，纯 Vue 3 Composition API
- 迁移成本最低：数据格式和配置项与 pivottable.js 高度相似

#### 3. `@visactor/vue-vtable` — ✅ 推荐作为**长期升级方案**

- 字节跳动出品，Canvas 渲染，**百万级数据毫秒响应**
- 功能最强大：透视表 + 图表 + 甘特图
- 但 **UI 交互模式不同**：它是配置式（通过 option 定义行列维度），不像 pivottable.js 那样有拖拽区域 UI
- 需要自行实现"字段拖拽到行/列/值区域"的交互面板
- 迁移成本最高，但长期收益最大

---

### 推荐方案

| 方案 | 适用场景 | 迁移成本 | 风险 |
|------|---------|---------|------|
| **A. vue-pivottable** | 快速去除 jQuery，保持界面一致 | **低** — 约 1-2 天 | 低 |
| **B. @visactor/vue-vtable** | 长期升级，追求极致性能 | **高** — 约 5-7 天 | 中（需自建拖拽 UI） |
| **C. 维持现状** | 不急于去除 jQuery | 无 | 无 |

**我的建议：** 如果目标是**去除 jQuery 依赖且保持界面一致**，选 **方案 A（vue-pivottable）**；如果追求**高性能和长期演进**，选 **方案 B（vue-vtable）**。


