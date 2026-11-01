// Copyright (c) 2026 wellykao and other lawful rights holders.
// SPDX-License-Identifier: LicenseRef-Proprietary
// This notice does not cover third-party questions or study materials.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

public partial class QuizForm {
 string MistakeStatus(Entry e){return e.MistakeArchived?"已移出":e.Wrong?"待巩固":e.Mastered?"已掌握":"历史错题";}
 List<Question> NotebookRows(string statusFilter,string bankFilter,string typeFilter,string topic,string query,string order){
  var result=all.Where(q=>E(q).WasWrong&&(statusFilter=="全部历史"||(statusFilter=="待巩固"?E(q).Wrong&&!E(q).MistakeArchived:statusFilter=="已掌握"?E(q).Mastered&&!E(q).MistakeArchived:E(q).MistakeArchived))&&(bankFilter=="全部题库"||q.banks.Contains(bankFilter))&&(typeFilter=="全部题型"||q.type==typeFilter)&&(topic=="全部专题"||Cat(q)==topic)&&(query.Length==0||q.prompt.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0));
  return order=="最近出错"?result.OrderByDescending(q=>E(q).LastWrongDate).ToList():order=="到期优先"?result.OrderByDescending(q=>Data.Due(E(q),DateTime.Today)).ThenBy(q=>E(q).NextReviewDate).ToList():result.OrderByDescending(q=>E(q).MistakeCount).ThenByDescending(q=>E(q).LastWrongDate).ToList();
 }
 ComboBox NotebookBox(Control parent,string[] items,int width){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=width};c.Items.AddRange(items);c.SelectedIndex=0;parent.Controls.Add(c);return c;}
 void StartNotebookRound(IEnumerable<Question> selected,bool allowPending){
  var list=selected.Where(q=>allowPending||(!q.review&&!E(q).Flagged)).Distinct().ToList();if(list.Count==0){if(!testing)MessageBox.Show("没有可练题目。待复核题默认不参与重练，可在错题本勾选后选做。","错题本");return;}
  SaveDraft();ArchiveRound();draftTimer.Stop();state.Bank="全部题库";state.Type="全部题型";state.Category="全部专题";state.Mode="顺序刷题";state.Search="";state.Quality=allowPending?"包含待复核":"排除待复核";RestoreFilters();RememberFilters();queue=list;index=0;state.Session=new Round{Key=FilterKey(),DataVersion=dataVersion,QuestionIds=list.Select(q=>q.id).ToList()};Render();Save();
 }
 void MistakesDialog(){
  SaveDraft();
  using(var dialog=new Form{Text="错题本 · Ctrl多选 · 筛选独立于主界面",Size=new Size(1120,730),MinimumSize=new Size(990,600),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var filters=new FlowLayoutPanel{Dock=DockStyle.Top,Height=130,Padding=new Padding(8),WrapContents=true};dialog.Controls.Add(filters);
   var statusFilter=NotebookBox(filters,new[]{"待巩固","已掌握","已移出","全部历史"},100);var bankFilter=NotebookBox(filters,new[]{"全部题库"}.Concat(all.SelectMany(q=>q.banks).Distinct()).ToArray(),170);var typeFilter=NotebookBox(filters,new[]{"全部题型","单选题","多选题","判断题","填空题","阅读理解"},100);var topic=NotebookBox(filters,category.Items.Cast<string>().ToArray(),120);var order=NotebookBox(filters,new[]{"错次最多","最近出错","到期优先"},100);var query=new TextBox{Width=180};filters.Controls.Add(query);var include=new CheckBox{Text="重练时包含待复核题",AutoSize=true};filters.Controls.Add(include);
   var count=new Label{Width=580,Height=27};filters.Controls.Add(count);
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=98,Padding=new Padding(8),WrapContents=true};dialog.Controls.Add(bottom);
   var table=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,MultiSelect=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White};
   foreach(string title in new[]{"状态","错次","最近错答","最近出错","下次复习","错因","题目"})table.Columns.Add(title,title);
   foreach(DataGridViewColumn column in table.Columns)column.FillWeight=column.Index==6?230:column.Index==1?40:85;
   Func<List<Question>> selected=()=>table.SelectedRows.Cast<DataGridViewRow>().OrderBy(r=>r.Index).Select(r=>(Question)r.Tag).ToList();
   Action refresh=()=>{
    var rows=NotebookRows(statusFilter.Text,bankFilter.Text,typeFilter.Text,topic.Text,query.Text.Trim(),order.Text);table.Rows.Clear();foreach(var q in rows){var e=E(q);int i=table.Rows.Add(MistakeStatus(e),e.MistakeCount,e.MistakeCount==0&&e.ManualMistake?"手动加入":e.LastWrongAnswer??"旧版未记录",e.LastWrongDate??"—",e.NextReviewDate??"—",e.MistakeReason??"未标记",q.prompt.Replace("\n"," "));table.Rows[i].Tag=q;table.Rows[i].Cells[6].ToolTipText=q.prompt;}
    count.Text=String.Format("显示 {0} 题；待巩固 {1}，已掌握 {2}，已移出 {3}。输入框按题干搜索。",rows.Count,all.Count(q=>E(q).Wrong&&!E(q).MistakeArchived),all.Count(q=>E(q).WasWrong&&E(q).Mastered&&!E(q).MistakeArchived),all.Count(q=>E(q).MistakeArchived));
   };
   filters.Controls.Add(B("应用筛选",(s,e)=>refresh(),true,100));filters.Controls.Add(B("加入当前题",(s,e)=>{if(Current==null)return;StudyOperations.AddMistake(E(Current),DateTime.Today);Save();UpdateStats();refresh();},false,110));
   bottom.Controls.Add(B("重练所选题",(s,e)=>{var qs=selected();if(qs.Count==0)return;bool allowed=include.Checked;if(!allowed&&qs.All(q=>q.review||E(q).Flagged)){MessageBox.Show("所选题均为待复核状态，请先核查或勾选包含待复核题。");return;}dialog.Close();StartNotebookRound(qs,allowed);},true,120));
   bottom.Controls.Add(B("查看错答详情",(s,e)=>{var qs=selected();if(qs.Count==0)return;var q=qs[0];var entry=E(q);TextDialog("错题详情",q.prompt+"\n"+String.Join("\n",q.options.Select((x,i)=>((char)('A'+i))+". "+x))+"\n\n最近错答："+(entry.LastWrongAnswer??"旧版未记录")+"\n参考答案："+AnswerText(q)+"\n答案状态："+q.verification+"\n解析："+q.explanation+"\n错因："+entry.MistakeReason+"\n笔记："+entry.Note+"\n来源："+q.source+" / "+q.locator);},false,130));
   bottom.Controls.Add(B("标记错因",(s,e)=>{var qs=selected();if(qs.Count==0)return;using(var reason=new Form{Text="为所选题标记错因",Size=new Size(390,180),StartPosition=FormStartPosition.CenterParent,Font=Font}){var cb=new ComboBox{Left=20,Top=20,Width=335,DropDownStyle=ComboBoxStyle.DropDownList};cb.Items.AddRange(new[]{"知识点不熟","概念混淆","审题失误","方法不当","时间不足","其他"});cb.SelectedIndex=0;reason.Controls.Add(cb);var ok=B("保存错因",(ss,ee)=>{foreach(var q in qs)E(q).MistakeReason=cb.Text;Save();reason.Close();refresh();},true);ok.Location=new Point(20,65);reason.Controls.Add(ok);reason.ShowDialog(dialog);}},false,110));
   bottom.Controls.Add(B("标为掌握",(s,e)=>{foreach(var q in selected())StudyOperations.Master(E(q),DateTime.Today);Save();UpdateStats();refresh();},false,110));
   bottom.Controls.Add(B("移出错题本",(s,e)=>{foreach(var q in selected())StudyOperations.Archive(E(q));Save();UpdateStats();refresh();},false,120));
   bottom.Controls.Add(B("重新巩固",(s,e)=>{foreach(var q in selected())StudyOperations.AddMistake(E(q),DateTime.Today);Save();UpdateStats();refresh();},false,110));
   bottom.Controls.Add(B("重置选题进度",(s,e)=>{var qs=selected();if(qs.Count==0)return;dialog.Close();PerformReset(ResetKind.SelectedProgress,qs.Select(q=>q.id).ToList(),"重置所选题进度");},false,130));
   bottom.Controls.Add(B("返回练习",(s,e)=>{dialog.Close();Render();},false,110));
   dialog.Controls.Add(table);table.BringToFront();refresh();PreviewOrShow(dialog,"10-mistake-notebook");
  }
 }
 List<string> BankScope(){return all.Where(q=>state.Bank=="全部题库"||q.banks.Contains(state.Bank)).Select(q=>q.id).ToList();}
 string OperationDescription(ResetKind kind,int count){
  if(kind==ResetKind.ReplayRound)return "重练当前轮的 "+count+" 题：清除本轮选择、评分与草稿，从第一题开始。\n保留累计答题次数、错题、收藏、笔记及历史成绩。";
  if(kind==ResetKind.ClearMistakes)return "清空全部错题本与复习安排。\n保留累计作答/正确次数、收藏、笔记和旧轮次成绩；以后答错会重新收录。";
  if(kind==ResetKind.Factory)return "恢复初始设置：清除全部做题进度、错题、历史成绩、收藏、笔记、个人分类、存疑及字号设置。\n内置2394道题及操作前恢复副本仍保留。";
  return "重置 "+count+" 道题的答题次数、正确次数、错题状态与复习安排，并清除它们在当前轮的作答。\n保留收藏、笔记、个人分类、存疑和字号。"+(kind==ResetKind.AllProgress?"\n全部进度重置会同时清空全部轮次历史。":"\n旧轮次历史是当时的成绩快照，不追溯修改；当前轮成绩将重新计算。")+"\n相同题目在多个题库共享记录，重置会同时影响这些入口。";
 }
 static bool ResetAllowed(bool agree,string input,bool strong){return agree&&(!strong||input=="重置");}
 bool ConfirmReset(string label,string content,bool strong){
  using(var dialog=new Form{Text=label,Size=new Size(700,420),MinimumSize=new Size(620,390),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var description=new TextBox{ReadOnly=true,Multiline=true,TabStop=false,BorderStyle=BorderStyle.None,Left=20,Top=20,Width=645,Height=190,Text=Lines(content+"\n\n执行前会保存独立恢复副本。备份失败则不执行。")};dialog.Controls.Add(description);
   var agree=new CheckBox{Text="我已核对上面的影响范围",Left=20,Top=225,Width=400};dialog.Controls.Add(agree);var input=new TextBox{Left=20,Top=278,Width=220,Visible=strong};dialog.Controls.Add(input);dialog.Controls.Add(new Label{Text=strong?"请输入“重置”后继续":"",Left=20,Top=255,Width=350});
   var yes=B("确认执行",(s,e)=>{dialog.DialogResult=DialogResult.OK;dialog.Close();},true,130);yes.Location=new Point(20,325);yes.Enabled=false;dialog.Controls.Add(yes);var cancel=B("取消",(s,e)=>dialog.Close(),false,100);cancel.Location=new Point(165,325);cancel.DialogResult=DialogResult.Cancel;dialog.CancelButton=cancel;dialog.Controls.Add(cancel);
   EventHandler changed=(s,e)=>yes.Enabled=ResetAllowed(agree.Checked,input.Text,strong);agree.CheckedChanged+=changed;input.TextChanged+=changed;if(testing)PreviewOrShow(dialog,"12-reset-confirm");else dialog.ShowDialog(this);return dialog.DialogResult==DialogResult.OK;
  }
 }
 void ApplyLoadedState(Progress incoming){draftTimer.Stop();evaluated=true;queue=new List<Question>();index=0;state=incoming;RestoreFilters();Rebuild(true);}
 void PerformReset(ResetKind kind,List<string> ids,string label){
  int count=kind==ResetKind.ReplayRound?queue.Count:ids.Count;if(count==0){MessageBox.Show("没有匹配的题目。");return;}
  if(!ConfirmReset(label,OperationDescription(kind,count),kind==ResetKind.AllProgress||kind==ResetKind.ClearMistakes||kind==ResetKind.Factory))return;
  try{SaveDraft();ArchiveRound();Save();var nextState=StudyOperations.Commit(statePath,state,p=>StudyOperations.Transform(p,kind,ids),label);ApplyLoadedState(nextState);status.Text="操作完成；可从“重置/恢复”恢复操作前副本。";}catch(Exception ex){MessageBox.Show("操作未完成："+ex.Message,"重置失败");}
 }
 void ResetDialog(){
  SaveDraft();using(var dialog=new Form{Text="重置与恢复 · 先看影响范围",Size=new Size(750,480),MinimumSize=new Size(690,440),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var action=new ComboBox{Left=20,Top=20,Width=680,DropDownStyle=ComboBoxStyle.DropDownList};action.Items.AddRange(new[]{"重练当前轮","重置当前题进度","重置当前题库进度","重置全部学习进度","清空全部错题本","恢复初始设置"});action.SelectedIndex=0;dialog.Controls.Add(action);
   var info=new TextBox{Left=20,Top=75,Width=680,Height=230,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None};dialog.Controls.Add(info);
   Func<ResetKind> kind=()=>action.SelectedIndex==0?ResetKind.ReplayRound:action.SelectedIndex==3?ResetKind.AllProgress:action.SelectedIndex==4?ResetKind.ClearMistakes:action.SelectedIndex==5?ResetKind.Factory:ResetKind.SelectedProgress;
   Func<List<string>> scope=()=>action.SelectedIndex==1?(Current==null?new List<string>():new List<string>{Current.id}):action.SelectedIndex==2?BankScope():all.Select(q=>q.id).ToList();
   Action refresh=()=>info.Text=Lines("已应用题库："+state.Bank+"。当前题库重置按整个题库计算，不受题型、专题和搜索筛选限制。\n\n"+OperationDescription(kind(),kind()==ResetKind.ReplayRound?queue.Count:scope().Count)+"\n\n最近操作："+(state.LastResetLabel??"暂无"));action.SelectedIndexChanged+=(s,e)=>refresh();refresh();
   var execute=B("执行所选操作",(s,e)=>{var k=kind();var ids=scope();string label=action.Text;dialog.Close();PerformReset(k,ids,label);},true,150);execute.Location=new Point(20,340);dialog.Controls.Add(execute);
   var undo=B("恢复操作前记录",(s,e)=>{dialog.Close();if(!ConfirmReset("恢复操作前记录","将整个学习记录恢复到上次重置之前，包括进度、收藏、笔记及历史；上次操作后的新作答也会回退。\n恢复前会再次保存当前记录，可再次恢复操作前记录进行切换。",false))return;try{SaveDraft();var restored=StudyOperations.Undo(statePath,state);ApplyLoadedState(restored);}catch(Exception ex){MessageBox.Show(ex.Message,"恢复失败");}},false,165);undo.Location=new Point(185,340);undo.Enabled=!String.IsNullOrEmpty(state.LastResetBackup);dialog.Controls.Add(undo);PreviewOrShow(dialog,"11-reset-panel");
  }
 }
 void RunNotebookChecks(string output,Action<bool,string> check){
  var qs=all.Where(q=>q.type=="单选题"&&!q.review).Take(3).ToList();var a=qs[0];var b=qs[1];DateTime day=new DateTime(2026,11,1);
  var entry=new Entry();Data.Record(entry,false,"D",day);Data.Record(entry,true,"A",day.AddDays(1));check(entry.MistakeCount==1&&entry.LastWrongAnswer=="D"&&entry.LastWrongDate=="2026-11-01","答对后仍保留最近错答与错次");StudyOperations.Master(entry,day.AddDays(2));check(!entry.Wrong&&entry.Mastered&&entry.Attempts==2,"手动掌握不伪造答题次数");StudyOperations.Archive(entry);Data.Record(entry,true,"A",day.AddDays(3));check(entry.MistakeArchived&&!entry.Wrong&&!Data.Due(entry,day.AddDays(50)),"已移出题不因答对重新入本");Data.Record(entry,false,"C",day.AddDays(4));check(entry.Wrong&&!entry.MistakeArchived&&entry.MistakeCount==2,"再次答错自动重新入本");
  var manual=new Entry();StudyOperations.AddMistake(manual,day);check(manual.Wrong&&manual.ManualMistake&&manual.Attempts==0&&manual.MistakeCount==0,"手动加入错题不伪造错误次数");state.Entries[a.id]=entry;state.Entries[b.id]=manual;check(NotebookRows("待巩固","全部题库","全部题型","全部专题","","错次最多").Any(q=>q.id==a.id),"错题本不受主界面筛选误导");entry.MistakeReason="概念混淆";Save();check(Data.Load(statePath).Entries[a.id].MistakeReason=="概念混淆","错因保存");MistakesDialog();ResetDialog();
  string legacy=Path.Combine(output,"legacy-mistakes.json");File.WriteAllText(legacy,"{\"Version\":2,\"Entries\":{\"old\":{\"Attempts\":5,\"Correct\":2,\"Wrong\":true,\"WasWrong\":true,\"LastSuccessDate\":\"2026-10-30\",\"LastAnswer\":\"A\"}}}");var migrated=Data.Load(legacy);check(migrated.Entries["old"].MistakeCount==3&&migrated.Entries["old"].LastWrongAnswer==null,"旧记录错次迁移且不猜测旧错答");
  var fixture=new Progress{ReadingSize=14};fixture.Entries[a.id]=new Entry{Attempts=4,Correct=1,WasWrong=true,Wrong=true,MistakeCount=3,Favorite=true,Flagged=true,Note="重要笔记",CategoryOverride="文学与典籍",NextReviewDate="2026-11-02"};fixture.Entries[b.id]=new Entry{Attempts=2,Correct=2,Mastered=true};fixture.Session=new Round{Key="fixture",DataVersion=dataVersion,QuestionIds=new List<string>{a.id,b.id}};fixture.Session.Answers[a.id]=new SessionAnswer{Answer="D",Revealed=true,Graded=true};fixture.Session.Answers[b.id]=new SessionAnswer{Answer="A",Revealed=true,Graded=true,Correct=true};fixture.History.Add(new RoundResult{Id="historic",Date="2026-10-31",Answered=2,Correct=1,Summary="历史快照"});
  string path=Path.Combine(output,"reset-check","progress.json");Data.Save(path,fixture);var reset=StudyOperations.Commit(path,fixture,p=>StudyOperations.Transform(p,ResetKind.SelectedProgress,new[]{a.id}),"重置单题");check(reset.Entries[a.id].Attempts==0&&!reset.Entries[a.id].WasWrong&&reset.Entries[a.id].Favorite&&reset.Entries[a.id].Flagged&&reset.Entries[a.id].Note=="重要笔记","单题重置保留收藏笔记与存疑");check(reset.Entries[b.id].Attempts==2&&!reset.Session.Answers.ContainsKey(a.id)&&reset.Session.Answers[b.id].Graded,"单题重置只清相应作答");check(fixture.Entries[a.id].Attempts==4&&Data.Load(path).Entries[a.id].Attempts==0,"重置克隆处理并落盘");check(File.Exists(Path.Combine(Path.GetDirectoryName(path),"operations",reset.LastResetBackup)),"重置前独立副本可读取");var undone=StudyOperations.Undo(path,reset);check(undone.Entries[a.id].Attempts==4&&undone.Session.Answers[a.id].Graded&&undone.Entries[a.id].Note=="重要笔记","撤销重置恢复完整记录");
  var replay=StudyOperations.Transform(StudyOperations.Clone(fixture),ResetKind.ReplayRound,new[]{a.id,b.id});check(replay.Session.QuestionIds.SequenceEqual(fixture.Session.QuestionIds)&&replay.Session.Answers.Count==0&&replay.Session.Id!=fixture.Session.Id&&replay.Entries[a.id].Attempts==4,"重练本轮不清累计成绩");
  var clear=StudyOperations.Transform(StudyOperations.Clone(fixture),ResetKind.ClearMistakes,new[]{a.id,b.id});check(!clear.Entries[a.id].WasWrong&&!clear.Entries[a.id].Wrong&&clear.Entries[a.id].Attempts==4&&clear.Entries[a.id].Note=="重要笔记"&&clear.Entries[b.id].Mastered,"清空错题本不清分数或正常题掌握");Data.Record(clear.Entries[a.id],false,"B",day);check(clear.Entries[a.id].MistakeCount==1,"清空后重新答错从一次累计");
  var allReset=StudyOperations.Transform(StudyOperations.Clone(fixture),ResetKind.AllProgress,new[]{a.id,b.id});check(allReset.Entries.Values.All(e=>e.Attempts==0)&&allReset.History.Count==0&&allReset.Session==null&&allReset.Entries[a.id].Favorite&&allReset.ReadingSize==14,"全部进度重置保留个人资料并清历史");var factory=StudyOperations.Transform(StudyOperations.Clone(fixture),ResetKind.Factory,new[]{a.id,b.id});check(factory.Entries.Count==0&&factory.History.Count==0&&factory.ReadingSize==11,"初始设置清除个人数据");
  string before=File.ReadAllText(path);bool failed=false;try{StudyOperations.Commit(path,undone,p=>{throw new InvalidOperationException("模拟变更失败");},"失败测试");}catch(InvalidOperationException){failed=true;}check(failed&&File.ReadAllText(path)==before&&undone.Entries[a.id].Attempts==4,"操作失败不覆盖现有记录");
  string blocked=Path.Combine(output,"reset-backup-failure","progress.json");Data.Save(blocked,fixture);File.WriteAllText(Path.Combine(Path.GetDirectoryName(blocked),"operations"),"占用备份目录名");before=File.ReadAllText(blocked);failed=false;try{StudyOperations.Commit(blocked,fixture,p=>new Progress(),"失败");}catch(IOException){failed=true;}check(failed&&File.ReadAllText(blocked)==before,"备份失败时拒绝执行重置");
  StartNotebookRound(new[]{a},true);Pick(0);var incoming=new Progress{Bank="全部题库",Type="单选题"};incoming.Session=new Round{Key="全部题库|单选题|全部专题|顺序刷题|排除待复核||20",DataVersion=dataVersion,QuestionIds=new List<string>{b.id}};ApplyLoadedState(incoming);check(Current.id==b.id&&CurrentAnswer==null,"恢复与重置不带入旧页面草稿");
  check(!ResetAllowed(false,"重置",true)&&!ResetAllowed(true,"",true)&&ResetAllowed(true,"重置",true)&&ResetAllowed(true,"",false),"重置确认必须核对范围且大范围需输入重置");check(!ConfirmReset("恢复初始设置确认",OperationDescription(ResetKind.Factory,all.Count),true),"关闭确认窗口不会执行重置");
 }
}
