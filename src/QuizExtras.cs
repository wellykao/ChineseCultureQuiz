// Copyright (c) 2026 wellykao and other lawful rights holders.
// SPDX-License-Identifier: LicenseRef-Proprietary
// See LICENSE and NOTICE.md; rights apply only where legally held.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

public partial class QuizForm {
 Timer draftTimer;
 readonly Dictionary<float,Font> readingFonts=new Dictionary<float,Font>();
 void SetupExtras(){draftTimer=new Timer{Interval=600};draftTimer.Tick+=(s,e)=>{draftTimer.Stop();SaveDraft();Save();};}
 Font ReadingFont(float size){Font font;if(!readingFonts.TryGetValue(size,out font)){font=new Font(Font.FontFamily,size);readingFonts[size]=font;}return font;}
 void ApplyReadingFonts(){prompt.Font=ReadingFont(state.ReadingSize+4);passage.Font=ReadingFont(state.ReadingSize);feedback.Font=ReadingFont(state.ReadingSize-1);foreach(var c in choices)c.Font=ReadingFont(state.ReadingSize);if(fill!=null)fill.Font=ReadingFont(state.ReadingSize+1);}
 void ChangeReadingSize(int delta){SaveDraft();state.ReadingSize=Math.Max(10,Math.Min(16,state.ReadingSize+delta));Render();Save();status.Text="阅读字号已保存："+state.ReadingSize;}
 void ScheduleDraft(){SaveDraft();if(draftTimer!=null){draftTimer.Stop();draftTimer.Start();}}
 void SaveDraft(){
  if(Current==null||state.Session==null||evaluated)return;
  var a=CurrentAnswer;if(a!=null&&a.Revealed)return;
  string text=Current.type=="填空题"?(fill==null?"":fill.Text):(choices.Count==0?"":Chosen());
  if(a==null){if(text.Length==0)return;a=new SessionAnswer();state.Session.Answers[Current.id]=a;}
  a.Answer=text;
 }
 void RestoreDraft(SessionAnswer draft){
  if(draft==null||draft.Revealed)return;
  for(int i=0;i<choices.Count;i++){
   choices[i].Selected=Current.type=="判断题"?draft.Answer==(i==0?"正确":"错误"):(draft.Answer??"").Contains((char)('A'+i));choices[i].Locked=false;choices[i].Invalidate();
  }
 }
 void ArchiveRound(){
  if(Current==null||state.Session==null||!state.Session.Answers.Values.Any(a=>a.Graded))return;
  string text=SummaryText();state.History.RemoveAll(h=>h.Id==state.Session.Id);
  state.History.Add(new RoundResult{Id=state.Session.Id,Date=DateTime.Today.ToString("yyyy-MM-dd"),Answered=state.Session.Answers.Values.Count(a=>a.Graded),Correct=state.Session.Answers.Values.Count(a=>a.Graded&&a.Correct),Summary=text});
  while(state.History.Count>200)state.History.RemoveAt(0);
 }
 string AnswerState(Question q){SessionAnswer a;if(!state.Session.Answers.TryGetValue(q.id,out a))return "未作答";if(a.Graded)return a.Correct?"答对":"答错";if(a.Revealed)return "待自评";return String.IsNullOrEmpty(a.Answer)?"未作答":"待提交";}
 int NextUnfinished(){for(int step=1;step<=queue.Count;step++){int n=(index+step)%queue.Count;SessionAnswer a;if(!state.Session.Answers.TryGetValue(queue[n].id,out a)||!a.Graded)return n;}return -1;}
 void JumpToQuestion(int target){if(target<0||target>=queue.Count)return;SaveDraft();index=target;Render();Save();}
 void PreviewOrShow(Form dialog,string name){
  if(!testing){dialog.ShowDialog(this);return;}
  dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show(this);Application.DoEvents();using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(0,0,dialog.Width,dialog.Height));bitmap.Save(Path.Combine(testOutput,name+".png"));}dialog.Close();
 }
 void AnswerSheetDialog(){
  if(Current==null)return;SaveDraft();
  using(var dialog=new Form{Text="本轮答题卡 · 双击题目跳转",Size=new Size(850,620),MinimumSize=new Size(660,420),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(10)};dialog.Controls.Add(buttons);
   var sheet=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None};
   sheet.Columns.Add("number","题号");sheet.Columns[0].FillWeight=12;sheet.Columns.Add("state","状态");sheet.Columns[1].FillWeight=19;sheet.Columns.Add("prompt","题目");sheet.Columns[2].FillWeight=100;
   for(int i=0;i<queue.Count;i++){
    string value=AnswerState(queue[i]);int row=sheet.Rows.Add(i+1,value,queue[i].prompt.Replace("\n"," "));sheet.Rows[row].Tag=i;sheet.Rows[row].Cells[2].ToolTipText=queue[i].prompt;
    sheet.Rows[row].DefaultCellStyle.BackColor=value=="答对"?Color.FromArgb(225,243,231):value=="答错"?Color.FromArgb(255,233,227):value=="待提交"||value=="待自评"?Color.FromArgb(255,247,217):Color.White;
   }
   EventHandler jump=(s,e)=>{if(sheet.CurrentRow==null)return;int target=(int)sheet.CurrentRow.Tag;dialog.Close();JumpToQuestion(target);};
   buttons.Controls.Add(B("跳转到所选题",jump,true,145));buttons.Controls.Add(B("下一道未完成",(s,e)=>{int target=NextUnfinished();if(target<0){MessageBox.Show("本轮已全部完成。");return;}dialog.Close();JumpToQuestion(target);},false,145));buttons.Controls.Add(B("返回练习",(s,e)=>dialog.Close(),false,120));
   sheet.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)jump(s,e);};dialog.Controls.Add(sheet);sheet.BringToFront();
   if(sheet.Rows.Count>0){sheet.ClearSelection();sheet.Rows[index].Selected=true;sheet.CurrentCell=sheet.Rows[index].Cells[0];sheet.FirstDisplayedScrollingRowIndex=index;}
   PreviewOrShow(dialog,"07-answer-sheet");
  }
 }
 void NotesDialog(){
  if(Current==null)return;SaveDraft();var q=Current;
  using(var dialog=new Form{Text="本题笔记 · 仅保存在本机",Size=new Size(760,560),MinimumSize=new Size(560,360),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var title=new Label{Text=q.prompt,AutoEllipsis=true,Dock=DockStyle.Top,Height=70,Padding=new Padding(10)};dialog.Controls.Add(title);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(10)};dialog.Controls.Add(buttons);
   var editor=new TextBox{Dock=DockStyle.Fill,Multiline=true,MaxLength=6000,ScrollBars=ScrollBars.Vertical,Font=ReadingFont(state.ReadingSize),Text=E(q).Note??""};dialog.Controls.Add(editor);editor.BringToFront();
   buttons.Controls.Add(B("保存笔记",(s,e)=>{E(q).Note=editor.Text;Save();status.Text="本题笔记已保存，可用“我的笔记”筛选查看。";dialog.Close();},true,140));buttons.Controls.Add(B("取消",(s,e)=>dialog.Close(),false,110));
   PreviewOrShow(dialog,"08-notes");
  }
 }
 void HistoryDialog(){
  SaveDraft();ArchiveRound();Save();
  using(var dialog=new Form{Text="学习记录 · 最近200轮",Size=new Size(900,650),MinimumSize=new Size(700,450),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   int attempts=all.Sum(q=>E(q).Attempts),correct=all.Sum(q=>E(q).Correct);var overview=new Label{Dock=DockStyle.Top,Height=65,Padding=new Padding(12),Text="累计作答 "+attempts+" 次 · 答对 "+correct+" 次 · "+(attempts==0?"暂无正确率":"正确率 "+(100.0*correct/attempts).ToString("0.0")+"%")+"\n切换新轮次时会自动归档已答记录。双击一行查看该轮专题与错题。"};dialog.Controls.Add(overview);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(10)};dialog.Controls.Add(buttons);
   var table=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White};
   table.Columns.Add("date","日期");table.Columns.Add("answered","已答");table.Columns.Add("correct","答对");table.Columns.Add("rate","正确率");table.Columns.Add("id","轮次");
   foreach(var record in state.History.AsEnumerable().Reverse()){int r=table.Rows.Add(record.Date,record.Answered,record.Correct,record.Answered==0?"—":(100.0*record.Correct/record.Answered).ToString("0.0")+"%",(record.Id??"").Substring(0,Math.Min(8,(record.Id??"").Length)));table.Rows[r].Tag=record;}
   EventHandler detail=(s,e)=>{if(table.CurrentRow!=null)TextDialog("历史轮次详情",((RoundResult)table.CurrentRow.Tag).Summary);};table.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)detail(s,e);};buttons.Controls.Add(B("查看所选轮次",detail,true,145));buttons.Controls.Add(B("导出所选总结",(s,e)=>{if(table.CurrentRow==null)return;using(var target=new SaveFileDialog{Filter="文本文件|*.txt",FileName="练习总结.txt"})if(target.ShowDialog()==DialogResult.OK)try{File.WriteAllText(target.FileName,((RoundResult)table.CurrentRow.Tag).Summary,new UTF8Encoding(true));}catch(Exception ex){MessageBox.Show(ex.Message,"导出失败");}},false,145));buttons.Controls.Add(B("返回练习",(s,e)=>dialog.Close(),false,110));dialog.Controls.Add(table);table.BringToFront();
   PreviewOrShow(dialog,"09-history");
  }
 }
}
