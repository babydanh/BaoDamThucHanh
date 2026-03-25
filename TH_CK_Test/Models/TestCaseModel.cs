namespace ParabankAutoTests.Models
{
    public class TestCaseModel
    {
        // ==========================================
        // 1. CÁC BIẾN CHUNG (Dùng để in Report)
        // ==========================================
        public string TestID { get; set; }
        public string Module { get; set; }
        public string TestName { get; set; }
        public string Action { get; set; }
        public string ExpectedResult { get; set; }

        // ==========================================
        // 2. CÁC BIẾN DÙNG CHO F4 (Transfer Funds)
        // ==========================================
        public string Amount { get; set; }
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }

        // ==========================================
        // 3. CÁC BIẾN DÙNG CHO F6 (Find Transactions)
        // ==========================================
        public string TransactionId { get; set; }
        public string FindByDate { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        // ==========================================
        // 4. CÁC BIẾN DÙNG CHO F9 (Request Loan)
        // ==========================================
        public string LoanAmount { get; set; }
        public string DownPayment { get; set; }
        public string ConfigValue { get; set; }
    }
}