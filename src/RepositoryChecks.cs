// Copyright (c) 2026 wellykao and other lawful rights holders.
// SPDX-License-Identifier: LicenseRef-Proprietary
// See LICENSE and NOTICE.md; rights apply only where legally held.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

public partial class QuizForm {
 public void RunChecks(string output){
  testOutput=output;
  var checks=new List<string>();
  Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("检查失败："+name);checks.Add(name);};
  check(all.Count==16&&all.All(x=>x.source=="编写练习（非真题）"),"仅包含16道自编示例题");
  check(queue.Count>0&&queue.All(x=>!x.review),"默认筛选有题且排除待复核");
  check(!bank.Items.Cast<string>().Any(b=>b!="全部题库"&&!all.Any(x=>x.banks.Contains(b))),"题库入口由实际数据生成");
  var q=Current;Submit();check(E(q).Attempts==0,"空白不计分");Pick(q.answer[0]-'A');Submit();Submit();
  check(E(q).Attempts==1&&CurrentAnswer.Correct,"正确作答与重复提交保护");
  ToggleFavorite();check(E(q).Favorite,"收藏保存");Navigate(1);Navigate(-1);
  check(Current.id==q.id&&evaluated&&E(q).Attempts==1,"返回已答题不重复计分");
  Snapshot(output,"01-answer");Size=new Size(1080,760);Snapshot(output,"02-small");
  check(submit.Parent.ClientRectangle.Contains(submit.Bounds)&&next.Parent.ClientRectangle.Contains(next.Bounds),"最小窗口操作按钮可见");
  StartNotebookRound(new[]{q},true);Pick((q.answer[0]-'A'+1)%q.options.Length);Submit();
  check(!CurrentAnswer.Correct&&E(q).Wrong&&E(q).MistakeCount==1,"答错自动加入错题本");
  check(Data.Load(statePath).Entries[q.id].Wrong,"错题实际写入隔离记录");
  StartNotebookRound(new[]{all.First(x=>x.type=="阅读理解")},true);q=Current;
  check(!String.IsNullOrEmpty(passage.Text),"阅读正文显示");Pick(q.answer[0]-'A');Submit();
  check(CurrentAnswer.Correct,"阅读题评分");Snapshot(output,"03-reading");
  var entry=new Entry();DateTime day=new DateTime(2026,11,1);Data.Record(entry,false,"A",day);
  Data.Record(entry,true,"B",day.AddDays(1));Data.Record(entry,true,"B",day.AddDays(1));
  check(entry.Wrong&&entry.ConsecutiveCorrect==1,"同日重复答对不直接掌握");
  Data.Record(entry,true,"B",day.AddDays(2));check(!entry.Wrong&&entry.Mastered,"跨日掌握");
  string store=Path.Combine(output,"recovery-check","progress.json");var fixture=new Progress();
  fixture.Entries["fixture"]=new Entry{Attempts=2,Correct=1,Note="隔离备份样本"};Data.Save(store,fixture);Data.Save(store,fixture);
  check(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(store),"backups")).Length>0,"每日有效记录备份");
  File.WriteAllText(store,"broken main");File.WriteAllText(store+".bak","broken backup");string recoveredFrom;
  var recovered=Data.Recover(store,out recoveredFrom);
  check(recovered.Entries["fixture"].Note=="隔离备份样本"&&recoveredFrom.Contains("backups"),"主记录损坏后从每日副本恢复");
  RunNotebookChecks(output,check);
  File.WriteAllText(Path.Combine(output,"test-results.json"),Data.Json().Serialize(new{passed=true,questions=all.Count,checks=checks,version="2.2-demo"}),Encoding.UTF8);
 }
}
