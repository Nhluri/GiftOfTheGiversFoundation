namespace GiftOfTheGiversFoundation.Services
{
    public class EmailSettings
    {
        public string GmailEmail { get; set; }
        public string GmailAppPassword { get; set; }
        public string FromName { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public int Timeout { get; set; }
    }
}
