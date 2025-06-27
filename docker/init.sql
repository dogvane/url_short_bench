-- 初始化数据库脚本
USE urlshort;

-- 创建短链表
CREATE TABLE IF NOT EXISTS short_links (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    alias VARCHAR(255) UNIQUE,
    url TEXT NOT NULL,
    expire BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_alias (alias)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 插入测试数据
INSERT INTO short_links (alias, url, expire) VALUES 
('test1', 'https://www.example.com', 0),
('test2', 'https://www.google.com', 0);

SHOW TABLES;
