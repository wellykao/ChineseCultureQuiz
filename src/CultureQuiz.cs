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

public class ChoiceCard:Control {
 public bool Selected,Multiple,Locked;
 public string Marker,Body,Result="";
 public event EventHandler Picked;
 public ChoiceCard(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);TabStop=true;Cursor=Cursors.Hand;Margin=new Padding(0,0,0,8);}
 public int FitHeight(int width){return Math.Max(53,TextRenderer.MeasureText((Marker==null?"":Marker+".  ")+Body,Font,new Size(Math.Max(80,width-66),0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height+24);}
 protected override void OnClick(EventArgs e){base.OnClick(e);Focus();if(!Locked&&Picked!=null)Picked(this,e);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Result=="correct"?Color.FromArgb(222,242,228):Result=="wrong"?Color.FromArgb(255,231,224):Selected?Color.FromArgb(226,238,234):Color.FromArgb(248,250,249));
  Color ink=Result=="correct"?Color.FromArgb(18,110,70):Result=="wrong"?Color.FromArgb(162,58,40):Color.FromArgb(30,46,53);
  using(var pen=new Pen(Selected||Result.Length>0?ink:Color.FromArgb(183,196,189),Selected?2:1)){
   var shape=new Rectangle(15,Math.Max(16,(Height-17)/2),17,17);if(Multiple)g.DrawRectangle(pen,shape);else g.DrawEllipse(pen,shape);
   if(Selected){using(var brush=new SolidBrush(ink)){var inside=Rectangle.Inflate(shape,-4,-4);if(Multiple)g.FillRectangle(brush,inside);else g.FillEllipse(brush,inside);}}
  }
  TextRenderer.DrawText(g,(Marker==null?"":Marker+".  ")+Body,Font,new Rectangle(47,10,Width-61,Height-16),ink,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter);
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-6,Height-6));
 }
}
public partial class QuizForm:Form {
 readonly List<Question> all;
 readonly Dictionary<string,Question> lookup;
 readonly string statePath,dataVersion;
 readonly bool testing;
 Progress state;
 List<Question> queue=new List<Question>();
 int index;
 bool evaluated,layoutBusy,saveErrorShown;
 Random random=new Random();
 ComboBox bank,type,category,mode,quality,roundSize;
 TextBox search,prompt,passage,feedback,fill;
 Label stats,position,meta,status;
 FlowLayoutPanel options,headerTools;
 TableLayoutPanel grid,body;
 Button submit,known,unknown,favorite,flag,previous,next,summary,evidence;
 List<ChoiceCard> choices=new List<ChoiceCard>();
 string testOutput;
 readonly Color ink=Color.FromArgb(30,46,53),green=Color.FromArgb(22,112,98),muted=Color.FromArgb(100,114,123);
 public QuizForm(List<Question> questions,string path,bool test,Progress supplied=null){
  all=questions;lookup=all.ToDictionary(q=>q.id);dataVersion=Data.Fingerprint(all);statePath=path;testing=test;state=supplied??ReadProgress();
  Text="中华文化 · 离线练习室 2.2";Size=new Size(1280,900);MinimumSize=new Size(1080,760);StartPosition=FormStartPosition.CenterScreen;
  BackColor=Color.FromArgb(245,247,246);Font=new Font("Microsoft YaHei UI",10);ForeColor=ink;AutoScaleMode=AutoScaleMode.Dpi;
  Build();SetupExtras();RestoreFilters();Rebuild(true);FormClosing+=(s,e)=>{SaveDraft();draftTimer.Stop();Save();};
 }
 Progress ReadProgress(){
  string source;var recovered=Data.Recover(statePath,out source);if(source!=null&&!testing)MessageBox.Show("已从备份恢复学习记录。\n"+source+"\n原损坏文件保留在记录目录中。","记录恢复");return recovered;
 }
 Entry E(Question q){Entry e;if(!state.Entries.TryGetValue(q.id,out e)){e=new Entry();state.Entries[q.id]=e;}return e;}
 string Cat(Question q){return String.IsNullOrEmpty(E(q).CategoryOverride)?q.category:E(q).CategoryOverride;}
 Question Current{get{return queue.Count==0?null:queue[index];}}
 SessionAnswer CurrentAnswer{get{SessionAnswer a;return Current!=null&&state.Session.Answers.TryGetValue(Current.id,out a)?a:null;}}
 Button B(string text,EventHandler action,bool primary=false,int width=110){var b=new Button{Text=text,Height=36,Width=width,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Margin=new Padding(0,0,8,0),BackColor=primary?green:Color.White,ForeColor=primary?Color.White:ink};b.FlatAppearance.BorderColor=primary?green:Color.FromArgb(212,221,216);b.Click+=action;return b;}
 ComboBox Box(Control parent,string label,IEnumerable<string> values){parent.Controls.Add(new Label{Text=label,Width=220,Height=22,ForeColor=muted,Margin=new Padding(0,10,0,0)});var cb=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=220,Margin=Padding.Empty};cb.Items.AddRange(values.Cast<object>().ToArray());cb.SelectedIndex=0;parent.Controls.Add(cb);return cb;}
 TextBox ReadBox(float size,Color color){return new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=color,Font=new Font(Font.FontFamily,size),ScrollBars=ScrollBars.Vertical,Margin=Padding.Empty};}
 void Build(){
  var sidebar=new Panel{Dock=DockStyle.Left,Width=270,BackColor=Color.White,Padding=new Padding(18,14,4,14)};Controls.Add(sidebar);
  var side=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};sidebar.Controls.Add(side);
  side.Controls.Add(new Label{Text="练 习 室  2.2",Font=new Font(Font.FontFamily,22,FontStyle.Bold),Width=220,Height=45,ForeColor=green});
  side.Controls.Add(new Label{Text="每一轮，都看得见进步。",ForeColor=muted,Width=220,Height=25});
  bank=Box(side,"题库",new[]{"中华传统文化专项","已核验基础题","文化大赛2000题","文化基础小测","阅读理解练习","全部题库"});
  type=Box(side,"题型",new[]{"单选题","多选题","判断题","填空题","阅读理解","全部题型"});
  bank.SelectionChangeCommitted+=(s,e)=>{if(bank.Text=="阅读理解练习")Set(type,"阅读理解");else if(type.Text=="阅读理解")Set(type,"单选题");};
  category=Box(side,"专题 · 支持个人调整",new[]{"全部专题","礼俗与生活","哲学与宗教","文学与典籍","艺术与建筑","古代科技","历史与制度","综合文化","阅读理解"});
  mode=Box(side,"练习方式",new[]{"顺序刷题","随机刷题","今日待复习","只看未做","错题复习","我的收藏","我的笔记"});
  quality=Box(side,"答案状态",new[]{"排除待复核","仅已核验","仅原资料答案","包含待复核","仅待复核"});
  roundSize=Box(side,"每轮题量",new[]{"20 题","10 题","50 题","全部匹配题目"});
  side.Controls.Add(new Label{Text="搜索题干或选项",Width=220,Height=22,ForeColor=muted,Margin=new Padding(0,10,0,0)});
  search=new TextBox{Width=220,Margin=new Padding(0,0,0,10)};side.Controls.Add(search);
  side.Controls.Add(B("开始 / 应用筛选",(s,e)=>Rebuild(false),true,220));
  var tools=new FlowLayoutPanel{Width=220,Height=130,Margin=new Padding(0,14,0,0)};
  tools.Controls.Add(B("备份记录",(s,e)=>Export(),false,102));tools.Controls.Add(B("恢复记录",(s,e)=>Import(),false,102));
  var folder=B("打开记录目录",(s,e)=>{Directory.CreateDirectory(Path.GetDirectoryName(statePath));System.Diagnostics.Process.Start("explorer.exe",Path.GetDirectoryName(statePath));},false,212);folder.Margin=new Padding(0,8,0,0);tools.Controls.Add(folder);side.Controls.Add(tools);
  var mistakes=B("错题本",(s,e)=>MistakesDialog(),true,102);mistakes.Margin=new Padding(0,8,8,0);tools.Controls.Add(mistakes);var reset=B("重置/恢复",(s,e)=>ResetDialog(),false,102);reset.Margin=new Padding(0,8,8,0);tools.Controls.Add(reset);
  side.Controls.Add(new Label{Text="数字 1—4 选择 · 回车提交/下一题\n错题需在不同日期连续答对两次\n原资料答案不等于已核验答案",Width=220,Height=67,ForeColor=muted,Font=new Font(Font.FontFamily,8.5f),Margin=new Padding(0,12,0,0)});

  var main=new Panel{Dock=DockStyle.Fill,Padding=new Padding(24,18,24,14)};Controls.Add(main);main.BringToFront();
  grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Margin=Padding.Empty};main.Controls.Add(grid);
  foreach(float h in new[]{62,32,50})grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
  grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,42));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,86));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,22));
  var heading=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};grid.Controls.Add(heading,0,0);
  var headerGrid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Margin=Padding.Empty};headerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));headerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,230));headerGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,37));headerGrid.RowStyles.Add(new RowStyle(SizeType.Percent,100));heading.Controls.Add(headerGrid);
  headerTools=new FlowLayoutPanel{Dock=DockStyle.Fill,Height=36,WrapContents=false,Margin=Padding.Empty};headerGrid.Controls.Add(headerTools,1,0);headerTools.Controls.Add(B("缩小",(s,e)=>ChangeReadingSize(-1),false,52));headerTools.Controls.Add(B("放大",(s,e)=>ChangeReadingSize(1),false,52));headerTools.Controls.Add(B("学习记录",(s,e)=>HistoryDialog(),false,100));
  headerGrid.Controls.Add(new Label{Text="把知识记住，也把依据看清。",Font=new Font(Font.FontFamily,20,FontStyle.Bold),Dock=DockStyle.Fill,AutoEllipsis=true,Margin=Padding.Empty},0,0);stats=new Label{Dock=DockStyle.Fill,ForeColor=muted,Margin=Padding.Empty};headerGrid.Controls.Add(stats,0,1);headerGrid.SetColumnSpan(stats,2);
  position=new Label{Dock=DockStyle.Fill,ForeColor=green,Font=new Font(Font.FontFamily,11,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty};grid.Controls.Add(position,0,1);
  meta=new Label{Dock=DockStyle.Fill,ForeColor=muted,Font=new Font(Font.FontFamily,9),AutoEllipsis=true,Margin=Padding.Empty};grid.Controls.Add(meta,0,2);
  body=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Margin=new Padding(0,0,0,10)};
  body.RowStyles.Add(new RowStyle(SizeType.Absolute,0));body.RowStyles.Add(new RowStyle(SizeType.Absolute,64));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));grid.Controls.Add(body,0,3);
  passage=ReadBox(11,Color.FromArgb(236,241,237));body.Controls.Add(passage,0,0);prompt=ReadBox(15,BackColor);prompt.Margin=new Padding(0,8,0,10);body.Controls.Add(prompt,0,1);
  options=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.White,Padding=new Padding(12),Margin=Padding.Empty};body.Controls.Add(options,0,2);
  body.SizeChanged+=(s,e)=>LayoutQuestion();options.SizeChanged+=(s,e)=>ResizeChoices();
  feedback=ReadBox(10,Color.FromArgb(233,240,235));feedback.Margin=new Padding(0,0,0,8);grid.Controls.Add(feedback,0,4);
  var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};grid.Controls.Add(footer,0,5);
  var actions=new FlowLayoutPanel{Width=800,Height=41,WrapContents=false,Margin=Padding.Empty};footer.Controls.Add(actions);
  submit=B("提交答案",(s,e)=>Submit(),true);known=B("本次答对",(s,e)=>SelfGrade(true));unknown=B("本次答错",(s,e)=>SelfGrade(false));favorite=B("☆ 收藏",(s,e)=>ToggleFavorite(),false,90);flag=B("标记存疑",(s,e)=>ToggleFlag(),false,90);evidence=B("核验与勘误",(s,e)=>EvidenceDialog(),false,110);
  foreach(var b in new[]{submit,known,unknown,favorite,flag,evidence})actions.Controls.Add(b);actions.Controls.Add(B("本题笔记",(s,e)=>NotesDialog(),false,86));
  var nav=new FlowLayoutPanel{Width=800,Height=41,WrapContents=false,Margin=Padding.Empty};footer.Controls.Add(nav);
  previous=B("← 上一题",(s,e)=>Navigate(-1));next=B("下一题 →",(s,e)=>Navigate(1));summary=B("结束本轮 / 总结",(s,e)=>ShowSummary(),false,150);
  nav.Controls.Add(previous);nav.Controls.Add(next);nav.Controls.Add(summary);nav.Controls.Add(B("调整本题专题",(s,e)=>ChangeCategory(),false,130));nav.Controls.Add(B("答题卡",(s,e)=>AnswerSheetDialog(),false,90));
  status=new Label{Dock=DockStyle.Fill,ForeColor=muted,Font=new Font(Font.FontFamily,8.5f),AutoEllipsis=true,Margin=Padding.Empty};grid.Controls.Add(status,0,6);
 }
 void Set(ComboBox cb,string value){if(cb.Items.Contains(value))cb.SelectedItem=value;}
 void RestoreFilters(){Set(bank,state.Bank);Set(type,state.Type);Set(category,state.Category);Set(mode,state.Mode);Set(quality,state.Quality);search.Text=state.Search??"";roundSize.SelectedIndex=state.RoundSize==10?1:state.RoundSize==50?2:state.RoundSize==0?3:0;}
 void RememberFilters(){state.Bank=bank.Text;state.Type=type.Text;state.Category=category.Text;state.Mode=mode.Text;state.Quality=quality.Text;state.Search=search.Text.Trim();state.RoundSize=roundSize.SelectedIndex==1?10:roundSize.SelectedIndex==2?50:roundSize.SelectedIndex==3?0:20;}
 string FilterKey(){return String.Join("|",new[]{state.Bank,state.Type,state.Category,state.Mode,state.Quality,state.Search,state.RoundSize.ToString()});}
 List<Question> Filter(){return all.Where(q=>{
  Entry entry=E(q);bool pending=q.review||entry.Flagged;
  bool approved=state.Quality=="包含待复核"||(state.Quality=="仅待复核"?pending:!pending&&(state.Quality!="仅已核验"||q.verification=="已核验")&&(state.Quality!="仅原资料答案"||q.verification=="原资料答案"));
  return (state.Bank=="全部题库"||q.banks.Contains(state.Bank))&&(state.Type=="全部题型"||q.type==state.Type)&&(state.Category=="全部专题"||Cat(q)==state.Category)&&approved
   &&(state.Search.Length==0||(q.prompt+String.Join(" ",q.options)).IndexOf(state.Search,StringComparison.OrdinalIgnoreCase)>=0)
   &&(state.Mode!="只看未做"||entry.Attempts==0)&&(state.Mode!="错题复习"||entry.Wrong)&&(state.Mode!="我的收藏"||entry.Favorite)&&(state.Mode!="我的笔记"||!String.IsNullOrWhiteSpace(entry.Note))&&(state.Mode!="今日待复习"||Data.Due(entry,DateTime.Today));
 }).ToList();}
 void Rebuild(bool resume){
  SaveDraft();if(!resume)ArchiveRound();
  RememberFilters();var old=state.Session;
  // 恢复的是上次的题目队列，不因作答后“未做/错题”集合变化而丢失本轮结果。
  if(resume&&old!=null&&old.Key==FilterKey()&&old.DataVersion==dataVersion&&old.QuestionIds.All(lookup.ContainsKey)){
   queue=old.QuestionIds.Select(id=>lookup[id]).ToList();index=Math.Max(0,Math.Min(queue.Count-1,old.Index));
  }else{
   var pool=Filter();
   if(state.Mode=="随机刷题")for(int i=pool.Count-1;i>0;i--){int j=random.Next(i+1);var temp=pool[i];pool[i]=pool[j];pool[j]=temp;}
   else if(state.Mode=="今日待复习")pool=pool.OrderBy(q=>E(q).NextReviewDate).ToList();
   else if(state.Mode=="顺序刷题"&&!resume){var practiced=new HashSet<string>(old==null?(IEnumerable<string>)new string[0]:old.QuestionIds);pool=pool.OrderBy(q=>practiced.Contains(q.id)?1:0).ThenBy(q=>E(q).Attempts>0?1:0).ToList();}
   int count=state.RoundSize==0?pool.Count:Math.Min(state.RoundSize,pool.Count);int start=0;
   if(resume&&old==null&&!String.IsNullOrEmpty(state.CurrentId)&&count>0){int pos=pool.FindIndex(q=>q.id==state.CurrentId);if(pos>=0)start=pos/count*count;}
   queue=pool.Skip(start).Take(count).ToList();index=0;
   state.Session=new Round{Key=FilterKey(),DataVersion=dataVersion,QuestionIds=queue.Select(q=>q.id).ToList()};
   if(resume){int pos=queue.FindIndex(q=>q.id==state.CurrentId);if(pos>=0)index=pos;}
  }
  Render();Save();
 }
 void ResizeChoices(){if(options==null)return;foreach(var card in choices){card.Width=Math.Max(120,options.ClientSize.Width-44);card.Height=card.FitHeight(card.Width);}if(fill!=null)fill.Width=Math.Max(120,options.ClientSize.Width-44);}
 void LayoutQuestion(){
  if(layoutBusy||body==null||Current==null)return;layoutBusy=true;
  try{
   int width=Math.Max(240,body.ClientSize.Width-24);int wanted=TextRenderer.MeasureText(Current.prompt,prompt.Font,new Size(width-20,0),TextFormatFlags.WordBreak).Height+28;
   bool reading=!String.IsNullOrEmpty(Current.passage);int ph=reading?Math.Max(92,Math.Min(155,body.Height/3)):0;
   body.RowStyles[0].Height=ph;body.RowStyles[1].Height=Math.Max(58,Math.Min(reading?115:165,wanted));passage.Visible=reading;ResizeChoices();
  }finally{layoutBusy=false;}
 }
 void UpdateStats(){int done=all.Count(q=>E(q).Attempts>0),wrong=all.Count(q=>E(q).Wrong),due=all.Count(q=>Data.Due(E(q),DateTime.Today));stats.Text=String.Format("题库 {0} 题    已练 {1}    待巩固 {2}    今日待复习 {3}",all.Count,done,wrong,due);}
 void Render(){
  foreach(Control c in options.Controls.Cast<Control>().ToArray())c.Dispose();options.Controls.Clear();choices.Clear();fill=null;UpdateStats();
  var q=Current;var attempt=CurrentAnswer;evaluated=attempt!=null&&attempt.Revealed;
  foreach(var b in new[]{submit,favorite,flag,previous,next,evidence,summary})b.Enabled=q!=null;known.Visible=unknown.Visible=false;
  if(q==null){position.Text="当前条件下没有题目";meta.Text="可选择全部题库、全部题型，或清空搜索后再开始。";prompt.Text="准备好，再开始一轮。";passage.Text="";passage.Visible=false;body.RowStyles[0].Height=0;feedback.Text="今日待复习只显示已经到期的错题。暂时没有到期题时，可改选“错题复习”。";grid.RowStyles[4].Height=64;status.Text="修改筛选后点击“开始 / 应用筛选”。";return;}
  state.CurrentId=q.id;var entry=E(q);int graded=state.Session.Answers.Values.Count(x=>x.Graded);
  position.Text=String.Format("第 {0} / {1} 题  ·  本轮已答 {2}  ·  {3} / {4}",index+1,queue.Count,graded,q.type,Cat(q));
  string badge=entry.Flagged?"个人存疑":q.verification;
  meta.Text="["+badge+"] "+q.locator+"   |   "+q.source+(q.verification=="已核验"?"\n核验日期："+q.verifiedDate:"\n按原资料答案练习，尚未逐题校对；可点击“核验与勘误”查看出处。");
  prompt.Text=Lines(q.prompt);passage.Text=Lines(q.passage??"");
  favorite.Text=entry.Favorite?"★ 已收藏":"☆ 收藏";flag.Text=entry.Flagged?"取消存疑":"标记存疑";
  previous.Enabled=index>0;next.Text=index==queue.Count-1?"本轮总结 →":"下一题 →";
  submit.Text=q.type=="填空题"?"查看参考答案":"提交答案";submit.Enabled=!evaluated;submit.Visible=!(q.type=="填空题"&&evaluated);
  if(q.type=="填空题"){
   fill=new TextBox{Multiline=true,Height=85,Font=new Font(Font.FontFamily,12),ScrollBars=ScrollBars.Vertical,Text=attempt==null?"":attempt.Answer,ReadOnly=evaluated};options.Controls.Add(fill);fill.TextChanged+=(s,e)=>ScheduleDraft();
   options.Controls.Add(new Label{Text="同义表述可自评；查看答案不会自动计分。",Width=600,Height=30,ForeColor=muted});
   known.Visible=unknown.Visible=evaluated;known.Enabled=unknown.Enabled=attempt==null||!attempt.Graded;
  }else{
   var values=q.type=="判断题"?new[]{"正确","错误"}:q.options;
   for(int i=0;i<values.Length;i++){
    int n=i;var card=new ChoiceCard{Marker=q.type=="判断题"?null:((char)('A'+i)).ToString(),Body=values[i],Multiple=q.type=="多选题",Font=ReadingFont(state.ReadingSize),AccessibleRole=q.type=="多选题"?AccessibleRole.CheckButton:AccessibleRole.RadioButton};
    card.AccessibleName=(card.Marker??"")+values[i];card.Picked+=(s,e)=>Pick(n);choices.Add(card);options.Controls.Add(card);
   }
  }
  if(evaluated){RestoreSelection(attempt.Answer);ShowAnswer(attempt.Graded?(attempt.Correct?"✓ 本次答对":"本次答错 · 请结合解析复习"):"对照参考答案，选择本次答对或答错。");if(attempt.Graded)MarkOptions(attempt.Answer);}
  else {RestoreDraft(attempt);feedback.Text=q.type=="填空题"?"先作答，再查看参考答案；快捷键 Ctrl + Enter 可查看。":"数字 1—4 选择，回车提交。多选题需完整选对。";feedback.ForeColor=muted;grid.RowStyles[4].Height=42;}
  status.Text=entry.WasWrong?String.Format("不同日期连续答对 {0}/2 次  ·  下次复习：{1}",Math.Min(2,entry.ConsecutiveCorrect),entry.NextReviewDate??"待安排"):"每轮每题只计分一次；未答题可跳过，结束时会列出未完成数量。";
  ApplyReadingFonts();LayoutQuestion();
 }
 static string Lines(string s){return (s??"").Replace("\r\n","\n").Replace("\n",Environment.NewLine);}
 void Pick(int i){if(Current==null||evaluated||i<0||i>=choices.Count)return;if(Current.type!="多选题")foreach(var c in choices)c.Selected=false;choices[i].Selected=Current.type=="多选题"?!choices[i].Selected:true;foreach(var c in choices)c.Invalidate();ScheduleDraft();}
 string Chosen(){if(Current.type=="判断题")return choices[0].Selected?"正确":choices[1].Selected?"错误":"";return String.Concat(choices.Select((c,i)=>c.Selected?((char)('A'+i)).ToString():""));}
 void RestoreSelection(string answer){for(int i=0;i<choices.Count;i++){choices[i].Selected=Current.type=="判断题"?(answer==(i==0?"正确":"错误")):(answer??"").Contains((char)('A'+i));choices[i].Locked=true;choices[i].Invalidate();}}
 void MarkOptions(string answer){for(int i=0;i<choices.Count;i++){string key=Current.type=="判断题"?(i==0?"正确":"错误"):((char)('A'+i)).ToString();bool correct=Current.type=="判断题"?Current.answer==key:Current.answer.Contains(key);bool picked=Current.type=="判断题"?answer==key:answer.Contains(key);choices[i].Result=correct?"correct":picked?"wrong":"";choices[i].Invalidate();}}
 string AnswerText(Question q){return q.type=="单选题"||q.type=="多选题"||q.type=="阅读理解"?q.answer+" · "+String.Join("；",q.answer.Select(c=>q.options[c-'A'])):q.answer;}
 void ShowAnswer(string headline){var q=Current;feedback.ForeColor=ink;feedback.Text=Lines(headline+"\n参考答案："+AnswerText(q)+"\n"+(String.IsNullOrEmpty(q.explanation)?"原资料未提供独立解析，可按来源回查。":q.explanation)+(String.IsNullOrEmpty(q.correctionNote)?"":"\n【勘误】"+q.correctionNote));int needed=TextRenderer.MeasureText(feedback.Text,feedback.Font,new Size(Math.Max(240,feedback.Width-28),0),TextFormatFlags.WordBreak).Height+20;grid.RowStyles[4].Height=Math.Max(74,Math.Min(Height<810?136:180,needed));feedback.SelectionStart=0;feedback.ScrollToCaret();LayoutQuestion();}
 void Submit(){
  if(Current==null||evaluated)return;var q=Current;
  string answer=q.type=="填空题"?fill.Text.Trim():Chosen();if(q.type!="填空题"&&answer.Length==0){status.Text="请先选择答案。";return;}
  var a=new SessionAnswer{Answer=answer,Revealed=true};state.Session.Answers[q.id]=a;
  if(q.type!="填空题"){a.Graded=true;a.Correct=Data.Grade(q,answer);Data.Record(E(q),a.Correct,answer,DateTime.Today);}
  Render();Save();
 }
 void SelfGrade(bool correct){if(Current==null||Current.type!="填空题"||CurrentAnswer==null||CurrentAnswer.Graded)return;var a=CurrentAnswer;a.Correct=correct;a.Graded=true;Data.Record(E(Current),correct,a.Answer,DateTime.Today);Render();Save();}
 void Navigate(int step){if(Current==null)return;SaveDraft();if(step>0&&index==queue.Count-1){ShowSummary();return;}index=Math.Max(0,Math.Min(queue.Count-1,index+step));Render();Save();}
 void ToggleFavorite(){if(Current==null)return;E(Current).Favorite=!E(Current).Favorite;favorite.Text=E(Current).Favorite?"★ 已收藏":"☆ 收藏";Save();}
 void ToggleFlag(){if(Current==null)return;E(Current).Flagged=!E(Current).Flagged;flag.Text=E(Current).Flagged?"取消存疑":"标记存疑";status.Text="个人存疑状态已保存，重新应用筛选时生效。";Save();}
 void EvidenceDialog(){
  if(Current==null)return;var q=Current;
  string content="答案状态："+q.verification+"\n原始来源："+q.source+" / "+q.locator+"\n\n";
  if(q.evidence!=null&&q.evidence.Length>0)foreach(var ev in q.evidence)content+=ev.title+"\n"+ev.url+"\n核对说明："+ev.comment+"\n\n";
  else content+="此题尚未逐题核验，参考答案取自原资料。\n";
  if(q.alsoSources!=null&&q.alsoSources.Length>0)content+="其他出处：\n"+String.Join("\n",q.alsoSources)+"\n\n";
  // 作答前不展示可直接泄露答案的核验说明或勘误。
  if(!evaluated)content="答案状态："+q.verification+"\n来源："+q.source+" / "+q.locator+"\n\n提交答案后可查看完整核验说明、原答案及勘误。";
  else content+="原资料答案："+q.originalAnswer+"\n当前采用答案："+q.answer+"\n"+q.correctionNote+"\n"+q.note;
  TextDialog("核验依据与勘误",content);
 }
 void TextDialog(string title,string content){using(var dialog=new Form{Text=title,Size=new Size(760,580),MinimumSize=new Size(520,360),StartPosition=FormStartPosition.CenterParent,Font=Font}){var text=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text=Lines(content),Font=Font};dialog.Controls.Add(text);dialog.ShowDialog(this);}}
 void ChangeCategory(){
  SaveDraft();
  if(Current==null)return;using(var dialog=new Form{Text="调整本题专题（仅保存在你的记录中）",Size=new Size(450,200),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var select=new ComboBox{Left=25,Top=25,Width=380,DropDownStyle=ComboBoxStyle.DropDownList};select.Items.AddRange(category.Items.Cast<string>().Where(x=>x!="全部专题").Cast<object>().ToArray());select.SelectedItem=Cat(Current);dialog.Controls.Add(select);
   var ok=B("保存专题",(s,e)=>{E(Current).CategoryOverride=select.Text;Save();dialog.Close();Render();},true);ok.Left=25;ok.Top=80;dialog.Controls.Add(ok);dialog.ShowDialog(this);
  }
 }
 string SummaryText(){
  int answered=state.Session.Answers.Values.Count(a=>a.Graded),correct=state.Session.Answers.Values.Count(a=>a.Graded&&a.Correct);
  var s=new StringBuilder();s.AppendLine(String.Format("本轮 {0} 题 · 已答 {1} · 未完成 {2} · 答对 {3}",queue.Count,answered,queue.Count-answered,correct));s.AppendLine(answered==0?"正确率：暂无（还未完成作答）":"正确率："+(100.0*correct/answered).ToString("0.0")+"%（按已答题计算）");
  s.AppendLine("\n专题表现：");foreach(var group in queue.GroupBy(Cat)){var graded=group.Where(q=>state.Session.Answers.ContainsKey(q.id)&&state.Session.Answers[q.id].Graded).ToList();int right=graded.Count(q=>state.Session.Answers[q.id].Correct);s.AppendLine(group.Key+"："+(graded.Count==0?"未作答":right+" / "+graded.Count+"，正确率 "+(100.0*right/graded.Count).ToString("0")+"%"));}
  s.AppendLine("\n本轮错题：");var wrong=queue.Where(q=>state.Session.Answers.ContainsKey(q.id)&&state.Session.Answers[q.id].Graded&&!state.Session.Answers[q.id].Correct).ToList();if(wrong.Count==0)s.AppendLine("暂无。");else foreach(var q in wrong)s.AppendLine("• "+q.prompt);
  s.AppendLine("\n复习规则：错题在不同日期连续答对两次才移出待巩固；答错安排次日复习，之后依次间隔 3、7、14、30、60 天。提前复习允许答题，但同日重复答对不推进复习阶段。");return s.ToString();
 }
 void ShowSummary(){
  if(Current==null)return;SaveDraft();var text=SummaryText();ArchiveRound();Save();
  using(var dialog=new Form{Text="本轮练习总结",Size=new Size(820,640),MinimumSize=new Size(680,480),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(10)};dialog.Controls.Add(buttons);
   buttons.Controls.Add(B("继续查看本轮",(s,e)=>dialog.Close(),false,140));
   buttons.Controls.Add(B("开始新一轮",(s,e)=>{dialog.Close();Rebuild(false);},true,140));
   buttons.Controls.Add(B("重练本轮错题",(s,e)=>{var wrong=queue.Where(q=>state.Session.Answers.ContainsKey(q.id)&&state.Session.Answers[q.id].Graded&&!state.Session.Answers[q.id].Correct).ToList();if(wrong.Count==0){MessageBox.Show("本轮没有错题。");return;}dialog.Close();SaveDraft();ArchiveRound();queue=wrong;index=0;state.Session=new Round{Key=FilterKey(),DataVersion=dataVersion,QuestionIds=wrong.Select(q=>q.id).ToList()};Render();Save();},false,150));
   var view=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text=Lines(text),Font=Font};dialog.Controls.Add(view);view.BringToFront();
   if(testing){dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show(this);Application.DoEvents();using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(0,0,dialog.Width,dialog.Height));bitmap.Save(Path.Combine(testOutput,"06-summary.png"));}dialog.Close();}else dialog.ShowDialog(this);
  }
 }
 void Save(){
  if(draftTimer!=null)draftTimer.Stop();
  if(state.Session!=null){state.Session.Index=index;if(Current!=null)state.CurrentId=Current.id;}
  try{Data.Save(statePath,state);}catch(Exception ex){if(status!=null)status.Text="记录保存失败："+ex.Message;if(!saveErrorShown&&!testing){saveErrorShown=true;MessageBox.Show("记录尚未写入磁盘，请先用“备份记录”导出。\n"+ex.Message,"保存失败");}}
 }
 void Export(){SaveDraft();using(var dialog=new SaveFileDialog{Filter="JSON 记录|*.json",FileName="中华文化学习记录-"+DateTime.Now.ToString("yyyyMMdd")+".json"})if(dialog.ShowDialog()==DialogResult.OK)try{File.WriteAllText(dialog.FileName,Data.Json().Serialize(state),new UTF8Encoding(false));status.Text="记录已备份。";}catch(Exception ex){MessageBox.Show(ex.Message,"导出失败");}}
 void Import(){using(var dialog=new OpenFileDialog{Filter="JSON 记录|*.json"})if(dialog.ShowDialog()==DialogResult.OK)try{var incoming=Data.Load(dialog.FileName);if(MessageBox.Show("恢复会替换当前学习记录；当前记录先保存为 before-import.json。是否继续？","恢复记录",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;SaveDraft();Directory.CreateDirectory(Path.GetDirectoryName(statePath));File.WriteAllText(Path.Combine(Path.GetDirectoryName(statePath),"before-import.json"),Data.Json().Serialize(state),new UTF8Encoding(false));ApplyLoadedState(incoming);}catch(Exception ex){MessageBox.Show(ex.Message,"恢复失败");}}
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){
  bool editable=(search.ContainsFocus||(fill!=null&&fill.ContainsFocus&&!fill.ReadOnly));bool combo=new[]{bank,type,category,mode,quality,roundSize}.Any(cb=>cb.ContainsFocus);
  if(keyData==(Keys.Control|Keys.Enter)){if(!evaluated)Submit();return true;}
  if(keyData==Keys.Enter&&search.ContainsFocus){Rebuild(false);return true;}
  if(keyData==Keys.Enter&&known.ContainsFocus&&known.Enabled){SelfGrade(true);return true;}
  if(keyData==Keys.Enter&&unknown.ContainsFocus&&unknown.Enabled){SelfGrade(false);return true;}
  if(!editable&&!combo){
   int digit=keyData>=Keys.D1&&keyData<=Keys.D4?(int)keyData-(int)Keys.D1:keyData>=Keys.NumPad1&&keyData<=Keys.NumPad4?(int)keyData-(int)Keys.NumPad1:-1;
   if(digit>=0){Pick(digit);return true;}
   if(keyData==Keys.Enter){if(!evaluated)Submit();else if(CurrentAnswer!=null&&CurrentAnswer.Graded)Navigate(1);return true;}
   if(keyData==Keys.Right){Navigate(1);return true;}if(keyData==Keys.Left){Navigate(-1);return true;}
  }
  return base.ProcessCmdKey(ref msg,keyData);
 }
 void Snapshot(string output,string name){PerformLayout();Application.DoEvents();LayoutQuestion();options.PerformLayout();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(Path.Combine(output,name+".png"));}}
 public void RunChecks(string output){
  testOutput=output;
  var checks=new List<string>();Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("检查失败："+name);checks.Add(name);};
  check(queue.Count==20&&queue.All(x=>!x.review),"默认20题且排除待复核");var q=Current;Submit();check(E(q).Attempts==0,"空白不计分");Pick(q.answer[0]-'A');Submit();Submit();check(E(q).Attempts==1&&CurrentAnswer.Correct,"单选与重复提交保护");
  check(choices.Count(x=>x.Result=="correct")==1,"正确选项标绿");ToggleFavorite();check(E(q).Favorite,"收藏保存");Navigate(1);Navigate(-1);check(Current.id==q.id&&evaluated&&E(q).Attempts==1,"本轮返回已答题不重复计分");
  Snapshot(output,"01-answer");Size=new Size(1080,760);Snapshot(output,"02-small");check(submit.Parent.ClientRectangle.Contains(submit.Bounds)&&next.Parent.ClientRectangle.Contains(next.Bounds),"最小窗口按钮可见");check(headerTools.Parent.ClientRectangle.Contains(headerTools.Bounds)&&headerTools.Height>=36&&headerTools.Controls.Cast<Control>().All(c=>headerTools.ClientRectangle.Contains(c.Bounds)),"字号与学习记录工具可见");
  type.SelectedItem="多选题";bank.SelectedItem="文化大赛2000题";Rebuild(false);q=Current;foreach(char letter in q.answer)Pick(letter-'A');Submit();check(CurrentAnswer.Correct,"多选完整匹配");Navigate(1);q=Current;Pick(0);Submit();check(CurrentAnswer.Correct==Data.Grade(q,"A"),"多选漏选判断");
  type.SelectedItem="判断题";Rebuild(false);q=Current;Pick(q.answer=="正确"?0:1);Submit();check(CurrentAnswer.Correct,"判断题");
  type.SelectedItem="填空题";Rebuild(false);q=Current;fill.Text="测试同义答案";Submit();check(!CurrentAnswer.Graded&&E(q).Attempts==0,"填空不自动评分");unknown.Focus();Message fillKey=new Message();check(ProcessCmdKey(ref fillKey,Keys.Enter)&&CurrentAnswer.Graded,"填空键盘自评");SelfGrade(false);SelfGrade(false);check(E(q).Attempts==1&&E(q).Wrong,"填空自评且防重复");
  var entry=new Entry();DateTime day=new DateTime(2026,11,1);Data.Record(entry,false,"A",day);check(entry.Wrong&&entry.NextReviewDate=="2026-11-02","错题安排次日复习");Data.Record(entry,true,"B",day.AddDays(1));Data.Record(entry,true,"B",day.AddDays(1));check(entry.Wrong&&entry.ConsecutiveCorrect==1,"同日反复答对不掌握");Data.Record(entry,true,"B",day.AddDays(4));check(!entry.Wrong&&entry.Mastered&&entry.NextReviewDate=="2026-11-12","跨日连续两次掌握与七日复习");Data.Record(entry,false,"A",day.AddDays(5));check(entry.Wrong&&!entry.Mastered&&entry.ConsecutiveCorrect==0,"再次答错重置掌握");check(Data.Due(entry,day.AddDays(6)),"到期判断");
  string legacy=Path.Combine(output,"legacy-v1.json");File.WriteAllText(legacy,"{\"Version\":1,\"Entries\":{\"old\":{\"Attempts\":3,\"Correct\":1,\"Wrong\":true,\"Favorite\":true}},\"Bank\":\"文化大赛2000题\"}");var migrated=Data.Load(legacy);check(migrated.Version==2&&migrated.Entries["old"].Attempts==3&&migrated.Entries["old"].Favorite&&migrated.Entries["old"].Wrong,"v1无损迁移");
  bank.SelectedItem="全部题库";type.SelectedItem="全部题型";mode.SelectedItem="错题复习";Rebuild(false);check(queue.Count>0&&queue.All(x=>E(x).Wrong),"错题筛选");mode.SelectedItem="我的收藏";Rebuild(false);check(queue.Count==1&&E(Current).Favorite,"收藏筛选");
  mode.SelectedItem="只看未做";Rebuild(false);check(queue.All(x=>E(x).Attempts==0),"未做筛选");q=Current;Pick(q.type=="判断题"?(q.answer=="正确"?0:1):0);if(q.type!="填空题")Submit();Save();string savedId=Current.id;var savedRound=state.Session;Rebuild(true);check(Current.id==savedId&&state.Session.Id==savedRound.Id,"恢复未做队列不丢本轮答案");
  mode.SelectedItem="随机刷题";roundSize.SelectedItem="50 题";Rebuild(false);check(queue.Count==50&&queue.Select(x=>x.id).Distinct().Count()==50,"50题随机无重复");quality.SelectedItem="仅已核验";mode.SelectedItem="顺序刷题";Rebuild(false);check(queue.Count>0&&queue.All(x=>x.verification=="已核验"),"仅核验题筛选");quality.SelectedItem="仅待复核";Rebuild(false);check(queue.All(x=>x.review||E(x).Flagged),"待复核隔离");
  quality.SelectedItem="排除待复核";bank.SelectedItem="阅读理解练习";type.SelectedItem="阅读理解";Rebuild(false);check(queue.Count==9&&queue.All(x=>!String.IsNullOrEmpty(x.passage)),"原创阅读九题");q=Current;Pick(q.answer[0]-'A');Submit();Snapshot(output,"03-reading");ShowSummary();check(SummaryText().Contains("未完成 8")&&state.History.Count(h=>h.Id==state.Session.Id)==1,"总结正确率与未答统计");ShowSummary();check(state.History.Count(h=>h.Id==state.Session.Id)==1,"重复查看总结不重复记历史");
  Snapshot(output,"04-reading-answer");string summaryPath=Path.Combine(output,"round-summary.txt");File.WriteAllText(summaryPath,SummaryText(),Encoding.UTF8);
  bank.SelectedItem="全部题库";type.SelectedItem="单选题";roundSize.SelectedItem="10 题";Rebuild(false);check(queue.Count==10,"10题轮次");submit.Focus();Message keyMessage=new Message();Keys answerKey=(Keys)((int)Keys.D1+Current.answer[0]-'A');check(ProcessCmdKey(ref keyMessage,answerKey)&&Chosen()==Current.answer,"数字键选择");ProcessCmdKey(ref keyMessage,Keys.Enter);check(evaluated&&CurrentAnswer.Graded,"回车提交");int beforeIndex=index;ProcessCmdKey(ref keyMessage,Keys.Enter);check(index==beforeIndex+1,"回车下一题");
  var dueEntry=E(Current);dueEntry.WasWrong=true;dueEntry.Wrong=true;dueEntry.NextReviewDate=DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");mode.SelectedItem="今日待复习";Rebuild(false);check(queue.Count>0&&queue.All(x=>Data.Due(E(x),DateTime.Today)),"今日待复习筛选");
  mode.SelectedItem="顺序刷题";roundSize.SelectedItem="全部匹配题目";Rebuild(false);var longest=queue.OrderByDescending(x=>x.prompt.Length+x.options.Sum(y=>y.Length)).First();index=queue.FindIndex(x=>x.id==longest.id);Render();Snapshot(output,"05-long-options");check(options.AutoScroll&&choices.All(x=>x.Height>=53),"长选项自动换行滚动");
  var corrected=all.First(x=>!String.IsNullOrEmpty(x.correctionNote));check(corrected.originalAnswer!=corrected.answer&&corrected.evidence.Length>0,"勘误保留原答案与依据");
  q=Current;E(q).CategoryOverride="礼俗与生活";Save();var restored=Data.Load(statePath);check(restored.Entries[q.id].CategoryOverride=="礼俗与生活"&&restored.Session.QuestionIds.Count==queue.Count,"专题调整及完整会话保存");
  bank.SelectedItem="中华传统文化专项";type.SelectedItem="单选题";mode.SelectedItem="顺序刷题";roundSize.SelectedItem="20 题";Rebuild(false);q=Current;Pick(q.answer[0]-'A');Submit();JumpToQuestion(1);Pick(0);string draftId=Current.id;int draftAttempts=E(Current).Attempts;JumpToQuestion(2);JumpToQuestion(1);check(Chosen()=="A"&&!evaluated&&E(Current).Attempts==draftAttempts,"未提交选择切题保留且不计分");Save();check(Data.Load(statePath).Session.Answers[draftId].Answer=="A"&&!Data.Load(statePath).Session.Answers[draftId].Graded,"选择草稿落盘");check(NextUnfinished()==2,"定位下一道未完成题");AnswerSheetDialog();
  string roundId=state.Session.Id;type.SelectedItem="填空题";bank.SelectedItem="文化大赛2000题";Rebuild(false);check(state.History.Any(h=>h.Id==roundId),"开始新轮自动归档已答成绩");q=Current;fill.Text="未提交的中文填空草稿";Navigate(1);Navigate(-1);check(fill.Text=="未提交的中文填空草稿"&&!evaluated&&E(q).Attempts==0,"填空草稿切题恢复");Save();state=Data.Load(statePath);Rebuild(true);check(fill.Text=="未提交的中文填空草稿"&&!CurrentAnswer.Revealed,"填空草稿重启恢复");
  E(Current).Note="记住概念区别，复习时回查原文。";Save();check(Data.Load(statePath).Entries[Current.id].Note.Contains("概念区别"),"个人笔记保存");mode.SelectedItem="我的笔记";bank.SelectedItem="全部题库";type.SelectedItem="全部题型";Rebuild(false);check(queue.Count==1&&E(Current).Note.Contains("概念区别"),"我的笔记筛选");int oldSize=state.ReadingSize;ChangeReadingSize(1);check(state.ReadingSize==oldSize+1&&Math.Abs(prompt.Font.Size-(oldSize+5))<0.1&&Data.Load(statePath).ReadingSize==state.ReadingSize,"阅读字号调整并保存");NotesDialog();HistoryDialog();
  var reversedClock=new Entry();Data.Record(reversedClock,false,"A",day);Data.Record(reversedClock,true,"B",day.AddDays(2));string dueBefore=reversedClock.NextReviewDate;Data.Record(reversedClock,true,"B",day.AddDays(1));check(reversedClock.ConsecutiveCorrect==1&&reversedClock.NextReviewDate==dueBefore,"日期倒退不增加掌握阶段");
  string store=Path.Combine(output,"store-check","progress.json");var fixture=new Progress();fixture.Entries["fixture"]=new Entry{Attempts=2,Correct=1,Note="备份样本"};Data.Save(store,fixture);Data.Save(store,fixture);string backups=Path.Combine(Path.GetDirectoryName(store),"backups");check(File.Exists(Path.Combine(backups,"progress-"+DateTime.Today.ToString("yyyy-MM-dd")+".json")),"每日有效记录备份");
  string expired=Path.Combine(backups,"progress-"+DateTime.Today.AddDays(-8).ToString("yyyy-MM-dd")+".json");File.WriteAllText(expired,Data.Json().Serialize(fixture));string keep=Path.Combine(backups,"keep.txt");File.WriteAllText(keep,"保留其他文件");Data.DailyBackup(store,DateTime.Today.AddDays(1));check(!File.Exists(expired)&&File.Exists(keep),"备份保留七天且不删除其他文件");File.WriteAllText(store,"broken main");File.WriteAllText(store+".bak","broken backup");string recoverySource;var recovered=Data.Recover(store,out recoverySource);check(recovered.Entries["fixture"].Note=="备份样本"&&recoverySource.Contains("backups")&&File.ReadAllText(store)=="broken main","主记录和即时备份损坏时从每日副本恢复");Data.Save(store,recovered);check(Data.Load(store).Entries["fixture"].Attempts==2,"损坏恢复后可正常保存");
  check(lookup.Count==2394,"题库及题目ID未变");
  RunNotebookChecks(output,check);
  File.WriteAllText(Path.Combine(output,"test-results.json"),Data.Json().Serialize(new{passed=true,questions=all.Count,checks=checks,version="2.2"}),Encoding.UTF8);
 }
}
static class Program {
 [STAThread]static void Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{
   var qs=Data.LoadQuestions();if(args.Length==2&&args[0]=="--self-test"){
    string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);using(var form=new QuizForm(qs,Path.Combine(output,"test-progress.json"),true,new Progress())){
     form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Shown+=(s,e)=>{try{form.RunChecks(output);}catch(Exception ex){File.WriteAllText(Path.Combine(output,"test-failure.txt"),ex.ToString());Environment.ExitCode=1;}finally{form.Close();}};Application.Run(form);
    }return;
   }
   string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChineseCultureQuiz","progress.json");
   using(var single=new System.Threading.Mutex(false,"Local\\ChineseCultureQuizOffline")){
    bool owns=false;try{try{owns=single.WaitOne(0,false);}catch(System.Threading.AbandonedMutexException){owns=true;}if(!owns){MessageBox.Show("练习室已打开，请切换到现有窗口。","中华文化练习室");return;}Application.Run(new QuizForm(qs,path,false));}finally{if(owns)single.ReleaseMutex();}
   }
  }catch(Exception ex){MessageBox.Show("程序未能启动：\n"+ex.Message,"中华文化练习室");Environment.ExitCode=1;}
 }
}
