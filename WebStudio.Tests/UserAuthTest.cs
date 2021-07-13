﻿using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace WebStudio.Tests
{
    public class UserAuthTest
    {
        private readonly IWebDriver _driver;

        public UserAuthTest()
        {
            _driver = new ChromeDriver();
        }

        public void Dispose()
        {
            _driver.Quit();
            _driver.Dispose();
        }

        [Fact]
        public void LoginCorrectDataReturnsSuccessAuth()
        {
            _driver.Navigate().GoToUrl("https://localhost:5001/Account/Login");
            _driver.FindElement(By.Id("email")).SendKeys("admin@admin.com");
            _driver.FindElement(By.Id("password")).SendKeys("Q1w2e3r4t%");
            _driver.FindElement(By.Id("enter")).Click();
            
            var suppliersRequestsButton = _driver.FindElement(By.LinkText("Запросы поставщикам"));
            var suppliersDataBaseButton = _driver.FindElement(By.LinkText("База поставщиков"));
            var suppliersRequestsButtonLink = suppliersRequestsButton.GetAttribute("href");
            var suppliersDataBaseButtonLink = suppliersDataBaseButton.GetAttribute("href");
            Assert.Contains("Выход", _driver.PageSource);
            Assert.Equal("https://localhost:5001/Requests", suppliersRequestsButtonLink);
            Assert.Equal("https://localhost:5001/Suppliers", suppliersDataBaseButtonLink);
        }
    }
}