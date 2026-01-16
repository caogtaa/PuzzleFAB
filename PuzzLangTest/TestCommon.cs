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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DOLE;
using PuzzLangLib;
using System.Runtime.CompilerServices;

namespace PuzzLangTest {
  public class TestCommon {

    // compile, run the tests
    protected void DoTests(
      string name, string variations, string template, string[,] tests, 
      Action<Compiler, string, string, string> dotest, 
      [CallerMemberName] string method = "")
    {
      foreach (var variation in variations.Split(';')) {
        var setup = template.Replace("{0}", variation);
        var compiled = DoCompile(name, name + ":" + method, setup);
        for (int i = 0; i < tests.GetLength(0); i++) {
          var moreinfo = $"[{variation}][{tests[i, 0]}] [{tests[i, 1]}]";
          dotest(compiled, tests[i, 0], tests[i, 1], moreinfo);
        }
      }
    }

    // compile the script
    protected Compiler DoCompile(string name, string title, string setup) {
      var testcase = StaticTestData.GetTestCase(name, title, setup);
      Logger.Level = 0;
      var game = new StringReader(testcase.Script);
      var compiler = Compiler.Compile(testcase.Title, game, Console.Out);
      Logger.Level = 2;
      Assert.IsTrue(compiler.Success, compiler.Message);
      compiler.Model.AcceptInputs("level 0");
      return compiler;
    }

    // accept input, check decoded level now matches expected
    protected void DoTestInputDecodeLevel(Compiler compiled, string inputs, string expected, string moreinfo) {
      var model = compiled.Model;
      model.AcceptInputs("level 0," + inputs);
      var endlevel = compiled.DecodeLevel(model.CurrentLevel);
      var result = DecodeResult(model);
      Assert.AreEqual(expected, endlevel + result, true, moreinfo);
    }

    // accept input, check object value
    protected void DoTestInputObjectText(Compiler compiled, string inputs, string expected, string moreinfo) {
      var model = compiled.Model;
      model.AcceptInputs("level 0," + inputs);
      var result = DecodeResult(model);
      var varname = expected.Before("=");
      var objid = compiled.Model.GetObjectId(varname);
      var objval = compiled.Model.GetObject(objid);
      Assert.AreEqual(expected, result ?? $"{varname}={objval.Text}", true, moreinfo);
    }

    protected void DoTestModel(Compiler compiled, string inputs, string tests, string moreinfo) {
      var model = compiled.Model;
      model.AcceptInputs("level 0," + inputs);
      var ts = tests.Split(';');
      var result = DecodeResult(model) ?? "ok";
      foreach (var test in ts) {
        var tss = test.Split('=');
        if (tss.Length == 1) Assert.AreEqual(test, result, moreinfo);
        if (tss[0] == "MSG") Assert.AreEqual(tss[1], model.CurrentMessage ?? "null", moreinfo);
        if (tss[0] == "STA") Assert.AreEqual(tss[1], model.Status ?? "null", moreinfo);
      }
      //Assert.AreEqual(ts[0], result, moreinfo);
      //Assert.AreEqual(ts[1], model.CurrentMessage ?? "null", moreinfo);
    }
    
    /// <summary>
    /// 对游戏应用输入后，检查每个位置上的对象是否符合预期
    /// </summary>
    /// <param name="compiled">编译后的游戏脚本</param>
    /// <param name="inputs">模拟玩家输入</param>
    /// <param name="predicts">
    ///   对每个位置的预期结果，格式为 "起始位置;方向;集合1 集合2 ..."
    ///   其中集合1 集合2 ... 是对应位置上预期的对象集合，一个位置上出现多个对象时按object id排序，集合内无空格隔开
    ///   比如"0;right;1 12 13 end"表示从0位置开始向右移动，分别出现对象1，对象1和2，对象1和3，继续往右到达版边
    ///     TODO: 很handy的表示法，但是有几个问题
    ///     1. 对象超过10个时，集合表示会出现歧义
    ///     2. test case耦合引用的游戏模板，object id对应关系不直观，test case难以阅读
    ///     3. 无法表示多行/列
    /// </param>
    /// <param name="moreinfo">出错时附带打印的test case信息</param>
    protected void DoTestLocationValue(Compiler compiled, string inputs, string predicts, string moreinfo) {
      var model = compiled.Model;
      model.AcceptInputs("level 0," + inputs);
      var modelResult = DecodeResult(model);
      
      var ts = predicts.Split(';');
      var location = ts[0].SafeIntParse();
      var direction = ts[1].Trim();
      var objs = ts[2].Trim().Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
      
      for (int i = 0; i < objs.Length; i++) {
        var result = modelResult ?? (location == null ? 
          "end" : 
          model.GetObjects(location.Value).OrderBy(v => v).Join(""));
        
        Assert.AreEqual(objs[i], result, moreinfo);
        
        // immutable调用，出界时location = null
        location = model.Step(location ?? 0, direction);
      }
    }
    
    protected void DoTestSymbols(Compiler compiled, string inputs, string predicts, string moreInfo) {
      var model = compiled.Model;
      model.AcceptInputs("level 0," + inputs);
      var modelResult = DecodeResult(model);
      
      var ts = predicts.Split(';');
      var location = ts[0].SafeIntParse();    // TODO: 这里没有马上验证是否出界，有缺陷
      var direction = ts[1].Trim();
      var symbols = ts[2].Trim().Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      
      for (int i = 0; i < symbols.Length; i++) {
        string specialToken = string.Empty;
        if (modelResult != null) {
          // 特殊状态，解析错误或者游戏状态变化
          specialToken = modelResult;
        } else if (location == null) {
          // 移动结束
          specialToken = "end";
        }

        if (string.IsNullOrEmpty(specialToken)) {
          // 比较对象符号
          var ids = model.GetObjects(location.Value);
          if (!compiled.SymbolEquals(ids, symbols[i])) {
            Assert.Fail($"{moreInfo}; " +
                        $"At location {location.Value}: " +
                        $"\"{compiled.ObjectIdsToSymbol(ids)}\" <-> \"{symbols[i]}\"");
          }
        } else {
          // 比较特殊字段
          Assert.AreEqual(symbols[i], specialToken, moreInfo);
        }

        // immutable调用，出界时location = null
        location = model.Step(location ?? 0, direction);
      }
    }

    string DecodeResult(GameModel model) {
      var result = (!model.Ok) ? "error"
        : (model.EndLevel) ? "done"
        : (model.GameOver) ? "over"
        : (model.InMessage) ? "msg"
        : (!model.InLevel) ? "unknown" : null;
      return result;
    }
  }
}