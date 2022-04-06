﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KBEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkySwordKill.Next.Extension;
using SkySwordKill.Next.StaticFace;
using UnityEngine;
using UnityEngine.Events;
using YSGame;
using GameObject = UnityEngine.GameObject;

namespace SkySwordKill.Next
{
    public partial class DialogAnalysis
    {
        #region 字段

        public static AvatarNextData AvatarNextData = new AvatarNextData();

        #endregion

        #region 属性



        #endregion

        #region 回调方法



        #endregion

        #region 公共方法

        public static string AnalysisInlineScript(string text,DialogEnvironment env)
        {
            StringBuilder finallyText = new StringBuilder(text);
            Regex regex = new Regex(@"\[&(?<expression>[\s\S]*?)&]");
            
            var matches = regex.Matches(text);
            var evaluate = DialogAnalysis.GetEvaluate(env);
            foreach(Match match in matches)
            {
                var expression = match.Groups["expression"].Value;
                var getValue = evaluate.Evaluate(expression).ToString();
                finallyText.Replace(match.Value, getValue);
            }
            return finallyText.ToString();
        }

        public static void SetCharacter(int getNum)
        {
            var sayDialog = Fungus.SayDialog.GetSayDialog();
            var num = getNum;
            if (NpcJieSuanManager.inst.ImportantNpcBangDingDictionary.ContainsKey(num))
            {
                num = NpcJieSuanManager.inst.ImportantNpcBangDingDictionary[num];
            }
            sayDialog.SetCharacter(null,num);
            sayDialog.SetCharacterImage(null,num);
        }

        public static void Say(string text,Action callback)
        {
            var say = Fungus.SayDialog.GetSayDialog();
            say.SetActive(true);
            say.Say(text,true,true,true,true,
                false,null, ()=>
                {
                    callback?.Invoke();
                });
        }

        public static string DealSayText(string text, int sayRoleID)
        {
            StringBuilder dealStr = new StringBuilder(text);
            var npcID = NPCEx.NPCIDToNew(sayRoleID);
            var player = Tools.instance.getPlayer();
            if (PlayerEx.IsDaoLv(npcID))
            {
                string daoLvNickName = PlayerEx.GetDaoLvNickName(npcID);
                dealStr
                    .Replace("{FirstName}", "")
                    .Replace("{gongzi}", daoLvNickName)
                    .Replace("{xiongdi}", daoLvNickName)
                    .Replace("{shidi}", daoLvNickName)
                    .Replace("{shixiong}", daoLvNickName);
            }
            else
            {
                dealStr
                    .Replace("{FirstName}", player.firstName)
                    .Replace("{gongzi}", (player.Sex == 1) ? "公子" : "姑娘")
                    .Replace("{xiongdi}", (player.Sex == 1) ? "兄弟" : "姑娘")
                    .Replace("{shidi}", (player.Sex == 1) ? "师弟" : "师妹")
                    .Replace("{shixiong}", (player.Sex == 1) ? "师兄" : "师姐");
            }
            dealStr
                .Replace("{LastName}", player.lastName)
                .Replace("{xiaozi}", (player.Sex == 1) ? "小子" : "丫头")
                .Replace("{ta}", (player.Sex == 1) ? "他" : "她")
                .Replace("{menpai}", Tools.getStr("menpai" + player.menPai));

            return dealStr.ToString();
        }

        public static void ClearMenu()
        {
            var menuDialog = Fungus.MenuDialog.GetMenuDialog();
            menuDialog.Clear();
        }
        
        public static void AddMenu(string text,Action callback)
        {
            var menuDialog = Fungus.MenuDialog.GetMenuDialog();
            menuDialog.SetActive(true);

            void OptionAction()
            {
                menuDialog.StopAllCoroutines();
                menuDialog.Clear();
                menuDialog.HideSayDialog();
                menuDialog.SetActive(false);
                var say = Fungus.SayDialog.GetSayDialog();
                say.Stop();
                callback?.Invoke();
            }

            addOptionMethod.Value.Invoke(menuDialog, new object[] { text, true, false, (UnityAction)OptionAction });
        }

        public static void LoadAvatarNextData(int id,int index)
        {
            var fileName = $"AvatarNextData{Tools.instance.getSaveID(id, index)}.sav";
            var savePath = Paths.GetSavePath();
            AvatarNextData = null;
            try
            {
                var path = $"{savePath}/{fileName}";
                if (File.Exists(path))
                {
                    AvatarNextData = JObject.Parse(File.ReadAllText(path)).ToObject<AvatarNextData>();
                }
                else
                {
                    AvatarNextData = new AvatarNextData();
                    // 导入旧数据
                    Main.LogInfo($"首次加载数据，导入旧数据。");
                    AvatarNextData.IntGroup.AddRange(GetAllOldInt());
                    AvatarNextData.StrGroup.AddRange(GetAllOldStr());
                }
            }
            catch (Exception e)
            {
                Main.LogError($"读取Next存档数据失败！");
                Main.LogError(e);
            }

            AvatarNextData = AvatarNextData ?? new AvatarNextData();
        }

        public static void SaveAvatarNextData(int id,int index)
        {
            var fileName = $"AvatarNextData{Tools.instance.getSaveID(id, index)}.sav";
            var savePath = Paths.GetSavePath();
            try
            {
                var path = $"{savePath}/{fileName}";
                if (AvatarNextData == null)
                    throw new NullReferenceException("AvatarNextData not exist！");
                var jsonData = JObject.FromObject(AvatarNextData);
                File.WriteAllText(path, jsonData.ToString(Formatting.None));
            }
            catch (Exception e)
            {
                Main.LogError($"保存Next存档数据失败！");
                Main.LogError(e);
            }
        }
        
        public static void SetIntOld(string group,string key,int value)
        {
            key = $"next_{group}Int_{key}";
            var data = Tools.instance.getPlayer().AvatarChengJiuData;
            if (value == 0)
            {
                if(data.HasField(key))
                    data.RemoveField(key);
            }
            else
            {
                data.SetField(key,value);
            }
        }

        public static int GetIntOld(string group,string key)
        {
            key = $"next_{group}Int_{key}";
            var field = Tools.instance.getPlayer().AvatarChengJiuData.GetField(key);
            if (field == null || field.type != JSONObject.Type.NUMBER)
                return 0;
            return field.I;
        }

        public static void SetInt(string key,int value)
        {
            AvatarNextData.IntGroup.Set(key,value);
        }

        public static int GetInt(string key)
        {
            return AvatarNextData.IntGroup.Get(key);
        }
        
        public static void SetInt(string group,string key,int value)
        {
            AvatarNextData.IntGroup.Set(group,key,value);
        }

        public static int GetInt(string group,string key)
        {
            return AvatarNextData.IntGroup.Get(group,key);
        }

        public static Dictionary<string, int> GetAllInt()
        {
            return AvatarNextData.IntGroup.GetDefaultGroup();
        }
        public static Dictionary<string, int> GetAllOldInt()
        {
            var dic = new Dictionary<string, int>();
            
            var player = (KBEngine.Avatar)KBEngineApp.app.player();
            
            var data = player.AvatarChengJiuData;
            if (data == null)
                return dic;
            
            foreach (var key in data.keys)
            {
                if (key.StartsWith("next_Int_"))
                {
                    dic.Add(key.Substring("next_Int_".Length),data.GetField(key).I);
                }
            }
            return dic;
        }

        public static void SetStrOld(string group,string key,string value)
        {
            key = $"next_{group}Str_{key}";
            var data = Tools.instance.getPlayer().AvatarChengJiuData;
            if (value == "")
            {
                if(data.HasField(key))
                    data.RemoveField(key);
            }
            else
            {
                data.SetField(key,value);
            }
        }

        public static string GetStrOld(string group,string key)
        {
            key = $"next_{group}Str_{key}";
            var field = Tools.instance.getPlayer().AvatarChengJiuData.GetField(key);
            if (field == null || field.type != JSONObject.Type.STRING)
                return string.Empty;
            return field.Str;
        }
        
        public static void SetStr(string key,string value)
        {
            if (string.IsNullOrEmpty(value))
                value = null;
            AvatarNextData.StrGroup.Set(key,value);
        }

        public static string GetStr(string key)
        {
            return AvatarNextData.StrGroup.Get(key) ?? string.Empty;
        }
        
        public static void SetStr(string group,string key,string value)
        {
            if (string.IsNullOrEmpty(value))
                value = null;
            AvatarNextData.StrGroup.Set(group,key,value);
        }

        public static string GetStr(string group,string key)
        {
            return AvatarNextData.StrGroup.Get(group,key) ?? string.Empty;
        }

        public static Dictionary<string, string> GetAllStr()
        {
            return AvatarNextData.StrGroup.GetDefaultGroup();
        }
        
        public static Dictionary<string, string> GetAllOldStr()
        {
            var dic = new Dictionary<string, string>();
            
            var player = (KBEngine.Avatar)KBEngineApp.app.player();
            
            var data = player.AvatarChengJiuData;
            if (data == null)
                return dic;
            
            foreach (var key in data.keys)
            {
                if (key.StartsWith("next_Str_"))
                {
                    dic.Add(key.Substring("next_Str_".Length),data.GetField(key).ToString());
                }
            }
            return dic;
        }
        
        public static JSONObject GetNpcRandomJsonData(int npcId)
        {
            int num = NPCEx.NPCIDToNew(npcId);
            return jsonData.instance.AvatarRandomJsonData[num.ToString()];
        }
        
        public static JSONObject GetNpcJsonData(int npcId)
        {
            int num = NPCEx.NPCIDToNew(npcId);
            return jsonData.instance.AvatarJsonData[num.ToString()];
        }

        public static string GetNpcName(int npcId)
        {
            int num = NPCEx.NPCIDToNew(npcId);
            JSONObject wuJiangBangDing = Tools.instance.getWuJiangBangDing(num);
            string nameText = "";
            if (jsonData.instance != null && jsonData.instance.AvatarRandomJsonData.HasField(num.ToString()))
            {
                if (num == 1)
                    nameText = Tools.instance.getPlayer().name;
                else if (wuJiangBangDing == null)
                    nameText = Tools.instance.Code64ToString(
                        jsonData.instance.AvatarRandomJsonData[num.ToString()]["Name"].str);
                else
                    nameText = Tools.Code64(wuJiangBangDing["Name"].str);
            }

            return nameText;
        }
        
        public static string GetNpcTitle(int npcId)
        {
            int num = NPCEx.NPCIDToNew(npcId);
            JSONObject wuJiangBangDing = Tools.instance.getWuJiangBangDing(num);
            string titleText = "";
            if (jsonData.instance != null && jsonData.instance.AvatarRandomJsonData.HasField(num.ToString()))
            {
                titleText = wuJiangBangDing != null
                    ? Tools.Code64(wuJiangBangDing["Title"].str)
                    : Tools.getMonstarTitle(num);
            }

            return titleText;
        }

        public static int GetNpcSex(int npcId)
        {
            var sexData = GetNpcJsonData(npcId)?["SexType"];

            return sexData?.I ?? 0;
        }

        public static int GetNpcAge(int npcId)
        {
            var ageData = GetNpcJsonData(npcId)?["age"];

            return ageData?.I ?? -1;
        }
        
        public static int GetNpcLife(int npcId)
        {
            var ageData = GetNpcJsonData(npcId)?["shouYuan"];

            return ageData?.I ?? -1;
        }
        
        public static int GetNpcSprite(int npcId)
        {
            var ageData = GetNpcJsonData(npcId)?["shengShi"];

            return ageData?.I ?? -1;
        }
        
        public static int GetNpcSchool(int npcId)
        {
            var ageData = GetNpcJsonData(npcId)?["MenPai"];

            return ageData?.I ?? 0;
        }
        
        public static int GetNpcLevel(int npcId)
        {
            var ageData = GetNpcJsonData(npcId)?["Level"];

            return ageData?.I ?? 1;
        }
        
        public static int GetNpcLevelType(int npcId)
        {
            var ageData = GetNpcJsonData(npcId)?["Level"];

            var level = ageData?.I ?? 1;
            return (level - 1) / 3 + 1;
        }

        public static CustomStaticFaceInfo GetNpcFaceInfo(int npcId)
        {
            var randomData = GetNpcRandomJsonData(npcId);
            var jsonData = GetNpcJsonData(npcId);
            
            var importantID = jsonData["BindingNpcID"]?.I ?? 0;
            if (randomData == null)
                return null;
            
            var staticFaceInfo =
                SetAvatarFaceRandomInfo.inst.StaticRandomInfo.Find(item => item.AvatarScope == importantID);
            
            if (staticFaceInfo != null)
            {
                // 重要NPC
                return StaticFaceUtils.GetFaceInfoByStaticFaceInfo(staticFaceInfo);
            }
            else
            {
                // 普通NPC
                return StaticFaceUtils.GetFaceInfoByJson(randomData);
            }
        }
        
        public static void SetNpcFaceData(int npcId,CustomStaticFaceInfo faceInfo)
        {
            var randomData = GetNpcRandomJsonData(npcId);

            foreach (var pair in faceInfo.RandomInfos)
            {
                randomData.SetField(pair.Key, pair.Value);
            }
        }
        
        public static void SetNpcFaceData(int npcId,int faceId)
        {
            var npcFaceData = GetFaceInfoById(faceId);
            if (npcFaceData != null)
            {
                SetNpcFaceData(npcId, npcFaceData);
                return;
            }

            Debug.LogError($"不存在对应的捏脸数据 {faceId}");
        }

        public static CustomStaticFaceInfo GetPlayerFaceData()
        {
            var randomData = jsonData.instance.AvatarRandomJsonData["1"];

            var faceInfo = StaticFaceUtils.GetFaceInfoByJson(randomData);

            return faceInfo;
        }

        public static void SetPlayerFaceData(CustomStaticFaceInfo faceInfo)
        {
            var randomData = jsonData.instance.AvatarRandomJsonData["1"];

            foreach (var pair in faceInfo.RandomInfos)
            {
                randomData.SetField(pair.Key, pair.Value);
            }
            
            if(UIHeadPanel.Inst != null)
                UIHeadPanel.Inst.Face.setFace();

            var playerRandomFace = GameObject.FindObjectOfType<PlayerSetRandomFace>();
            if (playerRandomFace != null)
            {
                playerRandomFace.setFaceByJson(randomData);
            }
        }
        
        public static void SetPlayerFaceData(int faceId)
        {
            var npcFaceData = GetFaceInfoById(faceId);
            if (npcFaceData != null)
            {
                SetPlayerFaceData(npcFaceData);
                return;
            }

            Debug.LogError($"不存在对应的捏脸数据 {faceId}");
        }

        public static CustomStaticFaceInfo GetFaceInfoById(int faceId)
        {
            if(StaticFaceUtils.HasFace(faceId))
            {
                return StaticFaceUtils.GetFace(faceId);
            }

            return GetNpcFaceInfo(faceId);
        }
        
        public static bool IsPlayerCouple(int npcId) => PlayerEx.IsDaoLv(NPCEx.NPCIDToNew(npcId));
        public static bool IsPlayerTeacher(int npcId) => PlayerEx.IsTheather(NPCEx.NPCIDToNew(npcId));
        public static bool IsPlayerStudent(int npcId) => PlayerEx.IsTuDi(NPCEx.NPCIDToNew(npcId));
        public static bool IsPlayerBrother(int npcId) => PlayerEx.IsBrother(NPCEx.NPCIDToNew(npcId));
        
        public static string GetSchoolName(string schoolId)
        {
            var data = jsonData.instance.CyShiLiNameData[schoolId];
            return data?["name"]?.Str ?? "School.Unknown".I18N();
        }
        
        public static string GetLevelName(int level)
        {
            var data = jsonData.instance.LevelUpDataJsonData[level.ToString()];
            return data?["Name"]?.Str ?? "Level.Unknown".I18N();
        }

        public static string GetSceneName(string sceneId)
        {
            var data = jsonData.instance.SceneNameJsonData[sceneId];
            return data?["MapName"]?.Str ?? "Map.Unknown".I18N();
        }

        public static string GetMapRoadName(string roadId)
        {
            var data = jsonData.instance.AllMapLuDainType[roadId];
            return data?["LuDianName"]?.Str ?? "Map.Unknown".I18N();
        }
        
        public static string GetGenderName(int gender)
        {
            switch (gender)
            {
                case 1:
                    return "Gender.Male".I18N();
                case 2:
                    return "Gender.Female".I18N();
            }

            return "Gender.Unknown".I18N();
        }
        
        public static string GetNpcLocationName(int npcId)
        {
            NPCMap npcMap = NpcJieSuanManager.inst.npcMap;
            string curPosName = "Map.Unknown".I18N();
            
            if (npcMap.bigMapNPCDictionary != null)
            {
                foreach (var pair in npcMap.bigMapNPCDictionary)
                {
                    foreach (var id in pair.Value)
                    {
                        if (id == npcId)
                        {
                            curPosName = $"{GetSceneName("AllMaps")} - {GetMapRoadName(pair.Key.ToString())}";
                            goto EndSearch;
                        }
                    }
                }
            }
            
            if (npcMap.threeSenceNPCDictionary != null)
            {
                foreach (var pair in npcMap.threeSenceNPCDictionary)
                {
                    foreach (var id in pair.Value)
                    {
                        if (id == npcId)
                        {
                            curPosName = GetSceneName(pair.Key);
                            goto EndSearch;
                        }
                    }
                }
            }

            if (npcMap.fuBenNPCDictionary != null)
            {
                foreach (var pair in npcMap.fuBenNPCDictionary)
                {
                    var posId = pair.Key;
                    foreach (var pair2 in pair.Value)
                    {
                        foreach (var id in pair2.Value)
                        {
                            if (id == npcId)
                            {
                                curPosName = GetSceneName(posId);
                                goto EndSearch;
                            }
                        }
                    }
                }
            }

            EndSearch:
            return curPosName;
        }

        public static void NpcForceInteract(int npcId)
        {
            var npcData = new UINPCData(npcId);
            npcData.RefreshData();
            
            UINPCJiaoHu.Inst.HideJiaoHuPop();
            UINPCJiaoHu.Inst.NowJiaoHuNPC = npcData;
            
            UINPCJiaoHu.Inst.ShowJiaoHuPop();
        }
        
        
        #endregion

        #region 私有方法



        #endregion


        
    }
}