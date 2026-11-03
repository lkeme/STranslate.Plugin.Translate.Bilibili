# STranslate.Plugin.Translate.Bilibili

基于哔哩哔哩开源多语种翻译大模型 [**Index-Translate**](https://github.com/bilibili/Index-Translate) 构建的 [STranslate](https://github.com/STranslate/STranslate) 文本翻译插件。

默认直连官方免费公开 API（`https://index-translate.bilibili.com/v1/chat/completions`，零配置开箱即用）。

## 功能特性

- **官方免费公开 API 零配置即用**：无需配置接口地址或申请 API Key，安装后即可直接调用旗舰模型 `Index-Translate-35B-A3B`。
- **多尺寸与专项模型支持**：
  - `Index-Translate-35B-A3B`：旗舰 MoE 模型（默认推荐）
  - `Index-Translate-9B`：均衡高质量模型
  - `Index-Translate-2B`：轻量极速模型
  - `Index-Nailong-9B` / `Index-Nailong-2B`：超长上下文文档级翻译模型（229K ~ 262K 上下文）
  - 支持在设置下拉框中手动输入并保存任意自定义模型名称
- **结构化翻译模板系统 (`LlmTranslatePluginBase`)**：
  - 保留 STranslate 原生模板选择能力，支持在主翻译窗口直接快捷切换当前模板。
  - 提供插件自有的无角色模板编辑器：每个模板由名称、自由文本“翻译要求”和术语 KV 表组成，不暴露 `role`、`$source`、`$target`、`$content` 等内部实现细节。
  - `翻译`是不可删除的默认模板；其术语表作为通用术语库，其他模板可选择继承，且同名术语以当前模板的本地译法优先。
  - 预置官网 [在线演示站](https://index-translate.bilibili.com/site/translate.html) 的全部 10 套指令遵循与梗翻译预设，并针对 STranslate 的全局语言选择改写为跨目标语言可用的版本。运行时始终生成唯一的 `user` 消息；翻译要求转换为 `【注意】`，术语表转换为 `【硬性要求】专名/术语对照`。
- **流式输出 (SSE) 与思考链过滤**：
  - 支持实时流式返回（`StreamPostAsyncEnumerable`）
  - 默认显式设置 `"chat_template_kwargs": { "enable_thinking": false }`，并自动剥离 `<think>...</think>` 思考内容
- **全语种映射**：完整覆盖 STranslate 全部 33 种语言枚举（含简体中文、繁体中文、粤语、英语、日语、韩语、维吾尔语、乌兹别克语等）。

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
