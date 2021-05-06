using Microsoft.VisualStudio.TestTools.UnitTesting;
using DashDownloader.DashFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat.Tests
{
    [TestClass()]
    public class FormattedStringTests
    {
        [TestMethod()]
        public void FormattedStringTest()
        {
            string template = "Hello $who$! Let's $verb$ this formatted string";
            FormattedString formattedString = new FormattedString(template);
            formattedString["who"] = "World";
            formattedString["verb"] = "test";

            string result = formattedString.ToString();
            string correctResult = "Hello World! Let's test this formatted string";

            Assert.AreEqual(correctResult, result);
        }

        [TestMethod()]
        public void FormattedStringNoVariablesTest()
        {
            string template = "Hello World!";
            FormattedString formattedString = new FormattedString(template);
            formattedString["who"] = "World";
            formattedString["verb"] = "test";

            string result = formattedString.ToString();
            string correctResult = template;

            Assert.AreEqual(correctResult, result);
        }
    }
}