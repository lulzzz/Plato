﻿using PlatoCore.Security.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Plato.Users.ViewModels
{
    public class LoginViewModel
    {
        
        [EmailAddress, DataType(DataType.EmailAddress), Display(Name = "email")]
        public string Email { get; set; }

        [Required, DataType(DataType.Text), Display(Name = "username")]
        public string UserName { get; set; }

        [Required, StringLength(100)]
        [DataType(DataType.Password), Display(Name = "password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

    }
}
