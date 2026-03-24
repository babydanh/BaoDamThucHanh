using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParabankAutoTests.Models;
using ParabankAutoTests.Pages;
using ParabankAutoTests.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace ParabankAutoTests.Tests
{
    [TestFixture]
    public class Test_RequestLoan
    {
        private IWebDriver driver = null!;
        private RequestLoanPage loanPage = null!;
        private TestCaseModel currentTestCase = null!;
        private string actualResultText = "";

        public static IEnumerable<TestCaseData> LoanData()
        {
            // ĐẢM BẢO FILE JSON TRÊN MÁY BẠN CÓ TÊN CHÍNH XÁC LÀ RequestLoan.json
            return JsonReader.ReadTestData("RequestLoanPage.json");
        }

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            // 1. Đăng nhập
            driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/index.htm");
            driver.FindElement(By.Name("username")).SendKeys("john");
            driver.FindElement(By.Name("password")).SendKeys("demo");
            driver.FindElement(By.XPath("//input[@value='Log In']")).Click();

            loanPage = new RequestLoanPage(driver);
        }

        [Test]
        [TestCaseSource(nameof(LoanData))]
        public void RunRequestLoanTests(TestCaseModel testData)
        {
            actualResultText = "";
            currentTestCase = testData;

            TestContext.WriteLine($"=== RUN {testData.TestID} - {testData.TestName} ===");

            // Hứng dữ liệu từ JSON 
            string loanAmount = testData.LoanAmount ?? "";
            string downPayment = testData.DownPayment ?? "";

            // Thao tác chung cho tất cả 11 Test Cases
            loanPage.ClickMenu();
            loanPage.EnterLoanAmount(loanAmount);
            loanPage.EnterDownPayment(downPayment);
            loanPage.ClickApply();

            string resultText = loanPage.GetResult();

            switch (testData.TestID)
            {
                case "TC_F9.1":
                case "TC_F9.2":
                case "TC_F9.6":
                    if (resultText.ToLower().Contains("denied") || resultText.ToLower().Contains("insufficient") || resultText.ToLower().Contains("not approved") || resultText.ToLower().Contains("error"))
                    {
                        actualResultText = $"Pass: Hệ thống chặn đúng logic. Phản hồi: {resultText}";
                        Assert.Pass(actualResultText);
                    }
                    else if (resultText.ToLower().Contains("approved"))
                    {
                        actualResultText = $"Bug Web: Ngân hàng duyệt bừa khoản vay vô lý (Vay: {loanAmount}, Cọc: {downPayment})!";
                        Assert.Fail(actualResultText);
                    }
                    else
                    {
                        actualResultText = $"Hệ thống báo: {resultText}";
                        Assert.Fail(actualResultText);
                    }
                    break;

                case "TC_F9.3":
                    if (resultText.ToLower().Contains("approved"))
                    {
                        string newAccountId = loanPage.GetNewAccountId();
                        if (!string.IsNullOrEmpty(newAccountId))
                        {
                            loanPage.ClickAccountById(newAccountId);
                            string balance = loanPage.GetAccountBalance();

                            if (balance.Contains(loanAmount) || (!balance.Contains("0.00") && !string.IsNullOrEmpty(balance)))
                            {
                                actualResultText = $"Pass: Vay thành công. Tạo tài khoản {newAccountId} với số dư {balance}";
                                Assert.Pass(actualResultText);
                            }
                            else
                            {
                                actualResultText = $"Bug Web: Duyệt vay thành công nhưng tài khoản {newAccountId} bị rỗng (Balance: {balance})";
                                Assert.Fail(actualResultText);
                            }
                        }
                        else
                        {
                            actualResultText = "Bug Web: Báo Approved nhưng không sinh ra số Account ID mới.";
                            Assert.Fail(actualResultText);
                        }
                    }
                    else
                    {
                        actualResultText = $"Lỗi: Khoản vay hợp lệ bị từ chối. Trạng thái: {resultText}";
                        Assert.Fail(actualResultText);
                    }
                    break;

                case "TC_F9.4":
                case "TC_F9.5":
                case "TC_F9.7":
                case "TC_F9.8":
                case "TC_F9.9":
                case "TC_F9.10":
                case "TC_F9.11":
                    if (string.IsNullOrEmpty(resultText) || resultText.ToLower().Contains("approved") || resultText.ToLower().Contains("processed"))
                    {
                        actualResultText = $"Bug Web: Lỗ hổng form! Hệ thống KHÔNG BẮT LỖI khi nhập Vay: '{loanAmount}', Cọc: '{downPayment}'";
                        Assert.Fail(actualResultText);
                    }
                    else
                    {
                        actualResultText = $"Pass: Form bắt lỗi thành công. Thông báo: {resultText}";
                        Assert.Pass(actualResultText);
                    }
                    break;

                default:
                    Assert.Ignore($"Chưa cấu hình code xử lý cho case {testData.TestID}");
                    break;
            }
        }

        [TearDown]
        public void TearDown()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            string result = (status == TestStatus.Passed) ? "PASS" : "FAIL";
            string screenshotPath = "";

            if (status == TestStatus.Failed)
            {
                actualResultText = "Lỗi: " + TestContext.CurrentContext.Result.Message;

                try
                {
                    var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    string dir = @"D:\dambaochatluong\Screenshots\";
                    Directory.CreateDirectory(dir);

                    screenshotPath = Path.Combine(dir, $"{currentTestCase.TestID}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    screenshot.SaveAsFile(screenshotPath);
                }
                catch { }
            }
            else if (status == TestStatus.Passed && string.IsNullOrEmpty(actualResultText))
            {
                actualResultText = "Pass đúng kỳ vọng.";
            }

            if (currentTestCase != null)
            {
                ExcelHelper.UpdateTestResult(
                    currentTestCase.TestID,
                    result,
                    actualResultText,
                    "Phú",
                    screenshotPath
                );
            }

            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
    }
}