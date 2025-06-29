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
