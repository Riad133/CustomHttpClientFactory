namespace HttpClientFactoryCustom.Models
{
    public class LoginResponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public UserDetails userDetails { get; set; }
        public string securityToken { get; set; }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
  

    public class UserDetails
    {
        public int userID { get; set; }
        public string pin { get; set; }
        public string fullName { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public object password { get; set; }
        public int roleID { get; set; }
        public int departmentType { get; set; }
        public int departmentID { get; set; }
        public object zone { get; set; }
        public object territory { get; set; }
        public object unit { get; set; }
        public object crmCenter { get; set; }
        public object rocName { get; set; }
        public int wingID { get; set; }
        public int supervisorUserID { get; set; }
        public string designation { get; set; }
        public bool isActive { get; set; }
        public object passwordSalt { get; set; }
        public object saltedHash { get; set; }
        public int branchId { get; set; }
        public object branchName { get; set; }
        public object sol { get; set; }
        public object homeAddress { get; set; }
        public object homePhone { get; set; }
        public object officePhone { get; set; }
        public object mobileNumber { get; set; }
        public object officeAddress { get; set; }
        public string themeName { get; set; }
        public string productTitle { get; set; }
        public bool isLocked { get; set; }
        public DateTime lastLockoutDate { get; set; }
        public bool isApproved { get; set; }
        public DateTime createDate { get; set; }
        public DateTime lastLoginDate { get; set; }
        public int failedPasswordAttemptCount { get; set; }
        public object failedPasswordAttemptWindowStart { get; set; }
        public DateTime lastPasswordChangedDate { get; set; }
        public string gender { get; set; }
        public int createdBy { get; set; }
        public int approvedBy { get; set; }
        public DateTime approvedDate { get; set; }
        public int userLoggedIn { get; set; }
        public string roleName { get; set; }
        public bool isDepositUser { get; set; }
        public bool isCVUUser { get; set; }
        public object photoByte { get; set; }
        public bool isOTPRequired { get; set; }
        public string managerPin { get; set; }
        public string managerName { get; set; }
    }


}
