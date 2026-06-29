# EAMS2026 企业管理系统 — ERP 单据修改模块设计文档 v0.4.2026.0630

---

## 1. 版本信息

| 项目 | 内容 |
|------|------|
| 文档版本 | v0.4.2026.0630 |
| 发布日期 | 2026-06-30 |
| 变更说明 | 文档版本更新，模块功能无变动 |

---

## 2. 模块概述

ERP 单据修改模块用于对 U8 ERP 系统中的销售订单、发货单和发货日期进行修改操作。

### 2.1 功能范围

- 销售订单客户修改（单笔）
- 发货单客户修改（单笔）
- 发货日期修改（单笔 + 批量）
- 所有操作记录日志

### 2.2 操作限制

- 只能修改未审核（`isettlenull`）的单据
- 只能修改客户编码，不修改客户名称以外的其他信息
- 客户修改支持可选的"同步发货单"（同时修改关联的未审核发货单的客户）
- 日期修改支持"下月第一个周日"自动计算

### 2.3 菜单路径

| 功能 | 前端路径 | 权限标识 |
|------|---------|---------|
| 订单客户修改 | `/erp/vouch-order` | `erp-vouchmodify:order` |
| 发货单客户修改 | `/erp/vouch-dispatch` | `erp-vouchmodify:dispatch` |
| 发货日期修改 | `/erp/vouch-date` | `erp-vouchmodify:date` |
| 修改日志 | `/erp/vouch-log` | `erp-vouchmodify:log` |

---

## 3. 数据库设计

### 3.1 ERP 数据源配置表（`erp_settings_datasource`）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | BIGSERIAL | 主键 |
| name | VARCHAR(100) | 标识名称 |
| display_name | VARCHAR(200) | 显示名称 |
| db_type | VARCHAR(50) | 数据库类型（SqlServer / Odbc） |
| server | VARCHAR(200) | 服务器地址 |
| port | INTEGER | 端口 |
| database_name | VARCHAR(100) | 数据库名（UFDATA_{账套}_{年份} 可通过字段替换） |
| username | VARCHAR(100) | 用户名 |
| password | TEXT | 密码（AES 加密存储） |
| connection_string | TEXT | 完整连接字符串 |
| extra_params | TEXT | 额外参数（如账套号、年份用于数据库名替换） |
| is_shared | BOOLEAN | 是否共享给所有用户 |
| sort_order | INTEGER | 排序 |
| is_deleted | BOOLEAN | 软删除标志 |
| created_at / updated_at | TIMESTAMP | 审计字段 |

### 3.2 业务员映射表（`erp_settings_salesperson_map`）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | BIGSERIAL | 主键 |
| employee_id | BIGINT | 系统员工 ID |
| salesperson_code | VARCHAR(50) | ERP 业务员编码 |
| is_deleted | BOOLEAN | 软删除标志 |
| created_at / updated_at | TIMESTAMP | 审计字段 |

### 3.3 操作日志表（`vouch_modify_logs`）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | BIGSERIAL | 主键 |
| user_id | BIGINT | 操作人 |
| vouch_type | VARCHAR(50) | 单据类型（order/dispatch/date） |
| vouch_code | VARCHAR(100) | 单据编号 |
| operation_type | VARCHAR(50) | 操作类型（customer_change/date_change） |
| old_value | TEXT | 修改前的客户编码或日期 |
| new_value | TEXT | 修改后的客户编码或日期 |
| cuscode | VARCHAR(50) | 客户编码（冗余字段，方便查询） |
| is_sync_dispatch | BOOLEAN | 是否同步修改发货单（仅客户修改） |
| created_at | TIMESTAMP | 操作时间 |

---

## 4. API 设计

### 4.1 ERP 数据源配置（`/api/erp/settings/datasource`）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/erp/settings/datasource` | 获取数据源列表 |
| GET | `/api/erp/settings/datasource/{id}` | 获取数据源详情 |
| POST | `/api/erp/settings/datasource` | 新增数据源 |
| PUT | `/api/erp/settings/datasource/{id}` | 更新数据源 |
| DELETE | `/api/erp/settings/datasource/{id}` | 删除数据源 |
| POST | `/api/erp/settings/datasource/test` | 测试数据源连接 |

### 4.2 业务员映射（`/api/erp/settings/salesperson`）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/erp/settings/salesperson` | 获取映射列表 |
| POST | `/api/erp/settings/salesperson` | 新增映射 |
| PUT | `/api/erp/settings/salesperson/{id}` | 更新映射 |
| DELETE | `/api/erp/settings/salesperson/{id}` | 删除映射 |

### 4.3 单据修改操作（`/api/erp/vouch-modify`）

#### /api/erp/vouch-modify/order

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/erp/vouch-modify/order/query` | 查询销售订单 |
| POST | `/api/erp/vouch-modify/order/modify` | 修改销售订单客户 |
| POST | `/api/erp/vouch-modify/order/validate` | 验证客户编码是否存在 |

#### /api/erp/vouch-modify/dispatch

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/erp/vouch-modify/dispatch/query` | 查询发货单 |
| POST | `/api/erp/vouch-modify/dispatch/modify` | 修改发货单客户 |

#### /api/erp/vouch-modify/date

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/erp/vouch-modify/date/query` | 查询未审核发货单 |
| POST | `/api/erp/vouch-modify/date/batch-modify` | 批量修改发货日期 |

#### /api/erp/vouch-modify/log

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/erp/vouch-modify/log` | 分页查询操作日志 |

---

## 5. 业务流程

### 5.1 数据源配置

1. 管理员进入 `ERP设置 → 数据源配置`
2. 新增 ERP 数据源，填写连接信息
3. 点击"测试连接"验证
4. 设置共享状态（所有用户可用 / 仅管理员可见）

### 5.2 业务员映射

1. 管理员进入 `ERP设置 → 业务员对照`
2. 选择系统员工，输入 ERP 业务员编码
3. 保存映射关系

### 5.3 订单客户修改

1. 进入 `ERP辅助 → 单据修改 → 订单客户修改`
2. 选择 ERP 数据源
3. 输入查询条件（客户编码、起始日期、结束日期）
4. 点击"查询"按钮
5. 结果列表中显示符合条件且未审核的销售订单
6. 点击目标订单的"修改客户"按钮
7. 在弹出的对话框中：
   a. 输入新的客户编码
   b. 点击"验证"按钮（后端查询 U8 Customer 表确认客户存在）
   c. 验证通过后，可选择"同步发货单"（同时修改关联发货单的客户编码）
   d. 点击"确认修改"
8. 修改成功/失败弹窗提示

### 5.4 发货单客户修改

1. 进入 `ERP辅助 → 单据修改 → 发货单客户修改`
2. 选择 ERP 数据源
3. 输入查询条件（客户编码、起始日期、结束日期）
4. 点击查询按钮
5. 结果列表中显示符合条件且未审核的发货单
6. 点击目标发货单的"修改客户"按钮
7. 输入新的客户编码 → 验证 → 确认修改

### 5.5 发货日期修改

1. 进入 `ERP辅助 → 单据修改 → 发货日期修改`
2. 选择 ERP 数据源
3. 输入查询条件（起始日期、结束日期）
4. 点击查询按钮
5. 结果列表中显示未审核的发货单（可多选）
6. 支持以下方式设置目标日期：
   - 系统自动计算"下月第一个周日"作为默认日期
   - 手动选择具体日期
7. 勾选需要修改的发货单
8. 点击"批量修改日期"按钮
9. 确认对话框 → 执行修改
10. 结果弹窗显示成功/失败统计，失败项可点击查看原因

---

## 6. 数据安全

- 所有连接字符串在存储时使用 AES 加密
- 操作日志记录完整的修改前后对比，便于审计
- 只有授权的用户才能执行修改操作
- 修改操作通过事务保证数据一致性
- `UpdateCustomerRequest` 和 `BatchUpdateDateRequest` 等 DTO 已添加 `[Required]` 验证注解，确保请求参数完整

---

## 7. 技术说明

### 7.1 ERP 连接

- 从 `erp_settings_datasource` 表中获取数据源配置
- 连接字符串支持 SQL Server 和 ODBC 两种模式
- 数据库名支持 `{账套号}` 和 `{年份}` 占位符替换
- 数据源密码使用 AES 加密存储，运行时动态解密

### 7.2 修改 SQL 示例

```sql
-- 修改销售订单客户编码
UPDATE SO_SOMain
SET cCusCode = @newCusCode
WHERE cCode = @orderCode AND isettlenull = 0

-- 修改发货单客户编码
UPDATE DispatchList
SET cCusCode = @newCusCode
WHERE cDLCode = @dispatchCode AND isettlenull = 0

-- 修改发货日期
UPDATE DispatchList
SET dDate = @newDate
WHERE cDLCode IN (@codes) AND isettlenull = 0
```

### 7.3 客户验证 SQL

```sql
-- 验证客户编码是否存在
SELECT 1 FROM Customer WHERE cCustCode = @cusCode
```

### 7.4 错误处理

- 连接 ERP 失败 → "无法连接 ERP 数据源，请检查数据源配置"
- 客户编码不存在 → "客户编码不存在，请重新输入"
- 单据已审核 → "单据已审核，无法修改"
- 事务超时 → 回滚所有修改，提示"操作超时，请重试"
