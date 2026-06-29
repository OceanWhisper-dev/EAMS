# ERP 单据修改模块设计文档

## 版本信息

| 项目 | 内容 |
|------|------|
| 模块名称 | ERP 单据修改（VouchModify） |
| 版本号 | v0.4.2026.0624 |
| 所属项目 | EAMS2026 |
| 后端框架 | .NET 8 + Dapper |
| 数据库 | U8 ERP (SQL Server) + 本地日志 (PostgreSQL) |

---

## 1. 功能概述

本模块用于对 U8 ERP 系统中的销售订单和发货单进行辅助性的数据修正操作，主要解决由于业务变更或录入错误导致的客户信息、发货日期需要调整的需求。

### 支持的业务操作

| 功能 | 说明 |
|------|------|
| 修改订单客户 | 修改销售订单的客户编码和名称，可同步修改关联的未审核发货单 |
| 修改发货单客户 | 修改发货单的客户编码和名称（仅限未审核单据） |
| 单笔修改发货日期 | 修改单个发货单的送货日期（仅限未审核单据） |
| 批量修改发货日期 | 批量修改多个未审核发货单的送货日期，支持自动计算"下月第一个周日" |

### 核心约束

- **已审核单据不可修改**：所有修改操作仅针对未审核（cVerifier IS NULL OR cVerifier = ''）的单据
- **订单关联校验**：修改订单客户前，检查该订单是否有关联的已审核发货单，如有则禁止修改
- **客户编码校验**：修改客户前，必须通过 U8 ERP 的 Customer 表验证新客户编码是否存在

---

## 2. 后端架构

模块采用三层架构：**Controller → Service → Repository**，遵循接口分离和依赖注入原则。

### 2.1 架构层次图

```
┌─────────────────────────────────────────────────┐
│  VouchModifyController  (API 层)                │
│  路由: /api/erp/vouch-modify                    │
│  负责: HTTP 请求接收、参数校验、结果返回          │
├─────────────────────────────────────────────────┤
│  IVouchModifyService / VouchModifyService       │
│  (Application 业务逻辑层)                        │
│  负责: 业务规则校验、操作日志记录、事务协调        │
├─────────────────────────────────────────────────┤
│  IVouchModifyRepository / ErpVouchModifyRepository │
│  (Infrastructure 数据访问层)                     │
│  负责: Dapper 操作 U8 ERP 的 SQL Server 数据库   │
│                                                   │
│  IVouchModifyLogRepository / VouchModifyLogRepository │
│  负责: Dapper 操作本地 PostgreSQL 日志表           │
└─────────────────────────────────────────────────┘
```

### 2.2 关键接口与实现

| 接口 | 实现类 | 数据源 | 技术 |
|------|--------|--------|------|
| `IVouchModifyService` | `VouchModifyService` | - | 业务逻辑层 |
| `IVouchModifyRepository` | `ErpVouchModifyRepository` | U8 ERP (SQL Server) | Dapper |
| `IVouchModifyLogRepository` | `VouchModifyLogRepository` | 本地 PostgreSQL | Dapper |

### 2.3 安全处理

`ErpVouchModifyRepository` 中的全部 ERP 查询均通过 `SafeErpQueryAsync<T>` 包装，在 SQL Server 不可达或数据源未配置时返回默认值而非抛出异常，保证系统不因 U8 ERP 的可用性问题而崩溃。

---

## 3. API 接口清单

基础路径：`/api/erp/vouch-modify`

认证方式：`[Authorize]` — 全部接口需要 JWT 认证

### 3.1 查询接口

| 方法 | 路径 | 说明 | 参数 |
|------|------|------|------|
| GET | `/orders` | 查询销售订单（含关联发货单） | `VouchQueryParam` |
| GET | `/dispatches` | 查询发货单 | `VouchQueryParam` |
| GET | `/unverified-dispatches` | 查询未审核发货单（用于日期批量修改） | `VouchQueryParam` |
| GET | `/dispatch/{dlcode}` | 按单号查询发货单详情 | `dlcode`(string) |
| GET | `/orders/{soid}/has-verified-dispatches` | 检查订单是否有已审核发货单 | `soid`(int) |

### 3.2 修改接口

| 方法 | 路径 | 说明 | 请求体 |
|------|------|------|--------|
| PUT | `/order/customer` | 修改订单客户 | `UpdateCustomerRequest` |
| PUT | `/dispatch/customer` | 修改发货单客户 | `UpdateCustomerRequest` |
| PUT | `/dispatch/date` | 单笔修改发货日期 | `UpdateDateRequest` |
| PUT | `/dispatch/date/batch` | 批量修改发货日期 | `BatchUpdateDateRequest` |

### 3.3 参照接口

| 方法 | 路径 | 说明 | 参数 |
|------|------|------|------|
| GET | `/customer-ref/{code}` | 客户参照（验证客户编码） | `code`(string) |
| GET | `/salespersons` | 获取业务员列表 | - |

### 3.4 日志接口

| 方法 | 路径 | 说明 | 参数 |
|------|------|------|------|
| GET | `/logs` | 查询操作日志 | `vouchType`, `operatorId`, `from`, `to`, `page`, `pageSize` |

### 3.5 数据传输对象（DTO）

**VouchQueryParam** — 查询参数：
- `CusCode`(string?) — 客户编码精确匹配
- `CusName`(string?) — 客户名称模糊匹配
- `CusAbbName`(string?) — 客户简称模糊匹配
- `CusContact`(string?) — 联系人编码
- `CusPPerson`(string?) — 业务员编码
- `CusPhone`(string?) — 联系电话模糊匹配
- `CusMobile`(string?) — 手机号模糊匹配
- `CusAddr`(string?) — 客户地址模糊匹配
- `VouchCode`(string?) — 单据号模糊匹配
- `VouchDateFrom/To`(DateTime?) — 日期范围
- `VerifierStatus`(string?) — 审核状态：null/空=全部，unverified=未审核，verified=已审核
- `Page`(int) — 页码（默认1）
- `PageSize`(int) — 每页条数（默认20）

**UpdateCustomerRequest** — 修改客户请求：
- `Soid`(int?) — 订单ID（修改订单时必填）
- `Dlid`(int?) — 发货单ID（修改发货单时必填）
- `NewCusCode`(string) — 新客户编码（必填）
- `NewCusName`(string) — 新客户名称（必填）
- `OldCusCode`(string?) — 原客户编码（日志记录）
- `OldCusName`(string?) — 原客户名称（日志记录）
- `SyncDispatches`(bool) — 是否同步修改关联发货单

**UpdateDateRequest** — 单笔修改日期请求：
- `DlCode`(string?) — 发货单号
- `Dlid`(int?) — 发货单ID
- `NewDate`(DateTime) — 新送货日期

**BatchUpdateDateRequest** — 批量修改日期请求：
- `Dlids`(List\<int\>) — 发货单ID列表
- `NewDate`(DateTime) — 目标日期
- `AutoCalculate`(bool) — 是否自动计算下月第一个周日

**VouchModifyResult** — 单笔修改结果：
- `Success`(bool)
- `Message`(string?)
- `AffectedRows`(int)

**VouchModifyBatchResult** — 批量修改结果：
- `TotalCount`(int)
- `SuccessCount`(int)
- `FailCount`(int)
- `Failures`(List\<BatchFailItem\>)

---

## 4. 数据库表设计

### 4.1 日志表：`erp_vouch_modify_logs`

该表存在于本地 PostgreSQL 数据库中，记录所有的单据修改操作。

```sql
CREATE TABLE erp_vouch_modify_logs (
    id BIGSERIAL PRIMARY KEY,
    vouch_type VARCHAR(50) NOT NULL,       -- 单据类型: order / dispatch
    vouch_id BIGINT NOT NULL,              -- 单据ID (soid / dlid)
    vouch_code VARCHAR(100) NOT NULL,      -- 单据编号 (cSOCode / cDLCode)
    field_name VARCHAR(100) NOT NULL,       -- 修改字段: cCusCode / dDate
    old_value TEXT,                         -- 原值
    new_value TEXT,                         -- 新值
    operator_id BIGINT NOT NULL,            -- 操作人ID
    operator_name VARCHAR(100) NOT NULL,    -- 操作人姓名
    operate_at TIMESTAMP NOT NULL DEFAULT NOW(),  -- 操作时间
    status VARCHAR(20) NOT NULL DEFAULT 'SUCCESS',  -- 状态: SUCCESS / FAILED
    error_msg TEXT                          -- 错误信息
);
```

### 4.2 关联的 U8 ERP 表（只读/更新）

本模块通过 Dapper 直连 U8 ERP 的 SQL Server 数据库，操作以下表：

| 表名 | 用途 |
|------|------|
| `SO_SOMain` | 销售订单主表 — 查询订单、更新客户字段 |
| `SO_SODetails` | 销售订单子表 — 查询订单明细 |
| `DispatchList` | 发货单主表 — 查询/更新发货单客户和日期 |
| `DispatchLists` | 发货单子表 — 查询发货明细 |
| `Customer` | 客户档案 — 参照校验客户编码 |
| `Inventory` | 存货档案 — 查询物料信息 |
| `Person` | 人员档案 — 查询业务员 |

---

## 5. 权限策略

### 5.1 权限体系

本模块的权限继承关系（基于 PostgreSQL 迁移脚本 `042_RestructurePermissions.sql`）：

```
erp (ERP辅助, ID=38)
├── erp-report (报表管理, ID=28, sort=10)
├── erp-vouchmodify (单据修改, 新增节点, sort=20)     ← 本模块根权限
│   ├── erp-vouchmodify:date      (发货日期修改)
│   ├── erp-vouchmodify:order     (订单客户修改)
│   ├── erp-vouchmodify:dispatch  (发货单客户修改)
│   └── erp-vouchmodify:log       (修改日志)
└── erp-settings (设置, 新增节点, sort=30)
```

### 5.2 权限映射

| 权限编码 | 对应页面 | API 路由前缀 |
|----------|----------|--------------|
| `erp-vouchmodify` | (菜单节点) | - |
| `erp-vouchmodify:date` | 发货日期修改 | `/api/erp/vouch-modify/unverified-dispatches`, `/api/erp/vouch-modify/dispatch/date` |
| `erp-vouchmodify:order` | 订单客户修改 | `/api/erp/vouch-modify/orders`, `/api/erp/vouch-modify/order/customer` |
| `erp-vouchmodify:dispatch` | 发货单客户修改 | `/api/erp/vouch-modify/dispatches`, `/api/erp/vouch-modify/dispatch/customer` |
| `erp-vouchmodify:log` | 修改日志 | `/api/erp/vouch-modify/logs` |

全部 API 接口统一使用 `[Authorize]` 特性进行 JWT 认证。

---

## 6. 前端页面说明

本模块包含 4 个前端页面，均位于 `src/views/erp/` 目录下，路由配置在 `src/router/index.ts` 中。

### 6.1 VouchModifyOrder.vue — 订单客户修改

- **路径**：`/erp/vouch-order`
- **路由名称**：`ErpVouchModifyOrder`
- **功能**：
  - 多条件查询销售订单（客户编码/名称/简称、联系人、业务员、电话、手机、地址、订单号、日期范围）
  - 表格可展开查看订单明细和关联发货单
  - 点击"修改客户"打开对话框，输入新客户编码后支持**实时验证**（调用 customer-ref 接口）
  - 可选择"同步发货单"（仅同步未审核的关联发货单）
  - 修改前自动检查订单是否有已审核发货单（调用 has-verified-dispatches 接口）
- **权限**：`erp-vouchmodify:order`
- **业务员限制**：受限业务员只能看到自己的单据

### 6.2 VouchModifyDispatch.vue — 发货单客户修改

- **路径**：`/erp/vouch-dispatch`
- **路由名称**：`ErpVouchModifyDispatch`
- **功能**：
  - 查询发货单（客户编码/名称、发货单号、业务员、日期范围、审核状态过滤）
  - 表格可展开查看发货明细
  - 显示审核状态（已审核/未审核），**已审核单据禁用修改按钮**
  - 修改操作仅针对未审核单据
- **权限**：`erp-vouchmodify:dispatch`

### 6.3 VouchModifyDate.vue — 发货日期修改（批量）

- **路径**：`/erp/vouch-date`
- **路由名称**：`ErpVouchModifyDate`
- **功能**：
  - 查询未审核发货单（默认只查询未审核单据）
  - 表格支持多选（checkbox），已审核行不可选
  - 工具栏显示选中数量和"下月第一个周日"的默认日期
  - 支持手动选择目标日期
  - 批量修改后以弹窗展示结果统计和失败明细
  - **自动计算日期**：下月第一个周日（`CalculateFirstSundayOfNextMonth`）
- **权限**：`erp-vouchmodify:date`

### 6.4 VouchModifyLog.vue — 修改日志

- **路径**：`/erp/vouch-log`
- **路由名称**：`ErpVouchModifyLog`
- **功能**：
  - 查询条件：单据类型（订单/发货单）、操作人ID、时间范围
  - 列表展示：ID、单据类型、单据编号、修改字段、原值、新值、操作人、操作时间、状态
  - 分页查询
- **权限**：`erp-vouchmodify:log`

### 6.5 前端 API 封装

所有 API 调用封装在 `src/api/erp.ts` 的 `vouchModifyApi` 对象中，包含完整的 TypeScript 类型定义：

- `queryOrders` — 查询订单
- `queryDispatches` — 查询发货单
- `queryUnverifiedDispatches` — 查询未审核发货单
- `getDispatch` — 按单号获取发货单详情
- `hasVerifiedDispatches` — 检查已审核发货单
- `updateOrderCustomer` — 修改订单客户
- `updateDispatchCustomer` — 修改发货单客户
- `updateDispatchDate` — 单笔修改日期
- `batchUpdateDispatchDate` — 批量修改日期
- `getCustomerRef` — 客户参照
- `queryLogs` — 查询日志
- `getSalespersons` — 获取业务员列表

---

## 7. 关键业务流程

### 7.1 修改订单客户

```
用户输入新客户编码 → 调用 customer-ref 接口验证客户存在
  → 调用 has-verified-dispatches 检查订单是否有已审核发货单（有则拒绝）
  → 调用 update-order-customer 修改 SO_SOMain 表的 cCusCode/cCusName
  → 如果 SyncDispatches=true，遍历关联的 DispatchList
      → 仅修改未审核的（cVerifier IS NULL OR ''）
  → 记录操作日志到 erp_vouch_modify_logs
```

### 7.2 批量修改发货日期

```
用户选择多个未审核发货单
  → 确定目标日期（自动计算下月第一个周日或手动选择）
  → 调用 batch-update-dispatch-date 接口
  → 后端逐条处理：
      → 检查是否未审核
      → 更新 DispatchList.dDate
      → 记录成功/失败
  → 返回批量结果（成功数、失败数、失败明细）
```