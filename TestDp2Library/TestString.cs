﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigitalPlatform.LibraryServer;
using DigitalPlatform.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestDp2Library
{
    /// <summary>
    /// 测试 Unescape() 函数。该函数目前用于 dp2ssl 的 TinyServer.cs 中
    /// </summary>
    [TestClass]
    public class TestString
    {
        [TestMethod]
        public void test_unescape_1()
        {
            var source = "\\r";
            var correct = "\r";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_2()
        {
            var source = "\\n";
            var correct = "\n";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_3()
        {
            var source = "\\t";
            var correct = "\t";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_4()
        {
            var source = "\\\\";
            var correct = "\\";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_5()
        {
            var source = "\\*";
            var correct = "*";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_6()
        {
            var source = "\\+";
            var correct = "+";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_7()
        {
            var source = "\\w";
            var correct = " ";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_unescape_8()
        {
            var source = "这是空格\\w这是回车\\r\\n";
            var correct = "这是空格 这是回车\r\n";

            var result = Unescape(source);
            Assert.AreEqual(correct, result);
        }

        static string Unescape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return System.Text.RegularExpressions.Regex.Unescape(text.Replace("\\w", " "));
        }

        [TestMethod]
        public void test_compareVersion_01()
        {
            try
            {
                int ret = StringUtil.CompareVersion("99", "0.02");
                Assert.Fail("应该抛出异常才对");
            }
            catch (Exception ex)
            {
            }
        }

        [TestMethod]
        public void test_compareVersion_02()
        {

            int ret = StringUtil.CompareVersion("99.0", "0.02");
            Assert.IsTrue(ret > 0);
        }

        [TestMethod]
        public void test_ParseBandwidth_01()
        {
            var source = "1024";
            long correct = 1024;

            var result = LibraryServerUtil.ParseBandwidth(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_ParseBandwidth_02()
        {
            var source = "5K";
            long correct = 5*1024;

            var result = LibraryServerUtil.ParseBandwidth(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_ParseBandwidth_03()
        {
            var source = "12M";
            long correct = 12 * 1024 * 1024;

            var result = LibraryServerUtil.ParseBandwidth(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_ParseBandwidth_04()
        {
            var source = "100G";
            long correct = (long)100 * 1024 * 1024 * 1024;

            var result = LibraryServerUtil.ParseBandwidth(source);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void test_ParseBandwidth_05()
        {
            var source = "100G1";

            try
            {
                var result = LibraryServerUtil.ParseBandwidth(source);
                Assert.Fail("不应该走到这里");
            }
            catch
            {
            }
        }
    }
}
