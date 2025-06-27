这是一个短链接项目，就2个接口
1. 创建一个短链接，有3个参数，url, expire
   - url: 不限制
   - expire: 可以不设置，不设置默认为永久。如果设置，为 int ,单位为秒。

   格式为 http://localhost:5214/create
   post {"url": "http://www.baidu.com", "expire": 3600}
2. 获取短链接，只有一个参数，alias
    请求格式为 http://localhost:5214/u/{alias}
    如果获得对应的长连接，则使用该url重定向到对应的长链接
      alias 的别名，可以通过一个base62编码算法，逆向获得一个id
      这个id，如果在当前内存字典里不存在，则去数据库里查询
      如果在数据库里也不存在，则返回404
    

3. 短链需要保存在要给数据库里
   数据库可配置，默认使用sqlite，可配置 mysql, postgresql 
   数据库表结构为
   ```
   CREATE TABLE IF NOT EXISTS `short_links` (
       `id` INTEGER PRIMARY KEY AUTOINCREMENT,
       `alias` TEXT NOT NULL UNIQUE,
       `url` TEXT NOT NULL,
       `expire` INTEGER DEFAULT 0,
       `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP
   );
   ```
4. 别名采用一个特殊的算法生成，映射的方式是一个id对应一个6位的ascii字符串
   算法如下：
   使用数据库插入后返回的id，使用一个base62编码算法将id转换为6位的字符串
   base62编码的字符集为：0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
