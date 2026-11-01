# 示例题与题库格式

`demo-questions.json` 为完整可用的自编示例题库：7 道基础事实题、9 道基于自编短文的阅读题；全部明确标识非真题。只用于功能演示，不足以覆盖考试范围。

根结构为题目数组。题目 ID 应稳定且全局唯一；学习记录按 ID 关联，修改 ID 会失去原记录关联。

| 字段 | 含义 |
| --- | --- |
| `id`, `prompt`, `answer` | 唯一 ID、题干、参考答案 |
| `type` | 单选题、多选题、判断题、填空题、阅读理解 |
| `options` | 选项文字数组；单选/阅读用 A 开始的字母作答案，多选用如 AC；判断用正确/错误 |
| `category` | 专题名称，对应程序专题筛选 |
| `bank`, `banks` | 展示归属和筛选归属列表；同一题可以属于多个题库 |
| `source`, `locator` | 内容出处与定位；导入资料需先确认权利 |
| `explanation`, `passage` | 解析与阅读正文，非阅读题正文可为空 |
| `review`, `verification` | 是否待复核、核查状态；未实际核查不得写成“已核验” |
| `originalAnswer`, `originalExplanation` | 原答案与原解析，便于对照订正 |
| `evidence` | 核查依据数组，包含 `title`, `url`, `comment` |
| `verifiedDate`, `correctionNote` | 核查日期和订正说明 |
| `note`, `alsoSources` | 资料说明和补充来源；个人笔记另存学习记录 |

构建脚本会验证基本结构与答案格式，不验证版权、答案事实或全部教学质量。不要把个人学习记录放进本目录。完整第三方题库不随仓库提供。
