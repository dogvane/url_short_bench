-- MySQL 优化脚本 for URL Shortener
-- 优化 short_links 表的性能

-- 1. 确保表使用最优的引擎和字符集
ALTER TABLE short_links 
ENGINE=InnoDB 
DEFAULT CHARSET=utf8mb4 
COLLATE=utf8mb4_unicode_ci 
ROW_FORMAT=COMPRESSED;

-- 2. 优化索引
-- 删除可能存在的旧索引
DROP INDEX IF EXISTS idx_alias ON short_links;
DROP INDEX IF EXISTS idx_expire ON short_links;
DROP INDEX IF EXISTS idx_created_at ON short_links;

-- 创建优化的索引
CREATE UNIQUE INDEX idx_alias_optimized ON short_links (alias) USING BTREE;
CREATE INDEX idx_expire_active ON short_links (expire) WHERE expire > 0;
CREATE INDEX idx_created_at_desc ON short_links (created_at DESC);

-- 3. 分析表以优化查询计划
ANALYZE TABLE short_links;

-- 4. 优化表结构
OPTIMIZE TABLE short_links;

-- 5. 显示表状态
SHOW TABLE STATUS LIKE 'short_links';

-- 6. 显示索引信息
SHOW INDEX FROM short_links;

-- 7. 设置一些会话级别的优化
SET SESSION innodb_lock_wait_timeout = 30;
SET SESSION max_execution_time = 30000;

-- 8. 显示当前连接数
SHOW STATUS LIKE 'Threads_connected';
SHOW STATUS LIKE 'Max_used_connections';

-- 9. 显示InnoDB状态
SHOW ENGINE INNODB STATUS\G
