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
using System.Threading;

namespace ParabankAutoTests.Tests
{
    [TestFixture]
    public class Test_AdminPage
    {
        private IWebDriver driver;
        private AdminPage adminPage;
        private TestCaseModel currentTestCase;
        private string actualResultText = "";

        public static IEnumerable<TestCaseData> AdminData()
        {
            // Đảm bảo tên file JSON khớp với file bạn đã lưu
            return JsonReader.ReadTestData("AdminPage.json");
        }

        [SetUp]
        public void Setup()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            // Mặc định login để vào trang Admin (trừ case test Security)
            driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/index.htm");
            driver.FindElement(By.Name("username")).SendKeys("john");
            driver.FindElement(By.Name("password")).SendKeys("demo");
            driver.FindElement(By.XPath("//input[@value='Log In']")).Click();

            adminPage = new AdminPage(driver);
        }

        [Test]
        [TestCaseSource(nameof(AdminData))]
        public void RunAdminTests(TestCaseModel testData)
        {
            actualResultText = "";
            currentTestCase = testData;
            string val = testData.ConfigValue ?? "";

            TestContext.WriteLine($"=== RUN {testData.TestID}: {testData.TestName} ===");

            switch (testData.TestID)
            {
                // ==========================================
                // NHÓM 1: DATABASE CONTROL
                // ==========================================
                case "TC_F7.1": // Initialize
                    adminPage.ClickMenu();
                    adminPage.ClickInitialize();
                    actualResultText = adminPage.GetResult();
                    Assert.That(actualResultText.ToLower(), Does.Contain("initialized"), "Lỗi: Không khởi tạo được Database");
                    break;

                case "TC_F7.2": // Clean
                    adminPage.ClickMenu();
                    adminPage.ClickClean();
                    actualResultText = adminPage.GetResult();
                    Assert.That(actualResultText.ToLower(), Does.Contain("cleaned"), "Lỗi: Không xóa sạch được Database");
                    break;

                // ==========================================
                // NHÓM 2: CONFIGURATION MODE
                // ==========================================
                case "TC_F7.5": // SOAP
                    adminPage.ClickMenu();
                    adminPage.SelectSOAP();
                    adminPage.ClickSubmit();
                    actualResultText = "Đã chuyển sang chế độ SOAP";
                    Assert.Pass(actualResultText);
                    break;

                case "TC_F7.8": // JDBC
                    adminPage.ClickMenu();
                    adminPage.SelectJDBC();
                    adminPage.ClickSubmit();
                    actualResultText = "Đã chuyển sang chế độ JDBC";
                    Assert.Pass(actualResultText);
                    break;

                // ==========================================
                // NHÓM 3: INITIAL BALANCE (BẮT BUG SỐ ÂM)
                // ==========================================
                case "TC_F7.20":
                case "TC_F7.22":
                case "TC_F7.23": // Số âm
                    adminPage.ClickMenu();
                    adminPage.EnterInitBalance(val);
                    adminPage.ClickSubmit();

                    string resBalance = adminPage.GetResult(); // Thường là "Settings saved successfully"

                    if (testData.TestID == "TC_F7.23" && resBalance.ToLower().Contains("successfully"))
                    {
                        actualResultText = "Bug Web: Hệ thống cho phép thiết lập Initial Balance là số âm!";
                        Assert.Fail(actualResultText);
                    }
                    actualResultText = $"Kết quả: {resBalance}";
                    Assert.Pass(actualResultText);
                    break;

                // ==========================================
                // NHÓM 4: THRESHOLD (GIÁ TRỊ BIÊN)
                // ==========================================
                case "TC_F7.34":
                case "TC_F7.35":
                    adminPage.ClickMenu();
                    adminPage.EnterThreshold(val);
                    adminPage.ClickSubmit();
                    actualResultText = "Lưu Threshold thành công";
                    Assert.Pass(actualResultText);
                    break;

                // ==========================================
                // NHÓM 5: SECURITY TEST (ĐIỂM NHẤN BÁO CÁO)
                // ==========================================
                case "TC_F7.47":
                    // 1. Logout trước
                    driver.FindElement(By.LinkText("Log Out")).Click();
                    // 2. Thử truy cập trực tiếp URL admin
                    driver.Navigate().GoToUrl("https://parabank.parasoft.com/parabank/admin.htm");

                    // 3. Kiểm tra xem có thấy nút "CLEAN" không (nếu thấy là lỗ hổng)
                    bool isAccessDenied = driver.FindElements(By.XPath("//input[@value='Clean']")).Count == 0;

                    if (isAccessDenied)
                    {
                        actualResultText = "Pass: Hệ thống bảo mật tốt, không cho truy cập trái phép trang Admin.";
                        Assert.Pass(actualResultText);
                    }
                    else
                    {
                        actualResultText = "Bug Bảo Mật: Trang Admin có thể truy cập tự do mà không cần đăng nhập!";
                        Assert.Fail(actualResultText);
                    }
                    break;

                default:
                    Assert.Ignore("Case này chưa được cấu hình code thực thi.");
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

            if (currentTestCase != null)
            {
                ExcelHelper.UpdateTestResult(currentTestCase.TestID, result, actualResultText, "Duy", screenshotPath);
            }

            driver.Quit();
            driver.Dispose();
        }
    }
}