-- 迁移脚本：移除 id 字段的 AUTO_INCREMENT 属性
-- 这个脚本用于修改现有的 short_links 表，使其与雪花算法兼容

USE urlshort;

-- 检查表是否存在
SELECT COUNT(*) FROM information_schema.tables 
WHERE table_schema = 'urlshort' AND table_name = 'short_links';

-- 如果表存在，修改表结构
-- 注意：这会删除AUTO_INCREMENT属性，但保留现有数据
-- 如果有数据，建议在低峰期执行，并做好备份

-- 第一步：修改列定义，移除AUTO_INCREMENT
ALTER TABLE short_links MODIFY COLUMN id BIGINT NOT NULL;

-- 第二步：确保索引正确
-- （这些索引应该已经存在，但确保一下）
CREATE INDEX IF NOT EXISTS idx_alias ON short_links(alias);
CREATE INDEX IF NOT EXISTS idx_expire ON short_links(expire);
CREATE INDEX IF NOT EXISTS idx_created_at ON short_links(created_at);

-- 显示表结构确认修改成功
DESCRIBE short_links;
