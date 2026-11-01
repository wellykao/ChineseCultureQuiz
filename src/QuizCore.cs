// Copyright (c) 2026 wellykao and other lawful rights holders.
// SPDX-License-Identifier: LicenseRef-Proprietary
// This notice does not cover third-party questions or study materials.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

public class Evidence { public string title,url,comment; }
public class Question {
 public string id,prompt,answer,explanation,type,category,source,locator,bank,note;
 public string originalAnswer,originalExplanation,verification,verifiedDate,correctionNote,passage;
 public string[] options,alsoSources,banks;
 public Evidence[] evidence;
 public bool review;
}
public class Entry {
 public int Attempts,Correct,ConsecutiveCorrect,ReviewStage,MistakeCount;
 public bool Wrong,Favorite,Flagged,WasWrong,Mastered,MistakeArchived,ManualMistake;
 public string LastAnswer,LastSuccessDate,NextReviewDate,CategoryOverride,Note,LastWrongAnswer,LastWrongDate,MistakeReason;
}
public class SessionAnswer { public string Answer; public bool Revealed,Graded,Correct; }
public class Round {
 public string Id=Guid.NewGuid().ToString("N"),Key,DataVersion;
 public List<string> QuestionIds=new List<string>();
 public Dictionary<string,SessionAnswer> Answers=new Dictionary<string,SessionAnswer>();
 public int Index;
}
public class RoundResult { public string Id,Date,Summary; public int Answered,Correct; }
public class Progress {
 public int Version=2,RoundSize=20,ReadingSize=11,MistakeSchema=1;
 public string LastResetBackup,LastResetLabel;
 public Dictionary<string,Entry> Entries=new Dictionary<string,Entry>();
 public string Bank="中华传统文化专项",Type="单选题",Category="全部专题",Mode="顺序刷题",Quality="排除待复核",Search="",CurrentId="";
 public Round Session;
 public List<RoundResult> History=new List<RoundResult>();
}
public static class Data {
 public static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=50000000,RecursionLimit=100};}
 public static List<Question> LoadQuestions(){
  using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("questions.json"))
  using(var r=new StreamReader(s,Encoding.UTF8))return Json().Deserialize<List<Question>>(r.ReadToEnd());
 }
 public static string Fingerprint(List<Question> qs){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(String.Join("|",qs.Select(q=>q.id+":"+q.answer))))).Replace("-","");}
 public static string Normalize(string s){return new string((s??"").Where(c=>!Char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();}
 public static bool Grade(Question q,string answer){
  if(q.type=="填空题")throw new InvalidOperationException("填空题采用自评");
  return String.Concat(Normalize(answer).Distinct().OrderBy(c=>c))==String.Concat(Normalize(q.answer).Distinct().OrderBy(c=>c));
 }
 public static bool Due(Entry e,DateTime today){DateTime due;return e.WasWrong&&!e.MistakeArchived&&DateTime.TryParseExact(e.NextReviewDate,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out due)&&due.Date<=today.Date;}
 public static void Record(Entry e,bool correct,string answer,DateTime today){
  string day=today.ToString("yyyy-MM-dd");e.Attempts++;if(correct)e.Correct++;e.LastAnswer=answer;
  if(!correct){e.MistakeCount++;e.LastWrongAnswer=answer;e.LastWrongDate=day;e.MistakeArchived=false;e.WasWrong=true;e.Wrong=true;e.Mastered=false;e.ConsecutiveCorrect=0;e.ReviewStage=0;e.LastSuccessDate=null;e.NextReviewDate=today.AddDays(1).ToString("yyyy-MM-dd");return;}
  if(e.MistakeArchived)return;
  if(!e.WasWrong){e.Mastered=true;return;}
  DateTime previousSuccess;
  bool later=!DateTime.TryParseExact(e.LastSuccessDate,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out previousSuccess)||today.Date>previousSuccess.Date;
  if(later){
   e.ConsecutiveCorrect++;e.LastSuccessDate=day;e.ReviewStage=Math.Min(5,e.ReviewStage+1);
   int[] intervals={1,3,7,14,30,60};e.NextReviewDate=today.AddDays(intervals[e.ReviewStage]).ToString("yyyy-MM-dd");
  }
  e.Mastered=e.ConsecutiveCorrect>=2;e.Wrong=!e.Mastered;
 }
 public static void Save(string path,Progress progress){
  Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
  DailyBackup(path,DateTime.Today);
  File.WriteAllText(temp,Json().Serialize(progress),new UTF8Encoding(false));
  if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
 }
 public static void DailyBackup(string path,DateTime today){
  if(!File.Exists(path))return;
  string folder=Path.Combine(Path.GetDirectoryName(path),"backups");string dest=Path.Combine(folder,"progress-"+today.ToString("yyyy-MM-dd")+".json");if(File.Exists(dest))return;
  // 仅归档能解析的旧记录；恢复过程不得把损坏文件归档为正常副本。
  try{Load(path);}catch(Exception){return;}
  Directory.CreateDirectory(folder);File.Copy(path,dest);
  foreach(string candidate in Directory.GetFiles(folder,"progress-*.json")){
   string stamp=Path.GetFileNameWithoutExtension(candidate).Substring("progress-".Length);DateTime date;
   if(DateTime.TryParseExact(stamp,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out date)&&date.Date<today.Date.AddDays(-6))File.Delete(candidate);
  }
 }
 public static Progress Recover(string path,out string recoverySource){
  recoverySource=null;bool existed=File.Exists(path);if(existed)try{return Load(path);}catch(Exception){}
  var candidates=new List<string>();if(File.Exists(path+".bak"))candidates.Add(path+".bak");
  string folder=Path.Combine(Path.GetDirectoryName(path),"backups");if(Directory.Exists(folder))candidates.AddRange(Directory.GetFiles(folder,"progress-*.json").Where(x=>System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(x),@"^progress-\d{4}-\d{2}-\d{2}\.json$")).OrderByDescending(x=>x));
  foreach(string candidate in candidates)try{
   var restored=Load(candidate);
   if(existed){string preserved=Path.Combine(Path.GetDirectoryName(path),"recovered-original-"+Guid.NewGuid().ToString("N")+".json");File.Copy(path,preserved);}
   recoverySource=candidate;return restored;
  }catch(Exception){}
  if(existed||candidates.Count>0)throw new InvalidDataException("学习记录和可用备份均无法读取，已保留原文件："+path);
  return new Progress();
 }
 public static Progress Load(string path){
  string text=File.ReadAllText(path,Encoding.UTF8);
  var schema=Json().DeserializeObject(text) as Dictionary<string,object>;
  if(schema==null||!schema.ContainsKey("Version")||!schema.ContainsKey("Entries"))throw new InvalidDataException("文件不是有效的学习记录");
  var p=Json().Deserialize<Progress>(text);
  if(p==null||(p.Version!=1&&p.Version!=2)||p.Entries==null||p.Entries.Any(x=>x.Value==null||x.Value.Attempts<0||x.Value.Correct<0||x.Value.Correct>x.Value.Attempts))throw new InvalidDataException("记录结构或版本无效");
  if(p.Version==1){
   foreach(var e in p.Entries.Values){e.WasWrong=e.Wrong;e.Mastered=e.Attempts>0&&!e.Wrong;if(e.Wrong)e.NextReviewDate=DateTime.Today.ToString("yyyy-MM-dd");}
   p.Session=null;p.Version=2;
  }
  if(!schema.ContainsKey("MistakeSchema"))foreach(var e in p.Entries.Values){if(e.WasWrong||e.Wrong)e.MistakeCount=Math.Max(1,e.Attempts-e.Correct);if(e.Wrong&&String.IsNullOrEmpty(e.LastSuccessDate))e.LastWrongAnswer=e.LastAnswer;}
  p.MistakeSchema=1;
  if(p.History==null)p.History=new List<RoundResult>();p.History=p.History.Where(h=>h!=null&&!String.IsNullOrEmpty(h.Id)&&h.Answered>=0&&h.Correct>=0&&h.Correct<=h.Answered).ToList();
  p.ReadingSize=Math.Max(10,Math.Min(16,p.ReadingSize));
  if(!new[]{0,10,20,50}.Contains(p.RoundSize))p.RoundSize=20;
  if(p.Session!=null&&(p.Session.QuestionIds==null||p.Session.QuestionIds.Any(String.IsNullOrEmpty)||p.Session.Answers==null||p.Session.Answers.Any(x=>x.Value==null)||p.Session.QuestionIds.Distinct().Count()!=p.Session.QuestionIds.Count))p.Session=null;
  if(p.Session!=null){var ids=new HashSet<string>(p.Session.QuestionIds);p.Session.Answers=p.Session.Answers.Where(x=>ids.Contains(x.Key)&&x.Value.Answer!=null&&(!x.Value.Graded||x.Value.Revealed)).ToDictionary(x=>x.Key,x=>x.Value);}
  return p;
 }
}
