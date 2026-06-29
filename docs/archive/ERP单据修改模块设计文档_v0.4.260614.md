# EAMS2026 ERP单据修改模块设计文档

> 版本：v0.4.260614  
> 状态：初稿  
> 关联文档：[EAMS_4.6三模块迁移分析报告.md](archive/EAMS_4.6三模块迁移分析报告.md)、[软件设计文档_v0.3.260613.md](软件设计文档_v0.3.260613.md)

---

## 1. 老模块概述

### 1.1 模块定位

| 维度 | 说明 |
|------|------|
| **模块名称** | ModifyCustomer（ERP单据客户信息修改） |
| **所在项目** | EAMS_4.6\4.6\EAMS\Sale\ModifyCustomer |
| **业务目标** | 对已创建但未审核的U8 ERP销售单据，批量修正其中的客户信息 |
| **操作对象** | 销售订单（SO_SOMain）、销售发货单（DispatchList） |
| **触发条件** | 业务员创建订单时选择错误的客户，需要在发货前修正 |

### 1.2 功能清单（旧系统）

| 功能 | 说明 | 实现状态 |
|------|------|----------|
| 销售订单客户修改 | 按条件检索订单 → 修改客户编码/名称 | ✅ 已实现 |
| 销售发货单客户修改 | 按条件检索发货单 → 修改客户编码/名称 | ✅ 已实现 |
| 发货单送货日期修改 | 按发货单号查询 → 修改发货单主表日期字段（dDate）为指定日期 | ✅ 已实现 |
| 客户参照查询 | 输入客户编码，参照显示客户名称 | ✅ 已实现 |
| 操作日志记录 | 记录每次修改操作的前后值 | ✅ 已实现 |

### 1.3 涉及的老项目文件

```
Sale/ModifyCustomer/
├── BLL.cs                          # 核心业务逻辑层
├── ModifyCustomer.csproj           # 项目文件 (netstandard2.0)
├── Model/
│   ├── Customer.cs                 # 客户实体（Dos.ORM映射）
│   ├── Dispatch.cs                 # 发货单视图模型
│   ├── Order.cs                    # 订单+发货单聚合模型
│   ├── QueryParam.cs               # 查询参数模型
│   ├── SaleDispatch.cs             # 发货单Dos.ORM实体（视图映射）
│   ├── SaleOrderDispatch.cs        # 订单+发货联合Dos.ORM实体（视图映射）
│   ├── updVouchCus.cs              # 更新参数模型
│   └── dalDBSession.cs             # 数据库会话工厂（引用外部）

MvcApp/Areas/Employee/Controllers/
├── ModifySaleOrderCustomerController.cs    # 销售订单客户修改控制器
├── ModifySaleCustomerController.cs         # 销售客户修改控制器（旧版）
├── ModifySaleDispatchCustomerController.cs # 发货单客户修改控制器
└── ModifySaleDispatchShippingDateController.cs # 发货日期修改控制器

MvcApp/Areas/Employee/Views/
├── ModifySaleOrderCustomer/Index.cshtml    # 订单客户修改视图
├── ModifySaleCustomer/Index.cshtml         # 客户修改视图（旧版）
├── ModifySaleDispatchCustomer/Index.cshtml # 发货单客户修改视图
└── ModifySaleDispatchShippingDate/Index.cshtml # 发货日期修改视图
```

---

## 2. 老模块代码分析

### 2.1 数据模型分析

#### 2.1.1 核心数据库表关系

```
SO_SOMain (销售订单主表)
  └── SO_SODetails (销售订单子表) ──→ Inventory (存货档案)
        │
        └── DispatchLists (发货单子表) ──→ Inventory (存货档案)
              │
              └── DispatchList (发货单主表) ──→ Customer (客户档案)

Customer (客户档案) ── 被 SO_SOMain.cCusCode 和 DispatchList.cCusCode 引用
```

#### 2.1.2 关键SQL（老系统中使用的联合查询）

```sql
-- 销售订单 + 发货单 + 客户联合查询（未审核发货单）
SELECT 
    m.cCusCode AS soCusCode, m.id AS soid, m.cSOCode, m.dDate AS soDate,
    s.cInvCode AS soInvCode, s.cInvName AS soInvName, sInv.cInvStd AS soInvStd,
    s.iQuantity AS soQuantity,
    d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate,
    ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
    ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier
FROM SO_SOMain AS m
LEFT JOIN SO_SODetails AS s ON m.id = s.id
LEFT JOIN DispatchLists AS ds ON ds.iSOsID = s.iSOsID
LEFT JOIN DispatchList AS d ON d.dlid = ds.dlid
LEFT JOIN Inventory AS sInv ON sInv.cInvCode = s.cInvCode
LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
LEFT JOIN Customer AS c ON c.cCusCode = m.cCusCode
WHERE 1 = 1
  -- 只查询所有发货单均未审核的记录
  AND 0 >= (SELECT LEN(MAX(ISNULL(cVerifier, ''))) 
            FROM DispatchList AS dm 
            LEFT JOIN DispatchLists AS ds ON dm.dlid = ds.dlid
            WHERE ds.iSOsID IN (SELECT iSOsID FROM SO_SODetails WHERE id = m.id))
```

#### 2.1.3 Dos.ORM实体（ORM映射）

| 实体类 | 映射对象 | 说明 |
|--------|----------|------|
| `SaleOrderDispatch` | 视图 `dflview_eams_SaleOrderDispatch` | 订单+发货单联合视图（使用Dos.ORM标签映射） |
| `SaleDispatch` | 视图 `dflview_eams_SaleDispatch` | 仅发货单视图 |
| `Customer` | 表 `Customer` | U8客户档案 |
| `QueryParam` | 无（查询参数） | 12个查询条件字段 |

### 2.2 业务逻辑分析（BLL.cs）

#### 2.2.1 查询流程

```
查询入口
  ├── getOrderResult(queryParam)  →  销售订单+关联发货单查询
  │     └── getSODResult(queryParam)
  │           └── SaleOrderDispatch LeftJoin Customer
  │                 └── Where条件组装（12个可选条件）
  │                       └── ToList<SaleOrderDispatch>()
  │                             └── SaleOrderDispatch2Order() 聚合转换
  │
  └── getDispatchResult(queryParam)  →  仅发货单查询
        └── getDLDResult(queryParam)
              └── SaleDispatch LeftJoin Customer
                    └── Where条件组装
                          └── SaleOrderDispatch2Order() 聚合转换
```

**查询条件（QueryParam）**：

| 字段 | 对应ERP字段 | 匹配方式 |
|------|-------------|----------|
| cusCode | Customer.cCusCode | 精确匹配 |
| cusName | Customer.cCusName | LIKE模糊 |
| cusAbbName | SaleOrderDispatch.soCusName | LIKE模糊 |
| cusContact | Customer.cCusPPerson | 精确匹配 |
| cusPPerson | SaleOrderDispatch.cPersonName | 精确匹配 |
| cusPhone | Customer.cCusPhone / cCusHand / cCusFax | LIKE模糊 |
| cusMobile | Customer.cCusPhone / cCusHand / cCusFax | LIKE模糊 |
| cusAddr | SaleOrderDispatch.cCusOAddress | LIKE模糊 |
| vouchCode | SaleOrderDispatch.cSOCode | LIKE模糊 |
| vouchDate1 | SaleOrderDispatch.soDate | >= |
| vouchDate2 | SaleOrderDispatch.soDate | <= |

#### 2.2.2 聚合转换（SaleOrderDispatch2Order）

将扁平的联合查询结果聚合为层级对象结构：

```
SaleOrderDispatch (扁平列表)
  │
  └── SaleOrderDispatch2Order()
        │
        └── List<Order>
              ├── OrderMain (1)       ← 按soid去重
              ├── OrderDetails (N)    ← 按soInvCode+soQuantity去重
              └── Dispatchs (0..N)
                    ├── DispatchMain (1)   ← 按dlid去重
                    └── DispatchDetails (N) ← 按dlInvCode+dlQuantity去重
```

#### 2.2.3 更新逻辑

```csharp
// 统一更新入口
updateCustomer(updVouchCus upd)
  ├── 有soid → updateSOCustomer()  → UPDATE SO_SOMain SET cCusCode, cCusName
  └── 有dlid → updateDLCustomer()  → UPDATE DispatchList SET cCusCode, cCusName

// 客户参照
refCustomer(code)
  └── SELECT cCusName FROM Customer WHERE cCusCode = code
```

#### 2.2.4 发货日期修改

```csharp
getDispatch(dlcode)     → 按发货单号查询发货单信息
updDispatchDate(dlcode, dateTime)
  → UPDATE DispatchList SET dDate = '{dateTime}' WHERE cdlcode = '{dlcode}'
```

**注意**：原老系统初期使用自定义字段 `cdefine4` 记录送货日期，后续实际业务中因客户原因需要直接修改发货单主表的标准日期字段 `dDate`，将发货日期调整到下月第一个周日。新系统将直接修改 `dDate` 字段。

### 2.3 安全与日志分析

| 方面 | 老系统做法 | 风险 |
|------|-----------|------|
| SQL注入 | BLL中使用字符串拼接参数，如 `$"'{cusCode}'"` | **高风险**，存在SQL注入漏洞 |
| 权限控制 | `[AuthorizeEx(Roles = "ModifySaleOrderCustomer")]` | 角色级权限，基础可用 |
| 操作日志 | `AppLogsDataAccess.createLog()` 记录前后值 | 基础日志，字段级别不够精细 |
| 数据校验 | 仅检查cusCode和cusName非空 | 校验不充分 |
| 防并发 | 无锁机制 | 多用户并发修改可能冲突 |

### 2.4 前端分析

| 方面 | 老系统做法 | 问题 |
|------|-----------|------|
| 技术栈 | jQuery + Razor视图 + 字符串拼接HTML | 难以维护，无组件化 |
| 表格渲染 | `genTable()` 函数动态拼接DOM | 代码冗长，性能差 |
| 交互 | 表格行点击展开/折叠，行内编辑 | 功能可用但粗糙 |
| 客户参照 | AJAX异步请求 `refCustomer()` | 单个字段参照，效率低 |
| 数据交互 | jQuery.ajax 同步请求 | 同步请求阻塞UI |
| Vue实验 | 发货日期修改页试用Vue.js | 未全面推广 |

---

## 3. 新模块设计

### 3.1 设计原则

| 原则 | 说明 |
|------|------|
| **分层解耦** | Controller → Service → Repository，遵循现有EAMS2026架构 |
| **类型安全** | 强类型DTO，杜绝DataTable传递 |
| **参数化查询** | 全部使用Dapper参数化查询，消除SQL注入 |
| **U8兼容** | SQL Server 2005兼容，不使用OFFSET/FETCH等新语法 |
| **操作审计** | 完整记录修改前后值、操作人、时间 |
| **前端组件化** | Vue 3 + Element Plus组件化开发 |

### 3.2 功能树

```
ERP单据修改 (VouchModify)
├── 1.0 销售订单客户修改
│   ├── 1.1 多条件查询（客户编码/名称/简称/联系人/电话/地址/订单编号/日期范围）
│   ├── 1.2 订单+关联发货单联合展示（树形表格）
│   ├── 1.3 客户参照（编码→名称自动填充）
│   ├── 1.4 单条/批量修改客户
│   └── 1.5 修改结果实时反馈
│
├── 2.0 销售发货单客户修改
│   ├── 2.1 多条件查询（同1.1）
│   ├── 2.2 发货单明细展示
│   ├── 2.3 客户参照
│   ├── 2.4 单条/批量修改客户
│   └── 2.5 修改结果实时反馈
│
├── 3.0 发货单日期批量修改
│   ├── 3.1 多条件查询未审核发货单
│   │   ├── 客户编码/名称/简称
│   │   ├── 发货单单号
│   │   ├── 日期范围筛选
│   │   └── 客户经理
│   ├── 3.2 发货单列表展示
│   │   ├── 单号、客户、原日期、审核人、金额
│   │   └── 复选框选择（可批量勾选）
│   ├── 3.3 目标日期计算
│   │   ├── 自动计算下月第一个周日
│   │   └── 支持手动调整目标日期
│   ├── 3.4 批量修改执行
│   │   ├── 修改前二次确认弹窗
│   │   ├── 逐条 UPDATE DispatchList SET dDate = @NewDate
│   │   └── 逐条记录操作日志
│   └── 3.5 修改结果反馈
│       ├── 成功/失败条数汇总
│       └── 失败原因明细展示
│
├── 4.0 修改模板管理（新增）
│   ├── 4.1 保存常用修改规则
│   ├── 4.2 应用模板批量修改
│   └── 4.3 模板列表管理
│
├── 5.0 修改日志（增强）
│   ├── 5.1 操作前后值完整记录
│   ├── 5.2 按时间/操作人/单据类型筛选
│   └── 5.3 日志导出
│
└── 6.0 权限控制
    ├── 6.1 销售订单修改权限
    ├── 6.2 发货单修改权限
    └── 6.3 模板管理权限
```

### 3.3 业务规则

| 规则 | 说明 |
|------|------|
| **仅修改未审核单据** | 仅查询和修改 `cVerifier IS NULL` 的发货单（发货单审核后不可修改） |
| **客户编码必填** | 修改时必须填写新的客户编码，且编码必须在Customer表中存在 |
| **客户名称自动填充** | 输入客户编码后自动从ERP Customer表参照出客户名称 |
| **订单发货联动** | 修改销售订单客户时，可选择是否同步修改关联的未审核发货单 |
| **不可逆操作确认** | 修改操作不可回滚，前端需弹窗确认 |
| **并发保护** | 修改前检查单据状态是否仍为未审核 |

### 3.3a 发货单日期修改业务规则

| 规则 | 说明 |
|------|------|
| **业务背景** | 因客户原因（如备货延迟、资金未到位等），需将已创建但未审核的发货单日期统一调整到下月 |
| **目标日期计算** | 默认自动计算**下月的第一个周日**，允许操作员手动修改目标日期 |
| **仅未审核单据** | 仅可选择 `cVerifier IS NULL` 的发货单，已审核的不可见 |
| **批量操作** | 支持勾选多条未审核发货单，统一修改为同一个目标日期 |
| **直接修改dDate** | 修改的是 `DispatchList.dDate` 标准日期字段，而非自定义字段 |
| **不可逆确认** | 批量修改前必须二次弹窗确认，展示将影响的单据数量 |
| **逐条执行** | 后端逐条 UPDATE，单条失败不影响其他条，最终汇总成功/失败 |
| **完整日志** | 每条修改记录修改前日期、修改后日期、操作人、操作时间 |

### 3.4 架构设计

```
┌──────────────────────────────────────────────────────────────┐
│                    前端 (Vue 3 + Element Plus)                │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  VouchModifyOrder.vue   发货单客户修改                  │  │
│  │  VouchModifyDispatch.vue 发货单客户修改                  │  │
│  │  VouchModifyDate.vue    发货单日期批量修改              │  │
│  │  VouchModifyTemplate.vue 修改模板管理                   │  │
│  │  VouchModifyLog.vue     修改日志查询                    │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────┬───────────────────────────────┘
                               │ HTTP/JSON (REST API)
┌──────────────────────────────┼───────────────────────────────┐
│                   后端 (.NET 8 Web API)                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Controller: VouchModifyController                     │  │
│  │  - GET    /api/vouch-modify/orders       查询订单      │  │
│  │  - GET    /api/vouch-modify/dispatches   查询发货单    │  │
│  │  - GET    /api/vouch-modify/dispatch/{code} 单笔查询   │  │
│  │  - POST   /api/vouch-modify/order-cus   修改订单客户   │  │
│  │  - POST   /api/vouch-modify/dispatch-cus 修改发货客户  │  │
│  │  - POST   /api/vouch-modify/dispatch-date 修改发货日期 │  │
│  │  - GET    /api/vouch-modify/customer-ref/{code} 客户参照│  │
│  │  - CRUD   /api/vouch-modify/templates    模板管理       │  │
│  │  - GET    /api/vouch-modify/logs         查询日志       │  │
│  └────────────────────────────────────────────────────────┘  │
│                               │                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Service: VouchModifyService                          │  │
│  │  - 查询条件组装                                       │  │
│  │  - 数据聚合转换（扁平 → 层级）                        │  │
│  │  - 修改前检查（单据状态验证）                         │  │
│  │  - 执行修改（参数化SQL）                              │  │
│  │  - 操作日志记录                                       │  │
│  └────────────────────────────────────────────────────────┘  │
│                               │                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Repository: IVouchModifyRepository                   │  │
│  │  - QueryOrders(QueryParam) → List<Order>              │  │
│  │  - QueryDispatches(QueryParam) → List<Dispatch>       │  │
│  │  - QueryUnverifiedDispatches(QueryParam) → List    │  │
│  │  - GetDispatchByCode(code) → Dispatch                 │  │
│  │  - UpdateOrderCustomer(soid, cusCode, cusName)        │  │
│  │  - UpdateDispatchCustomer(dlid, cusCode, cusName)     │  │
│  │  - UpdateDispatchDate(dlid, newDate) → bool           │  │
│  │  - BatchUpdateDispatchDate(List<dlid>, newDate) →     │  │
│  │  - GetCustomerName(code) → string                     │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────┬───────────────────────────────┘
                               │
                    ┌──────────┴──────────┐
                    │                     │
            PostgreSQL (主库)     SQL Server (U8 ERP)
            - 修改模板                  - SO_SOMain
            - 操作日志                  - SO_SODetails
            - 权限数据                  - DispatchList
                                        - DispatchLists
                                        - Customer
                                        - Inventory
```

### 3.5 数据库设计

#### 3.5.1 新表（PostgreSQL主库）

```sql
-- 修改模板表
CREATE TABLE vouch_modify_templates (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,            -- 模板名称
    description TEXT,                       -- 模板描述
    vouch_type VARCHAR(20) NOT NULL,        -- 单据类型: order/dispatch
    from_cus_code VARCHAR(50),              -- 原客户编码（可选）
    from_cus_name VARCHAR(100),             -- 原客户名称（可选）
    to_cus_code VARCHAR(50) NOT NULL,       -- 目标客户编码
    to_cus_name VARCHAR(100) NOT NULL,      -- 目标客户名称
    is_active BOOLEAN DEFAULT TRUE,         -- 是否启用
    created_by INTEGER NOT NULL,            -- 创建人
    created_at TIMESTAMP DEFAULT NOW(),     -- 创建时间
    updated_at TIMESTAMP DEFAULT NOW()      -- 更新时间
);

-- 修改日志表
CREATE TABLE vouch_modify_logs (
    id SERIAL PRIMARY KEY,
    vouch_type VARCHAR(20) NOT NULL,        -- 单据类型: order/dispatch
    vouch_id INTEGER NOT NULL,              -- 单据ID (soid/dlid)
    vouch_code VARCHAR(50) NOT NULL,        -- 单据编号 (cSOCode/cdlcode)
    field_name VARCHAR(50) NOT NULL,        -- 修改字段
    old_value VARCHAR(200),                  -- 原值
    new_value VARCHAR(200),                  -- 新值
    operator_id INTEGER NOT NULL,           -- 操作人ID
    operator_name VARCHAR(50) NOT NULL,     -- 操作人姓名
    operate_at TIMESTAMP DEFAULT NOW(),     -- 操作时间
    status VARCHAR(10) DEFAULT 'SUCCESS',   -- 状态: SUCCESS/FAILED
    error_msg TEXT                          -- 错误信息
);

CREATE INDEX idx_vouch_logs_operator ON vouch_modify_logs(operator_id);
CREATE INDEX idx_vouch_logs_time ON vouch_modify_logs(operate_at);
CREATE INDEX idx_vouch_logs_type ON vouch_modify_logs(vouch_type);
```

#### 3.5.2 U8 ERP表（只读+写入）

| 表名 | 操作 | 关键字段 | 说明 |
|------|------|----------|------|
| SO_SOMain | 写（UPDATE cCusCode, cCusName） | id, cSOCode, cCusCode, cCusName, dDate | 销售订单主表 |
| SO_SODetails | 只读 | id, iSOsID, cInvCode, cInvName, iQuantity | 销售订单子表 |
| DispatchList | 写（UPDATE cCusCode, cCusName / UPDATE dDate） | dlid, cDLCode, cCusCode, cCusName, dDate, cVerifier | 发货单主表，日期修改为目标日期 |
| DispatchLists | 只读 | dlid, iSOsID, cInvCode, cInvName, iQuantity | 发货单子表 |
| Customer | 只读 | cCusCode, cCusName, cCusAbbName, cCusPhone, ... | 客户档案 |
| Inventory | 只读 | cInvCode, cInvName, cInvStd | 存货档案 |
| Person | 只读 | cPersonCode, cPersonName | 人员档案 |

### 3.6 API设计

#### 3.6.1 接口清单

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/vouch-modify/orders` | 查询销售订单（支持多条件） |
| GET | `/api/vouch-modify/dispatches` | 查询发货单（支持多条件） |
| GET | `/api/vouch-modify/dispatches-unverified` | 查询未审核发货单（支持多条件，用于日期批量修改） |
| GET | `/api/vouch-modify/dispatch/{dlcode}` | 按单号查询发货单详情 |
| POST | `/api/vouch-modify/order-customer` | 修改销售订单客户 |
| POST | `/api/vouch-modify/dispatch-customer` | 修改发货单客户 |
| POST | `/api/vouch-modify/dispatch-date` | 修改单笔发货单日期 |
| POST | `/api/vouch-modify/dispatch-date/batch` | 批量修改发货单日期（含下月首个周日自动计算） |
| GET | `/api/vouch-modify/customer-ref/{code}` | 客户编码参照 |
| POST | `/api/vouch-modify/templates` | 创建修改模板 |
| GET | `/api/vouch-modify/templates` | 查询修改模板列表 |
| PUT | `/api/vouch-modify/templates/{id}` | 更新修改模板 |
| DELETE | `/api/vouch-modify/templates/{id}` | 删除修改模板 |
| GET | `/api/vouch-modify/logs` | 查询修改日志（分页） |

#### 3.6.2 请求/响应DTO

```csharp
// 查询参数
public class VouchQueryParam
{
    public string? CusCode { get; set; }         // 客户编码
    public string? CusName { get; set; }          // 客户名称
    public string? CusAbbName { get; set; }       // 客户简称
    public string? CusContact { get; set; }       // 联系人
    public string? CusPPerson { get; set; }       // 客户经理
    public string? CusPhone { get; set; }         // 电话
    public string? CusMobile { get; set; }        // 手机
    public string? CusAddr { get; set; }          // 地址
    public string? VouchCode { get; set; }        // 单据编号
    public DateTime? VouchDateFrom { get; set; }  // 日期起
    public DateTime? VouchDateTo { get; set; }    // 日期止
    public int Page { get; set; } = 1;            // 页码
    public int PageSize { get; set; } = 20;       // 每页大小
}

// 修改请求
public class UpdateCustomerRequest
{
    public int? Soid { get; set; }            // 订单ID（修改订单时必填）
    public int? Dlid { get; set; }            // 发货单ID（修改发货单时必填）
    public string NewCusCode { get; set; }    // 新客户编码（必填）
    public string NewCusName { get; set; }    // 新客户名称（必填）
    public string? OldCusCode { get; set; }   // 原客户编码（日志记录）
    public string? OldCusName { get; set; }   // 原客户名称（日志记录）
    public bool SyncDispatches { get; set; }  // 是否同步修改关联发货单
}

public class UpdateDateRequest
{
    public string DlCode { get; set; }        // 发货单号（单笔修改时使用）
    public int? Dlid { get; set; }            // 发货单ID
    public DateTime NewDate { get; set; }     // 新送货日期
}

public class BatchUpdateDateRequest
{
    public List<int> Dlids { get; set; }      // 发货单ID列表（批量修改）
    public DateTime NewDate { get; set; }     // 目标日期，默认为下月第一个周日
    public bool AutoCalculate { get; set; }  // 是否自动计算下月第一个周日
}

// 响应
public class VouchModifyResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int AffectedRows { get; set; }
}

public class VouchModifyBatchResult
{
    public int TotalCount { get; set; }       // 总处理条数
    public int SuccessCount { get; set; }     // 成功条数
    public int FailCount { get; set; }        // 失败条数
    public List<BatchFailItem>? Failures { get; set; }  // 失败明细
}

public class BatchFailItem
{
    public int Dlid { get; set; }
    public string DlCode { get; set; }
    public string ErrorMessage { get; set; }
}
```

### 3.7 后端实现要点

#### 3.7.1 数据访问层（Repository）

```
IVouchModifyRepository (接口)
  ├── U8VouchModifyRepository (实现)
  │     ├── 使用 Dapper + Microsoft.Data.SqlClient 连接U8 SQL Server
  │     ├── 全部使用参数化查询（new { Param = value }）
  │     └── SQL兼容SQL Server 2005语法
  │
  └── 关键查询SQL模板：
        ├── 订单查询：SO_SOMain LEFT JOIN SO_SODetails LEFT JOIN ...
        ├── 发货单查询：DispatchList LEFT JOIN DispatchLists LEFT JOIN ...
        ├── 更新订单：UPDATE SO_SOMain SET cCusCode=@code, cCusName=@name WHERE id=@id
        └── 更新发货单：UPDATE DispatchList SET cCusCode=@code, cCusName=@name WHERE dlid=@dlid
```

#### 3.7.2 业务逻辑层（Service）

```
VouchModifyService
  ├── QueryOrdersAsync(param) → PagedResult<OrderDto>
  │     ├── 参数校验
  │     ├── 调用Repository查询
  │     ├── 数据聚合（Order → OrderMain + OrderDetails + Dispatchs）
  │     └── 分页处理
  │
  ├── QueryDispatchesAsync(param) → PagedResult<DispatchDto>
  │     └── 同上，仅查询发货单
  │
  ├── UpdateOrderCustomerAsync(request) → VouchModifyResult
  │     ├── 校验：NewCusCode非空，Customer表存在
  │     ├── 检查：单据是否仍为未审核状态（读当前数据库）
  │     ├── 执行：UPDATE SO_SOMain
  │     ├── 同步：若SyncDispatches=true，更新关联的未审核发货单
  │     ├── 日志：记录修改前后值
  │     └── 返回：影响行数
  │
  ├── UpdateDispatchCustomerAsync(request) → VouchModifyResult
  │     └── 类似UpdateOrderCustomerAsync
  │
  ├── UpdateDispatchDateAsync(request) → VouchModifyResult
  │     ├── 校验：dlcode非空
  │     ├── 执行：UPDATE DispatchList SET dDate = @NewDate
  │     └── 日志记录
  │
  ├── BatchUpdateDispatchDateAsync(request) → VouchModifyBatchResult
  │     ├── 参数：List<dlid>, NewDate (DateTime)
  │     ├── 默认目标日期：自动计算下月第一个周日
  │     │     ├── 取当前日期 → 下月1日 → 找到第一个Sunday
  │     │     └── 支持手动覆盖目标日期
  │     ├── 逐条处理：
  │     │     ├── 1) SELECT 检查单据仍为未审核
  │     │     ├── 2) UPDATE DispatchList SET dDate = @NewDate WHERE dlid = @dlid
  │     │     └── 3) 写入操作日志 vouch_modify_logs
  │     ├── 失败容忍：单条失败仅记录错误，继续处理下一条
  │     └── 返回：汇总统计（总条数、成功数、失败数、失败明细）
  │
  └── GetCustomerReference(code) → CustomerRefDto
        └── SELECT cCusCode, cCusName FROM Customer WHERE cCusCode = @code
```

#### 3.7.3 安全性措施

| 措施 | 实现方式 |
|------|----------|
| 防SQL注入 | Dapper参数化查询，杜绝字符串拼接 |
| 权限控制 | `[Authorize(Policy = "VouchModifyOrder")]` 基于策略的授权 |
| 数据校验 | FluentValidation或DataAnnotations校验请求DTO |
| 操作审计 | 所有修改操作写入 `vouch_modify_logs` 表 |
| 并发保护 | UPDATE前SELECT验证单据状态仍为未审核 |
| 事务保护 | 涉及多表更新（订单+发货单同步）时使用事务 |

### 3.8 前端设计要点

#### 3.8.1 页面结构

```
VouchModifyOrder.vue
├── 查询面板（折叠式）
│   ├── 客户编码输入
│   ├── 客户名称输入
│   ├── 客户简称输入
│   ├── 联系人输入
│   ├── 客户经理输入
│   ├── 电话/手机输入
│   ├── 地址输入
│   ├── 订单编号输入
│   └── 日期范围选择器
├── 操作栏
│   ├── 查询按钮
│   ├── 重置按钮
│   └── 批量修改按钮
├── 结果表格（树形数据，el-table）
│   ├── 行1: 订单主表 (rowspan聚合)
│   │   ├── 修改操作列（客户编码输入 + 参照按钮）
│   │   ├── 订单编号、日期、原客户编码、原客户名称、客户经理
│   │   └── 订单明细（展开后显示存货编码/名称/规格/数量）
│   └── 行2-N: 关联发货单 (展开显示)
│       ├── 修改操作列（可选同步修改）
│       ├── 发货单号、日期、客户
│       └── 发货明细
├── 分页组件
└── 客户参照对话框（el-dialog）
    ├── 客户编码/名称搜索
    └── 客户列表选择
```

#### 3.8.2 组件树

```
views/
└── erp/
    ├── VouchModifyOrder.vue          # 销售订单客户修改
    ├── VouchModifyDispatch.vue       # 发货单客户修改
    ├── VouchModifyDate.vue           # 发货单日期批量修改
    ├── VouchModifyTemplate.vue       # 修改模板管理
    └── VouchModifyLog.vue            # 修改日志查询

components/
└── erp/
    ├── CustomerRefDialog.vue         # 客户参照对话框（复用）
    ├── VouchQueryPanel.vue           # 查询条件面板（复用）
    ├── VouchModifyTable.vue          # 单据修改表格（复用）
    └── VouchModifyConfirm.vue        # 修改确认对话框
```

#### 3.8.3 API调用封装

```typescript
// api/erp/vouchModify.ts
export const vouchModifyApi = {
  // 查询订单
  queryOrders(params: VouchQueryParam): Promise<PagedResult<OrderDto>>,
  // 查询发货单
  queryDispatches(params: VouchQueryParam): Promise<PagedResult<DispatchDto>>,
  // 查询单笔发货单
  getDispatch(dlcode: string): Promise<DispatchDto>,
  // 修改订单客户
  updateOrderCustomer(req: UpdateCustomerReq): Promise<VouchModifyResult>,
  // 修改发货单客户
  updateDispatchCustomer(req: UpdateCustomerReq): Promise<VouchModifyResult>,
  // 修改发货日期
  updateDispatchDate(req: UpdateDateReq): Promise<VouchModifyResult>,
  // 客户参照
  getCustomerRef(code: string): Promise<CustomerRefDto>,
  // 模板管理
  getTemplates(): Promise<TemplateDto[]>,
  createTemplate(req: TemplateReq): Promise<TemplateDto>,
  updateTemplate(id: number, req: TemplateReq): Promise<TemplateDto>,
  deleteTemplate(id: number): Promise<void>,
  // 日志查询
  getLogs(params: LogQueryParam): Promise<PagedResult<VouchLogDto>>,
};
```

### 3.9 与老系统的关键差异

| 对比项 | 老系统 (EAMS_4.6) | 新系统 (EAMS2026) |
|--------|-------------------|-------------------|
| **ORM** | Dos.ORM（第三方ORM） | Dapper（轻量级） |
| **数据传递** | DataTable + 反射 | 强类型DTO |
| **SQL安全** | 字符串拼接，SQL注入风险 | Dapper参数化查询 |
| **架构** | BLL扁平两层 | Controller → Service → Repository |
| **前端** | jQuery + Razor字符串拼HTML | Vue 3 + Element Plus组件化 |
| **分页** | 无分页（全量加载） | 服务端分页 |
| **日志** | 写入日志库自定义表 | 独立日志表 `vouch_modify_logs` |
| **模板** | 无 | 新增模板管理功能 |
| **并发** | 无保护 | UPDATE前验证单据状态 |
| **事务** | 单条SQL无事务 | 多表修改使用事务 |
| **客户参照** | 单编码输入 | 支持搜索选择对话框 |

### 3.10 项目文件结构

```
src/
├── EAMS2026.Domain/
│   ├── DTOs/
│   │   └── Erp/
│   │       ├── OrderDto.cs
│   │       ├── DispatchDto.cs
│   │       ├── VouchQueryParam.cs
│   │       ├── UpdateCustomerRequest.cs
│   │       ├── UpdateDateRequest.cs
│   │       ├── VouchModifyResult.cs
│   │       └── CustomerRefDto.cs
│   └── Entities/
│       └── Erp/
│           ├── VouchModifyTemplate.cs
│           └── VouchModifyLog.cs
│
├── EAMS2026.Application/
│   ├── Common/Interfaces/
│   │   └── IVouchModifyService.cs
│   └── Services/
│       └── VouchModifyService.cs
│
├── EAMS2026.Infrastructure/
│   ├── Data/
│   │   ├── Repositories/
│   │   │   └── U8VouchModifyRepository.cs
│   │   └── Mappings/
│   │       └── VouchModifyTemplateMapping.cs
│   └── Services/
│       └── VouchModifyService.cs  (实现)
│
└── EAMS2026.Api/
    └── Controllers/
        └── VouchModifyController.cs
```

### 3.11 发货单日期批量修改页面设计（VouchModifyDate.vue）

#### 3.11.1 页面布局

```
VouchModifyDate.vue 页面结构
├── 查询面板（折叠式，el-collapse）
│   ├── 客户编码输入 (el-input, v-model="query.cusCode")
│   ├── 客户名称输入 (el-input, v-model="query.cusName")
│   ├── 发货单单号输入 (el-input, v-model="query.vouchCode")
│   ├── 客户经理下拉选择 (el-select, v-model="query.cusPPerson")
│   ├── 原日期范围选择器 (el-date-picker, type="daterange", v-model="query.dateRange")
│   └── 操作按钮区
│       ├── [查询] 按钮 (el-button type="primary")
│       └── [重置] 按钮 (el-button)
│
├── 结果工具栏
│   ├── 选中计数: "已选择 N 条未审核单据"
│   ├── 目标日期设置
│   │   ├── 自动计算: "下月第一个周日: 2026-07-05" (el-tag, type="success")
│   │   ├── 手动覆盖日期选择器 (el-date-picker, v-model="targetDate")
│   │   └── [应用到选中项] 预览按钮 (el-button, 更新预览列)
│   └── [批量修改日期] 执行按钮 (el-button type="warning", :disabled="!hasSelection")
│
├── 发货单列表 (el-table, @selection-change="onSelectionChange")
│   ├── 列复选框 (type="selection", :selectable="row => !row.verifier")
│   ├── 发货单号 (cDLCode)
│   ├── 客户编码/名称 (cCusCode / cCusName)
│   ├── 原日期 (dDate)
│   ├── 目标日期 (newDate) — 预览列，显示将修改到的目标日期
│   ├── 审核人 (cVerifier) — 显示"未审核"标签
│   ├── 客户经理 (cPersonName)
│   └── 金额 (iMoney, 可选)
│
├── 分页组件 (el-pagination)
│
└── 修改确认对话框 (el-dialog)
    ├── 展示内容:
    │   ├── 待修改单据数量: N 条
    │   ├── 原日期范围: YYYY-MM-DD ~ YYYY-MM-DD
    │   └── 目标日期: YYYY-MM-DD (下月第一个周日)
    ├── [确认修改] 按钮 (el-button type="primary")
    └── [取消] 按钮 (el-button)
```

#### 3.11.2 交互流程

```
用户操作 → 前端行为 → API调用

1. 进入页面
   └── 默认加载最近30天的未审核发货单
   └── GET /api/vouch-modify/dispatches-unverified?dateFrom=...

2. 输入查询条件 → 点击[查询]
   └── 调用查询接口，刷新表格
   └── GET /api/vouch-modify/dispatches-unverified?cusCode=xxx&...

3. 勾选需要修改的发货单
   └── 表格选择变化 → 显示"已选中 N 条"
   └── 目标日期自动计算并显示

4. 目标日期计算（前端自动计算 + 后端验证）
   └── function calculateFirstSundayOfNextMonth(): Date {
   │     const now = new Date();
   │     const nextMonth = new Date(now.getFullYear(), now.getMonth() + 1, 1);
   │     const dayOfWeek = nextMonth.getDay(); // 0=Sun, 1=Mon, ...
   │     const daysUntilSunday = dayOfWeek === 0 ? 0 : 7 - dayOfWeek;
   │     nextMonth.setDate(nextMonth.getDate() + daysUntilSunday);
   │     return nextMonth;
   │   }
   └── 支持手动选择其他日期 → 预览列实时更新

5. 点击[批量修改日期]
   └── 弹窗确认: "即将修改 N 条发货单的日期为 YYYY-MM-DD，是否继续？"
   └── 用户确认 →
   └── POST /api/vouch-modify/dispatch-date/batch
   │     {
   │       "dlids": [1, 2, 3, ...],
   │       "newDate": "2026-07-05",
   │       "autoCalculate": true
   │     }
   └── 返回结果:
       {
         "totalCount": 10,
         "successCount": 8,
         "failCount": 2,
         "failures": [
           { "dlid": 5, "dlCode": "2026060005", "errorMessage": "单据已审核" }
         ]
       }
   └── 前端展示结果汇总:
       ├── 成功: 8 条 ✅  (绿色标签)
       └── 失败: 2 条 ❌  (展开失败明细列表)

6. 修改完成后
   └── 自动刷新表格，已修改单据不再显示（因日期已变更或状态变化）
   └── 可点击[重置]重新查询
```

#### 3.11.3 数据模型（前端 TypeScript）

```typescript
// 查询参数
interface UnverifiedDispatchQuery {
  cusCode?: string;
  cusName?: string;
  vouchCode?: string;
  cusPPerson?: string;
  dateFrom?: string;
  dateTo?: string;
  page: number;
  pageSize: number;
}

// 发货单行数据
interface DispatchRow {
  dlid: number;
  cDLCode: string;
  cCusCode: string;
  cCusName: string;
  dDate: string;          // 原日期
  targetDate?: string;    // 目标日期（预览列）
  cVerifier: string | null;
  cPersonName?: string;
  iMoney?: number;
}

// 批量修改请求
interface BatchUpdateDateReq {
  dlids: number[];
  newDate: string;
  autoCalculate: boolean;
}

// 批量修改结果
interface BatchUpdateDateRes {
  totalCount: number;
  successCount: number;
  failCount: number;
  failures: BatchFailItem[];
}

interface BatchFailItem {
  dlid: number;
  dlCode: string;
  errorMessage: string;
}
```

#### 3.11.4 下月第一个周日计算算法

```typescript
/**
 * 获取下个月第一个周日
 * 算法：
 *   1. 取当前日期
 *   2. 计算下个月1日
 *   3. 找到该日期之后的第一个周日（星期天）
 */
function getFirstSundayOfNextMonth(): Date {
  const today = new Date();
  // 下个月1日
  const firstOfNextMonth = new Date(today.getFullYear(), today.getMonth() + 1, 1);
  // 获取星期几 (0=Sunday, 1=Monday, ..., 6=Saturday)
  const dayOfWeek = firstOfNextMonth.getDay();
  // 距离第一个周日需要的天数
  const daysToAdd = dayOfWeek === 0 ? 0 : 7 - dayOfWeek;
  // 计算结果
  firstOfNextMonth.setDate(firstOfNextMonth.getDate() + daysToAdd);
  return firstOfNextMonth;
}

// 示例: 2026-06-13 → 下月1日 2026-07-01 (周三) → 距周日4天 → 2026-07-05
```

#### 3.11.5 API调用封装

```typescript
// api/erp/vouchModify.ts 追加
export const vouchModifyApi = {
  // ... 已有方法 ...

  /** 查询未审核发货单（用于日期批量修改） */
  queryUnverifiedDispatches(
    params: UnverifiedDispatchQuery
  ): Promise<PagedResult<DispatchRow>> {
    return http.get('/api/vouch-modify/dispatches-unverified', { params });
  },

  /** 批量修改发货单日期 */
  batchUpdateDispatchDate(
    req: BatchUpdateDateReq
  ): Promise<BatchUpdateDateRes> {
    return http.post('/api/vouch-modify/dispatch-date/batch', req);
  },

  /** 单笔修改发货单日期 */
  updateDispatchDate(
    req: UpdateDateRequest
  ): Promise<VouchModifyResult> {
    return http.post('/api/vouch-modify/dispatch-date', req);
  },
};
```

---

## 4. 时序图

### 4.1 查询订单时序

```
用户                    前端                    API                    Service                 U8 ERP
 │                       │                      │                       │                       │
 │  打开页面              │                      │                       │                       │
 │──────────────────────>│                      │                       │                       │
 │                       │   GET /api/vouch-modify/orders                │                       │
 │  点击查询              │─────────────────────>│                       │                       │
 │──────────────────────>│                      │   QueryOrdersAsync()   │                       │
 │                       │                      │──────────────────────>│                       │
 │                       │                      │                       │  SELECT ... FROM       │
 │                       │                      │                       │  SO_SOMain LEFT JOIN   │
 │                       │                      │                       │  SO_SODetails ...      │
 │                       │                      │                       │──────────────────────>│
 │                       │                      │                       │                       │
 │                       │                      │                       │  DataReader → List     │
 │                       │                      │                       │<──────────────────────│
 │                       │                      │                       │                       │
 │                       │                      │     PagedResult       │                       │
 │                       │                      │<──────────────────────│                       │
 │                       │    JSON Result       │                       │                       │
 │                       │<─────────────────────│                       │                       │
 │  树形表格渲染           │                      │                       │                       │
 │<──────────────────────│                      │                       │                       │
```

### 4.2 修改客户时序

```
用户                    前端                    API                    Service                 U8 ERP
 │                       │                      │                       │                       │
 │  输入新客户编码         │                      │                       │                       │
 │                       │   GET /customer-ref/{code}                    │                       │
 │  客户参照              │─────────────────────>│                       │                       │
 │                       │                      │   GetCustomerName()    │                       │
 │                       │                      │──────────────────────>│                       │
 │                       │                      │                       │  SELECT cCusName      │
 │                       │                      │                       │  FROM Customer         │
 │                       │                      │                       │──────────────────────>│
 │                       │                      │                       │<──────────────────────│
 │  显示客户名称           │<─────────────────────│                       │                       │
 │                       │                      │                       │                       │
 │  点击确认修改           │                      │                       │                       │
 │──────────────────────>│   POST /api/vouch-modify/order-customer       │                       │
 │                       │─────────────────────>│                       │                       │
 │                       │                      │  UpdateOrderCustomerAsync()                    │
 │                       │                      │──────────────────────>│                       │
 │                       │                      │                       │  1. SELECT 检查状态    │
 │                       │                      │                       │──────────────────────>│
 │                       │                      │                       │<──────────────────────│
 │                       │                      │                       │  2. UPDATE SO_SOMain   │
 │                       │                      │                       │──────────────────────>│
 │                       │                      │                       │<──────────────────────│
 │                       │                      │                       │  3. (可选) UPDATE      │
 │                       │                      │                       │     DispatchList       │
 │                       │                      │                       │──────────────────────>│
 │                       │                      │                       │<──────────────────────│
 │                       │                      │                       │  4. 写入操作日志      │
 │                       │                      │                       │  (PostgreSQL)          │
 │                       │                      │                       │                       │
 │                       │                      │   VouchModifyResult   │                       │
 │                       │                      │<──────────────────────│                       │
 │  显示修改结果           │<─────────────────────│                       │                       │
 │<──────────────────────│                      │                       │                       │
```

### 4.3 发货单日期批量修改时序

```
用户                    前端(VouchModifyDate.vue)   API                    Service                 U8 ERP
 │                       │                          │                       │                       │
 │  进入页面              │                          │                       │                       │
 │──────────────────────>│                          │                       │                       │
 │                       │  GET /api/vouch-modify/dispatches-unverified     │                       │
 │  加载未审核发货单       │─────────────────────────>│                       │                       │
 │                       │                          │  QueryUnverified()    │                       │
 │                       │                          │──────────────────────>│                       │
 │                       │                          │                       │  SELECT FROM          │
 │                       │                          │                       │  DispatchList         │
 │                       │                          │                       │  WHERE cVerifier IS   │
 │                       │                          │                       │  NULL                 │
 │                       │                          │                       │──────────────────────>│
 │                       │                          │                       │<──────────────────────│
 │                       │  PagedResult<Dispatch>   │                       │                       │
 │                       │<─────────────────────────│                       │                       │
 │  渲染表格              │                          │                       │                       │
 │<──────────────────────│                          │                       │                       │
 │                       │                          │                       │                       │
 │  勾选N条单据           │                          │                       │                       │
 │──────────────────────>│                          │                       │                       │
 │                       │  预览目标日期自动计算      │                       │                       │
 │                       │  下月第一个周日: 2026-07-05                      │                       │
 │<──────────────────────│                          │                       │                       │
 │                       │                          │                       │                       │
 │  点击[批量修改]         │                          │                       │                       │
 │──────────────────────>│                          │                       │                       │
 │                       │  弹窗确认                 │                       │                       │
 │<──────────────────────│                          │                       │                       │
 │                       │                          │                       │                       │
 │  确认修改              │                          │                       │                       │
 │──────────────────────>│  POST /api/vouch-modify/dispatch-date/batch      │                       │
 │                       │─────────────────────────>│                       │                       │
 │                       │                          │  BatchUpdateDate()    │                       │
 │                       │                          │──────────────────────>│                       │
 │                       │                          │                       │  FOR EACH dlid:       │
 │                       │                          │                       │  ┌────────────────┐   │
 │                       │                          │                       │  │ 1. SELECT 检查  │──>│
 │                       │                          │                       │  │    未审核状态   │<──│
 │                       │                          │                       │  │ 2. UPDATE dDate │──>│
 │                       │                          │                       │  │    = @NewDate   │<──│
 │                       │                          │                       │  │ 3. 写入日志     │   │
 │                       │                          │                       │  └────────────────┘   │
 │                       │                          │                       │                       │
 │                       │                          │  VouchModifyBatchResult                       │
 │                       │                          │<──────────────────────│                       │
 │                       │  JSON Result             │                       │                       │
 │                       │<─────────────────────────│                       │                       │
 │  展示汇总结果          │                          │                       │                       │
 │  "成功8条, 失败2条"    │                          │                       │                       │
 │<──────────────────────│                          │                       │                       │
 │                       │                          │                       │                       │
 │  查看失败明细          │                          │                       │                       │
 │──────────────────────>│                          │                       │                       │
 │  展开列表展示          │                          │                       │                       │
 │<──────────────────────│                          │                       │                       │
```

---

## 5. 迁移实施计划

### 5.1 迁移策略

| 阶段 | 内容 | 预估工时 | 交付物 |
|------|------|----------|--------|
| **P0** 基础设施 | 创建DTO、Entity、Repository接口和实现 | 1天 | Repository层代码 |
| **P1** 业务逻辑 | Service层实现（查询+修改+日志） | 1.5天 | Service层代码 |
| **P2** API层 | Controller实现，权限配置 | 0.5天 | API接口 |
| **P3** 前端页面 | 5个Vue页面 + 4个复用组件 | 2天 | 前端代码 |
| **P4** 模板+日志增强 | 模板管理CRUD，日志查询与导出 | 1天 | 完整功能 |
| **P5** 测试+验证 | 连接U8测试数据库验证功能 | 1天 | 测试报告 |
| **合计** | | **7天** | |

### 5.2 依赖项

| 依赖 | 说明 | 状态 |
|------|------|------|
| U8 ERP数据库连接配置 | appsettings.json中添加ErpDb连接字符串 | 待配置 |
| Microsoft.Data.SqlClient NuGet包 | SQL Server数据访问 | 已存在 |
| Dapper NuGet包 | 轻量级ORM | 已存在 |
| 权限策略配置 | VouchModifyOrder/VouchModifyDispatch权限定义 | 待添加 |
| PostgreSQL日志表创建 | vouch_modify_logs和vouch_modify_templates | 待创建 |

### 5.3 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| U8 SQL Server 2005语法限制 | 新语法不可用 | 使用ROW_NUMBER()分页，不使用OFFSET/FETCH |
| ERP数据库连接不稳定 | 操作超时 | 添加重试策略和超时配置 |
| 多用户并发修改同单据 | 数据不一致 | UPDATE前验证状态，乐观锁 |
| 日期计算逻辑错误 | 目标日期不符合业务预期 | 前端预览目标日期，确认后执行 |

---

## 附录A：老系统关键代码片段

### A.1 核心查询SQL（SaleOrderDispatch视图）

```sql
SELECT 
    m.cCusCode AS soCusCode, m.id AS soid, m.cSOCode, m.dDate AS soDate,
    s.cInvCode AS soInvCode, s.cInvName AS soInvName, sInv.cInvStd AS soInvStd,
    s.iQuantity AS soQuantity,
    d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate,
    ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
    ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier
FROM SO_SOMain AS m
LEFT JOIN SO_SODetails AS s ON m.id = s.id
LEFT JOIN DispatchLists AS ds ON ds.iSOsID = s.iSOsID
LEFT JOIN DispatchList AS d ON d.dlid = ds.dlid
LEFT JOIN Inventory AS sInv ON sInv.cInvCode = s.cInvCode
LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
LEFT JOIN Customer AS c ON c.cCusCode = m.cCusCode
WHERE 1 = 1
  AND 0 >= (SELECT LEN(MAX(ISNULL(cVerifier, ''))) 
            FROM DispatchList AS dm 
            LEFT JOIN DispatchLists AS ds ON dm.dlid = ds.dlid
            WHERE ds.iSOsID IN (SELECT iSOsID FROM SO_SODetails WHERE id = m.id))
```

### A.2 发货单查询SQL（SaleDispatch视图）

```sql
SELECT 
    d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate,
    ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
    ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier
FROM DispatchList AS d
LEFT JOIN DispatchLists AS ds ON d.dlid = ds.dlid
LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
LEFT JOIN Customer AS c ON c.cCusCode = d.cCusCode
LEFT JOIN CustomerClass AS cc ON cc.cCCCode = c.cCCCode
LEFT JOIN Person AS p ON p.cPersonCode = c.cCusPPerson
WHERE 1 = 1
  AND d.cVerifier IS NULL
  AND ds.iSOsID IS NULL  -- 仅独立发货单（非订单关联）
  AND cc.cCCName LIKE '%济南市%'
```

### A.3 更新SQL

```sql
-- 修改销售订单客户
UPDATE SO_SOMain SET cCusCode = '{newCode}', cCusName = '{newName}' WHERE id = {soid}

-- 修改发货单客户
UPDATE DispatchList SET cCusCode = '{newCode}', cCusName = '{newName}' WHERE dlid = {dlid}

-- 修改发货单送货日期（直接修改dDate标准字段）
UPDATE DispatchList SET dDate = '{dateTime}' WHERE cDLCode = '{dlcode}'
```

---

## 附录B：新系统推荐SQL（Dapper参数化）

```sql
-- 订单查询（参数化）
SELECT m.cCusCode AS soCusCode, m.id AS soid, m.cSOCode, m.dDate AS soDate,
       s.cInvCode AS soInvCode, s.cInvName AS soInvName, sInv.cInvStd AS soInvStd,
       s.iQuantity AS soQuantity,
       d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate,
       ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
       ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier,
       p.cPersonName
FROM SO_SOMain AS m
LEFT JOIN SO_SODetails AS s ON m.id = s.id
LEFT JOIN DispatchLists AS ds ON ds.iSOsID = s.iSOsID
LEFT JOIN DispatchList AS d ON d.dlid = ds.dlid
LEFT JOIN Inventory AS sInv ON sInv.cInvCode = s.cInvCode
LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
LEFT JOIN Customer AS c ON c.cCusCode = m.cCusCode
LEFT JOIN Person AS p ON p.cPersonCode = c.cCusPPerson
WHERE 1 = 1
  AND (@CusCode IS NULL OR m.cCusCode = @CusCode)
  AND (@CusName IS NULL OR c.cCusName LIKE @CusName)
  AND (@CusAbbName IS NULL OR m.cCusName LIKE @CusAbbName)
  AND (@CusContact IS NULL OR c.cCusPPerson = @CusContact)
  AND (@CusPPerson IS NULL OR p.cPersonName = @CusPPerson)
  AND (@CusPhone IS NULL OR c.cCusPhone LIKE @CusPhone OR c.cCusHand LIKE @CusPhone OR c.cCusFax LIKE @CusPhone)
  AND (@VouchCode IS NULL OR m.cSOCode LIKE @VouchCode)
  AND (@DateFrom IS NULL OR m.dDate >= @DateFrom)
  AND (@DateTo IS NULL OR m.dDate <= @DateTo)
  AND 0 >= (SELECT LEN(MAX(ISNULL(cVerifier, ''))) 
            FROM DispatchList AS dm 
            LEFT JOIN DispatchLists AS ds ON dm.dlid = ds.dlid
            WHERE ds.iSOsID IN (SELECT iSOsID FROM SO_SODetails WHERE id = m.id))

-- 更新订单客户（参数化）
UPDATE SO_SOMain SET cCusCode = @NewCusCode, cCusName = @NewCusName 
WHERE id = @Soid

-- 更新发货单客户（参数化）
UPDATE DispatchList SET cCusCode = @NewCusCode, cCusName = @NewCusName 
WHERE dlid = @Dlid
  AND (cVerifier IS NULL OR cVerifier = '')

-- 更新发货日期（参数化，单笔）
UPDATE DispatchList SET dDate = @NewDate 
WHERE dlid = @Dlid
  AND (cVerifier IS NULL OR cVerifier = '')

-- 批量查询未审核发货单（用于日期批量修改）
SELECT dlid, cDLCode, cCusCode, cCusName, dDate, dDate AS oldDate
FROM DispatchList 
WHERE (cVerifier IS NULL OR cVerifier = '')
  AND (@CusCode IS NULL OR cCusCode LIKE @CusCode)
  AND (@CusName IS NULL OR cCusName LIKE @CusName)
  AND (@DateFrom IS NULL OR dDate >= @DateFrom)
  AND (@DateTo IS NULL OR dDate <= @DateTo)
ORDER BY dDate DESC
```

---

> **审批**：待定