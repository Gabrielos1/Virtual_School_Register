﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Virtual_School_Register.Helpers;

namespace Virtual_School_Register.ViewModels
{
    public class UserCreateViewModel
    {
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Login must be between 1 and 20 characters!")]
        public string UserName { get; set; }

        [StringLength(20, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 20 characters!")]
        public string Name { get; set; }

        [StringLength(20, MinimumLength = 1, ErrorMessage = "Surname must be between 1 and 20 characters!")]
        public string Surname { get; set; }

        public string Sex { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [MinLength(1, ErrorMessage = "Adress cannot be empty!")]
        public string Adress { get; set; }

        public string ParentId { get; set; }

        [RequiredIf("Type", "Uczen", "Student must be assigned to the class")]
        public int? ClassId { get; set; }

        [DataType(DataType.Password)]
        [RegularExpression(Consts.PASSWORD_REGEX, ErrorMessage = "Password must contain at least 8 characters, a special character and a capital letter")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public string Type { get; set; }
    }
}
