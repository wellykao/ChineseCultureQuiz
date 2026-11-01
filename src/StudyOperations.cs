// Copyright (c) 2026 wellykao and other lawful rights holders.
// SPDX-License-Identifier: LicenseRef-Proprietary
// See LICENSE and NOTICE.md; rights apply only where legally held.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public enum ResetKind { ReplayRound, SelectedProgress, AllProgress, ClearMistakes, Factory }
public static class StudyOperations {
 public static Progress Clone(Progress p){return Data.Json().Deserialize<Progress>(Data.Json().Serialize(p));}
 public static void AddMistake(Entry e,DateTime today){e.WasWrong=true;e.Wrong=true;e.ManualMistake=true;e.MistakeArchived=false;e.Mastered=false;e.ConsecutiveCorrect=0;e.ReviewStage=0;e.LastSuccessDate=null;e.NextReviewDate=today.ToString("yyyy-MM-dd");}
 public static void Master(Entry e,DateTime today){e.WasWrong=true;e.Wrong=false;e.MistakeArchived=false;e.Mastered=true;e.ConsecutiveCorrect=2;e.LastSuccessDate=today.ToString("yyyy-MM-dd");e.NextReviewDate=today.AddDays(7).ToString("yyyy-MM-dd");e.ReviewStage=2;}
 public static void Archive(Entry e){e.MistakeArchived=true;e.Wrong=false;e.NextReviewDate=null;}
 public static Progress Transform(Progress p,ResetKind kind,IEnumerable<string> scope){
  var ids=new HashSet<string>(scope);
  if(kind==ResetKind.Factory)return new Progress();
  if(kind==ResetKind.AllProgress)ids.UnionWith(p.Entries.Keys);
  if(kind==ResetKind.ReplayRound){if(p.Session==null)throw new InvalidOperationException("没有可重练的当前轮次");var old=p.Session;p.Session=new Round{Key=old.Key,DataVersion=old.DataVersion,QuestionIds=old.QuestionIds.ToList()};return p;}
  foreach(string id in ids){Entry e;if(!p.Entries.TryGetValue(id,out e))continue;
   if(kind==ResetKind.ClearMistakes){if(!e.WasWrong&&!e.Wrong&&!e.MistakeArchived)continue;e.Wrong=false;e.WasWrong=false;e.Mastered=false;e.MistakeCount=0;e.ManualMistake=false;e.MistakeArchived=false;e.ConsecutiveCorrect=0;e.ReviewStage=0;e.NextReviewDate=null;e.LastSuccessDate=null;e.LastWrongAnswer=null;e.LastWrongDate=null;e.MistakeReason=null;}
   else p.Entries[id]=new Entry{Favorite=e.Favorite,Flagged=e.Flagged,CategoryOverride=e.CategoryOverride,Note=e.Note};
  }
  if(kind==ResetKind.AllProgress){p.History.Clear();p.Session=null;p.CurrentId="";p.Mode="顺序刷题";}
  else if(kind==ResetKind.SelectedProgress&&p.Session!=null){foreach(string id in ids)p.Session.Answers.Remove(id);p.History.RemoveAll(h=>h.Id==p.Session.Id);}
  return p;
 }
 public static Progress Commit(string path,Progress current,Func<Progress,Progress> transform,string label){
  string folder=Path.Combine(Path.GetDirectoryName(path),"operations");Directory.CreateDirectory(folder);
  string name="before-reset-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json";
  string backup=Path.Combine(folder,name);File.WriteAllText(backup,Data.Json().Serialize(current),new UTF8Encoding(false));Data.Load(backup);
  var next=transform(Clone(current));next.LastResetBackup=name;next.LastResetLabel=label;Data.Save(path,next);return next;
 }
 public static Progress Undo(string path,Progress current){
  string name=current.LastResetBackup;
  if(String.IsNullOrEmpty(name)||name!=Path.GetFileName(name)||!name.StartsWith("before-reset-")||!name.EndsWith(".json"))throw new InvalidDataException("没有可恢复的操作副本");
  var target=Data.Load(Path.Combine(Path.GetDirectoryName(path),"operations",name));
  return Commit(path,current,p=>target,"恢复操作前记录");
 }
}
