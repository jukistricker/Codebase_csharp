using Codebase.Models.Enums;

namespace Codebase.Entities.Auth
{
    public class User:BaseEntity
    {
        public String Username { get; set; }
        public String Password { get; set; }
        public LanguageEnum Lang { get; set; }
    }

}
