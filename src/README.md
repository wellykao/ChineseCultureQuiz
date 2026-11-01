# 源码目录

| 文件 | 用途 |
| --- | --- |
| `CultureQuiz.cs` | WinForms 主界面、作答、筛选、会话及启动入口 |
| `QuizCore.cs` | 题目/记录结构、评分、复习安排、持久化与损坏恢复 |
| `QuizExtras.cs` | 答题卡、笔记、历史与字号等辅助界面 |
| `QuizNotebook.cs` | 错题本、重置范围、确认及验收检查 |
| `StudyOperations.cs` | 重置变换、操作前副本和撤销 |
| `app.manifest` | Windows 应用权限与 DPI 配置 |

本目录全部为软件代码；题库以嵌入资源 `questions.json` 加载，默认由构建脚本选择 `data/questions.json`。
