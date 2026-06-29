# EAMS_4.6 三模块迁移分析报告

> 编制时间：2026-06-05
> 分析范围：报表模块、单据修改模块、Strategy模块

---

## 目录
1. [报表模块 (report)](#1-报表模块-report)
2. [单据修改模块 (ModifyCustomer)](#2-单据修改模块-modifycustomer)
3. [Strategy模块 (strategyLib)](#3-strategy模块-strategylib)
4. [迁移优先级与建议](#4-迁移优先级与建议)

---

## 1. 报表模块 (report)

### 1.1 功能定位

| 维度 | 说明 |
|------|------|
| **模块全称** | report（报表管理系统） |
| **业务价值** | 自定义报表设计、数据聚合展示、字段级权限控制 |
| **技术定位** | 报表引擎核心，支持多数据源、自定义查询、透视分析 |

### 1.2 核心实体结构

**实体关系图**：
```
Report (报表)
├── Fields (字段列表)
├── FilterFields (过滤条件)
├── OrderFields (排序字段)
├── PivotViews (透视视图)
└── Permissions (权限配置)
```

**实体详细定义**：

| 实体 | 核心字段 | 说明 |
|------|---------|------|
| **Report** | reportID, Name, Title, QueryBase, GroupCmd, OrderCmd | 报表主表，存储基础SQL查询 |
| **Field** | fieldID, reportID, fieldName, fieldTitle, isDisplay | 字段定义，控制显示/隐藏 |
| **FilterField** | filterID, reportID, fieldName, operator, defaultValue | 过滤条件配置 |
| **OrderField** | orderID, reportID, fieldName, sortOrder | 排序字段配置 |
| **PivotView** | autoid, reportID, rowField, colField, dataField | 透视表配置 |
| **Permission** | AutoID, reportID, RoleID/UserID, AccessType | 字段级权限 |

### 1.3 核心业务逻辑

**报表数据获取流程**（`BLL.cs:176-197`）：
```csharp
// 1. 拼接查询命令
string cmd = QueryBase + FilterCmd + OtherQueryCmd + GroupCmd + OrderCmd;

// 2. 执行查询获取数据源
reportDSDA.setQueryCommand(cmd);
reportDSDA.getDataSource(dateField, dyear, personField, personName, OrderString);

// 3. 根据字段配置过滤显示列
foreach (Field f in fields) {
    if (r.Columns.Contains(f.fieldName) && f.isDisplay) {
        r.Columns[f.fieldName].Caption = f.fieldTitle;
    }
}
```

**权限控制机制**：
- 支持**角色级**和**用户级**权限（UserID=-999表示公共配置）
- 通过 `v_ReportPermission` 视图实现权限过滤
- 字段级显示控制（`isDisplay` 属性）

### 1.4 数据库依赖

| 依赖类型 | 说明 |
|---------|------|
| **主表** | `Report`, `ReportField`, `ReportFilter`, `ReportOrder`, `ReportPivotView`, `ReportPermission` |
| **视图** | `v_ReportPermission`（权限关联视图） |
| **数据源** | 支持任意业务表（通过动态SQL） |

### 1.5 迁移评估

| 维度 | 评估 |
|------|------|
| **复杂度** | ⭐⭐⭐（中等） |
| **工作量** | 中等，需实现报表设计器前端 |
| **风险点** | 动态SQL注入风险、性能优化 |
| **复用价值** | ⭐⭐⭐⭐（高） |

**迁移建议**：
- 后端：按 `eams-module-designer` 规范，创建 `rpt_` 前缀表
- 前端：需开发可视化报表设计器
- 安全：动态SQL需参数化处理

---

## 2. 单据修改模块 (ModifyCustomer)

### 2.1 功能定位

| 维度 | 说明 |
|------|------|
| **模块全称** | ModifyCustomer（单据客户信息修改） |
| **业务价值** | 批量修正销售订单/发货单中的客户信息错误 |
| **技术定位** | U8 ERP 数据修正工具，支持订单与发货单关联查询 |

### 2.2 核心实体结构

**实体关系图**：
```
Order (订单)
├── OrderMain (订单主表)
├── OrderDetail (订单明细)
└── Dispatch (发货单)
    ├── DispatchMain (发货单主表)
    └── DispatchDetail (发货单明细)
```

**核心实体字段**：

| 实体 | 关键字段 | 来源表 |
|------|---------|--------|
| **OrderMain** | soid, cSOCode, soDate, soCusCode, soCusName | SO_SOMain |
| **OrderDetail** | soInvCode, soInvName, soInvStd, soQuantity | SO_SODetails |
| **DispatchMain** | dlid, cdlcode, dlDate, dlCusCode, dlCusName | DispatchList |
| **DispatchDetail** | dlInvCode, dlInvName, dlInvStd, dlQuantity | DispatchLists |
| **SaleOrderDispatch** | 联合视图（订单+发货单） | 多表联合 |

### 2.3 核心业务逻辑

**查询逻辑**（`BLL.cs:29-60`）：
```csharp
// 支持多条件模糊查询
FromSection<SaleOrderDispatch> section = session
    .From<SaleOrderDispatch>()
    .LeftJoin<Customer>((s, c) => s.soCusCode == c.cCusCode);

// 查询条件：客户地址、编码、名称、联系人、电话、手机、单据号、日期范围
where.And(SaleOrderDispatch._.cCusOAddress.Like(queryParam.cusAddr));
where.And(SaleOrderDispatch._.soCusCode == queryParam.cusCode);
// ... 更多条件
```

**客户信息修改逻辑**（`BLL.cs:142-159`）：
```csharp
public int updateCustomer(updVouchCus updCus) {
    if (updCus.soid.HasValue)
        return updateSOCustomer(updCus.soid.Value, updCus.cusCode, updCus.cusName);
    else
        return updateDLCustomer(updCus.dlid.Value, updCus.cusCode, updCus.cusName);
}
```

**数据转换逻辑**（`BLL.cs:62-120`）：
- 将 `SaleOrderDispatch`（扁平化视图）转换为层级结构 `Order`
- 支持一对多关系（一个订单对应多个发货单）

### 2.4 数据库依赖

| 依赖类型 | 表名 | 说明 |
|---------|------|------|
| **主业务表** | `SO_SOMain`, `SO_SODetails` | 销售订单 |
| **发货单表** | `DispatchList`, `DispatchLists` | 发货单 |
| **客户表** | `Customer` | 客户主数据 |
| **存货表** | `Inventory` | 存货主数据 |

### 2.5 迁移评估

| 维度 | 评估 |
|------|------|
| **复杂度** | ⭐⭐（低） |
| **工作量** | 小，逻辑相对简单 |
| **风险点** | U8 ERP 表结构依赖 |
| **复用价值** | ⭐⭐（中） |

**迁移建议**：
- 若新项目不对接 U8，需评估是否保留此功能
- 若保留，需确认新项目的订单/发货单表结构

---

## 3. Strategy模块 (strategyLib)

### 3.1 功能定位

| 维度 | 说明 |
|------|------|
| **模块全称** | strategyLib（价格策略验证库） |
| **业务价值** | 销售价格合规性检查，确保售价不低于策略规定的底价 |
| **技术定位** | 策略引擎，支持多级折扣、有效期控制、客户/地区分级 |

### 3.2 核心实体结构

**实体关系图**：
```
Main (策略主表)
└── Detail (策略明细)
    ├── cinvACode/APrice/AQuantity (条件物料)
    └── cinvBCode/BPrice/BQuantity (关联物料)
```

**核心实体字段**：

| 实体 | 关键字段 | 说明 |
|------|---------|------|
| **Main** | ID, cLevel, cVouchType, cDWCode, cDCName, dEffDate, dExpDate | 策略级别、单据类型、客户、地区、有效期 |
| **Detail** | autoid, ID, cinvACode, invAPrice, invAQuantity, cinvBCode, invBPrice, invBQuantity | 物料A（条件）、物料B（关联） |

**策略匹配优先级**（`strategyProcedure.sql:54-55`）：
```sql
order by cLevel desc, cVouchCode desc, cDWCode desc, cDCName desc, deffdate, dexpdate desc
```

### 3.3 核心业务逻辑

**价格验证流程**（存储过程 `saleFHD_validStrategy`）：

```
┌─────────────────────────────────────────────────────────────┐
│ 1. 获取单据信息 (dlid → vouchCode, cusCode, dDate)         │
│ 2. 获取客户地区 (cusCode → dcName)                          │
│ 3. 查询匹配策略 (vw_strategy 视图)                          │
│    └─ 条件: cinvACode=@invcode, cVouchType, cDWCode, cDCName │
│    └─ 有效期: deffdate <= @dDate <= dexpdate                │
│    └─ 排序: cLevel desc, cVouchCode desc...                 │
│ 4. 价格验证逻辑                                             │
│    ├─ IF 售价 >= APrice AND 数量 >= AQuantity → 合规        │
│    ├─ ELSE → 不合规                                         │
│    └─ 关联物料验证 (B物料比例检查)                          │
│ 5. 记录验证日志 (strategyLogs)                              │
└─────────────────────────────────────────────────────────────┘
```

**验证规则**（`strategyProcedure.sql:101-128`）：
```sql
-- 基础验证：售价 >= 策略底价 AND 数量 >= 最小起订量
if (@iTaxUnitPrice >= @Aprice AND @iquantity >= @AQuantity)
    BEGIN
        set @rvalid = '合规'
        -- 关联物料验证（可选）
        if (@Bcode is not null)
            BEGIN
                -- 检查B物料比例和价格
                if ((@iquantity / @AQuantity) = (@erpBQuantity / @BQuantity) 
                    AND (@Bprice <= @erpBPrice))
                    set @rvalid = '合规'
                ELSE
                    set @rvalid = '不合规!配套物料不符'
            END
    END
ELSE
    set @rvalid = '不合规!价格/数量不满足'
```

### 3.4 数据库依赖

| 依赖类型 | 表名 | 说明 |
|---------|------|------|
| **策略主表** | `strategyMain` | 策略头部信息 |
| **策略明细** | `strategyDetail` | 物料价格策略 |
| **视图** | `vw_strategy` | 策略联合视图 |
| **日志表** | `strategyLogs` | 验证日志 |
| **单据表** | `DispatchList`, `DispatchLists`, `SO_SOMain`, `SO_SODetails` | 待验证单据 |
| **基础表** | `Customer`, `DistrictClass`, `Inventory` | 客户、地区、存货 |

### 3.5 存储过程说明

| 存储过程 | 用途 | 调用时机 |
|---------|------|---------|
| `saleFHD_validStrategy` | 发货单价格验证 | 发货单保存前 |
| `saleOrder_validStrategy` | 销售订单价格验证 | 订单保存前 |

### 3.6 迁移评估

| 维度 | 评估 |
|------|------|
| **复杂度** | ⭐⭐⭐⭐（高） |
| **工作量** | 大，需将存储过程逻辑转为C# |
| **风险点** | 复杂业务规则、触发器依赖 |
| **复用价值** | ⭐⭐⭐⭐⭐（极高） |

**迁移建议**：
- 将存储过程逻辑迁移至 C# Service 层（按 `eams-module-designer` 规范）
- 创建 `StrategyValidationService` 封装验证逻辑
- 考虑使用策略模式重构规则引擎
- 表名添加 `stg_` 前缀

---

## 4. 迁移优先级与建议

### 4.1 优先级排序

| 优先级 | 模块 | 理由 |
|--------|------|------|
| **P0** | Strategy | 核心业务规则，销售价格合规性关键 |
| **P1** | 报表 | 业务价值高，已有基础架构 |
| **P2** | 单据修改 | 辅助工具，非核心流程 |

### 4.2 迁移路线图

```
Phase 1 (1-2周)
├── Strategy模块设计与实现
│   ├── 实体设计 (stg_main, stg_detail)
│   ├── Service层实现验证逻辑
│   └── API接口开发
└── 数据库表结构创建

Phase 2 (2-3周)
├── 报表模块设计与实现
│   ├── 实体设计 (rpt_report, rpt_field, rpt_filter...)
│   ├── Service层实现
│   └── 前端报表设计器开发
└── 权限集成

Phase 3 (1周)
└── 单据修改模块实现
    ├── 实体设计 (ord_order, ord_dispatch)
    └── Service层实现
```

### 4.3 技术选型建议

| 模块 | 后端 | 前端 | 数据库 |
|------|------|------|--------|
| Strategy | .NET 10 + SugarSQL | Vue 3 + Element Plus | SQL Server |
| 报表 | .NET 10 + Dapper | Vue 3 + 自定义报表设计器 | SQL Server |
| 单据修改 | .NET 10 + SugarSQL | Vue 3 + Element Plus | SQL Server |

### 4.4 注意事项

1. **U8 ERP 依赖**：若新项目不对接U8，单据修改模块需调整数据源
2. **触发器处理**：`strategyTrigger.sql` 中的触发器需评估是否保留
3. **日志迁移**：`strategyLogs` 日志表建议保留用于审计
4. **性能优化**：动态SQL和复杂查询需添加索引

---

## 附录：文件清单

### 报表模块
- `report/BLL.cs` - 业务逻辑层
- `report/Model.cs` - 实体定义
- `report/DataAccess.cs` - 数据访问层
- `report/reportDataAccess.cs` - 报表数据访问
- `report/reportDataSourceAccess.cs` - 数据源访问

### 单据修改模块
- `Sale/ModifyCustomer/BLL.cs` - 业务逻辑层
- `Sale/ModifyCustomer/Model/Customer.cs` - 客户实体
- `Sale/ModifyCustomer/Model/Order.cs` - 订单实体
- `Sale/ModifyCustomer/Model/SaleOrderDispatch.cs` - 联合实体
- `Sale/ModifyCustomer/Model/updVouchCus.cs` - 更新参数

### Strategy模块
- `strategyLib/strategyModel.cs` - 实体定义
- `strategyLib/strategyDAL.cs` - 数据访问层
- `strategyLib/strategyProcedure.sql` - 存储过程
- `strategyLib/strategyTrigger.sql` - 触发器
- `strategyLib/ModelStrategy.edmx` - EF模型


迁移顺序：
报表
单据修改
Strategy 价格政策