/// Puzzlang is a pattern matching language for abstract games and puzzles. See http://www.polyomino.com/puzzlang.
///
/// Copyright © Polyomino Games 2018. All rights reserved.
/// 
/// This is free software. You are free to use it, modify it and/or 
/// distribute it as set out in the licence at http://www.polyomino.com/licence.
/// You should have received a copy of the licence with the software.
/// 
/// This software is distributed in the hope that it will be useful, but with
/// absolutely no warranty, express or implied. See the licence for details.
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOLE;
using PuzzLangLib;

namespace PuzzLangTest {
    public class StaticTestCase {
        public string Title;
        public string Script;

        public string Inputs;

        //public List<InputEvent> Inputs;
        public int TargetLevel;
        public double RandomSeed;

        public static StaticTestCase Create(string title, string script, string inputs) {
            return new StaticTestCase { Title = title, Script = script, Inputs = inputs };
        }
    }

    public class Preset {
        public string BodyTemplate;
        public string PreludeTemplate;
        public string CaseName;
        public string SectionArgs;
        public string Inputs;
    }

    public static class StaticTestData {
        const string Crlf = "\r\n";
        static readonly char[] Colon = new char[] { ':' };

        static public List<Preset> _presetTestCases {
            get {
                return new List<Preset> {
                    new Preset {
                        CaseName = "Simple Block Pushing",
                        PreludeTemplate = _sbp,
                        BodyTemplate = null,
                        SectionArgs = null,
                        Inputs = "right",
                    },
                    new Preset {
                        CaseName = "PRBG filled",
                        PreludeTemplate = _extraprelude,
                        BodyTemplate = _template,
                        SectionArgs = _game_prbg,
                        Inputs = "right",
                    },
                    new Preset {
                        CaseName = "PRBGX extended",
                        PreludeTemplate = _extraprelude,
                        BodyTemplate = _template_ext,
                        SectionArgs = _game_prbg_ext,
                        Inputs = "right",
                    },
                    //new Preset { "Simple Block Pushing", _sbp, null, null, "right" },
                    //new Preset { "PRBG filled", _extraprelude, _template, _game_prbg, "right" },
                    //new Preset { "PRBGX extended", _extraprelude, _template_ext, _game_prbg_ext, "right" },
                };
            }
        }

        // TODO: 绕了一圈，为什么不直接返回_presetTestCases?
        public static IEnumerable<StaticTestCase> TestCases {
            get {
                foreach (var name in TestCaseNames)
                    yield return GetTestCase(name);
            }
        }

        public static IList<string> TestCaseNames {
            get { return _presetTestCases.Select(t => t.CaseName).ToList(); }
        }

        /// <summary>
        /// 挑选一个预设的游戏模板，并可选地替换section内容
        /// </summary>
        /// <param name="key">用于在预设的testcase里弱匹配case name</param>
        /// <param name="title">脚本的最终title</param>
        /// <param name="newSectionArgs">替换section占位符的内容，比preset里的sectionArgs有更高优先级</param>
        /// <returns></returns>
        public static StaticTestCase GetTestCase(string key, string title = null, string newSectionArgs = null) {
            var preset = _presetTestCases.Find(t => t.CaseName.Contains(key));

            // 如果没有指定新的section args，就用preset里的。否则用新的替换preset里的
            var finalSectionArgs = string.IsNullOrEmpty(newSectionArgs)
                ? preset.SectionArgs
                : SubstituteArgs(preset.SectionArgs, newSectionArgs);

            // 主体为空，只保留前导节？
            var script = string.IsNullOrEmpty(finalSectionArgs)
                ? preset.PreludeTemplate
                : MakeScript(title ?? preset.CaseName, preset.PreludeTemplate, preset.BodyTemplate, finalSectionArgs);

            return new StaticTestCase {
                Title = title ?? preset.CaseName,
                Script = script,
                Inputs = preset.Inputs,
            };
        }

        private static string[] SplitAndRemoveEmpty(this string str, char sep) {
            return str.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string SubstituteArgs(string subs, string newsubs) {
            if (string.IsNullOrEmpty(subs))
                return newsubs;

            var lookup = subs.SplitAndRemoveEmpty('@').ToDictionary(t => t.Substring(0, 5), t => t);
            foreach (var s in newsubs.SplitAndRemoveEmpty('@').Where(s => s.Length >= 4))
                lookup[s.Substring(0, 5)] = s;
            return lookup.Values.Join("@");
        }

        /// <summary>
        /// 通过模板+参数生成游戏脚本
        /// </summary>
        /// <param name="title"></param>
        /// <param name="prelude">前导节模板，title需要被替换</param>
        /// <param name="template">文件体模板，只有section header。每个section的内容用占位符暂替</param>
        /// <param name="args">section的内容，需要替换template里的占位符</param>
        /// <returns></returns>
        static string MakeScript(string title, string prelude, string template, string args) {
            var script = String.Format(prelude, title) + template;
            //var game = template.Replace("(pre)", String.Format(prelude, title));
            // split args on ...@(sec)subs@... then 
            var argsInList = args.SplitAndRemoveEmpty('@');
            foreach (var arg in argsInList) {
                var parts = arg.Split(new char[] { ':' }, 2);
                script = script.Replace(parts[0], parts[1]);
            }

            return script.Replace(";", Crlf);
        }

        static string _extraprelude = "title {0};author puzzlang testing;homepage puzzlang.org;debug;verbose_logging;";

        // Simple game using template substitutions
        static string _game_prbg =
            "@(pre):;" +
            "@(obj):Background;black;;PLAYER P;white;;R;RED;;B;BLUE;;G;green;;Y;yellow;;K;Pink;;" +
            "@(leg):. = Background;a = R and K;o = R or G or B or Y;ork = R or K;oyk = Y or K;obk = B or K" +
            "@(col):Background;Y;Player,R,B,G;K;" +
            "@(rul):[ > P | a] -> [ > P | > a];" +
            "@(win):no p;" +
            "@(lev):message starting;.......;.g.P.a.;...R...;.......;";

        // Ditto with extensions
        static string _game_prbg_ext =
            "@(pre):;" +
            "@(obj):Background;black;;PLAYER P;white;;R;RED;;B;BLUE;;G;green;;Y;yellow;;K;Pink;;S;purple;text SSS" +
            "@(leg):. = Background;a = R and K;o = R or G or B or Y;ork = R or K;oyk = Y or K" +
            "@(col):Background;Y;Player,R,B,G;K;S;" +
            "@(scr):;" +
            "@(rul):[ > P | a] -> [ > P | > a];" +
            "@(win):no p;" +
            "@(lev):message starting;.......;.g.P.a.;...R...;.......;";

        // packed template allows sections to be substituted
        static string _template =
            "(pre)" +
            ";========;OBJECTS;========;(obj)" +
            ";=======;LEGEND;=======;(leg)" +
            ";=======;SOUNDS;=======;(sou)" +
            ";================;COLLISIONLAYERS;================;(col)" +
            ";======;RULES;======;(rul)" +
            ";==============;WINCONDITIONS;==============;(win)" +
            ";=======;LEVELS;=======;(lev)";

        static string _template_ext =
            "(pre)" +
            ";==;OBJECTS;========;(obj)" +
            ";==;LEGEND;=======;(leg)" +
            ";==;SOUNDS;=======;(sou)" +
            ";==;COLLISIONLAYERS;================;(col)" +
            ";==;SCRIPTS;======;(scr)" +
            ";==;RULES;======;(rul)" +
            ";==;WINCONDITIONS;==============;(win)" +
            ";==;LEVELS;=======;(lev)";

        //--- full game verbatim from https://www.puzzlescript.net/editor.html
        static string _sbp = @"title Simple Block Pushing Game
author Stephen Lavelle
homepage www.puzzlescript.net

========
OBJECTS
========

Background
LIGHTGREEN GREEN
11111
01111
11101
11111
10111


Target
DarkBlue
.....
.000.
.0.0.
.000.
.....

Wall
BROWN DARKBROWN
00010
11111
01000
11111
00010

Player
Black Orange White Blue
.000.
.111.
22222
.333.
.3.3.

Crate
Orange Yellow
00000
0...0
0...0
0...0
00000


=======
LEGEND
=======

. = Background
# = Wall
P = Player
* = Crate
@ = Crate and Target
O = Target


=======
SOUNDS
=======

Crate MOVE 36772507

================
COLLISIONLAYERS
================

Background
Target
Player, Wall, Crate

======
RULES
======

[ >  Player | Crate] -> [  >  Player | > Crate]

==============
WINCONDITIONS
==============

All Target on Crate

=======
LEVELS
=======


####..
#.O#..
#..###
#@P..#
#..*.#
#..###
####..


######
#....#
#.#P.#
#.*@.#
#.O@.#
#....#
######
";
    }
}