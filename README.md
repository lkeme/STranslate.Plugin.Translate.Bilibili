# STranslate.Plugin.Translate.Bilibili

基于哔哩哔哩开源多语种翻译大模型 [**Index-Translate**](https://github.com/bilibili/Index-Translate) 构建的 [STranslate](https://github.com/STranslate/STranslate) 文本翻译插件。

## 配置项说明

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| **模型 (Model)** | `Index-Translate-35B-A3B` | 可选预置模型或手动输入自定义模型名称 |
| **翻译模板** | `翻译` | 选择模板，点击“管理”打开无角色模板编辑器；默认模板不可删除，其术语表可作为其他模板按需继承的通用术语库；模板自身术语会覆盖同名通用术语 |
| **采样温度 (Temperature)** | `0.0` | 官方推荐 `0.0`（贪婪解码）以获得最稳定的翻译结果 |

## 开发与打包

### 1. 主仓库联调

按照 [STranslate 社区插件开发调试指南](https://github.com/STranslate/STranslate/blob/main/src/docs/community-plugin-development.md)：

1. 将本仓库克隆或放置于 `<STranslate源码根目录>\src\Plugins\ThirdPlugins\STranslate.Plugin.Translate.Bilibili`。
2. 在 `STranslate.slnx` 中添加现有项目 `STranslate.Plugin.Translate.Bilibili\STranslate.Plugin.Translate.Bilibili.csproj`。
3. 使用 `Debug` 配置启动 `STranslate` 主程序即可自动加载并断点调试。

### 2. Release 打包 `.spkg`

```powershell
dotnet build .\STranslate.Plugin.Translate.Bilibili\STranslate.Plugin.Translate.Bilibili.csproj -c Release
```

构建完成后会在 `.artifacts\plugins\STranslate.Plugin.Translate.Bilibili.spkg` 生成可直接导入 STranslate 的插件包。

## 参考资料

- [bilibili/Index-Translate](https://github.com/bilibili/Index-Translate)
- [Index-Translate Free Public API (`call_api.py`)](https://github.com/bilibili/Index-Translate/blob/main/inference/llm/call_api.py)
- [Index-Translate 在线演示](https://index-translate.bilibili.com/site/translate.html)
- [STranslate 社区插件开发调试指南](https://github.com/STranslate/STranslate/blob/main/src/docs/community-plugin-development.md)
