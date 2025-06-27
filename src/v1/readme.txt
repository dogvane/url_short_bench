这是一个短链接项目，就2个接口
1. 创建一个短链接，有3个参数，url, expire
   - url: 不限制
   - expire: 可以不设置，不设置默认为永久。如果设置，为 int ,单位为秒。

   格式为 http://localhost:5214/create
   post {"url": "http://www.baidu.com", "expire": 3600}
2. 获取短链接，只有一个参数，alias
    请求格式为 http://localhost:5214/u/{alias}
    如果获得对应的长连接，则使用该url重定向到对应的长链接
    如果为获得，则返回404