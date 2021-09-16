﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 扩展方法集合
    /// </summary>
    public static class Ex
    {
        public static dynamic GetValue(this HtmlNode rootNode, CustomXpath xpath)
        {
            if (xpath == null) return null;
            var isMulti = xpath.IsMultiValues;
            if (isMulti)
            {
                var nodes = rootNode.SelectNodes(xpath.Path);
                if (nodes == null) return null;
                var list = new List<string>();
                switch (xpath.Mode)
                {
                    case CustomXpathMode.Attribute:
                        foreach (HtmlNode hnode in nodes)
                        {
                            list.Add(hnode.Attributes[xpath.Attribute]?.Value);
                        }
                        break;
                    case CustomXpathMode.InnerText:
                        foreach (HtmlNode hnode in nodes)
                        {
                            list.Add(hnode.InnerText);
                        }
                        break;
                    case CustomXpathMode.Node:
                        return nodes;
                }

                if (xpath.Pre != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i] = $"{xpath.Pre}{list[i]}";
                    }
                }

                return list;
            }
            else
            {
                var node = rootNode.SelectSingleNode(xpath.Path);
                if (node == null) return null;
                var str = string.Empty;
                switch (xpath.Mode)
                {
                    case CustomXpathMode.Attribute:
                        str = node.Attributes[xpath.Attribute].Value;
                        break;
                    case CustomXpathMode.InnerText:
                        str = node.InnerText;
                        break;
                    case CustomXpathMode.Node:
                        return node;
                }

                if (xpath.Pre != null)
                {
                    str = $"{xpath.Pre}{str}";
                }

                if (xpath.RegexPattern != null)
                {
                    var matches = new Regex(xpath.RegexPattern).Matches(str);
                    str = matches.Any() ? matches[0].Value : str;
                }

                if (xpath.Replace != null && xpath.ReplaceTo!=null)
                {
                    str = str.Replace(xpath.Replace, xpath.ReplaceTo);
                }

                return str;
            }
            

        }

        public static string Delete(this string text, params string[] deleteStrs)
        {
            foreach (var deleteStr in deleteStrs)
            {
                text = text.Replace(deleteStr, "");
            }

            return text;
        }
        public static string ToPairsString(this Pairs pairs)
        {
            var query = string.Empty;
            var i = 0;
            if (pairs == null) return query;
            foreach (var para in pairs.Where(para => !para.Value.IsEmpty()))
            {
                query += $"{(i > 0 ? "&" : "?")}{para.Key}={para.Value}";
                i++;
            }

            return query;
        }
        public static dynamic GetList(dynamic dyObj) => dyObj ?? new List<dynamic>();

        public static bool IsEmpty(this string text) => string.IsNullOrWhiteSpace(text);

        public static DateTime? ToDateTime(this string dateTime)
        {
            if (dateTime.IsEmpty()) return null;
            var timeInt = dateTime.ToUlong();
            if (timeInt != 0)
            {
                var dt = new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(timeInt);
                return dt;
            }

            var b = DateTime.TryParse(dateTime, out var dt2);
            if (b) return dt2;
            else
            {
                // todo: covert

            }
            return null;
        }
        public static int ToInt(this string idStr)
        {
            if (idStr == null) return 0;
            var b = int.TryParse(idStr.Trim(), out var id);
            if (!b)
            {
                var regex = new Regex(@"(\d+)");
                var s = regex.Matches(idStr);
                var ss = "";
                if (s.Count > 0) ss = s[0].Value;
                int.TryParse(ss, out id);
            }
            return id;
        }

        public static long ToLong(this string idStr)
        {
            if (idStr == null) return 0;
            long.TryParse(idStr.Trim(), out var id);
            return id;
        }

        public static ulong ToUlong(this string idStr)
        {
            if (idStr == null) return 0;
            ulong.TryParse(idStr.Trim(), out var id);
            return id;
        }

        public static string ToEncodedUrl(this string orgStr) => HttpUtility.UrlEncode(orgStr, Encoding.UTF8);

        public static string ToDecodedUrl(this string orgStr) => HttpUtility.UrlDecode(orgStr, Encoding.UTF8);

        public static void GoUrl(this string url)
        {
            if (url.IsEmpty()) return;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var p = new Process();
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;    //不使用shell启动
                    p.StartInfo.RedirectStandardInput = true;//喊cmd接受标准输入
                    p.StartInfo.RedirectStandardOutput = false;//不想听cmd讲话所以不要他输出
                    p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                    p.StartInfo.CreateNoWindow = true;//不显示窗口
                    p.Start();

                    //向cmd窗口发送输入信息 后面的&exit告诉cmd运行好之后就退出
                    var nurl = url.Replace("&", "^&").Replace("?", "^?");
                    p.StandardInput.WriteLine($"start {nurl} &exit");
                    p.StandardInput.AutoFlush = true;
                    p.WaitForExit();//等待程序执行完退出进程
                    p.Close();
                }
            }
            catch
            {
                if (Debugger.IsAttached) throw;
                Log($"go url:{url} fail!!");
            }
        }

        public static void GoDirectory(this string path)
        {
            if (path.IsEmpty()) return;
            try
            {
                Process.Start("explorer.exe", path);
            }
            catch
            {
                if (Debugger.IsAttached) throw;
                Log($"go path:{path} fail!!");
            }
        }

        public static void GoFile(this string path)
        {
            if (path.IsEmpty()) return;
            try
            {
                Process.Start("explorer.exe", $"/select,{path}");
            }
            catch
            {
                if (Debugger.IsAttached) throw;
                Log($"go path:{path} fail!!");
            }
        }

        public static void Log(params object[] objs)
        {
            var str = $"{DateTime.Now:yyMMdd-HHmmss-ff}>>{objs.Aggregate((o, o1) => $"{o}\r\n{o1}")}";
            Debug.WriteLine(str);
            LogAction?.Invoke(str);
        }
        public static Action<string, string, MessagePos> ShowMessageAction;

        public static Action<string> LogAction;

        /// <summary>
        /// 显示信息
        /// </summary>
        public static void ShowMessage(string message, string detailMes = null, MessagePos pos = MessagePos.Popup)
        {
            ShowMessageAction?.Invoke(message, detailMes, pos);
        }

        public enum MessagePos
        {
            InfoBar,
            Popup,
            Searching,
            Window,
            Page
        }

        public static string Encode(this string str)
        {
            var key = Encoding.ASCII.GetBytes("leaf");
            var iv = Encoding.ASCII.GetBytes("1234");
            MemoryStream ms = null;
            CryptoStream cs = null;
            StreamWriter sw = null;

            var des = new DESCryptoServiceProvider();
            try
            {
                ms = new MemoryStream();
                cs = new CryptoStream(ms, des.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                sw = new StreamWriter(cs);
                sw.Write(str);
                sw.Flush();
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
            finally
            {
                sw?.Close();
                cs?.Close();
                ms?.Close();
            }
        }

        public static string Decode(this string str)
        {
            var key = Encoding.ASCII.GetBytes("leaf");
            var iv = Encoding.ASCII.GetBytes("1234");
            MemoryStream ms = null;
            CryptoStream cs = null;
            StreamReader sr = null;

            var des = new DESCryptoServiceProvider();
            try
            {
                ms = new MemoryStream(Convert.FromBase64String(str));
                cs = new CryptoStream(ms, des.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            finally
            {
                sr?.Close();
                cs?.Close();
                ms?.Close();
            }
        }
    }
}

