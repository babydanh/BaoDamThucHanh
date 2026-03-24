using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParabankAutoTests.Models;
using ParabankAutoTests.Pages;
using ParabankAutoTests.Utils;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace ParabankAutoTests.Tests
{
    [TestFixture]
    public class Test_FindTransactions
    {
        private IWebDriver driver = null!;
        private FindTransactionsPage findTxPage = null!;
        private TestCaseModel currentTestCase = null!;
        private string actualResultText = "";

        // DÙNG BIẾN STATIC ĐỂ CHỈ PHẢI TẠO DATA 1 LẦN DUY NHẤT (Tránh sập web)
        private static bool isDataSeeded = false;
        private static string dynamicTxnId = "";
        private static string dynamicDate = "";
        private static string dynamicAmount = "10";

        public static IEnumerable<TestCaseData> FindTransactionsData()
        {
            return JsonReader.ReadTestData("FindTransactionsPage.json");
        }

        [SetUp]
        public void SetupTest()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            // 1. Mở trang chủ & Login
            driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/index.htm");
            driver.FindElement(By.Name("username")).SendKeys("john");
            driver.FindElement(By.Name("password")).SendKeys("demo");
            driver.FindElement(By.XPath("//input[@value='Log In']")).Click();
            Thread.Sleep(1000);

            // 2. TẠO DATA MỒI (CHỈ CHẠY 1 LẦN CHO TEST CASE ĐẦU TIÊN)
            if (!isDataSeeded)
            {
                TestContext.WriteLine("=== [SETUP] ĐANG TẠO DATA MỒI ===");
                driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/transfer.htm");
                Thread.Sleep(1500);
                driver.FindElement(By.Id("amount")).SendKeys(dynamicAmount);
                driver.FindElement(By.XPath("//input[@value='Transfer']")).Click();
                Thread.Sleep(1500);

                driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/overview.htm");
                driver.FindElement(By.XPath("//table[@id='accountTable']/tbody/tr[1]/td[1]/a")).Click();
                Thread.Sleep(1500);
                driver.FindElement(By.XPath("//table[@id='transactionTable']/tbody/tr[1]/td[2]/a")).Click();
                Thread.Sleep(1500);

                string currentUrl = driver.Url;
                dynamicTxnId = currentUrl.Substring(currentUrl.IndexOf("id=") + 3);
                dynamicDate = driver.FindElement(By.XPath("//b[text()='Date:']/../following-sibling::td")).Text;

                isDataSeeded = true;
            }

            TestContext.WriteLine($"=> ĐÃ LẤY ĐƯỢC DATA THẬT: ID = {dynamicTxnId}, Date = {dynamicDate}");

            // 3. Vào trang Find Transactions để bắt đầu Test
            driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/findtrans.htm");
            findTxPage = new FindTransactionsPage(driver);
        }

        [Test, TestCaseSource(nameof(FindTransactionsData))]
        public void ExecuteFindTransactionsTests(TestCaseModel testCase)
        {
            actualResultText = "";
            currentTestCase = testCase;
            TestContext.WriteLine($"=== [THỰC THI] {testCase.TestID} - {testCase.TestName} ===");

            findTxPage.SelectAccountByIndex(0);

            // Hứng dữ liệu JSON (Tránh lỗi null)
            string txnId = testCase.TransactionId ?? "";
            string date = testCase.FindByDate ?? "";
            string fromDate = testCase.FromDate ?? "";
            string toDate = testCase.ToDate ?? "";
            string amount = testCase.Amount ?? "";

            switch (testCase.TestID)
            {
                // =====================================
                // NHÓM 1: HAPPY PATH (TÌM THẤY GIAO DỊCH)
                // =====================================
                case "TC_F6.1":
                    findTxPage.FindByTransactionId(dynamicTxnId);
                    int count1 = findTxPage.GetTransactionResultsCount();
                    if (count1 >= 1)
                    {
                        actualResultText = $"Pass: Tìm thấy {count1} giao dịch với ID thật {dynamicTxnId}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        actualResultText = "Bug ParaBank: API tìm kiếm bằng ID không hoạt động.";
                        Assert.Fail(actualResultText);
                    }
                    break;

                case "TC_F6.2":
                    findTxPage.FindByDate(dynamicDate);
                    int count2 = findTxPage.GetTransactionResultsCount();
                    if (count2 >= 1)
                    {
                        actualResultText = $"Pass: Tìm thấy {count2} giao dịch trong ngày {dynamicDate}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        actualResultText = $"Bug ParaBank: Có giao dịch ngày {dynamicDate} nhưng hệ thống tìm không ra (Trả về 0).";
                        Assert.Fail(actualResultText);
                    }
                    break;

                case "TC_F6.3":
                    findTxPage.FindByAmount(dynamicAmount);
                    int count3 = findTxPage.GetTransactionResultsCount();
                    if (count3 >= 1)
                    {
                        actualResultText = $"Pass: Tìm thấy {count3} giao dịch với số tiền {dynamicAmount}$";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        actualResultText = "Bug ParaBank: Có giao dịch 10$ nhưng API Find By Amount bị lỗi backend (Trả về 0).";
                        Assert.Fail(actualResultText);
                    }
                    break;

                case "TC_F6.8":
                    findTxPage.FindByDateRange(dynamicDate, dynamicDate);
                    int count4 = findTxPage.GetTransactionResultsCount();
                    if (count4 >= 1)
                    {
                        actualResultText = $"Pass: Tìm thấy {count4} giao dịch trong khoảng {dynamicDate}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        actualResultText = "Bug ParaBank: Có giao dịch nhưng API Find By Date Range bị lỗi backend (Trả về 0).";
                        Assert.Fail(actualResultText);
                    }
                    break;

                // =====================================
                // NHÓM 2: LỖI VALIDATION & DỮ LIỆU SAI (SỬA LẠI ĐÚNG CHUẨN UX/QA)
                // =====================================
                case "TC_F6.4":
                case "TC_F6.5":
                case "TC_F6.13":
                    findTxPage.FindByTransactionId(txnId);
                    string idError = findTxPage.GetResultMessage();
                    int idRows = findTxPage.GetTransactionResultsCount();

                    if (!string.IsNullOrEmpty(idError))
                    {
                        actualResultText = $"Pass: Hệ thống báo lỗi rõ ràng: {idError}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        // Web Câm nín (Không có thông báo lỗi) -> FAIL THẲNG TAY
                        if (idRows == 0)
                        {
                            actualResultText = "Bug Web: Trả về bảng rỗng nhưng KHÔNG CÓ dòng thông báo lỗi/trạng thái nào cho người dùng biết.";
                            Assert.Fail(actualResultText);
                        }
                        else
                        {
                            actualResultText = $"Bug Web: Nhập ID sai '{txnId}' nhưng lại lòi ra {idRows} kết quả bậy bạ.";
                            Assert.Fail(actualResultText);
                        }
                    }
                    break;

                case "TC_F6.6":
                case "TC_F6.7":
                case "TC_F6.14":
                    findTxPage.FindByDate(date);
                    string dateError = findTxPage.GetResultMessage();
                    int dateRows = findTxPage.GetTransactionResultsCount();

                    if (!string.IsNullOrEmpty(dateError))
                    {
                        actualResultText = $"Pass: Hệ thống báo lỗi rõ ràng: {dateError}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        if (dateRows == 0)
                        {
                            actualResultText = "Bug Web: Trả về bảng rỗng nhưng KHÔNG CÓ dòng thông báo lỗi/trạng thái nào cho người dùng biết.";
                            Assert.Fail(actualResultText);
                        }
                        else
                        {
                            actualResultText = $"Bug Web: Nhập ngày sai '{date}' nhưng lại lòi ra {dateRows} kết quả bậy bạ.";
                            Assert.Fail(actualResultText);
                        }
                    }
                    break;

                case "TC_F6.9":
                case "TC_F6.10":
                    findTxPage.FindByDateRange(fromDate, toDate);
                    string rangeError = findTxPage.GetResultMessage();
                    int rangeRows = findTxPage.GetTransactionResultsCount();

                    if (!string.IsNullOrEmpty(rangeError))
                    {
                        actualResultText = $"Pass: Hệ thống báo lỗi rõ ràng: {rangeError}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        if (rangeRows == 0)
                        {
                            actualResultText = "Bug Web: Trả về bảng rỗng nhưng KHÔNG CÓ dòng thông báo lỗi/trạng thái nào cho người dùng biết.";
                            Assert.Fail(actualResultText);
                        }
                        else
                        {
                            actualResultText = $"Bug Web: Nhập Range sai nhưng lại lòi ra {rangeRows} kết quả bậy bạ.";
                            Assert.Fail(actualResultText);
                        }
                    }
                    break;

                case "TC_F6.11":
                    findTxPage.FindByAmount(amount);
                    string amountError = findTxPage.GetResultMessage();
                    int amountRows = findTxPage.GetTransactionResultsCount();

                    if (!string.IsNullOrEmpty(amountError))
                    {
                        actualResultText = $"Pass: Hệ thống báo lỗi rõ ràng: {amountError}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        if (amountRows == 0)
                        {
                            actualResultText = "Bug Web: Tìm số tiền âm trả về bảng rỗng nhưng KHÔNG CÓ thông báo lỗi giải thích.";
                            Assert.Fail(actualResultText);
                        }
                        else
                        {
                            actualResultText = $"Bug Web: Tìm tiền âm '{amount}' mà lại lòi ra {amountRows} kết quả bậy bạ.";
                            Assert.Fail(actualResultText);
                        }
                    }
                    break;

                case "TC_F6.12":
                    findTxPage.FindByAmount("");
                    string emptyError = findTxPage.GetResultMessage();
                    int emptyRows = findTxPage.GetTransactionResultsCount();

                    if (!string.IsNullOrEmpty(emptyError))
                    {
                        actualResultText = $"Pass: Hệ thống báo lỗi rõ ràng: {emptyError}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        if (emptyRows == 0)
                        {
                            actualResultText = "Bug Web: Bấm tìm kiếm khi để trống form, trả về bảng rỗng nhưng KHÔNG CÓ thông báo lỗi yêu cầu nhập liệu.";
                            Assert.Fail(actualResultText);
                        }
                        else
                        {
                            actualResultText = $"Bug Web: Submit form rỗng mà lòi ra {emptyRows} kết quả bậy bạ.";
                            Assert.Fail(actualResultText);
                        }
                    }
                    break;

                default:
                    Assert.Ignore($"Chưa cấu hình code cho case {testCase.TestID}");
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
                    string screenshotDir = @"D:\dambaochatluong\Screenshots\";
                    Directory.CreateDirectory(screenshotDir);
                    screenshotPath = Path.Combine(screenshotDir, $"{currentTestCase.TestID}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
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
                ExcelHelper.UpdateTestResult(currentTestCase.TestID, result, actualResultText, "Hào", screenshotPath);
            }

            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
    }
}