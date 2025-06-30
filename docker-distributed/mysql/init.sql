-- 初始化数据库和表结构
CREATE DATABASE IF NOT EXISTS urlshort CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE urlshort;

CREATE TABLE IF NOT EXISTS short_links (
    id BIGINT PRIMARY KEY,
    alias VARCHAR(255) UNIQUE,
    url TEXT NOT NULL,
    expire BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_alias (alias),
    INDEX idx_expire (expire),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 创建用户和授权
CREATE USER IF NOT EXISTS 'urlshort'@'%' IDENTIFIED BY 'urlshort123';
GRANT ALL PRIVILEGES ON urlshort.* TO 'urlshort'@'%';
FLUSH PRIVILEGES;

CREATE USER IF NOT EXISTS 'exporter'@'%' IDENTIFIED BY 'exporter123';
GRANT PROCESS, REPLICATION CLIENT, SELECT ON *.* TO 'exporter'@'%';
FLUSH PRIVILEGES;

-- 迁移脚本：移除 id 字段的 AUTO_INCREMENT 属性（如有）
-- 注意：这会删除AUTO_INCREMENT属性，但保留现有数据
-- 如果有数据，建议在低峰期执行，并做好备份
ALTER TABLE short_links MODIFY COLUMN id BIGINT NOT NULL;

-- 确保索引存在
CREATE INDEX IF NOT EXISTS idx_alias ON short_links(alias);
CREATE INDEX IF NOT EXISTS idx_expire ON short_links(expire);
CREATE INDEX IF NOT EXISTS idx_created_at ON short_links(created_at);

-- 显示表结构确认修改成功
