﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
