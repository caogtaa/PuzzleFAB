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
    public class GameScriptTemplate {
        public string Title;
        public string PreludeSection;
        public string ObjectsSection;
        public string LegendSection;
        public string SoundsSection;
        public string LayersSection;
        public string RulesSection;
        public string WinSection;
        public string LevelsSection;
    }
    public static partial class StaticTestData {
        static GameScriptTemplate _rgbGameTemplate = new GameScriptTemplate {
            Title = "Test Level",
            PreludeSection = @"
title Test Level
author caogtaa
",
            ObjectsSection = @"
Background 
transparent

Player P 
white

R
RED

G
GREEN

B
BLUE
",
            LegendSection = @"
. = Background
W = R AND G AND B (White)
Y = R AND G
U = R AND B (pUrple)
C = G AND B (Cyan)
N = R OR G OR B (aNy)
",
            SoundsSection = @"",
            LayersSection = @"
Background
P
R
G
B
",
            RulesSection = @"
[ > P | N ] -> [ > P | > N ]
",
            WinSection = @"",
            LevelsSection = @"
message test level

.BPR.
..G..
.....
"
        };
        
        // 重构好之前暂时先用Legacy字符串
        // RGB在单层，LEGEND里不能有AND的定义
        static string _rgb1Layer =
            "@(pre):;" +
            "@(obj):Background;black;;PLAYER P;white;;R;RED;;G;GREEN;;B;BLUE;;" +
            "@(leg):. = Background;N = R OR G OR B (aNy);" +
            "@(col):Background;P,R,G,B;" +
            "@(rul):[ > P | N ] -> [ > P | > N ];" +
            "@(win):NO P;" +
            "@(lev):;.BPR.;..G..;.....";
        
        // R G B 3个箱子分3层
        static string _rgb3Layers = 
            "@(pre):;" +
            "@(obj):Background;black;;PLAYER P;white;;R;RED;;G;GREEN;;B;BLUE;;" +
            "@(leg):. = Background;W = R AND G AND B (White);Y = R AND G;U = R AND B (pUrple);C = G AND B (Cyan);N = R OR G OR B (aNy);" +
            "@(col):Background;P;R;G;B;" +
            "@(rul):[ > P | N ] -> [ > P | > N ];" +
            "@(win):NO P;" +
            "@(lev):;.BPR.;..G..;.....";
    }
}