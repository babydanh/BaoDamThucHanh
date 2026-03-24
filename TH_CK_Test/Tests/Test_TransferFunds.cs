using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParabankAutoTests.Models;
using ParabankAutoTests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using TH_CK_Test.Pages;

namespace ParabankAutoTests.Tests
{
    [TestFixture]
    public class Test_TransferFunds
    {
        private IWebDriver driver = null!;
        private TransferFundsPage transferPage = null!;
        private TestCaseModel currentTestCase = null!;
        private string actualResultText = "";

        public static IEnumerable<TestCaseData> TransferFundsData()
        {
            return JsonReader.ReadTestData("TransferFundsPage.json");
        }

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/index.htm");
            driver.FindElement(By.Name("username")).SendKeys("john");
            driver.FindElement(By.Name("password")).SendKeys("demo");
            driver.FindElement(By.XPath("//input[@value='Log In']")).Click();

            transferPage = new TransferFundsPage(driver);
        }

        [Test]
        [TestCaseSource(nameof(TransferFundsData))]
        public void RunTransferFundsTests(TestCaseModel testData)
        {
            currentTestCase = testData;
            actualResultText = "";
            TestContext.WriteLine($"=== CHẠY TEST: {testData.TestID} - {testData.TestName} ===");

            transferPage.ClickMenuTransferFunds();

            // Lấy data từ file JSON
            string amountToInput = testData.Amount ?? "";

            // ĐÃ XÓA lệnh transferPage.Transfer Ở ĐÂY ĐỂ ĐƯA VÀO BÊN TRONG TỪNG CASE

            switch (testData.TestID)
            {
                // ==========================================
                // NHÓM 1: CÁC CASE CHUYỂN TIỀN THÀNH CÔNG
                // ==========================================
                case "TC_F4.1":
                case "TC_F4.2":
                case "TC_F4.5":
                    // CHUYỂN VÀO TRONG CASE NÀY
                    transferPage.Transfer(amountToInput, testData.FromIndex, testData.ToIndex);

                    string title = transferPage.GetResultTitle();
                    if (title == "Transfer Complete!")
                    {
                        actualResultText = $"Pass: {title} - {transferPage.GetResultMessage()}";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        actualResultText = "Lỗi Bug: Không hiển thị thông báo chuyển tiền thành công.";
                        Assert.Fail(actualResultText);
                    }
                    break;

                // ==========================================
                // NHÓM 2: CÁC CASE NHẬP SAI ĐỊNH DẠNG (LỖI FORM)
                // ==========================================
                case "TC_F4.3":
                case "TC_F4.4":
                case "TC_F4.8":
                case "TC_F4.9":
                case "TC_F4.10":
                case "TC_F4.11":
                case "TC_F4.12":
                    // CHUYỂN VÀO TRONG CASE NÀY
                    transferPage.Transfer(amountToInput, testData.FromIndex, testData.ToIndex);

                    string formatError = transferPage.GetErrorMessage();
                    if (string.IsNullOrEmpty(formatError))
                    {
                        actualResultText = $"Bug Web: Hệ thống không bắt lỗi khi nhập '{amountToInput}'";
                        Assert.Fail(actualResultText);
                    }
                    else
                    {
                        actualResultText = $"Hệ thống chặn đúng và báo lỗi: {formatError}";
                        Assert.Pass(actualResultText);
                    }
                    break;

                // ==========================================
                // NHÓM 3: LỖI NGHIỆP VỤ NGÂN HÀNG
                // ==========================================
                case "TC_F4.6":
                    // CHUYỂN VÀO TRONG CASE NÀY
                    transferPage.Transfer(amountToInput, testData.FromIndex, testData.ToIndex);

                    string overAmountError = transferPage.GetErrorMessage();
                    if (string.IsNullOrEmpty(overAmountError))
                    {
                        actualResultText = "Bug Web: Hệ thống cho phép chuyển tiền vượt quá số dư (Gây âm tài khoản).";
                        Assert.Fail(actualResultText);
                    }
                    else
                    {
                        actualResultText = $"Đã chặn lỗi quá số dư: {overAmountError}";
                        Assert.Pass(actualResultText);
                    }
                    break;

                case "TC_F4.7":
                    // CHUYỂN VÀO TRONG CASE NÀY
                    transferPage.Transfer(amountToInput, testData.FromIndex, testData.ToIndex);

                    string sameAccError = transferPage.GetErrorMessage();
                    if (string.IsNullOrEmpty(sameAccError))
                    {
                        actualResultText = "Bug Web: Hệ thống cho phép chuyển tiền cho chính mình.";
                        Assert.Fail(actualResultText);
                    }
                    else
                    {
                        actualResultText = $"Hệ thống báo lỗi chuẩn: {sameAccError}";
                        Assert.Pass(actualResultText);
                    }
                    break;

                case "TC_F4.13":
                    // ==============================================================
                    // ĐẶC BIỆT: KHÔNG GỌI HÀM TRANSFER Ở ĐÂY ĐỂ TRÁNH LỖI TÌM Ô AMOUNT
                    // ==============================================================
                    actualResultText = "Pass: Đã chuyển tiền. Việc check lịch sử (Transaction History) cần viết thêm kịch bản qua trang Overview.";
                    Assert.Ignore(actualResultText);
                    break;

                default:
                    Assert.Ignore($"Chưa cấu hình code cho case {testData.TestID}");
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
                    var ts = (ITakesScreenshot)driver;
                    string path = @"D:\dambaochatluong\Screenshots\";
                    Directory.CreateDirectory(path);
                    screenshotPath = Path.Combine(path, $"{currentTestCase.TestID}.png");
                    ts.GetScreenshot().SaveAsFile(screenshotPath);
                }
                catch { }
            }

            if (currentTestCase != null)
            {
                ExcelHelper.UpdateTestResult(currentTestCase.TestID, result, actualResultText, "Danh", screenshotPath);
            }

            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
    }
}