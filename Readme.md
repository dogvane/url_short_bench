这是一个为了url短链性能测试建立的项目。

目前在src下有三个项目分类: v1 v2 v3

v1 是纯内存结构，主要作为接口本身的基准测试时使用的
v2 是基于本地数据库做的单机版本基准性能测试用。
v3 是基于分布式数据库做的集群版本基准性能测试用。

v1、v2、v3的接口基本一致。

bench_test 是放置基准测试的代码。性能测试使用 locust 作为测试工具。

当前版本下，所有代码为ai辅助生成，未做个人理解整理，仅通过提示词让vscode写，有错误，也是将错误发给ai负责修改。
代码仅保证基本可完成测试用例测试。
当前项目仅限于观察AI的代码生成能力。


性能测试启动命令：
dotnet run --configuration Release --gcServer