using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureEntraUserBulkImporter.Models
{
    public class UserData
    {
        public string DisplayName { get; set; }
        public string MailNickname { get; set; }
        public string UserPrincipalName { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
    }
}
