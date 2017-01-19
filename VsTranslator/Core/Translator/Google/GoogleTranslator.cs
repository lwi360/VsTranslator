﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using VsTranslator.Core.Translator.Entities;
using VsTranslator.Core.Translator.Enums;
using VsTranslator.Core.Translator.Google.Entities;
using VsTranslator.Core.Translator.Utils;

namespace VsTranslator.Core.Translator.Google
{
    public class GoogleTranslator : ITranslator
    {
        private readonly CSharpRunJavascript _javascript = new CSharpRunJavascript();

        private static readonly List<TranslationLanguage> TargetLanguages;
        private static readonly List<TranslationLanguage> SourceLanguages;


        static  GoogleTranslator()
        {
            TargetLanguages = new List<TranslationLanguage>()
            {
                new TranslationLanguage ("af", "Afrikaans / 南非荷兰语"),
                new TranslationLanguage ("sq", "Albanian / 阿尔巴尼亚语"),
                new TranslationLanguage ("ar", "Arabic / 阿拉伯语"),
                new TranslationLanguage ("be", "Belarusian / 白俄罗斯语"),
                new TranslationLanguage ("bg", "Bulgarian / 保加利亚语"),
                new TranslationLanguage ("ca", "Catalan / 加泰罗尼亚语"),
                new TranslationLanguage ("zh-CN", "Chinese (Simplified) / 简体中文"),
                new TranslationLanguage ("zh-TW", "Chinese (Traditional) / 繁体中文"),
                new TranslationLanguage ("hr", "Croatian / 克罗地亚语"),
                new TranslationLanguage ("cs", "Czech / 捷克语"),
                new TranslationLanguage ("da", "Danish / 丹麦语"),
                new TranslationLanguage ("nl", "Dutch / 荷兰语"),
                new TranslationLanguage ("en", "English / 英语"),
                new TranslationLanguage ("et", "Estonian / 爱沙尼亚语"),
                new TranslationLanguage ("tl", "Filipino / 菲律宾语"),
                new TranslationLanguage ("fi", "Finnish / 芬兰语"),
                new TranslationLanguage ("fr", "French / 法语"),
                new TranslationLanguage ("gl", "Galician / 加利西亚语"),
                new TranslationLanguage ("de", "German / 德语"),
                new TranslationLanguage ("el", "Greek / 希腊语"),
                new TranslationLanguage ("iw", "Hebrew / 希伯来语"),
                new TranslationLanguage ("hi", "Hindi / 印地语"),
                new TranslationLanguage ("hu", "Hungarian / 匈牙利语"),
                new TranslationLanguage ("is", "Icelandic / 冰岛语"),
                new TranslationLanguage ("id", "Indonesian / 印度尼西亚语"),
                new TranslationLanguage ("ga", "Irish / 爱尔兰语"),
                new TranslationLanguage ("it", "Italian / 意大利语"),
                new TranslationLanguage ("ja", "Japanese / 日语"),
                new TranslationLanguage ("ko", "Korean / 朝鲜语"),
                new TranslationLanguage ("lv", "Latvian / 拉脱维亚语"),
                new TranslationLanguage ("lt", "Lithuanian / 立陶宛语"),
                new TranslationLanguage ("mk", "Macedonian / 马其顿语"),
                new TranslationLanguage ("ms", "Malay / 马来语"),
                new TranslationLanguage ("mt", "Maltese / 马耳他语"),
                new TranslationLanguage ("fa", "Persian / 波斯语"),
                new TranslationLanguage ("pl", "Polish / 波兰语"),
                new TranslationLanguage ("pt", "Portugese / 葡萄牙语"),
                new TranslationLanguage ("ro", "Romanian / 罗马尼亚语"),
                new TranslationLanguage ("ru", "Russian / 俄语"),
                new TranslationLanguage ("sr", "Serbian / 塞尔维亚语"),
                new TranslationLanguage ("sk", "Slovak / 斯洛伐克语"),
                new TranslationLanguage ("sl", "Slovenian / 斯洛文尼亚语"),
                new TranslationLanguage ("es", "Spanish / 西班牙语"),
                new TranslationLanguage ("sw", "Swahili / 斯瓦希里语"),
                new TranslationLanguage ("sv", "Swedish / 瑞典语"),
                new TranslationLanguage ("th", "Thai / 泰语"),
                new TranslationLanguage ("tr", "Turkish / 土耳其语"),
                new TranslationLanguage ("uk", "Ukranian / 乌克兰语"),
                new TranslationLanguage ("vi", "Vietnamese / 越南语"),
                new TranslationLanguage ("cy", "Welsh / 威尔士语"),
                new TranslationLanguage ("yi", "Yiddish / 意第绪语"),
                new TranslationLanguage ("mn", "Mongolian / 蒙古语"),
                new TranslationLanguage ("la", "Latin / 拉丁语")
            };

            SourceLanguages = new List<TranslationLanguage>() { new TranslationLanguage("auto", "Auto-detect / 自动检测") };
            SourceLanguages.AddRange(TargetLanguages);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">text's length must between 0 and 5000</param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private GoogleTransResult TranslateByHttp(string text, string from = "en", string to = "zh-CN")
        {
            if (!(text.Length > 0 && text.Length < 5000))
            {
                return null;
            }
            var tk = GetTk(text);

            var result = new HttpHelper().GetHtml(new HttpItem()
            {
                Url = $"http://translate.google.cn/translate_a/single?client=t&sl={from}&tl={to}&hl=zh-CN&dt=t&ie=UTF-8&oe=UTF-8&ssel=6&tsel=3&kc=0&tk={tk}&q={HttpUtility.UrlEncode(text)}"
            }).Html;

            //@"\[\[\[""(.+?)"",""(.+?)"",,,[0-9]+?\](,\[""(.+?)"",""(.+?)"",,,[0-9]+\])*\],,""(.+?)""\]"
            //[[["这是一个非常小的项目，如果你熟悉Visual Studio编辑器扩展可能是相当自我解释。","It's a very small project and may be fairly self explanatory if you are familiar with Visual Studio editor extensions.",,,3],["扩展有两个组件：","There are two components to the extension:",,,3]],,"en"]
            var mc = new Regex("\\[\\[\\[\"(.+?)\",\"(.+?)\",,,.+?\\]\\],,\"(.+?)\"\\]").Match(result);

            var nextAll = new Regex(@",\[""(.+?)"",""(.+?)"",,,[0-9]+\]{1,}").Matches(result);

            var targetText = mc.Groups[1].Value;

            targetText = nextAll.Cast<Match>().Aggregate(targetText, (current, match) => current + " " + match.Groups[1].Value);

            return new GoogleTransResult()
            {
                From = mc.Groups[3].Value,
                TargetText = targetText
            };

            //[[["You have a good day today","你今天过得好不好",,,3]],,"zh-CN"]
            //[[["你好”","hello\"",,,1]],,"en"]
            //[[["你好","hello",,,1]],,"en"]
        }


        private string GetTk(string text)
        {
            if (text.Contains("'"))
            {
                text = text.Replace("'", "\\'");
            }
            return _javascript.Eval("((function() {var TKK = ((function() {var a = 561666268;var b = 1526272306;return 406398 + '.' + (a + b);  })()); function b(a, b) {for (var d = 0; d < b.length - 2; d += 3) {        var c = b.charAt(d + 2),            c = 'a' <= c ? c.charCodeAt(0) - 87 : Number(c),            c = '+' == b.charAt(d + 1) ? a >>> c : a << c;        a = '+' == b.charAt(d) ? a + c & 4294967295 : a ^ c    }    return a  }    function tk(a) {      for (var e = TKK.split('.'), h = Number(e[0]) || 0, g = [], d = 0, f = 0; f < a.length; f++) {          var c = a.charCodeAt(f);          128 > c ? g[d++] = c : (2048 > c ? g[d++] = c >> 6 | 192 : (55296 == (c & 64512) && f + 1 < a.length && 56320 == (a.charCodeAt(f + 1) & 64512) ? (c = 65536 + ((c & 1023) << 10) + (a.charCodeAt(++f) & 1023), g[d++] = c >> 18 | 240, g[d++] = c >> 12 & 63 | 128) : g[d++] = c >> 12 | 224, g[d++] = c >> 6 & 63 | 128), g[d++] = c & 63 | 128)      }      a = h;      for (d = 0; d < g.length; d++) a += g[d], a = b(a, '+-a^+6');      a = b(a, '+-3^+b+-f');      a ^= Number(e[1]) || 0;      0 > a && (a = (a & 2147483647) + 2147483648);      a %= 1E6;      return a.toString() + '.' + (a ^ h)  }  return tk('" + text + "'); })())").ToString();
        }

        public static string GetName()
        {
            return "Google Translator / 谷歌翻译";
        }

        public static string GetDescription()
        {
            return "place the single request control within 5000 bytes in length (One Chinese Is a byte), you can on the website translation https://translate.google.cn/";
        }
        public static string GetWebsite()
        {
            return "https://translate.google.cn/";
        }

        public static List<TranslationLanguage> GetTargetLanguages()
        {
            return TargetLanguages;
        }

        public static  List<TranslationLanguage> GetSourceLanguages()
        {
            return SourceLanguages;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">text's length must between 0 and 5000</param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public TranslationResult Translate(string text, string @from = "en", string to = "zh-CN")
        {
            TranslationResult result = new TranslationResult()
            {
                SourceLanguage = @from,
                TargetLanguage = to,
                SourceText = text,
                TargetText = "",
                FailedReason = ""
            };
            if (SourceLanguages.Count(sl => sl.Code == @from) <= 0)
            {
                result.TranslationResultTypes = TranslationResultTypes.Failed;
                result.FailedReason = "unrecognizable source language";
            }
            else if (TargetLanguages.Count(tl => tl.Code == to) <= 0)
            {
                result.TranslationResultTypes = TranslationResultTypes.Failed;
                result.FailedReason = "unrecognizable target language";
            }
            else
            {
                try
                {
                    result.TranslationResultTypes = TranslationResultTypes.Successed;
                    GoogleTransResult googleTransResult = TranslateByHttp(text, from, to);
                    result.SourceLanguage = googleTransResult.From;
                    result.TargetText = googleTransResult.TargetText;
                }
                catch (Exception exception)
                {
                    result.FailedReason = exception.Message;
                    result.TranslationResultTypes = TranslationResultTypes.Failed;
                }
            }
            return result;
        }
    }
}