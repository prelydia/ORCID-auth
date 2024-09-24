using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace diploma.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Surname { get; set; } // фамилия пользователя
        public string Name { get; set; } // имя пользователя
        public string Patronymic { get; set; } // отчество пользователя
        public string Login { get; set; } // логин пользователя
        public string Password { get; set; } // пароль пользователя
        public int? RoleId { get; set; }
        public Role Role { get; set; }
        public string Email { get; set; }

        public bool isOauth { get; set; }
    }
}
