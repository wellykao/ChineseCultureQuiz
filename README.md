# 中华文化离线练习室

Windows 本地刷题程序，使用 C# / .NET Framework WinForms，支持单选、多选、判断、填空自评与阅读理解。学习记录保存在本机，日常刷题不需要联网。

本仓库用于私有保存与授权协作。软件按 [专有权利声明](LICENSE) 管理，不提供开源许可。仓库所有者：[@wellykao](https://github.com/wellykao)。

## 本仓库包含什么

| 目录或文件 | 内容 |
| --- | --- |
| `src/` | 程序源码、界面、学习记录、错题本及重置逻辑 |
| `data/` | 16 道自编示例题及题库格式说明，非考试真题 |
| `scripts/` | Windows 构建、题库校验与隔离自测 |
| `docs/` | 使用说明、免责声明、权利边界及提交范围 |
| `LICENSE` | 保留依法享有权利的专有声明 |
| `NOTICE.md` | 归属、第三方依赖和 AI 辅助开发说明 |
| `SECURITY.md` | 漏洞与敏感内容报告方式 |

原始 PDF、Word、Excel、完整第三方题库、内嵌完整题库的本地 EXE 和个人学习记录均不纳入此仓库。原本的完整本地程序继续保留在资料目录中。

## 构建与启动

需要 Windows、Windows PowerShell 5.1 或更高版本及 .NET Framework 4.x 编译器。本项目不下载 NuGet 包。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
.\dist\ChineseCultureQuiz-Demo.exe
```

默认输出为仅包含 16 道示例题的独立 EXE。示例版使用独立记录目录 `%LOCALAPPDATA%\ChineseCultureQuizDemo`，与原本完整版本的记录分开。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

自测在 `test-output/` 内创建测试记录与截图，不读取真实学习记录。结果见该目录的 `test-results.json`。不把未经实际运行的检查记为通过。

如拥有另一份题库的使用及分发许可，可指定本地 JSON 路径：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 -QuestionBank 'C:\path\to\authorized-questions.json'
```

题目会被嵌入生成的 EXE，分享 EXE 也会分享其中题目。构建前需确认相应权利；私有仓库同样不能代替第三方授权。自定义数据保存在仓库外或已忽略的 `private-data/`，不要提交真实学习记录。

## 功能

- 顺序、随机、未做、到期复习、错题、收藏与笔记筛选；保留本轮草稿和作答。
- 错题次数、最近错答、错因、手动收录、掌握、移出与重新巩固。
- 同一天反复答对不直接视为掌握；复习计划采用本机日期。
- 重练本轮、重置所选题、重置全部进度、清空错题本及恢复初始设置。
- 重置前创建独立恢复副本；支持恢复操作前记录。大范围操作需确认范围并输入“重置”。
- 学习记录备份、恢复、每日备份及损坏恢复；重置前副本不等于彻底删除数据。

详见 [使用说明](docs/使用说明.md)、[免责声明](docs/免责声明.md)、[代码保护说明](docs/代码保护说明.md)。题目、答案与复习安排仅用于辅助学习，不构成招生机构认可或考试成绩保证。
