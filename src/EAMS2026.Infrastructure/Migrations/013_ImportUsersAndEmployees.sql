-- ============================================================
-- 013_ImportUsersAndEmployees.sql
-- 从老系统EMData2导入用户管理和员工管理数据
-- 密码统一初始化为123，用户首次登录后可修改
-- ============================================================

-- 006 徐艳春
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (4, '006', '徐艳春', NULL, '13306409150', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (5, '006', '$2a$11$sw5EkkWlpCCo0CBmEKVP2OHGB1W/j7TmJWtRmNDaTrYyy7ZVYm5NK', 4, TRUE, TRUE, 1, 1);

-- 016 苏月军
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (5, '016', '苏月军', NULL, '13306406691', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (6, '016', '$2a$11$iTaFQ8QdGmL/R0E4/HT3..o3ko1Po4K3vj9/8bxIFkqkBWqtDdNM2', 5, TRUE, TRUE, 1, 1);

-- 017 李隆勋
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (6, '017', '李隆勋', NULL, '13306406681', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (7, '017', '$2a$11$qyGQ3yhut9so3kqen27oUu5.veRCue6yAnzZiHzC6DGQ2OcbRnL2K', 6, TRUE, TRUE, 1, 1);

-- 020 董富海
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (7, '020', '董富海', NULL, '13306406690', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (8, '020', '$2a$11$XON7IGpOmhV0Ews5y3KUu.Vr8fN/hGP.odhqvNnK1k9gk0GQjiU22', 7, TRUE, TRUE, 1, 1);

-- 022 徐飞
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (8, '022', '徐飞', NULL, '13306409130', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (9, '022', '$2a$11$M7Z0i6Ckd6JnF7hXoo7R7uqj4rspi0HHE1PP6tA6TyzM/Q2x6MQ5.', 8, TRUE, TRUE, 1, 1);

-- 034 韩钦梅
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (9, '034', '韩钦梅', NULL, '13306408309', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (10, '034', '$2a$11$Ss23uhg6weWOk0vHf0OJT.dtGigC5dg4M5biFG1iVbySGz5gZbg9y', 9, TRUE, TRUE, 1, 1);

-- 043 陈庆云
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (10, '043', '陈庆云', NULL, '18053143837', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (11, '043', '$2a$11$kmekSZMZqu4E9aFFklxsROTf4Xcjh63K2xYjzz1gvgnonP0un7RV2', 10, TRUE, TRUE, 1, 1);

-- 046 董业武
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (11, '046', '董业武', NULL, '13306406683', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (12, '046', '$2a$11$mAMYeNbFm9N7kdUUj.ZiIeeU53Butbqa3LtP0Q9S4RHPm6UjeT4w6', 11, TRUE, TRUE, 1, 1);

-- 050 范哲
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (12, '050', '范哲', NULL, '13306406682', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (13, '050', '$2a$11$AIQSZHDI56HTQ8qgZTA55uqlFuGxuCo60zitSI.26f8uQ38LXx9Ru', 12, TRUE, TRUE, 1, 1);

-- 060 隋艳丽
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (13, '060', '隋艳丽', NULL, '18615682604', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (14, '060', '$2a$11$0HOQgNmhcKRQUb7KeGlHs.LZyBica7e5jBbjilGERjbLvgsx3FOqm', 13, TRUE, TRUE, 1, 1);

-- 062 郭瑞婷
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (14, '062', '郭瑞婷', NULL, '13396412900', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (15, '062', '$2a$11$6UV9HWDDlNjb/avdXsWvZeTUcJJFygyohOQ4DCGN1k8DMR5toOTSu', 14, TRUE, TRUE, 1, 1);

-- 063 刘见
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (15, '063', '刘见', NULL, '13306406651', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (16, '063', '$2a$11$Z4p9KWFUTRTxEVi9q43ZBuCcWvoMSAo0GII9STZOIfDibb42xCMSK', 15, TRUE, TRUE, 1, 1);

-- 077 李健
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (16, '077', '李健', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (17, '077', '$2a$11$MrfBzRLz7OxYq6XX2r/aYeq6b.IAiM8MAsfF.5GcjvMeMu5qTIC3y', 16, TRUE, TRUE, 1, 1);

-- 080 孟颖
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (17, '080', '孟颖', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (18, '080', '$2a$11$lUQiphmgwc2V5kon9tKllOf1mcle6zqnvb.qOwscrRPueaxnginPG', 17, TRUE, TRUE, 1, 1);

-- 081 王珊
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (18, '081', '王珊', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (19, '081', '$2a$11$bQVzcoJhTMIIb9vEGznNkO9o/ZXdEorjTh9WI5HcRAhOOQplISTMy', 18, TRUE, TRUE, 1, 1);

-- 084 郑娜
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (19, '084', '郑娜', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (20, '084', '$2a$11$Jvkub5yuP642ol7OduIGHuC0LP2J76lWOehgLiHZgTqYAHyg2gUyi', 19, TRUE, TRUE, 1, 1);

-- 085 陈旭
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (20, '085', '陈旭', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (21, '085', '$2a$11$brML/mzxQSmocDu6.o7thuXJOa9pLa68.8yLdWbc7BZW7QctMxx/m', 20, TRUE, TRUE, 1, 1);

-- 086 王菲
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (21, '086', '王菲', NULL, '15054170522', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (22, '086', '$2a$11$LwnNquUo2cqIZImX3zrOLuMmUU.b/J8P.ksm3pSBdezRBmCsKrqqK', 21, TRUE, TRUE, 1, 1);

-- 087 程双
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (22, '087', '程双', NULL, '15053143010', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (23, '087', '$2a$11$dKHkoyxV9CwaAI7y6Nh9ruO6T7jkg2t8oVhWuWsw8sTYX/Uuk9rgC', 22, TRUE, TRUE, 1, 1);

-- 089 张晶
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (23, '089', '张晶', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (24, '089', '$2a$11$ZQZ8e.UcP7lPcdDYvK4TKOKKPW..jSwHh5v.zLPtRymlu4pOrwJwy', 23, TRUE, TRUE, 1, 1);

-- 092 赵梦娇
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (24, '092', '赵梦娇', '男', NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (25, '092', '$2a$11$djeUf5A195gaMliUikaEj.faoxsNEuCZBMgIlIkQVxCzJJeC9hQz2', 24, TRUE, TRUE, 1, 1);

-- 093 邵明慧
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (25, '093', '邵明慧', NULL, '13869500521', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (26, '093', '$2a$11$WZVDd.LlPmj1fyKwNkVg/.USl7uZ/yl75rpXxGUfBGdgGxEaBoLSu', 25, TRUE, TRUE, 1, 1);

-- 094 杨杨
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (26, '094', '杨杨', NULL, '18558661558', NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (27, '094', '$2a$11$R1K4SEoeqQPfcmeOfJWLWe8Ep0h6P/DUPGRTxUap6r3lLQQ/M2Vdi', 26, TRUE, TRUE, 1, 1);

-- 095 曹颖
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (27, '095', '曹颖', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (28, '095', '$2a$11$kY3oqw4G.2v7ezn0CjUoz.77IXUCBhtR4MfZVF8CTfkPra/r4ZTse', 27, TRUE, TRUE, 1, 1);

-- 096 宋雪彤
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (28, '096', '宋雪彤', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (29, '096', '$2a$11$LQ8HuCSnEGHikUru0q5iSu3eDv3pPI2aprmHpiy.S2.TRXeckT2vm', 28, TRUE, TRUE, 1, 1);

-- wc 魏超
INSERT INTO employees (id, employee_no, name, gender, phone, email, status, created_by, updated_by)
VALUES (29, 'wc', '魏超', NULL, NULL, NULL, TRUE, 1, 1);

INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
VALUES (30, 'wc', '$2a$11$1aJBq16eo6JOsr5lo2KOB.XtVq8SzyY/fEIFra0BahMLQ5thgbl6S', 29, TRUE, TRUE, 1, 1);

-- ============================================================
-- 修复：如果数据库已有用户ID 4（如用户'002'），
-- 则需要为员工006徐艳春单独创建用户账号
-- INSERT INTO users (id, username, password_hash, employee_id, status, force_change_password, created_by, updated_by)
-- VALUES (30, '006', '$2a$11$NUpkevi.AyDRsnHwuHWxv.nzAWFL6qCmM7PO5x.DZ0yjXWcgODWh.', 4, TRUE, TRUE, 1, 1);
-- ============================================================

